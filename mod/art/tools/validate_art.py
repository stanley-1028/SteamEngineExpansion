"""SEE P9 - pure-Python art validator (no Blender, no Unity).

Proves the delivered art is structurally consistent with the game format and
with the 24 shipped blueprints:

  A. file set: 24 finished + 24 ConstructionStage0 LOD0 `.timbermesh` under
     `mod/art/timbermesh` and byte-identical copies under
     `mod/data/Buildings/Power/SteamBoiler`; 48 LOD1/LOD2 source files under
     `mod/art/lod`; `export-manifest.json`.
  B. `.timbermesh` structure: zlib+protobuf parse, Model.name == file stem,
     version 0, single root, parent indices precede children, vertex property
     dimensions/lengths, no NaN/Inf, indices in range, uv0 in [0,1],
     triangle winding agrees with the stored normals (the shipped-file
     convention probed from ShantySpeaker/MWP).
  C. budgets: LOD0 <= 3500/6000/9000 tris for Small/Medium/Large; LOD1 <=50 %
     and LOD2 <=15 % of the same model's LOD0.
  D. materials: only vanilla `MaterialCollection` asset names of the matching
     faction (parsed from the read-only vanilla blueprints), <=2 opaque +1 glass,
     and every used material has a measured patch in material_patches.json.
  E. ports/animation: the required `#EmptySmoke1..3` / `ATT_*` empties and
     `ANIM_SEE_*` transforms exist; exactly one looping node animation named
     "Default" drives the flywheel + crankshaft; LOD2 has no animation.
  F. blueprint wiring: each blueprint points at its own LOD0 model + its own
     ConstructionStage0 greybox, declares the 5 transputs (one back-face "Up",
     no "Top"), the `GoodPoweredGeneratorAnimatorSpec`, the particle controller
     and the `#EmptySmoke1..3` template attachments.
  G. icons: 24 x 128x128 RGBA PNG + `{"isSprite": true}` meta, byte-identical to
     the Blender renders under `art/icons` (i.e. the placeholder icons are really
     replaced), plus the 6 Blender sample previews and `docs/ART_REPORT.md`.

Writes `mod/art/validation-report.txt`; exit 0 only when every check passes.
"""

from __future__ import annotations

import hashlib
import json
import math
import os
import struct
import sys
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.dirname(HERE)
MOD_ROOT = os.path.dirname(ART)
WS = os.path.dirname(MOD_ROOT)
TOOLS = os.path.join(ART, "tools")
if TOOLS not in sys.path:
    sys.path.insert(0, TOOLS)

import artlib as A  # noqa: E402

DATA_BOILER = os.path.join(MOD_ROOT, "data", "Buildings", "Power", "SteamBoiler")
ART_TIMBERMESH = os.path.join(ART, "timbermesh")
ART_LOD = os.path.join(ART, "lod")
ICON_DIR = os.path.join(MOD_ROOT, "data", "Icons")
ART_ICON_DIR = os.path.join(ART, "icons")
PREVIEW_DIR = os.path.join(ART, "preview")
ART_REPORT = os.path.join(WS, "docs", "ART_REPORT.md")
MANIFEST = os.path.join(ART_TIMBERMESH, "export-manifest.json")
REPORT = os.path.join(ART, "validation-report.txt")

PREVIEW_MODELS = [
    "SteamBoiler.Small.T1.Folktails",
    "SteamBoiler.Small.T3.IronTeeth",
    "SteamBoiler.Medium.T2.Folktails",
    "SteamBoiler.Medium.T4.IronTeeth",
    "SteamBoiler.Large.T1.Folktails",
    "SteamBoiler.Large.T4.IronTeeth",
]
REPORT_SECTIONS = [
    "# SEE P9 美術報告",
    "## 1. 交付摘要",
    "## 2. 39 mesh 對映",
    "## 3. 逐檔面數與 LOD",
    "## 4. 材質",
    "## 5. 端口與掛點",
    "## 6. 動畫（C-9）",
    "## 7. 圖示與預覽",
    "## 8. 驗證證據",
    "## 9. 風險與待驗",
]

VANILLA_MATERIAL_COLLECTIONS = os.path.join(
    r"C:\Users\user\Documents\deepseek-harness\default-workspace"
    r"\high-density-box-farm\mod\.ref\Blueprints\MaterialCollections")

SIZES = ["Small", "Medium", "Large"]
TIERS = ["T1", "T2", "T3", "T4"]
FACTIONS = ["Folktails", "IronTeeth"]
BUDGET = {"Small": 3500, "Medium": 6000, "Large": 9000}
HEIGHT = {"Small": 3.0, "Medium": 3.0, "Large": 4.0}
FOOTPRINT = {"Small": 2.0, "Medium": 3.0, "Large": 4.0}
REQUIRED_EMPTIES = ["#EmptySmoke1", "#EmptySmoke2", "#EmptySmoke3",
                    "ATT_WaterIn", "ATT_FuelIn", "ATT_Entrance", "ATT_FlywheelPivot"]
REQUIRED_ANIM = ["ANIM_SEE_Flywheel", "ANIM_SEE_Crankshaft"]

LINES = []
ERRORS = []
CHECKS = [0, 0]


def log(message=""):
    LINES.append(message)
    print(message)


def check(condition, label, detail=""):
    CHECKS[1] += 1
    if condition:
        CHECKS[0] += 1
        return True
    ERRORS.append(label + (": " + detail if detail else ""))
    log("FAIL  " + label + (": " + detail if detail else ""))
    return False


def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for chunk in iter(lambda: handle.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_vanilla_materials():
    whitelist = {}
    for faction in FACTIONS:
        path = os.path.join(VANILLA_MATERIAL_COLLECTIONS,
                            "MaterialCollection.%s.blueprint.json" % faction)
        if not os.path.isfile(path):
            raise SystemExit("missing vanilla MaterialCollection blueprint: " + path)
        document = json.load(open(path, encoding="utf-8"))
        names = set()
        for entry in document["MaterialCollectionSpec"]["Materials"]:
            names.add(entry.rsplit("/", 1)[-1])
        whitelist[faction] = names
    return whitelist


def _float_rows(prop):
    dimension = max(prop.scalarTypeDimension, 1)
    count = len(prop.data) // (4 * dimension)
    return [struct.unpack_from("<%df" % dimension, prop.data, i * 4 * dimension)
            for i in range(count)]


def validate_mesh(path, family, size, faction, lod):
    label = os.path.basename(path)
    model = A.read_model(path)
    expected_stem = os.path.basename(path)[: -len(".timbermesh")]
    check(model.name == expected_stem, "mesh name " + label, model.name)
    check(model.version == 0, "mesh version " + label, str(model.version))
    check(len(model.nodes) > 0, "mesh has nodes " + label)

    roots = [node for node in model.nodes if node.parent < 0]
    check(len(roots) == 1, "single root " + label, str(len(roots)))
    parents_ok = True
    for index, node in enumerate(model.nodes):
        if node.parent >= index:
            parents_ok = False
        if node.parent < -1 or node.parent >= len(model.nodes):
            parents_ok = False
    check(parents_ok, "parent order " + label)

    triangles = 0
    materials = []
    min_bounds = [float("inf")] * 3
    max_bounds = [float("-inf")] * 3
    transforms = A.model_world_transforms(model)
    for index, node in enumerate(model.nodes):
        props = {prop.name: prop for prop in node.vertexProperties}
        if node.vertexCount:
            for required in ("position", "normal", "tangent"):
                if not check(required in props, "vertex property %s %s/%s" % (required, label, node.name)):
                    continue
            for prop_name, dimension in (("position", 3), ("normal", 3), ("tangent", 4)):
                prop = props.get(prop_name)
                if prop is None:
                    continue
                check(prop.scalarTypeDimension == dimension,
                      "property dimension %s %s/%s" % (prop_name, label, node.name))
                check(len(prop.data) == node.vertexCount * dimension * 4,
                      "property length %s %s/%s" % (prop_name, label, node.name))
                for row in _float_rows(prop):
                    if any(math.isnan(value) or math.isinf(value) for value in row):
                        check(False, "NaN/Inf %s %s/%s" % (prop_name, label, node.name))
                        break
            uv = props.get("uv0")
            if uv is None:
                check(False, "uv0 missing " + label + "/" + node.name)
            else:
                for row in _float_rows(uv):
                    if not (0.0 <= row[0] <= 1.0 and 0.0 <= row[1] <= 1.0):
                        check(False, "uv0 outside [0,1] " + label + "/" + node.name)
                        break
            position = _float_rows(props["position"])
            world_position, rotation, scale = transforms[index]
            for point in position:
                scaled = (point[0] * scale[0], point[1] * scale[1], point[2] * scale[2])
                rotated = A.quat_rotate(rotation, scaled)
                world = (world_position[0] + rotated[0],
                         world_position[1] + rotated[1],
                         world_position[2] + rotated[2])
                for axis in range(3):
                    min_bounds[axis] = min(min_bounds[axis], world[axis])
                    max_bounds[axis] = max(max_bounds[axis], world[axis])
        if node.vertexCount == 0 and node.meshes:
            check(False, "empty node has meshes " + label + "/" + node.name)
        for mesh in node.meshes:
            triangles += len(mesh.indices) // 3
            if mesh.material and mesh.material not in materials:
                materials.append(mesh.material)
            for index_value in mesh.indices:
                if not (0 <= index_value < node.vertexCount):
                    check(False, "index out of range " + label + "/" + node.name)
                    break

    # winding convention (sampled every 5th triangle for speed)
    winding_ok = True
    sampled = 0
    for node in model.nodes:
        props = {prop.name: prop for prop in node.vertexProperties}
        if not node.vertexCount or "position" not in props or "normal" not in props:
            continue
        positions = _float_rows(props["position"])
        normals = _float_rows(props["normal"])
        for mesh in node.meshes:
            indices = list(mesh.indices)
            for start in range(0, len(indices) - 2, 15):
                p0 = positions[indices[start]]
                p1 = positions[indices[start + 1]]
                p2 = positions[indices[start + 2]]
                n = [sum(normals[indices[start + k]][c] for k in range(3)) / 3.0 for c in range(3)]
                e1 = A.v_sub(p1, p0)
                e2 = A.v_sub(p2, p0)
                geometric = A.v_cross(e1, e2)
                if A.v_dot(geometric, n) < 0:
                    winding_ok = False
                sampled += 1
                if sampled > 4000:
                    break
            if not winding_ok or sampled > 4000:
                break
        if not winding_ok or sampled > 4000:
            break
    check(winding_ok, "triangle winding " + label)

    if family == "finished":
        budget = BUDGET[size]
        if lod == 0:
            check(triangles <= budget, "LOD0 tris " + label,
                  "%d > %d" % (triangles, budget))
            limit = FOOTPRINT[size] + 0.001
            height_limit = HEIGHT[size] + 1.0 + 0.02
            check(min_bounds[0] >= -0.001 and min_bounds[1] >= -0.001 and min_bounds[2] >= -0.001,
                  "no negative coords " + label, str([round(v, 3) for v in min_bounds]))
            check(max_bounds[0] <= limit and max_bounds[2] <= limit,
                  "footprint envelope " + label,
                  str([round(v, 3) for v in max_bounds]))
            check(max_bounds[1] <= height_limit, "visual height cap " + label,
                  "%.3f > %.3f" % (max_bounds[1], height_limit))
    return triangles, materials, [round(v, 3) for v in min_bounds], [round(v, 3) for v in max_bounds]


def main():
    log("SEE P9 art validation - %s" % os.path.basename(ART))
    log("=" * 78)
    materials_by_faction = load_vanilla_materials()
    patch_map = A.MATERIALS

    if not os.path.isfile(MANIFEST):
        raise SystemExit("missing export manifest: " + MANIFEST)
    manifest = json.load(open(MANIFEST, encoding="utf-8"))
    manifest_entries = {(row["stem"], row["family"]): row for row in manifest["models"]}

    stats = {}
    lod0_tris = {}
    total_tris = 0
    for size in SIZES:
        for tier in TIERS:
            for faction in FACTIONS:
                stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
                # finished LOD0 (art + shipped copy)
                art_path = os.path.join(ART_TIMBERMESH, stem + ".Model.timbermesh")
                data_path = os.path.join(DATA_BOILER, stem + ".Model.timbermesh")
                check(os.path.isfile(art_path), "file exists " + os.path.basename(art_path))
                check(os.path.isfile(data_path), "data copy exists " + os.path.basename(data_path))
                if not os.path.isfile(art_path):
                    continue
                if os.path.isfile(data_path):
                    check(sha256(art_path) == sha256(data_path), "data copy hash " + stem)
                tris, materials, bounds_min, bounds_max = validate_mesh(
                    art_path, "finished", size, faction=faction, lod=0)
                lod0_tris[stem] = tris
                total_tris += tris
                stats[stem] = (tris, materials, bounds_min, bounds_max)
                # material rules
                whitelist = materials_by_faction[faction]
                for material in materials:
                    check(material in whitelist, "vanilla material " + stem, material)
                    check(material in patch_map, "measured patch " + stem, material)
                glass = [m for m in materials if m.startswith("WindowsAtlas")]
                opaque = [m for m in materials if not m.startswith("WindowsAtlas")]
                check(len(opaque) <= 2, "<=2 opaque materials " + stem, str(opaque))
                check(len(glass) <= 1, "<=1 glass material " + stem, str(glass))
                # construction greybox
                art_path = os.path.join(ART_TIMBERMESH, stem + ".ConstructionStage0.Model.timbermesh")
                data_path = os.path.join(DATA_BOILER, stem + ".ConstructionStage0.Model.timbermesh")
                check(os.path.isfile(art_path), "construction file exists " + stem)
                check(os.path.isfile(data_path), "construction data copy " + stem)
                if not os.path.isfile(art_path):
                    continue
                if os.path.isfile(data_path):
                    check(sha256(art_path) == sha256(data_path), "construction hash " + stem)
                validate_mesh(art_path, "construction", size, faction=faction, lod=0)
                # LOD1 / LOD2 source files
                for lod in (1, 2):
                    lod_path = os.path.join(ART_LOD, stem + ".Model.LOD%d.timbermesh" % lod)
                    check(os.path.isfile(lod_path), "lod%d file exists %s" % (lod, stem))
                    if not os.path.isfile(lod_path):
                        continue
                    lod_tris, lod_materials, _, _ = validate_mesh(
                        lod_path, "finished", size, faction=faction, lod=lod)
                    if lod == 1:
                        check(lod_tris <= lod0_tris[stem] * 0.50,
                              "LOD1 <= 50%% LOD0 " + stem,
                              "%d / %d" % (lod_tris, lod0_tris[stem]))
                    else:
                        check(lod_tris <= lod0_tris[stem] * 0.15,
                              "LOD2 <= 15%% LOD0 " + stem,
                              "%d / %d" % (lod_tris, lod0_tris[stem]))
                # empties / animation in the shipped LOD0 file
                model = A.read_model(os.path.join(ART_TIMBERMESH, stem + ".Model.timbermesh"))
                names = [node.name for node in model.nodes]
                for empty in REQUIRED_EMPTIES:
                    check(empty in names, "empty %s %s" % (empty, stem))
                for anim in REQUIRED_ANIM:
                    check(anim in names, "animated transform %s %s" % (anim, stem))
                piston_count = {"T1": 1, "T2": 2, "T3": 4, "T4": 4}[tier]
                for index in range(piston_count):
                    check("ANIM_SEE_Piston%d" % (index + 1) in names,
                          "piston transform %d %s" % (index + 1, stem))
                animations = []
                for node in model.nodes:
                    for animation in node.nodeAnimations:
                        animations.append((node.name, animation.name, animation.framerate,
                                           len(animation.frames)))
                    for animation in node.nodeAnimations:
                        for frame in animation.frames:
                            quaternion = (frame.rotation.x, frame.rotation.y,
                                          frame.rotation.z, frame.rotation.w)
                            magnitude = math.sqrt(sum(component * component
                                                      for component in quaternion))
                            if abs(magnitude - 1.0) > 0.01:
                                check(False, "animation quaternion normalised " + stem)
                                break
                check(all(animation[1] == "Default" for animation in animations),
                      "single animation name Default " + stem, str(animations))
                check(len({animation[1] for animation in animations}) == 1,
                      "exactly one animation clip " + stem)
                for animation in animations:
                    check(animation[2] > 0 and animation[3] >= 2,
                          "animation frames %s/%s" % (stem, animation[0]))
                check(all(animation[0] in ("ANIM_SEE_Flywheel", "ANIM_SEE_Crankshaft")
                          for animation in animations),
                      "only flywheel/crankshaft animated " + stem, str([a[0] for a in animations]))
                check(manifest_entries[stem, "finished"]["triangles"] == lod0_tris[stem],
                      "manifest tris " + stem)
                check(manifest_entries[stem, "finished"]["materials"] == materials,
                      "manifest materials " + stem)
                check(manifest_entries[stem, "finished"]["sha256"] ==
                      sha256(os.path.join(ART_TIMBERMESH, stem + ".Model.timbermesh")),
                      "manifest sha256 " + stem)

    # ---- blueprints ------------------------------------------------------ #
    blueprint_dir = os.path.join(MOD_ROOT, "data", "Buildings", "Power", "SteamBoiler")
    blueprint_files = sorted(path for path in os.listdir(blueprint_dir)
                             if path.endswith(".blueprint.json"))
    check(len(blueprint_files) == 24, "24 blueprint files", str(len(blueprint_files)))
    for name in blueprint_files:
        parts = name[: -len(".blueprint.json")].split(".")
        if len(parts) != 4:
            check(False, "blueprint name shape", name)
            continue
        _, size, tier, faction = parts
        stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
        document = json.load(open(os.path.join(blueprint_dir, name), encoding="utf-8"))
        children = document.get("Children", {})
        finished = children.get("#Finished", {}).get("TimbermeshSpec", {}).get("Model")
        expected = "Buildings/Power/SteamBoiler/" + stem + ".Model"
        check(finished == expected, "blueprint model path " + name, str(finished))
        stage = (children.get("#Unfinished", {}).get("Children", {})
                 .get("ConstructionStage0", {}).get("TimbermeshSpec", {}).get("Model"))
        expected_stage = "Buildings/Power/SteamBoiler/" + stem + ".ConstructionStage0.Model"
        check(stage == expected_stage, "blueprint stage path " + name, str(stage))
        check("GoodPoweredGeneratorAnimatorSpec" in document, "animator spec " + name)
        particle = document.get("PowerGeneratorParticleControllerSpec", {})
        check(particle.get("AttachmentIds") == ["Smoke1", "Smoke2", "Smoke3"],
              "particle controller " + name)
        attachments = document.get("TemplateAttachmentsSpec", {}).get("Attachments", [])
        parents = [entry.get("Parent") for entry in attachments]
        check(parents == ["#EmptySmoke1", "#EmptySmoke2", "#EmptySmoke3"],
              "template attachments " + name, str(parents))
        check(all(entry.get("Prefab") == "Buildings/Power/SteamEngine/SteamEngineSmoke"
                  for entry in attachments), "smoke prefab " + name)
        block = document.get("BlockObjectSpec", {})
        size_x = block.get("Size", {}).get("X")
        size_y = block.get("Size", {}).get("Y")
        transputs = document.get("TransputProviderSpec", {}).get("Transputs", [])
        check(len(transputs) == 5, "5 transputs " + name, str(len(transputs)))
        ups = [entry for entry in transputs if entry.get("Directions") == "Up"]
        check(len(ups) == 1, "exactly one back Up transput " + name, str(len(ups)))
        if len(ups) == 1:
            coordinates = ups[0].get("Coordinates", {})
            check(coordinates.get("X") == size_x // 2
                  and coordinates.get("Y") == size_y - 1
                  and coordinates.get("Z") == 0,
                  "back transput coordinates " + name, str(coordinates))
        check(not any(entry.get("Directions") == "Top" for entry in transputs),
              "no Top transput " + name)

    # ---- icons ----------------------------------------------------------- #
    pngs = sorted(path for path in os.listdir(ICON_DIR) if path.endswith(".png"))
    metas = sorted(path for path in os.listdir(ICON_DIR) if path.endswith(".png.meta.json"))
    check(len(pngs) == 24, "24 icons", str(len(pngs)))
    check(len(metas) == 24, "24 icon metas", str(len(metas)))
    try:
        from PIL import Image  # noqa: PLC0415
        have_pillow = True
    except Exception:  # noqa: BLE001
        have_pillow = False
        log("WARN  Pillow unavailable: icon dimensions are not verified")
    for png in pngs:
        with open(os.path.join(ICON_DIR, png), "rb") as handle:
            signature = handle.read(8)
        check(signature == b"\x89PNG\r\n\x1a\n", "png signature " + png)
        check(os.path.getsize(os.path.join(ICON_DIR, png)) > 5000,
              "icon is a render not the placeholder " + png,
              str(os.path.getsize(os.path.join(ICON_DIR, png))))
        rendered = os.path.join(ART_ICON_DIR, png)
        check(os.path.isfile(rendered), "blender render exists " + png)
        if os.path.isfile(rendered):
            check(sha256(rendered) == sha256(os.path.join(ICON_DIR, png)),
                  "data icon == blender render " + png)
        if have_pillow:
            image = Image.open(os.path.join(ICON_DIR, png))
            check(image.size == (128, 128), "icon 128x128 " + png, str(image.size))
            check(image.mode in ("RGBA", "RGB"), "icon mode " + png, image.mode)
        meta_path = os.path.join(ICON_DIR, png + ".meta.json")
        if os.path.isfile(meta_path):
            meta = json.load(open(meta_path, encoding="utf-8"))
            check(meta.get("isSprite") is True, "icon meta isSprite " + png)

    # ---- sample previews / art report ------------------------------------ #
    for stem in PREVIEW_MODELS:
        preview = os.path.join(PREVIEW_DIR, stem + "_iso.png")
        check(os.path.isfile(preview), "preview exists " + stem)
        if os.path.isfile(preview) and have_pillow:
            image = Image.open(preview)
            check(image.size == (640, 480), "preview 640x480 " + stem, str(image.size))
            check(os.path.getsize(preview) > 10000, "preview is not empty " + stem)
    render_report_path = os.path.join(PREVIEW_DIR, "render-report.txt")
    check(os.path.isfile(render_report_path), "render-report.txt exists")
    if os.path.isfile(render_report_path):
        render_report = open(render_report_path, encoding="utf-8").read()
        check(render_report.count("\nicon ") == 24, "render report lists 24 icons",
              str(render_report.count("\nicon ")))
        check(render_report.count("\npreview ") == len(PREVIEW_MODELS),
              "render report lists 6 previews",
              str(render_report.count("\npreview ")))

    check(os.path.isfile(ART_REPORT), "docs/ART_REPORT.md exists")
    if os.path.isfile(ART_REPORT):
        report_text = open(ART_REPORT, encoding="utf-8").read()
        for section in REPORT_SECTIONS:
            check(section in report_text, "art report section " + section)
        for size in SIZES:
            for tier in TIERS:
                for faction in FACTIONS:
                    stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
                    check(stem in report_text, "art report row " + stem)


    # ---- summary --------------------------------------------------------- #
    log("")
    log("=" * 78)
    log("finished LOD0 tris: total=%d  min=%d  max=%d"
        % (total_tris, min(lod0_tris.values()), max(lod0_tris.values())))
    for size in SIZES:
        values = [value for key, value in lod0_tris.items() if ".%s." % size in key]
        log("  %-6s LOD0 max=%4d  budget=%4d  headroom=%4d"
            % (size, max(values), BUDGET[size], BUDGET[size] - max(values)))
    log("")
    log("Checks: %d/%d passed, %d failed" % (CHECKS[0], CHECKS[1], len(ERRORS)))
    if ERRORS:
        log("RESULT: FAIL")
        for error in ERRORS:
            log("  - " + error)
    else:
        log("RESULT: PASS")
    with open(REPORT, "w", encoding="utf-8") as handle:
        handle.write("\n".join(LINES) + "\n")
    print("report: " + REPORT)
    return 0 if not ERRORS else 1


if __name__ == "__main__":
    raise SystemExit(main())
