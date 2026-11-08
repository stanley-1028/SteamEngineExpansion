"""SEE P9 - pure-Python timbermesh authoring library (no Blender, no Unity).

Geometry is authored directly in the game's model space, i.e. the coordinate
space stored inside a `.timbermesh`:

    X = width   -> [0, sizeX]      (right)
    Y = height  -> [0, ...]        (up)
    Z = depth   -> [0, sizeY]      (front is the Z=0 face)

That is the block-corner origin convention measured by the sister project HDBF
(`art/README-timbermesh.md` section 3) and confirmed by the original game models
(MWP / ShantySpeaker): the importer sets `localPosition` / `localRotation` /
`localScale` from the DTO node exactly, so authoring in this space needs no
Blender coordinate compensation.

Winding: probe_winding.py measured 100% of triangles in the shipped
ShantySpeaker/MWP meshes to have cross(p1-p0, p2-p0) aligned with the stored
vertex normals (right-handed reading).  MeshBuilder.add_tri enforces exactly
that, so primitives only have to supply outward normals.

Materials: only vanilla `MaterialCollection` asset names are stored.  UVs are
fixed patches of the material's own 1024^2 `_D` texture (see
material_patches.json, derived from HDBF ART-05/ART-07).
"""

from __future__ import annotations

import json
import math
import os
import struct
import sys
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.dirname(HERE)

VENDORED_PLUGIN = os.path.join(
    r"C:\Users\user\Documents\Timberborn\Mods\HighDensityBoxFarm\art\tools",
    "timbermesh_blender_plugin")

MATERIAL_PATCHES_PATH = os.path.join(ART, "material_patches.json")


def _pb():
    """Lazy protobuf import: Blender's bundled Python can import the geometry
    modules without the protobuf runtime, which is only needed to write/read
    `.timbermesh` files."""
    if VENDORED_PLUGIN not in sys.path:
        sys.path.insert(0, VENDORED_PLUGIN)
    import model_pb2  # noqa: E402
    return model_pb2


# --------------------------------------------------------------------------- #
# material palette
# --------------------------------------------------------------------------- #
def load_materials():
    with open(MATERIAL_PATCHES_PATH, "r", encoding="utf-8") as handle:
        return json.load(handle)["materials"]


MATERIALS = load_materials()

#: A / B are the two horizontal axes of the model (X width, Z depth).
SIZE_SPECS = {
    "Small": {"x": 2, "z": 2, "h": 3},
    "Medium": {"x": 3, "z": 3, "h": 3},
    "Large": {"x": 4, "z": 4, "h": 4},
}
TIERS = ["T1", "T2", "T3", "T4"]
FACTIONS = ["Folktails", "IronTeeth"]
SIZES = ["Small", "Medium", "Large"]
LOD0_BUDGET = {"Small": 3500, "Medium": 6000, "Large": 9000}


class Palette:
    """Logical slot -> vanilla material asset name for one (size, tier, faction)."""

    def __init__(self, size, tier, faction):
        self.size = size
        self.tier = tier
        self.faction = faction
        ft = faction == "Folktails"
        if tier == "T1":
            slots = {
                "wood": "BaseWood_Brown.Folktails" if ft else "BaseWood_DarkBrown.IronTeeth",
                "metal": "BaseMetal.Folktails" if ft else "BaseMetal.IronTeeth",
                # T1 has no separate brass/steel accent material; the metal slot
                # doubles as the accent so T1 stays at 2 opaque materials.
                "detail": "BaseMetal.Folktails" if ft else "BaseMetal.IronTeeth",
            }
        elif tier == "T2":
            # Requirement 16.5: at most 2 opaque material slots + 1 glass.
            # T2 is "wood + brass", so the structural metal slot resolves to the
            # brass/copper detail texture too (2 unique materials).
            slots = {
                "wood": "BaseWood_LightBrown.Folktails" if ft else "BaseWood_Grey.IronTeeth",
                "metal": "Details.Folktails" if ft else "Details.IronTeeth",
                "detail": "Details.Folktails" if ft else "Details.IronTeeth",
            }
        elif tier == "T3":
            # T3 is cast iron + steel: the structural wood slot becomes metal.
            slots = {
                "wood": "BaseMetal.Folktails" if ft else "BaseMetal.IronTeeth",
                "metal": "BaseMetal.Folktails" if ft else "BaseMetal.IronTeeth",
                "detail": "Details.Folktails" if ft else "Details.IronTeeth",
            }
        else:  # T4: refined metal + glass (exactly 2 opaque + 1 glass)
            slots = {
                "wood": "BaseMetal.Folktails" if ft else "BaseMetal.IronTeeth",
                "metal": "BaseMetal.Folktails" if ft else "BaseMetal.IronTeeth",
                "detail": "Details.Folktails" if ft else "Details.IronTeeth",
                "glass": "WindowsAtlas.Folktails" if ft else "WindowsAtlas.IronTeeth",
            }
        self.slots = slots

    def name(self, slot):
        return self.slots[slot]

    def uv_rect(self, slot):
        material = self.slots[slot]
        return MATERIALS[material]["uv_rect"]

    def has(self, slot):
        return slot in self.slots


#: Mk.I (T1) and Mk.II (T2) are the all-wood tiers: every visible slot resolves
#: to one of the faction's two wood materials, so the whole building reads as
#: timber (no metal/brass anywhere).  The two tones swap between tiers so Mk.I
#: and Mk.II stay visually distinct, and "detail" reuses the accent wood so the
#: <=2 opaque material budget of requirement 16.5 still holds.
ALL_WOOD_SLOTS = {
    ("T1", "Folktails"): ("BaseWood_Brown.Folktails", "BaseWood_LightBrown.Folktails"),
    ("T2", "Folktails"): ("BaseWood_LightBrown.Folktails", "BaseWood_Brown.Folktails"),
    ("T1", "IronTeeth"): ("BaseWood_DarkBrown.IronTeeth", "BaseWood_Grey.IronTeeth"),
    ("T2", "IronTeeth"): ("BaseWood_Grey.IronTeeth", "BaseWood_DarkBrown.IronTeeth"),
}


class AllWoodPalette(Palette):
    """All-wood slot map for Mk.I/Mk.II (T1/T2): structural wood + accent wood."""

    def __init__(self, size, tier, faction):
        if (tier, faction) not in ALL_WOOD_SLOTS:
            raise ValueError("AllWoodPalette only covers T1/T2: %s/%s" % (tier, faction))
        super().__init__(size, tier, faction)
        self.tier = tier
        self.faction = faction
        structural, accent = ALL_WOOD_SLOTS[(tier, faction)]
        self.slots = {"wood": structural, "metal": accent, "detail": accent}


# --------------------------------------------------------------------------- #
# small vector helpers (pure python)
# --------------------------------------------------------------------------- #
def v_add(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def v_sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def v_mul(a, s):
    return (a[0] * s, a[1] * s, a[2] * s)


def v_dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def v_cross(a, b):
    return (a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0])


def v_len(a):
    return math.sqrt(v_dot(a, a))


def v_norm(a):
    length = v_len(a)
    if length < 1e-12:
        return (0.0, 0.0, 0.0)
    return (a[0] / length, a[1] / length, a[2] / length)


def quat_from_axis_angle(axis, radians):
    axis = v_norm(axis)
    half = radians / 2.0
    s = math.sin(half)
    return (axis[0] * s, axis[1] * s, axis[2] * s, math.cos(half))


def quat_mul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (
        aw * bx + ax * bw + ay * bz - az * by,
        aw * by - ax * bz + ay * bw + az * bx,
        aw * bz + ax * by - ay * bx + az * bw,
        aw * bw - ax * bx - ay * by - az * bz,
    )


def quat_rotate(q, vector):
    q = _quat_normalise(q)
    x, y, z, w = q
    vx, vy, vz = vector
    # t = 2 * cross(q.xyz, v)
    tx = 2.0 * (y * vz - z * vy)
    ty = 2.0 * (z * vx - x * vz)
    tz = 2.0 * (x * vy - y * vx)
    return (vx + w * tx + (y * tz - z * ty),
            vy + w * ty + (z * tx - x * tz),
            vz + w * tz + (x * ty - y * tx))


def _quat_normalise(q):
    length = math.sqrt(sum(component * component for component in q))
    if length < 1e-12:
        return (0.0, 0.0, 0.0, 1.0)
    return tuple(component / length for component in q)


def rotate_vector(vector, axis, radians):
    """Rodrigues rotation (used by the few slanted struts)."""
    axis = v_norm(axis)
    c = math.cos(radians)
    s = math.sin(radians)
    return v_add(v_add(v_mul(vector, c),
                       v_mul(v_cross(axis, vector), s)),
                 v_mul(axis, v_dot(axis, vector) * (1.0 - c)))


# --------------------------------------------------------------------------- #
# UV patch packer
# --------------------------------------------------------------------------- #
class UvPatch:
    """Splits a material patch into deterministic sub-cells so faces do not all
    sample the exact same texels (mirrors HDBF ART-04's flat-patch idea, but in
    the material's real texture space)."""

    INSET = 0.06  # keep bilinear filtering inside the validated patch
    CELLS = 2     # 2 x 2 sub-cells per patch

    def __init__(self, rect):
        self.u0, self.v0, self.u1, self.v1 = rect
        self.du = (self.u1 - self.u0) / self.CELLS
        self.dv = (self.v1 - self.v0) / self.CELLS

    def cell(self, index):
        index %= self.CELLS * self.CELLS
        cx = index % self.CELLS
        cy = index // self.CELLS
        inset_u = self.du * self.INSET
        inset_v = self.dv * self.INSET
        u0 = self.u0 + cx * self.du + inset_u
        v0 = self.v0 + cy * self.dv + inset_v
        u1 = self.u0 + (cx + 1) * self.du - inset_u
        v1 = self.v0 + (cy + 1) * self.dv - inset_v
        return (u0, v0, u1, v1)

    def uv(self, index, u, v):
        u0, v0, u1, v1 = self.cell(index)
        return (u0 + (u1 - u0) * u, v0 + (v1 - v0) * v)


# --------------------------------------------------------------------------- #
# mesh builder
# --------------------------------------------------------------------------- #
class MeshBuilder:
    """Collects flat/smooth triangles grouped by vanilla material asset name."""

    def __init__(self):
        self.groups = {}   # material -> list of corner tuples
        self.order = []    # material first-seen order
        self._face = 0

    def add_tri(self, material, corners, uv_index=None):
        """corners = ((p0,n0,uv0),(p1,n1,uv1),(p2,n2,uv2)). Winding is fixed
        automatically so the right-handed geometric normal matches the supplied
        averaged vertex normal (the shipped-file convention)."""
        p0, n0, uv0 = corners[0]
        p1, n1, uv1 = corners[1]
        p2, n2, uv2 = corners[2]
        geometric = v_cross(v_sub(p1, p0), v_sub(p2, p0))
        average = ((n0[0] + n1[0] + n2[0]) / 3.0,
                   (n0[1] + n1[1] + n2[1]) / 3.0,
                   (n0[2] + n1[2] + n2[2]) / 3.0)
        if v_len(geometric) < 1e-12:
            return
        if v_dot(geometric, average) < 0.0:
            corners = (corners[0], corners[2], corners[1])
        if material not in self.groups:
            self.groups[material] = []
            self.order.append(material)
        self.groups[material].extend(corners)
        self._face += 1

    # -- primitives ------------------------------------------------------- #
    _FACE_BASIS = {
        # normal -> (u axis, v axis) with cross(u, v) == normal
        (0.0, 1.0, 0.0): ((1.0, 0.0, 0.0), (0.0, 0.0, 1.0)),
        (0.0, -1.0, 0.0): ((1.0, 0.0, 0.0), (0.0, 0.0, -1.0)),
        (1.0, 0.0, 0.0): ((0.0, 0.0, 1.0), (0.0, -1.0, 0.0)),
        (-1.0, 0.0, 0.0): ((0.0, 0.0, 1.0), (0.0, 1.0, 0.0)),
        (0.0, 0.0, 1.0): ((1.0, 0.0, 0.0), (0.0, 1.0, 0.0)),
        (0.0, 0.0, -1.0): ((-1.0, 0.0, 0.0), (0.0, 1.0, 0.0)),
    }

    def box(self, palette, slot, center, size, uv_index=None, rotation=None):
        """Axis-aligned (or rotated about a given axis) box, 12 triangles.

        rotation = (axis, radians) rotates the box around its own centre; the UV
        basis is taken from the un-rotated face so textures stay upright.
        """
        material = palette.name(slot)
        patch = palette.uv_patch(slot)
        cx, cy, cz = center
        sx, sy, sz = (size[0] / 2.0, size[1] / 2.0, size[2] / 2.0)
        base_axis = rotation[0] if rotation else None
        angle = rotation[1] if rotation else 0.0

        def place(offset):
            x, y, z = offset
            if base_axis is not None:
                x, y, z = rotate_vector((x, y, z), base_axis, angle)
            return (cx + x, cy + y, cz + z)

        faces = [
            ((0.0, 1.0, 0.0), [(+1, +1, +1), (+1, +1, -1), (-1, +1, -1), (-1, +1, +1)]),
            ((0.0, -1.0, 0.0), [(+1, -1, -1), (+1, -1, +1), (-1, -1, +1), (-1, -1, -1)]),
            ((1.0, 0.0, 0.0), [(+1, +1, -1), (+1, +1, +1), (+1, -1, +1), (+1, -1, -1)]),
            ((-1.0, 0.0, 0.0), [(-1, +1, +1), (-1, +1, -1), (-1, -1, -1), (-1, -1, +1)]),
            ((0.0, 0.0, 1.0), [(-1, +1, +1), (+1, +1, +1), (+1, -1, +1), (-1, -1, +1)]),
            ((0.0, 0.0, -1.0), [(+1, +1, -1), (-1, +1, -1), (-1, -1, -1), (+1, -1, -1)]),
        ]
        cell_index = self._face if uv_index is None else uv_index

        for normal, signs in faces:
            local = [(sx * a, sy * b, sz * c) for a, b, c in signs]
            u_axis, v_axis = self._FACE_BASIS[tuple(round(x) for x in normal)]
            u_extent = max(abs(v_dot(corner, u_axis)) for corner in local)
            v_extent = max(abs(v_dot(corner, v_axis)) for corner in local)
            world_normal = (rotate_vector(normal, base_axis, angle)
                            if base_axis is not None else normal)
            uv = []
            for corner in local:
                uu = 0.5 + 0.5 * (v_dot(corner, u_axis) / u_extent if u_extent > 1e-9 else 0.0)
                vv = 0.5 + 0.5 * (v_dot(corner, v_axis) / v_extent if v_extent > 1e-9 else 0.0)
                uv.append(patch.uv(cell_index, uu, vv))
            points = [place(corner) for corner in local]
            material_corners = [(points[i], world_normal, uv[i]) for i in range(4)]
            self.add_tri(material, (material_corners[0], material_corners[1], material_corners[2]))
            self.add_tri(material, (material_corners[0], material_corners[2], material_corners[3]))
            cell_index += 1

    def cylinder(self, palette, slot, center, radius, length, axis, segments=10,
                 caps=True, uv_index=None):
        """Cylinder whose local axis is one of (1,0,0)/(0,1,0)/(0,0,1)."""
        material = palette.name(slot)
        patch = palette.uv_patch(slot)
        axis = tuple(float(v) for v in axis)
        if abs(axis[0]) > 0.5:
            basis_b, basis_c = (0.0, 1.0, 0.0), (0.0, 0.0, 1.0)
        elif abs(axis[1]) > 0.5:
            basis_b, basis_c = (1.0, 0.0, 0.0), (0.0, 0.0, 1.0)
        else:
            basis_b, basis_c = (1.0, 0.0, 0.0), (0.0, 1.0, 0.0)
        half = length / 2.0
        start = v_sub(center, v_mul(axis, half))
        end = v_add(center, v_mul(axis, half))
        cell = self._face if uv_index is None else uv_index

        def ring_point(origin, angle):
            return v_add(origin,
                         v_add(v_mul(basis_b, radius * math.cos(angle)),
                               v_mul(basis_c, radius * math.sin(angle))))

        def radial(angle):
            return v_norm(v_add(v_mul(basis_b, math.cos(angle)),
                                v_mul(basis_c, math.sin(angle))))

        for index in range(segments):
            a0 = 2.0 * math.pi * index / segments
            a1 = 2.0 * math.pi * (index + 1) / segments
            p00 = ring_point(start, a0)
            p10 = ring_point(start, a1)
            p01 = ring_point(end, a0)
            p11 = ring_point(end, a1)
            n0 = radial(a0)
            n1 = radial(a1)
            uv00 = patch.uv(cell, index / float(segments), 0.0)
            uv10 = patch.uv(cell, (index + 1) / float(segments), 0.0)
            uv01 = patch.uv(cell, index / float(segments), 1.0)
            uv11 = patch.uv(cell, (index + 1) / float(segments), 1.0)
            # quad: start a0 -> start a1 -> end a1 -> end a0
            self.add_tri(material, ((p00, n0, uv00), (p10, n1, uv10), (p11, n1, uv11)))
            self.add_tri(material, ((p00, n0, uv00), (p11, n1, uv11), (p01, n0, uv01)))

        if caps:
            for origin, normal, flip in ((start, v_mul(axis, -1.0), False),
                                         (end, axis, True)):
                center_point = origin
                for index in range(segments):
                    a0 = 2.0 * math.pi * index / segments
                    a1 = 2.0 * math.pi * (index + 1) / segments
                    p0 = ring_point(origin, a0)
                    p1 = ring_point(origin, a1)
                    uv0 = patch.uv(cell, 0.5 + 0.25 * math.cos(a0), 0.5 + 0.25 * math.sin(a0))
                    uv1 = patch.uv(cell, 0.5 + 0.25 * math.cos(a1), 0.5 + 0.25 * math.sin(a1))
                    uvc = patch.uv(cell, 0.5, 0.5)
                    if flip:
                        self.add_tri(material, ((center_point, normal, uvc), (p0, normal, uv0), (p1, normal, uv1)))
                    else:
                        self.add_tri(material, ((center_point, normal, uvc), (p1, normal, uv1), (p0, normal, uv0)))

    def ring(self, palette, slot, center, axis, outer_radius, inner_radius, thickness,
             segments=12, uv_index=None):
        """Flat annulus (flywheel rim): front + back annulus and two bands."""
        material = palette.name(slot)
        patch = palette.uv_patch(slot)
        axis = tuple(float(v) for v in axis)
        if abs(axis[0]) > 0.5:
            basis_b, basis_c = (0.0, 1.0, 0.0), (0.0, 0.0, 1.0)
        else:
            basis_b, basis_c = (1.0, 0.0, 0.0), (0.0, 1.0, 0.0)
        half = thickness / 2.0
        minus_center = v_sub(center, v_mul(axis, half))
        plus_center = v_add(center, v_mul(axis, half))
        cell = self._face if uv_index is None else uv_index

        def point(origin, radius, angle):
            return v_add(origin,
                         v_add(v_mul(basis_b, radius * math.cos(angle)),
                               v_mul(basis_c, radius * math.sin(angle))))

        def radial(angle):
            return v_norm(v_add(v_mul(basis_b, math.cos(angle)),
                                v_mul(basis_c, math.sin(angle))))

        for index in range(segments):
            a0 = 2.0 * math.pi * index / segments
            a1 = 2.0 * math.pi * (index + 1) / segments
            for origin, normal, ustart in ((minus_center, v_mul(axis, -1.0), 0.0),
                                           (plus_center, axis, 0.5)):
                o0 = point(origin, outer_radius, a0)
                o1 = point(origin, outer_radius, a1)
                i0 = point(origin, inner_radius, a0)
                i1 = point(origin, inner_radius, a1)
                uv_o0 = patch.uv(cell, ustart, 0.0)
                uv_o1 = patch.uv(cell, ustart + 0.25, 0.0)
                uv_i0 = patch.uv(cell, ustart, 1.0)
                uv_i1 = patch.uv(cell, ustart + 0.25, 1.0)
                self.add_tri(material, ((i0, normal, uv_i0), (o0, normal, uv_o0), (o1, normal, uv_o1)))
                self.add_tri(material, ((i0, normal, uv_i0), (o1, normal, uv_o1), (i1, normal, uv_i1)))
            # outer band
            n0 = radial(a0)
            n1 = radial(a1)
            b0 = point(minus_center, outer_radius, a0)
            b1 = point(minus_center, outer_radius, a1)
            b2 = point(plus_center, outer_radius, a1)
            b3 = point(plus_center, outer_radius, a0)
            self.add_tri(material, ((b0, n0, patch.uv(cell, 0.0, 0.0)),
                                    (b1, n1, patch.uv(cell, 0.2, 0.0)),
                                    (b2, n1, patch.uv(cell, 0.2, 1.0))))
            self.add_tri(material, ((b0, n0, patch.uv(cell, 0.0, 0.0)),
                                    (b2, n1, patch.uv(cell, 0.2, 1.0)),
                                    (b3, n0, patch.uv(cell, 0.0, 1.0))))
            # inner band
            inward0 = v_mul(n0, -1.0)
            inward1 = v_mul(n1, -1.0)
            c0 = point(minus_center, inner_radius, a0)
            c1 = point(minus_center, inner_radius, a1)
            c2 = point(plus_center, inner_radius, a1)
            c3 = point(plus_center, inner_radius, a0)
            self.add_tri(material, ((c0, inward0, patch.uv(cell, 0.4, 0.0)),
                                    (c1, inward1, patch.uv(cell, 0.6, 0.0)),
                                    (c2, inward1, patch.uv(cell, 0.6, 1.0))))
            self.add_tri(material, ((c0, inward0, patch.uv(cell, 0.4, 0.0)),
                                    (c2, inward1, patch.uv(cell, 0.6, 1.0)),
                                    (c3, inward0, patch.uv(cell, 0.4, 1.0))))

    def disc(self, palette, slot, center, axis, radius, thickness, segments=12):
        self.cylinder(palette, slot, center, radius, thickness, axis,
                      segments=segments, caps=True)

    def triangle_count(self):
        return sum(len(corners) // 3 for corners in self.groups.values())


# --------------------------------------------------------------------------- #
# palette UV patch cache (per palette instance)
# --------------------------------------------------------------------------- #
_patch_cache = {}


def _uv_patch(self, slot):
    key = (self.faction, slot)
    if key not in _patch_cache:
        _patch_cache[key] = UvPatch(self.uv_rect(slot))
    return _patch_cache[key]


Palette.uv_patch = _uv_patch


#: Mk.I/Mk.II use their own cache keyed by material name.  The legacy cache above
#: is keyed by (faction, slot) and is therefore order-dependent; keeping the two
#: caches apart means the all-wood tiers can never perturb the P9 UV state of the
#: untouched Mk.III/IV files (see gen_models._seed_legacy_uv_cache).
_all_wood_patch_cache = {}


def _all_wood_uv_patch(self, slot):
    material = self.slots[slot]
    if material not in _all_wood_patch_cache:
        _all_wood_patch_cache[material] = UvPatch(self.uv_rect(slot))
    return _all_wood_patch_cache[material]


AllWoodPalette.uv_patch = _all_wood_uv_patch


# --------------------------------------------------------------------------- #
# node / model construction
# --------------------------------------------------------------------------- #
class Node:
    def __init__(self, name, parent=None, position=(0.0, 0.0, 0.0),
                 rotation=(0.0, 0.0, 0.0, 1.0), scale=(1.0, 1.0, 1.0)):
        self.name = name
        self.parent = parent          # Node or None
        self.position = position
        self.rotation = rotation
        self.scale = scale
        self.mesh = MeshBuilder()
        self.node_animations = []     # list of (name, framerate, frames)

    def add_animation(self, name, framerate, frames):
        """frames = list of (position, rotation, scale)."""
        self.node_animations.append((name, framerate, frames))


class Model:
    def __init__(self, name):
        self.name = name
        self.nodes = []               # pre-order, parents before children

    def add_node(self, node):
        assert node not in self.nodes
        self.nodes.append(node)
        return node

    def finish(self):
        """Validate parent order and return the DTO message."""
        model_pb2 = _pb()
        model = model_pb2.Model()
        model.version = 0
        model.name = self.name
        index_of = {}
        for index, node in enumerate(self.nodes):
            index_of[id(node)] = index
        for index, node in enumerate(self.nodes):
            dto = model.nodes.add()
            dto.name = node.name
            dto.parent = index_of[id(node.parent)] if node.parent is not None else -1
            if dto.parent >= index:
                raise ValueError("parent must precede child: %s" % node.name)
            dto.position.x, dto.position.y, dto.position.z = node.position
            dto.rotation.x, dto.rotation.y, dto.rotation.z, dto.rotation.w = node.rotation
            dto.scale.x, dto.scale.y, dto.scale.z = node.scale
            # geometry is authored in model space; a child node's vertices are
            # stored relative to the node's own localPosition (the pivot).
            _fill_node_mesh(dto, node.mesh, node.position)
            for animation_name, framerate, frames in node.node_animations:
                animation = dto.nodeAnimations.add()
                animation.name = animation_name
                animation.framerate = float(framerate)
                for position, rotation, scale in frames:
                    frame = animation.frames.add()
                    frame.position.x, frame.position.y, frame.position.z = position
                    frame.rotation.x, frame.rotation.y, frame.rotation.z, frame.rotation.w = rotation
                    frame.scale.x, frame.scale.y, frame.scale.z = scale
        return model


def _fill_node_mesh(dto, mesh, origin=(0.0, 0.0, 0.0)):
    positions = []
    normals = []
    tangents = []
    colors = []
    uv0 = []
    index_lists = {material: [] for material in mesh.order}
    for material in mesh.order:
        corners = mesh.groups[material]
        indices = index_lists[material]
        for offset in range(0, len(corners), 3):
            tri = corners[offset:offset + 3]
            base = len(positions)
            # tangent from the triangle's UV/position deltas
            p0, n0, t0 = tri[0]
            p1, n1, t1 = tri[1]
            p2, n2, t2 = tri[2]
            tangent, sign = _triangle_tangent(p0, p1, p2, t0, t1, t2, n0)
            for point, normal, uv in tri:
                positions.append((point[0] - origin[0],
                                  point[1] - origin[1],
                                  point[2] - origin[2]))
                normals.append(normal)
                tangents.append((tangent[0], tangent[1], tangent[2], sign))
                colors.append((1.0, 1.0, 1.0, 1.0))
                uv0.append(uv)
            indices.extend((base, base + 1, base + 2))
    model_pb2 = _pb()
    dto.vertexCount = len(positions)
    dto.vertexProperties.append(_property(positions, "position", 3))
    dto.vertexProperties.append(_property(normals, "normal", 3))
    dto.vertexProperties.append(_property(tangents, "tangent", 4))
    if uv0:
        # the official plugin writes color/uv0 only when the source mesh has them
        dto.vertexProperties.append(_property(colors, "color", 4))
        dto.vertexProperties.append(_property(uv0, "uv0", 2))
    for material in mesh.order:
        dto_mesh = dto.meshes.add()
        dto_mesh.material = material
        dto_mesh.indices.extend(index_lists[material])


def _triangle_tangent(p0, p1, p2, t0, t1, t2, normal):
    e1 = v_sub(p1, p0)
    e2 = v_sub(p2, p0)
    du1, dv1 = t1[0] - t0[0], t1[1] - t0[1]
    du2, dv2 = t2[0] - t0[0], t2[1] - t0[1]
    determinant = du1 * dv2 - du2 * dv1
    if abs(determinant) < 1e-12:
        tangent = (1.0, 0.0, 0.0)
        return tangent, 1.0
    # dP/du = (e1*dv2 - e2*dv1) / det ; dP/dv = (e2*du1 - e1*du2) / det
    tangent = v_norm(v_mul(v_sub(v_mul(e1, dv2), v_mul(e2, dv1)), 1.0 / determinant))
    bitangent = v_norm(v_mul(v_sub(v_mul(e2, du1), v_mul(e1, du2)), 1.0 / determinant))
    sign = 1.0 if v_dot(v_cross(normal, tangent), bitangent) >= 0.0 else -1.0
    return tangent, sign


def _property(rows, name, dimension):
    model_pb2 = _pb()
    container = model_pb2.VertexProperty()
    container.name = name
    container.scalarType = model_pb2.SCALAR_TYPE_FLOAT
    container.scalarTypeDimension = dimension
    payload = bytearray()
    for row in rows:
        for component in row:
            payload += struct.pack("<f", float(component))
    container.data = bytes(payload)
    return container


def empty_node(name, parent, position):
    """Marker empty (matched by name from the blueprint attachment specs)."""
    return Node(name, parent=parent, position=position)


# --------------------------------------------------------------------------- #
# write / read
# --------------------------------------------------------------------------- #
def write_model(model, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as handle:
        handle.write(zlib.compress(model.SerializeToString()))


def read_model(path):
    model = _pb().Model()
    model.ParseFromString(zlib.decompress(open(path, "rb").read()))
    return model


def float_rows(property_message):
    dimension = max(property_message.scalarTypeDimension, 1)
    count = len(property_message.data) // (4 * dimension)
    return [struct.unpack_from("<%df" % dimension, property_message.data, i * 4 * dimension)
            for i in range(count)]


def model_world_transforms(model):
    """Compose each DTO node's world transform (parent chain, pre-order)."""
    transforms = []
    for node in model.nodes:
        local_position = (node.position.x, node.position.y, node.position.z)
        local_rotation = (node.rotation.x, node.rotation.y, node.rotation.z, node.rotation.w)
        local_scale = (node.scale.x, node.scale.y, node.scale.z)
        if node.parent < 0:
            transforms.append((local_position, local_rotation, local_scale))
            continue
        parent_position, parent_rotation, parent_scale = transforms[node.parent]
        scaled = (local_position[0] * parent_scale[0],
                  local_position[1] * parent_scale[1],
                  local_position[2] * parent_scale[2])
        rotates = quat_rotate(parent_rotation, scaled)
        world_position = (parent_position[0] + rotates[0],
                          parent_position[1] + rotates[1],
                          parent_position[2] + rotates[2])
        world_rotation = quat_mul(parent_rotation, local_rotation)
        world_scale = (parent_scale[0] * local_scale[0],
                       parent_scale[1] * local_scale[1],
                       parent_scale[2] * local_scale[2])
        transforms.append((world_position, world_rotation, world_scale))
    return transforms


def model_stats(model):
    triangles = 0
    vertices = 0
    materials = []
    bounds_min = [float("inf")] * 3
    bounds_max = [float("-inf")] * 3
    transforms = model_world_transforms(model)
    for index, node in enumerate(model.nodes):
        vertices += node.vertexCount
        props = {prop.name: prop for prop in node.vertexProperties}
        if node.vertexCount and "position" in props:
            position, rotation, scale = transforms[index]
            for point in float_rows(props["position"]):
                scaled = (point[0] * scale[0], point[1] * scale[1], point[2] * scale[2])
                rotated = quat_rotate(rotation, scaled)
                world = (position[0] + rotated[0],
                         position[1] + rotated[1],
                         position[2] + rotated[2])
                for axis in range(3):
                    bounds_min[axis] = min(bounds_min[axis], world[axis])
                    bounds_max[axis] = max(bounds_max[axis], world[axis])
        for mesh in node.meshes:
            triangles += len(mesh.indices) // 3
            if mesh.material and mesh.material not in materials:
                materials.append(mesh.material)
    if vertices == 0:
        bounds_min = [0.0, 0.0, 0.0]
        bounds_max = [0.0, 0.0, 0.0]
    return {
        "triangles": triangles,
        "vertices": vertices,
        "materials": materials,
        "bounds_min": [round(v, 4) for v in bounds_min],
        "bounds_max": [round(v, 4) for v in bounds_max],
        "nodes": [node.name for node in model.nodes],
    }
