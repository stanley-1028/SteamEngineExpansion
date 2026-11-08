"""SEE P9 - offline Blender renders: 24 building icons + 6 sample previews.

Blender 4.2.1 LTS headless.  Geometry is read from the **shipped** LOD0
`.timbermesh` files (`mod/art/timbermesh/<stem>.Model.timbermesh`, the same
bytes copied to `mod/data/`), with each node's world transform composed from
the protobuf; so the picture is the exported mesh, not a separate mock-up.
The `Default` node animation is not applied (still image, rest pose).

Materials bind the real vanilla `_D` albedo PNG at the measured UV patch and
multiply it by the material `_Color` tint (same data as material_patches.json,
sourced from HDBF ART-05/ART-07).  This is an offline render, NOT a game
screenshot; in-game appearance still needs the P8 human gate.

Outputs:
  mod/art/icons/<IconName>.png                 128x128 RGBA (transparent bg)
  mod/art/preview/<stem>_iso.png               640x480 (dark studio bg)
  mod/art/preview/render-report.txt

Run (after `python mod/art/gen_models.py`):
  blender.exe -b --factory-startup -noaudio --python mod/art/render_previews.py
Optional:
  ... -- --preview-only        (skip the 24 icons)
  ... -- --icons-only          (skip the 6 previews)
  ... -- --only SteamBoiler.Small.T1.Folktails
  ... -- --tiers T1,T2         (only render the T1/T2 icons and previews;
                                --tiers=T1,T2 is accepted as well)

With `--tiers` the PNGs outside the filter are left untouched and the render
report is merged (the previous lines for everything not re-rendered are kept),
so the report still lists all 24 icons and 6 previews in the canonical order.
"""

from __future__ import annotations

import math
import os
import sys

import bpy
import mathutils

HERE = os.path.dirname(os.path.abspath(__file__))
ART = HERE
ART_TIMBERMESH = os.path.join(ART, "timbermesh")
MOD_ROOT = os.path.dirname(ART)
TOOLS = os.path.join(ART, "tools")
for path in (TOOLS, ART):
    if path not in sys.path:
        sys.path.insert(0, path)

import artlib as A  # noqa: E402

ICON_DIR = os.path.join(ART, "icons")
PREVIEW_DIR = os.path.join(ART, "preview")

PREVIEW_MODELS = [
    "SteamBoiler.Small.T1.Folktails",
    "SteamBoiler.Small.T3.IronTeeth",
    "SteamBoiler.Medium.T2.Folktails",
    "SteamBoiler.Medium.T4.IronTeeth",
    "SteamBoiler.Large.T1.Folktails",
    "SteamBoiler.Large.T4.IronTeeth",
]

SAMPLES_ICON = 28
SAMPLES_PREVIEW = 48
ICON_RESOLUTION = 128
PREVIEW_RESOLUTION = (640, 480)


# --------------------------------------------------------------------------- #
# scene helpers
# --------------------------------------------------------------------------- #
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.use_denoising = True
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.image_settings.color_depth = '8'
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.display_settings.display_device = 'sRGB'
    world = bpy.data.worlds.new("SEE_world")
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs[0].default_value = (0.62, 0.65, 0.68, 1.0)
    background.inputs[1].default_value = 0.45
    scene.world = world
    return scene


def add_sun(name, energy, rotation):
    light = bpy.data.lights.new(name, 'SUN')
    light.energy = energy
    light.angle = math.radians(8.0)
    obj = bpy.data.objects.new(name, light)
    obj.rotation_euler = rotation
    bpy.context.scene.collection.objects.link(obj)


def setup_lights():
    # Moderate key/fill/rim: the previous 4.5/2.0/1.4 sum blew the vanilla
    # textures out to near-white in the Standard view transform.
    add_sun("key", 3.0, (math.radians(52), math.radians(6), math.radians(-38)))
    add_sun("fill", 1.1, (math.radians(65), 0.0, math.radians(150)))
    add_sun("rim", 0.6, (math.radians(70), 0.0, math.radians(40)))


# --------------------------------------------------------------------------- #
# materials
# --------------------------------------------------------------------------- #
def _material_for(name):
    entry = A.MATERIALS[name]
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    for node in list(nodes):
        nodes.remove(node)
    output = nodes.new("ShaderNodeOutputMaterial")
    principled = nodes.new("ShaderNodeBsdfPrincipled")
    link = links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    category = entry["patch_class"]
    tint = entry.get("material_color_tint") or [1.0, 1.0, 1.0]
    texture_path = entry.get("texture_path")
    if texture_path and os.path.isfile(texture_path):
        image_node = nodes.new("ShaderNodeTexImage")
        image = bpy.data.images.load(texture_path, check_existing=True)
        image.colorspace_settings.name = 'sRGB'
        image_node.image = image
        multiply = nodes.new("ShaderNodeMix")
        multiply.data_type = 'RGBA'
        multiply.blend_type = 'MULTIPLY'
        multiply.inputs["Factor"].default_value = 1.0
        multiply.inputs[6].default_value = (tint[0], tint[1], tint[2], 1.0)
        links.new(image_node.outputs["Color"], multiply.inputs[7])
        links.new(multiply.outputs[2], principled.inputs["Base Color"])
        links.new(image_node.outputs["Color"], multiply.inputs[7])
    else:
        principled.inputs["Base Color"].default_value = (0.7, 0.7, 0.7, 1.0)
    if category in ("metal", "details", "forged_planks"):
        principled.inputs["Metallic"].default_value = 0.55
        principled.inputs["Roughness"].default_value = 0.42
    elif category == "glass":
        # Preview-safe tinted glass. A transmissive material renders as a black
        # hole against the transparent icon background, so use an opaque light
        # blue panel with low roughness instead (offline preview only).
        base_color = principled.inputs["Base Color"]
        for link in list(base_color.links):
            links.remove(link)
        base_color.default_value = (0.52, 0.70, 0.80, 1.0)
        principled.inputs["Metallic"].default_value = 0.0
        principled.inputs["Roughness"].default_value = 0.12
    else:
        principled.inputs["Metallic"].default_value = 0.0
        principled.inputs["Roughness"].default_value = 0.72
    return material


def build_object(mesh_path, name):
    """Build the Blender object from the SHIPPED `.timbermesh` file (LOD0).

    Reads the exported protobuf (not the generator source), composes each node's
    world transform with `artlib.model_world_transforms` and keeps the stored
    normals/UVs, so a preview shows the geometry the game will actually load
    (rest pose; the `Default` node animation is not applied for a still).
    """
    model = A.read_model(mesh_path)
    transforms = A.model_world_transforms(model)
    materials = {}
    ordered_materials = []
    vertices = []
    faces = []
    loop_uvs = []
    loop_normals = []
    material_indices = []
    for node_index, node in enumerate(model.nodes):
        props = {prop.name: prop for prop in node.vertexProperties}
        positions = A.float_rows(props["position"]) if "position" in props else []
        normals = A.float_rows(props["normal"]) if "normal" in props else []
        uvs = A.float_rows(props["uv0"]) if "uv0" in props else []
        world_position, world_rotation, world_scale = transforms[node_index]
        world_positions = []
        world_normals = []
        for position, normal in zip(positions, normals):
            scaled = (position[0] * world_scale[0],
                      position[1] * world_scale[1],
                      position[2] * world_scale[2])
            rotated = A.quat_rotate(world_rotation, scaled)
            world_positions.append((world_position[0] + rotated[0],
                                    world_position[1] + rotated[1],
                                    world_position[2] + rotated[2]))
            world_normals.append(A.quat_rotate(world_rotation, normal))
        for node_mesh in node.meshes:
            material_name = node_mesh.material
            if material_name not in materials:
                materials[material_name] = len(ordered_materials)
                ordered_materials.append(material_name)
            indices = list(node_mesh.indices)
            for offset in range(0, len(indices) - 2, 3):
                triangle = indices[offset:offset + 3]
                base = len(vertices)
                for vertex_index in triangle:
                    px, py, pz = world_positions[vertex_index]
                    nx, ny, nz = world_normals[vertex_index]
                    u, v = uvs[vertex_index] if uvs else (0.0, 0.0)
                    # game/export space (X right, Y up, Z depth, front Z=0)
                    # -> Blender authoring space (X right, Y depth, Z up).
                    # (x, -z, y) has determinant +1, so the stored winding (CCW
                    # outward in the game space) stays CCW in Blender.
                    vertices.append((px, -pz, py))
                    loop_uvs.append((u, v))
                    loop_normals.append((nx, -nz, ny))
                faces.append((base, base + 1, base + 2))
                material_indices.append(materials[material_name])
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for loop_index, uv in enumerate(loop_uvs):
        uv_layer.data[loop_index].uv = uv
    for material_name in ordered_materials:
        mesh.materials.append(_material_for(material_name))
    for polygon, material_index in zip(mesh.polygons, material_indices):
        polygon.material_index = material_index
        polygon.use_smooth = False
    try:
        mesh.normals_split_custom_set(loop_normals)
    except Exception as error:  # noqa: BLE001 - 4.x API drift, flat shading still works
        print("WARN custom normals unavailable:", error)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def frame_camera(scene, obj, resolution, direction, margin=1.05):
    """Frame the whole model: fit its bounding sphere into the limiting FOV.

    `margin` is a multiplier on the exact sphere-fit distance (>1 adds padding);
    the previous version used `distance = bbox diagonal`, which cropped the
    building because the 85 mm lens needs ~2.4x the diagonal to fit it.
    """
    mesh = obj.data
    coordinates = [obj.matrix_world @ vertex.co for vertex in mesh.vertices]
    min_corner = mathutils.Vector((min(v.x for v in coordinates),
                                   min(v.y for v in coordinates),
                                   min(v.z for v in coordinates)))
    max_corner = mathutils.Vector((max(v.x for v in coordinates),
                                   max(v.y for v in coordinates),
                                   max(v.z for v in coordinates)))
    center = (min_corner + max_corner) * 0.5
    radius = (max_corner - min_corner).length * 0.5
    camera_data = bpy.data.cameras.new("camera")
    camera_data.lens = 85.0
    camera_data.sensor_width = 36.0
    camera = bpy.data.objects.new("camera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    scene.camera = camera
    view_direction = mathutils.Vector(direction).normalized()
    # Blender's default sensor fit uses the horizontal sensor for landscape and
    # square images, so the limiting half-FOV is the vertical one for 640x480.
    aspect = float(resolution[1]) / float(resolution[0])
    half_fov = math.atan(0.5 * camera_data.sensor_width * min(1.0, aspect) / camera_data.lens)
    distance = radius / math.sin(half_fov) * margin
    camera.location = center + view_direction * distance
    # aim the camera at the model centre
    look = center - camera.location
    camera.rotation_euler = look.to_track_quat('-Z', 'Y').to_euler()
    scene.render.resolution_x, scene.render.resolution_y = resolution
    return camera


def render_model(mesh_path, stem, output_path, resolution, samples, transparent):
    scene = reset_scene()
    scene.render.film_transparent = transparent
    setup_lights()
    obj = build_object(mesh_path, stem)
    if not transparent:
        scene.world.node_tree.nodes["Background"].inputs[0].default_value = (0.16, 0.17, 0.18, 1.0)
        scene.world.node_tree.nodes["Background"].inputs[1].default_value = 0.5
    scene.cycles.samples = samples
    scene.render.resolution_x, scene.render.resolution_y = resolution
    camera = frame_camera(scene, obj, resolution, (0.86, 1.0, 0.62))
    # frontal fill (a sun aligned with the camera view): the 3-point rig leaves
    # the camera-facing facades of a box-shaped building quite dark.
    front_light = bpy.data.lights.new("front_fill", 'SUN')
    front_light.energy = 1.1
    front_light.angle = math.radians(25.0)
    front_obj = bpy.data.objects.new("front_fill", front_light)
    front_obj.rotation_euler = camera.rotation_euler
    bpy.context.scene.collection.objects.link(front_obj)
    scene.render.filepath = output_path
    bpy.ops.render.render(write_still=True)
    return output_path


# --------------------------------------------------------------------------- #
# arguments / report merging
# --------------------------------------------------------------------------- #
def parse_tier_filter(arguments):
    """Return the tier subset selected by post-`--` args, or None for all tiers.

    Accepts `--tiers T1,T2` and `--tiers=T1,T2` (tier names case-insensitive,
    spaces around commas allowed).  An unknown or empty tier list aborts with a
    clear error instead of silently rendering nothing.
    """
    value = None
    for index, argument in enumerate(arguments):
        if argument == "--tiers":
            if index + 1 >= len(arguments):
                raise SystemExit("--tiers expects a comma separated list, "
                                 "e.g. --tiers T1,T2")
            value = arguments[index + 1]
        elif argument.startswith("--tiers="):
            value = argument.split("=", 1)[1]
    if value is None:
        return None
    tiers = []
    for token in value.split(","):
        token = token.strip().upper()
        if not token:
            continue
        if token not in A.TIERS:
            raise SystemExit("--tiers: unknown tier %r (expected a comma separated "
                             "subset of %s)" % (token, ", ".join(A.TIERS)))
        if token not in tiers:
            tiers.append(token)
    if not tiers:
        raise SystemExit("--tiers: expected at least one tier, e.g. --tiers T1,T2")
    return tiers


def _report_key(kind, stem):
    return (kind, stem)


def _read_previous_report(report_path):
    """Return (header_lines, {(kind, stem): line}) from an existing report.

    A partial (`--tiers`) render must not drop the rows it did not re-render:
    the previous report is the source for those rows, so the deliverable stays
    a complete 24 icon + 6 preview document.
    """
    header = []
    lines = {}
    if not os.path.isfile(report_path):
        return header, lines
    with open(report_path, "r", encoding="utf-8") as handle:
        rows = handle.read().splitlines()
    header = rows[:2]
    for row in rows[2:]:
        kind, separator, rest = row.partition(" ")
        if not separator or kind not in ("icon", "preview") or " -> " not in rest:
            continue
        stem = rest.split(" -> ", 1)[0].strip()
        lines[_report_key(kind, stem)] = row
    return header, lines


def _compose_report(header, previous, rendered):
    """Merge the previous report with this run's rows, in the canonical order.

    Order: the 24 icons in SIZES x TIERS x FACTIONS order (the original report
    order), then the six sample previews in PREVIEW_MODELS order.  The old two
    header lines are kept verbatim.
    """
    if len(header) < 2:
        header = ["SEE P9 Blender render report",
                  "blender: %s" % bpy.app.version_string]
    order = [_report_key("icon", stem) for stem, _icon_name in _variants()]
    order += [_report_key("preview", stem) for stem in PREVIEW_MODELS]
    rows = list(header)
    missing = []
    for key in order:
        line = rendered.get(key) or previous.get(key)
        if line is None:
            missing.append(key)
            continue
        rows.append(line)
    for kind, stem in missing:
        print("WARN render report has no %s row for %s; the report will not list it"
              % (kind, stem))
    return rows


def _stem_tier(stem):
    for part in stem.split("."):
        if part in A.TIERS:
            return part
    raise SystemExit("cannot determine tier of model stem: " + stem)


def select_models(tiers=None, only=None):
    """(stem, mesh_path) list to render, honouring the --tiers/--only filters."""
    selected = []
    for size in A.SIZES:
        for tier in A.TIERS:
            if tiers and tier not in tiers:
                continue
            for faction in A.FACTIONS:
                stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
                if only and stem != only:
                    continue
                mesh_path = os.path.join(ART_TIMBERMESH, stem + ".Model.timbermesh")
                if not os.path.isfile(mesh_path):
                    raise SystemExit("missing shipped model: " + mesh_path
                                     + " (run python mod/art/gen_models.py first)")
                selected.append((stem, mesh_path))
    return selected


# --------------------------------------------------------------------------- #
# main
# --------------------------------------------------------------------------- #
def main():
    arguments = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    icon_only = "--icons-only" in arguments
    preview_only = "--preview-only" in arguments
    tiers = parse_tier_filter(arguments)
    only = None
    for index, argument in enumerate(arguments):
        if argument == "--only" and index + 1 < len(arguments):
            only = arguments[index + 1]
    os.makedirs(ICON_DIR, exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    report_path = os.path.join(PREVIEW_DIR, "render-report.txt")
    header, previous_report = _read_previous_report(report_path)
    rendered = {}
    if tiers:
        print("tier filter:", ", ".join(tiers))

    built = select_models(tiers, only)
    for stem, mesh_path in built:
        print("loaded", stem, "from", os.path.basename(mesh_path))

    if not preview_only:
        for stem, mesh_path in built:
            variant = next((row for row in _variants() if row[0] == stem), None)
            icon_name = variant[1]
            path = os.path.join(ICON_DIR, icon_name + ".png")
            render_model(mesh_path, stem, path, (ICON_RESOLUTION, ICON_RESOLUTION),
                         SAMPLES_ICON, True)
            rendered[_report_key("icon", stem)] = (
                "icon  %s -> %s" % (stem, os.path.relpath(path, MOD_ROOT)))
            print("rendered icon", path)
    if not icon_only:
        for stem in PREVIEW_MODELS:
            if tiers and _stem_tier(stem) not in tiers:
                continue
            if only and stem != only:
                continue
            row = next((item for item in built if item[0] == stem), None)
            mesh_path = row[1] if row else os.path.join(ART_TIMBERMESH, stem + ".Model.timbermesh")
            path = os.path.join(PREVIEW_DIR, stem + "_iso.png")
            render_model(mesh_path, stem, path, PREVIEW_RESOLUTION, SAMPLES_PREVIEW, False)
            rendered[_report_key("preview", stem)] = (
                "preview %s -> %s" % (stem, os.path.relpath(path, MOD_ROOT)))
            print("rendered preview", path)

    report = _compose_report(header, previous_report, rendered)
    with open(report_path, "w", encoding="utf-8") as handle:
        handle.write("\n".join(report) + "\n")
    print("render report:", report_path)


def _variants():
    """icon name used by the blueprint generator (LabeledEntitySpec.Icon)."""
    rows = []
    for size in A.SIZES:
        for tier in A.TIERS:
            for faction in A.FACTIONS:
                rows.append(("SteamBoiler.%s.%s.%s" % (size, tier, faction),
                             "SteamBoiler%s%s%s" % (size, tier, faction)))
    return rows


if __name__ == "__main__":
    main()
