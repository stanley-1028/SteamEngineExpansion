"""SEE P9 - procedural SteamBoiler model generator.

Builds the self-made `.timbermesh` art the SEE steam boiler family needs:

  * 24 finished models   (3 sizes x 4 tiers x 2 factions)
  * 24 ConstructionStage0 greyboxes
  * LOD0 / LOD1 / LOD2 source meshes (LOD1 <= 50 %, LOD2 <= 15 % of LOD0)

Composition follows the requirement document 16.4: Body(Size) + TierKit(Size,
Tier) + FactionKit(Size, Faction) are combined into ONE .timbermesh per
blueprint + faction, so 24 blueprints still map to 12 geometries x 2 material
palettes instead of 24 unique models.

Coordinate / material / winding conventions live in artlib.py:
  export space = game model space (block-corner origin, front Z=0),
  vanilla MaterialCollection material names only,
  UV = fixed patches of each material's own 1024^2 vanilla texture.

Run:
  python mod/art/gen_models.py
"""

from __future__ import annotations

import hashlib
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
TOOLS = os.path.join(HERE, "tools")
if TOOLS not in sys.path:
    sys.path.insert(0, TOOLS)

import artlib as A  # noqa: E402

MOD_ROOT = os.path.dirname(HERE)
DATA_BOILER = os.path.join(MOD_ROOT, "data", "Buildings", "Power", "SteamBoiler")
ART_TIMBERMESH = os.path.join(HERE, "timbermesh")
ART_LOD = os.path.join(HERE, "lod")

ANIMATION_NAME = "Default"
ANIMATION_FRAMERATE = 30
ANIMATION_FRAMES = 31

#: per-size massing (export space: X width, Y up, Z depth, front at Z=0).
PARAMS = {
    "Small": {
        "W": 2.0, "D": 2.0, "H": 3.0, "base_h": 0.18,
        "body_w": 1.30, "body_d": 1.20, "body_y0": 0.26, "body_top": 2.20,
        "barrel_r": 0.40, "barrel_y": 1.05, "barrel_n": 1,
        "fly_r": 0.72, "fly_y": 1.34,
        "chim_top": {"T1": 3.30, "T2": 3.45, "T3": 3.60, "T4": 3.75},
        "platform": False, "gear": 0.22, "rivet": 0.05,
    },
    "Medium": {
        "W": 3.0, "D": 3.0, "H": 3.0, "base_h": 0.18,
        "body_w": 1.95, "body_d": 1.65, "body_y0": 0.28, "body_top": 2.30,
        "barrel_r": 0.52, "barrel_y": 1.10, "barrel_n": 2,
        "fly_r": 0.95, "fly_y": 1.45,
        "chim_top": {"T1": 3.35, "T2": 3.50, "T3": 3.70, "T4": 3.85},
        "platform": True, "gear": 0.25, "rivet": 0.055,
    },
    "Large": {
        "W": 4.0, "D": 4.0, "H": 4.0, "base_h": 0.20,
        "body_w": 2.55, "body_d": 2.30, "body_y0": 0.30, "body_top": 3.10,
        "barrel_r": 0.66, "barrel_y": 1.35, "barrel_n": 3,
        "fly_r": 1.22, "fly_y": 1.85,
        "chim_top": {"T1": 4.35, "T2": 4.50, "T3": 4.70, "T4": 4.95},
        "platform": True, "gear": 0.30, "rivet": 0.06,
    },
}

TIER_INDEX = {"T1": 0, "T2": 1, "T3": 2, "T4": 3}
CHIMNEY_COUNT = {
    "Small": {"T1": 1, "T2": 1, "T3": 1, "T4": 1},
    "Medium": {"T1": 1, "T2": 2, "T3": 2, "T4": 2},
    "Large": {"T1": 2, "T2": 2, "T3": 3, "T4": 3},
}
FLYWHEEL_COUNT = {"T1": 1, "T2": 2, "T3": 2, "T4": 1}
PISTON_COUNT = {"T1": 1, "T2": 2, "T3": 4, "T4": 4}
STACK_STATIONS = (0.28, 0.50, 0.72)


# --------------------------------------------------------------------------- #
# helpers
# --------------------------------------------------------------------------- #
def _lod_detail(lod):
    """0 = LOD0 (full), 1 = LOD1 (~50 %), 2 = LOD2 (~15 %)."""
    return lod


def _segments(lod, full=12):
    return {0: full, 1: max(6, full // 2), 2: 6}[lod]


def _slot_for_tier(tier, wood_slot="wood"):
    return {
        "T1": wood_slot,
        "T2": wood_slot,
        "T3": "metal",
        "T4": "metal",
    }[tier]


def _barrel_slot(tier):
    return {"T1": "wood", "T2": "detail", "T3": "metal", "T4": "metal"}[tier]


def _band_slot(tier):
    return {"T1": "metal", "T2": "detail", "T3": "metal", "T4": "detail"}[tier]


def _finished_palette(size, tier, faction):
    """Mk.I/Mk.II (T1/T2) are all-wood; Mk.III/Mk.IV keep the P9 palettes."""
    if tier in ("T1", "T2"):
        return A.AllWoodPalette(size, tier, faction)
    return A.Palette(size, tier, faction)


def _construction_palette(size, tier, faction):
    """Mk.I/Mk.II construction stages are all-wood too; Mk.III/IV greyboxes keep
    the P9 T1 palette (wood + metal) byte-for-byte."""
    if tier in ("T1", "T2"):
        return A.AllWoodPalette(size, tier, faction)
    return A.Palette(size, "T1", faction)


def _stack_x(params, index):
    return params["W"] * STACK_STATIONS[index]


def _stacks(params, size, tier):
    """Returns [(x, z, top_y, kind)] for the three smoke stacks."""
    count = CHIMNEY_COUNT[size][tier]
    tall_indices = {1: (1,), 2: (0, 2), 3: (0, 1, 2)}[count]
    z = params["D"] * 0.76
    result = []
    for index in range(3):
        x = _stack_x(params, index)
        if index in tall_indices:
            top = params["chim_top"][tier]
            kind = "chimney"
        else:
            top = params["body_top"] + 0.28
            kind = "vent"
        result.append((x, z, top, kind))
    return result


def _flywheel_layout(params, tier):
    """Return (x positions, disc thickness, hub thickness) for the tier.

    T1/T4 have the single external flywheel of the P9 design: one disc on the
    +X side.  T2/T3 keep their required double flywheel, but the P9 build placed
    both discs *inside* the housing (x = 0.28W / 0.72W) where the shell hid them
    completely.  The pair is now mounted externally on the same +X side as the
    T1/T4 wheel: both discs stay inside the footprint (the validator allows no
    flywheel overhang) and clear of the left-hand walkway built by
    `build_platform`, and the crankshaft is lengthened to reach them.
    """
    thickness = 0.16 + 0.02 * TIER_INDEX[tier]
    if FLYWHEEL_COUNT[tier] == 1:
        return [params["W"] - 0.22], thickness, thickness * 1.4
    body_right = params["W"] * 0.5 + params["body_w"] * 0.5
    clearance = 0.012
    available = params["W"] - body_right - clearance - 0.002
    gap = 0.10 if available >= 0.40 else 0.02
    thickness = min(thickness, (available - gap) / 2.0)
    hub = thickness
    first = body_right + clearance + hub * 0.5
    second = first + hub + gap
    return [first, second], thickness, hub


def _piston_x(params, tier):
    count = PISTON_COUNT[tier]
    span = params["body_w"] * 0.72
    step = span / max(1, count)
    return [params["W"] * 0.5 + (index - (count - 1) / 2.0) * step for index in range(count)]


def _uv_index(base, offset):
    return base + offset


# --------------------------------------------------------------------------- #
# parts
# --------------------------------------------------------------------------- #
def build_foundation(mesh, palette, params, lod):
    W, D, base_h = params["W"], params["D"], params["base_h"]
    base_slot = "wood" if palette.has("wood") else "metal"
    mesh.box(palette, base_slot, (W * 0.5, base_h * 0.5, D * 0.5),
             (W - 0.10, base_h, D - 0.10))
    mesh.box(palette, "metal", (W * 0.5, base_h + 0.035, D * 0.5),
             (W - 0.26, 0.07, D - 0.26))
    if lod <= 1:
        for sx in (0.11, W - 0.11):
            for sz in (0.11, D - 0.11):
                mesh.box(palette, "metal", (sx, base_h + 0.12, sz), (0.16, 0.16, 0.16))


def build_body(mesh, palette, params, tier, lod):
    W, D = params["W"], params["D"]
    body_w, body_d = params["body_w"], params["body_d"]
    y0, top = params["body_y0"], params["body_top"]
    height = top - y0
    z = D * 0.5
    front = z - body_d * 0.5
    back = z + body_d * 0.5
    shell = _slot_for_tier(tier)
    mesh.box(palette, shell, (W * 0.5, y0 + height * 0.5, z),
             (body_w, height, body_d))
    # top cap
    mesh.box(palette, "metal", (W * 0.5, top - 0.04, z), (body_w * 0.98, 0.10, body_d * 0.98))
    if lod <= 1:
        mesh.box(palette, "metal", (W * 0.5, y0 + 0.04, z), (body_w * 1.03, 0.08, body_d * 1.03))
    # corner posts
    if lod <= 1:
        for sx in (-1, 1):
            for sz in (-1, 1):
                mesh.box(palette, "metal",
                         (W * 0.5 + sx * body_w * 0.48, y0 + height * 0.5, z + sz * body_d * 0.48),
                         (0.11, height * 0.98, 0.11))
    # facade language: planks for T1/T2, riveted sheet metal for T3/T4
    if tier in ("T1", "T2"):
        count = {0: 8, 1: 3, 2: 0}[lod]
        for index in range(count):
            u = (index + 0.5) / count
            x = W * 0.5 - body_w * 0.5 + body_w * u
            plank_h = height * 0.82
            mesh.box(palette, shell, (x, y0 + height * 0.47, front - 0.012),
                     (0.045, plank_h, 0.035))
            mesh.box(palette, shell, (x, y0 + height * 0.47, back + 0.012),
                     (0.045, plank_h, 0.035))
        if lod == 0:
            for side in (-1, 1):
                mesh.box(palette, shell,
                         (W * 0.5 + side * (body_w * 0.5 + 0.012), y0 + height * 0.47, z),
                         (0.035, height * 0.70, body_d * 0.16))
            # horizontal carvel beams + corner bolts
            for level in (0.30, 0.72):
                for side in (-1, 1):
                    mesh.box(palette, "metal",
                             (W * 0.5, y0 + height * level, z + side * (body_d * 0.5 + 0.03)),
                             (body_w * 0.94, 0.07, 0.05))
            for sx in (-1, 1):
                for sz in (-1, 1):
                    mesh.box(palette, "detail",
                             (W * 0.5 + sx * body_w * 0.46, y0 + height * 0.10,
                              z + sz * (body_d * 0.46)),
                             (0.07, 0.07, 0.07))
    else:
        # horizontal panel seams + rivet columns
        seam_rows = 3 if lod <= 1 else 1
        for row in range(seam_rows):
            y = y0 + height * (0.22 + row * 0.24)
            mesh.box(palette, "metal", (W * 0.5, y, front - 0.014), (body_w * 0.96, 0.05, 0.03))
            mesh.box(palette, "metal", (W * 0.5, y, back + 0.014), (body_w * 0.96, 0.05, 0.03))
        if lod == 0:
            cols = 5 if W <= 2.0 else (8 if W <= 3.0 else 10)
            rows = 2 if W <= 2.0 else 3
            for index in range(cols):
                x = W * 0.5 - body_w * 0.44 + body_w * 0.88 * index / (cols - 1)
                for row in range(rows):
                    y = y0 + height * (0.22 + row * 0.24) + 0.10
                    mesh.box(palette, "detail", (x, y, front - 0.028), (params["rivet"], params["rivet"], 0.02))
                    mesh.box(palette, "detail", (x, y, back + 0.028), (params["rivet"], params["rivet"], 0.02))
            # side panel ribs
            rib_rows = 1 if W <= 2.0 else 4
            for side in (-1, 1):
                for row in range(rib_rows):
                    y = y0 + height * (0.18 + row * (0.80 / max(1, rib_rows - 1)))
                    mesh.box(palette, "metal",
                             (W * 0.5 + side * (body_w * 0.5 + 0.014), y, z),
                             (0.03, 0.05, body_d * 0.88))
    # belts / hoops
    belt_count = 2 + (1 if TIER_INDEX[tier] >= 1 else 0) + (1 if TIER_INDEX[tier] >= 3 else 0)
    if lod == 2:
        belt_count = 1
    if lod == 1:
        belt_count = min(belt_count, 3)
    for index in range(belt_count):
        y = y0 + height * (0.18 + index * 0.62 / max(1, belt_count - 1 or 1)) if belt_count > 1 else y0 + height * 0.5
        slot = _band_slot(tier)
        mesh.box(palette, slot, (W * 0.5, y, front - 0.02), (body_w * 1.01, 0.085, 0.045))
        mesh.box(palette, slot, (W * 0.5, y, back + 0.02), (body_w * 1.01, 0.085, 0.045))
        if lod <= 1:
            for side in (-1, 1):
                mesh.box(palette, slot,
                         (W * 0.5 + side * (body_w * 0.5 + 0.02), y, z),
                         (0.045, 0.085, body_d * 1.01))
    return {"front": front, "back": back, "mid": z}


def build_barrels(mesh, palette, params, tier, lod):
    W, D = params["W"], params["D"]
    r = params["barrel_r"]
    y = params["barrel_y"]
    n = params["barrel_n"]
    z = D * 0.5
    # Keep the boiler barrel inside the wooden/metal housing: if the end caps
    # poke through the shell they read as a dark circular "hole" on the front
    # face in the offline previews (the caps sample a dark wood patch).
    length = params["body_d"] - 0.04
    slot = _barrel_slot(tier)
    segments = _segments(lod, 14)
    for index in range(n):
        x = W * 0.5 + (index - (n - 1) / 2.0) * (params["body_w"] * 0.62 / max(1, n))
        mesh.cylinder(palette, slot, (x, y, z), r, length, (0.0, 0.0, 1.0),
                      segments=segments, caps=True)
        if lod <= 1:
            for end in (-1, 1):
                mesh.cylinder(palette, "metal", (x, y, z + end * (length * 0.5 - 0.02)),
                              r * 1.14, 0.08, (0.0, 0.0, 1.0), segments=segments, caps=True)
            band_count = 2 if lod == 0 else 1
            for band in range(band_count):
                bz = z - length * 0.28 + band * length * 0.56
                mesh.ring(palette, _band_slot(tier), (x, y, bz), (0.0, 0.0, 1.0),
                          r + 0.035, r - 0.02, 0.10, segments=segments)
    return y


def build_chimneys(mesh, palette, params, size, tier, lod):
    stacks = _stacks(params, size, tier)
    segments = _segments(lod, 10)
    for x, z, top, kind in stacks:
        base = params["body_top"] - 0.02
        height = top - base
        radius = (0.20 if kind == "chimney" else 0.10) * (0.85 + 0.12 * (params["W"] - 2))
        mesh.cylinder(palette, "metal", (x, base + height * 0.5, z), radius, height,
                      (0.0, 1.0, 0.0), segments=segments, caps=True)
        if lod <= 1:
            mesh.cylinder(palette, _band_slot(tier), (x, base + 0.10, z),
                          radius * 1.30, 0.12, (0.0, 1.0, 0.0), segments=segments, caps=True)
            mesh.cylinder(palette, "metal", (x, top - 0.06, z),
                          radius * 1.40, 0.14, (0.0, 1.0, 0.0), segments=segments, caps=True)
        if lod == 0:
            # flue bolt ring + access ladder + soot-band
            for step in range(4):
                mesh.box(palette, "detail", (x, top - 0.55 + step * 0.11, z - radius),
                         (radius * 2.1, 0.03, radius * 0.30))
            for step in range(3):
                mesh.box(palette, "metal", (x + radius + 0.05, base + height * 0.35 + step * 0.18, z),
                         (0.05, 0.04, 0.20))
            if kind == "vent":
                mesh.cylinder(palette, "detail", (x, top + 0.04, z), radius * 1.10, 0.08,
                              (0.0, 1.0, 0.0), segments=segments, caps=True)
    return stacks


def build_flywheel(mesh, palette, params, tier, lod):
    R = params["fly_r"]
    y, z = params["fly_y"], params["D"] * 0.5
    rim_slot = "metal"
    # Mk.I/Mk.II are all-wood: spoke in the structural wood, rim in the accent
    # wood (both factions now match the Folktails P9 look).
    spoke_slot = "wood" if tier in ("T1", "T2") else "metal"
    segments = _segments(lod, 16)
    positions, thickness, hub = _flywheel_layout(params, tier)
    for x in positions:
        if lod == 2:
            # LOD2 silhouette: solid disc + hub, no spokes (fewer than 60 tris)
            mesh.disc(palette, rim_slot, (x, y, z), (1.0, 0.0, 0.0), R * 0.92,
                      thickness, segments=6)
            mesh.cylinder(palette, "metal", (x, y, z), 0.16, hub,
                          (1.0, 0.0, 0.0), segments=6, caps=True)
            continue
        mesh.ring(palette, rim_slot, (x, y, z), (1.0, 0.0, 0.0),
                  R, R - (0.13 if lod == 0 else 0.16), thickness, segments=segments)
        mesh.cylinder(palette, "metal", (x, y, z), 0.16, hub,
                      (1.0, 0.0, 0.0), segments=_segments(lod, 10), caps=True)
        spokes = 10 if lod == 0 else 4
        inner = R - 0.16
        for index in range(spokes):
            angle = 2.0 * math.pi * index / spokes
            spoke_length = inner - 0.12
            center = (x,
                      y + math.cos(angle) * (0.12 + spoke_length * 0.5),
                      z + math.sin(angle) * (0.12 + spoke_length * 0.5))
            mesh.box(palette, spoke_slot, center,
                     (thickness * 0.55, 0.085, spoke_length),
                     rotation=((1.0, 0.0, 0.0), angle - math.pi / 2.0))
        if lod == 0:
            # bolted rim plates + counterweight
            for index in range(8):
                angle = 2.0 * math.pi * index / 8.0
                mesh.box(palette, "detail",
                         (x, y + math.cos(angle) * (R - 0.06), z + math.sin(angle) * (R - 0.06)),
                         (thickness * 0.8, 0.06, 0.06),
                         rotation=((1.0, 0.0, 0.0), angle - math.pi / 2.0))
            if TIER_INDEX[tier] >= 2:
                mesh.box(palette, "detail", (x, y - inner * 0.82, z),
                         (thickness * 0.7, 0.24, 0.30))
    return y, z


def build_crankshaft(mesh, palette, params, tier, lod):
    W = params["W"]
    y, z = params["fly_y"], params["D"] * 0.5
    length = W - 0.34
    if FLYWHEEL_COUNT[tier] > 1:
        # the externally mounted T2/T3 flywheel pair sits past the P9 shaft end
        last = max(_flywheel_layout(params, tier)[0])
        length = max(length, 2.0 * (last - W * 0.5) + 0.10)
    mesh.cylinder(palette, "metal", (W * 0.5, y, z), 0.085, length,
                  (1.0, 0.0, 0.0), segments=_segments(lod, 10), caps=True)
    if lod == 2:
        return
    for index in range(2):
        x = W * 0.5 + (index - 0.5) * W * 0.21
        mesh.cylinder(palette, "detail", (x, y, z), 0.20, 0.10,
                      (1.0, 0.0, 0.0), segments=_segments(lod, 10), caps=True)
        if lod <= 1:
            mesh.box(palette, "metal", (x, y - 0.16, z), (0.10, 0.24, 0.14))


def build_pistons(model, root, palette, params, tier, lod):
    """Piston + connecting-rod transforms; separate named nodes so a future
    controller can drive them with the programmatic sin (C-9 ③).  They are
    static in the art deliverable."""
    if lod == 2:
        return
    count = PISTON_COUNT[tier]
    y = params["fly_y"]
    front = params["D"] * 0.5 - params["body_d"] * 0.5
    housing_z = front + 0.10
    r = 0.13 + 0.01 * (params["W"] - 2)
    for index in range(count):
        x = _piston_x(params, tier)[index]
        node = A.Node("ANIM_SEE_Piston%d" % (index + 1), root, (x, y, housing_z))
        node.mesh.cylinder(palette, "metal", (x, y, housing_z), r, 0.52,
                           (0.0, 0.0, 1.0), segments=_segments(lod, 10), caps=True)
        node.mesh.cylinder(palette, "detail", (x, y, housing_z + 0.26), r * 0.55, 0.34,
                           (0.0, 0.0, 1.0), segments=_segments(lod, 8), caps=True)
        model.add_node(node)
        rod = A.Node("ANIM_SEE_ConnectingRod%d" % (index + 1), root, (x, y, housing_z + 0.42))
        rod.mesh.box(palette, "metal", (x, y, housing_z + 0.42), (0.07, 0.07, 0.42))
        model.add_node(rod)


def build_platform(mesh, palette, params, lod, size):
    if not params["platform"] or lod > 1:
        return
    W, D = params["W"], params["D"]
    slot = "wood" if palette.has("wood") else "metal"
    levels = 2 if size == "Large" else 1
    for level in range(levels):
        y = 1.25 + level * 0.85
        mesh.box(palette, slot, (0.30, y, D * 0.50), (0.46, 0.07, D * 0.82))
        if lod == 0:
            mesh.box(palette, "metal", (0.06, y + 0.30, D * 0.50), (0.05, 0.05, D * 0.82))
            for post in range(4):
                mesh.box(palette, "metal", (0.06, y + 0.15, D * (0.14 + post * 0.24)),
                         (0.05, 0.30, 0.05))
    if lod == 0:
        steps = 5 if size == "Large" else 4
        for step in range(steps):
            mesh.box(palette, "metal", (0.52, 0.10 + step * 0.22, D * 0.18),
                     (0.30, 0.05, 0.22))


def build_tier_kit(mesh, palette, params, size, tier, lod, faces):
    W, D = params["W"], params["D"]
    front, back, mid = faces["front"], faces["back"], faces["mid"]
    tier_index = TIER_INDEX[tier]
    if lod == 2:
        # only the tier-defining large shapes survive
        if tier_index >= 2:
            mesh.box(palette, "detail", (W * 0.30, params["body_top"] + 0.16, back + 0.12),
                     (0.55, 0.30, 0.35))
        if tier == "T4":
            mesh.box(palette, "glass", (W * 0.5, params["body_top"] - 0.40, front - 0.03),
                     (0.16, 0.46, 0.04))
        return
    # gauges (T2+)
    gauge_count = {0: 0, 1: 2, 2: 2, 3: 3}[tier_index]
    for index in range(gauge_count):
        x = W * 0.5 - 0.42 + index * 0.42
        mesh.cylinder(palette, "detail", (x, params["body_top"] - 0.32, front - 0.035),
                      0.115, 0.06, (0.0, 0.0, 1.0), segments=_segments(lod, 10), caps=True)
        if lod == 0:
            mesh.box(palette, "metal", (x, params["body_top"] - 0.30, front - 0.075),
                     (0.03, 0.10, 0.02))
    # glass water-level gauge (T4)
    if tier == "T4":
        mesh.box(palette, "metal", (W * 0.5, params["body_top"] - 0.40, front - 0.045),
                 (0.22, 0.62, 0.06))
        mesh.box(palette, "glass", (W * 0.5, params["body_top"] - 0.40, front - 0.085),
                 (0.13, 0.50, 0.035))
        if lod == 0:
            for side in (-1, 1):
                mesh.box(palette, "detail", (W * 0.5 + side * 0.09, params["body_top"] - 0.40, front - 0.10),
                         (0.04, 0.56, 0.03))
    # gearbox + gear wheel (T3/T4)
    if tier_index >= 2:
        mesh.box(palette, "metal", (W * 0.30, 0.86, back + 0.13), (0.62, 0.56, 0.32))
        mesh.cylinder(palette, "detail", (W * 0.30, 0.86, back + 0.30), 0.26, 0.09,
                      (0.0, 0.0, 1.0), segments=_segments(lod, 12), caps=True)
        if lod == 0:
            for tooth in range(8):
                angle = 2.0 * math.pi * tooth / 8.0
                mesh.box(palette, "metal",
                         (W * 0.30 + math.cos(angle) * 0.30, 0.86 + math.sin(angle) * 0.30, back + 0.30),
                         (0.09, 0.09, 0.07))
    # condenser (T3/T4)
    if tier_index >= 2:
        mesh.cylinder(palette, "metal", (W * 0.72, params["body_top"] + 0.18, back + 0.10),
                      0.22, 0.62, (1.0, 0.0, 0.0), segments=_segments(lod, 10), caps=True)
    # exhaust valves (T3/T4)
    if tier_index >= 2:
        for index in range(2):
            x = W * (0.36 + index * 0.28)
            mesh.cylinder(palette, "detail", (x, params["body_top"] + 0.14, front + 0.18),
                          0.075, 0.26, (0.0, 1.0, 0.0), segments=_segments(lod, 8), caps=True)
    # governor (T2+)
    if tier_index >= 1:
        base_y = params["body_top"] + 0.10
        mesh.cylinder(palette, "metal", (W * 0.5, base_y, mid), 0.05, 0.34,
                      (0.0, 1.0, 0.0), segments=_segments(lod, 8), caps=True)
        for side in (-1, 1):
            mesh.box(palette, "detail", (W * 0.5 + side * 0.10, base_y + 0.34, mid),
                     (0.07, 0.07, 0.07))
    # T4 polished trim
    if tier == "T4" and lod == 0:
        for side in (-1, 1):
            mesh.box(palette, "detail",
                     (W * 0.5 + side * (params["body_w"] * 0.5 + 0.01), params["body_top"] - 0.16, mid),
                     (0.03, 0.08, params["body_d"] * 0.9))


def build_faction_kit(mesh, palette, params, size, tier, lod):
    if lod == 2:
        return
    W, D = params["W"], params["D"]
    front, back, mid = params["D"] * 0.5 - params["body_d"] * 0.5, params["D"] * 0.5 + params["body_d"] * 0.5, params["D"] * 0.5
    if palette.faction == "Folktails":
        slot = "wood"
        for side in (-1, 1):
            x = W * 0.5 + side * (params["body_w"] * 0.5 + 0.10)
            mesh.box(palette, slot, (x, 0.95, back - 0.16), (0.10, 1.45, 0.10),
                     rotation=((1.0, 0.0, 0.0), side * 0.16))
        mesh.box(palette, slot, (W * 0.5, params["body_top"] - 0.62, front - 0.06),
                 (0.52, 0.30, 0.06))
        if lod == 0:
            for index in range(3):
                mesh.box(palette, "detail",
                         (W * 0.5 - 0.18 + index * 0.18, params["body_top"] - 0.62, front - 0.095),
                         (0.06, 0.04, 0.02))
    else:
        for side in (-1, 1):
            x = W * 0.5 + side * (params["body_w"] * 0.5 + 0.09)
            mesh.box(palette, "metal", (x, 0.95, back - 0.14), (0.11, 1.40, 0.11),
                     rotation=((1.0, 0.0, 0.0), side * 0.30))
        mesh.box(palette, "metal", (W * 0.5, params["body_top"] - 0.62, front - 0.06),
                 (0.50, 0.26, 0.05))
        if lod == 0:
            for index in range(4):
                mesh.box(palette, "detail",
                         (W * 0.5 - 0.18 + index * 0.12, params["body_top"] - 0.62, front - 0.095),
                         (0.04, 0.04, 0.02))


# --------------------------------------------------------------------------- #
# finished model
# --------------------------------------------------------------------------- #
def build_finished(size, tier, faction, lod):
    params = PARAMS[size]
    palette = _finished_palette(size, tier, faction)
    stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
    display = ("SM_SEE_SteamBoiler_%s_%s_%s_LOD%d" % (size, tier, faction, lod))
    model = A.Model(stem + ".Model" + ("" if lod == 0 else ".LOD%d" % lod))
    root = A.Node(display)
    model.add_node(root)
    mesh = root.mesh

    build_foundation(mesh, palette, params, lod)
    faces = build_body(mesh, palette, params, tier, lod)
    build_barrels(mesh, palette, params, tier, lod)
    stacks = build_chimneys(mesh, palette, params, size, tier, lod)
    build_platform(mesh, palette, params, lod, size)
    build_tier_kit(mesh, palette, params, size, tier, lod, faces)
    build_faction_kit(mesh, palette, params, size, tier, lod)

    # animated flywheel / crankshaft pivots (same axis line, both spin about +X)
    fly_y = params["fly_y"]
    fly_z = params["D"] * 0.5
    pivot = (params["W"] * 0.5, fly_y, fly_z)
    flywheel = A.Node("ANIM_SEE_Flywheel", root, pivot)
    model.add_node(flywheel)
    build_flywheel(flywheel.mesh, palette, params, tier, lod)
    crankshaft = A.Node("ANIM_SEE_Crankshaft", root, pivot)
    model.add_node(crankshaft)
    build_crankshaft(crankshaft.mesh, palette, params, tier, lod)

    if lod <= 1:
        frames = []
        for frame in range(ANIMATION_FRAMES):
            angle = 2.0 * math.pi * frame / (ANIMATION_FRAMES - 1)
            frames.append((pivot, A.quat_from_axis_angle((1.0, 0.0, 0.0), angle), (1.0, 1.0, 1.0)))
        flywheel.add_animation(ANIMATION_NAME, ANIMATION_FRAMERATE, frames)
        crankshaft.add_animation(ANIMATION_NAME, ANIMATION_FRAMERATE, frames)

    # pistons + connecting rods are separate named transforms (program hook, C-9 ③)
    build_pistons(model, root, palette, params, tier, lod)

    # smoke points / attachment empties (only needed in the shipped LOD0 file)
    if lod == 0:
        for index, (x, z, top, kind) in enumerate(stacks):
            model.add_node(A.empty_node("#EmptySmoke%d" % (index + 1), root, (x, top + 0.05, z)))
        model.add_node(A.empty_node("ATT_WaterIn", root, (params["W"] * 0.5, 0.36, params["D"] - 0.02)))
        model.add_node(A.empty_node("ATT_FuelIn", root, (0.06, 0.42, params["D"] * 0.28)))
        model.add_node(A.empty_node("ATT_Entrance", root, (params["W"] * 0.5, 0.10, 0.0)))
        model.add_node(A.empty_node("ATT_FlywheelPivot", root, pivot))
    return model


def build_construction(size, tier, faction):
    params = PARAMS[size]
    palette = _construction_palette(size, tier, faction)
    stem = "SteamBoiler.%s.%s.%s.ConstructionStage0" % (size, tier, faction)
    model = A.Model(stem + ".Model")
    root = A.Node("SM_SEE_SteamBoiler_%s_%s_%s_ConstructionStage0" % (size, tier, faction))
    model.add_node(root)
    mesh = root.mesh
    W, D, H = params["W"], params["D"], params["H"]
    wood = "wood" if palette.has("wood") else "metal"
    # foundation
    mesh.box(palette, wood, (W * 0.5, params["base_h"] * 0.5, D * 0.5),
             (W - 0.12, params["base_h"], D - 0.12))
    mesh.box(palette, "metal", (W * 0.5, params["base_h"] + 0.03, D * 0.5),
             (W - 0.30, 0.06, D - 0.30))
    # four corner posts + two low walls
    post = 0.14
    for sx in (-1, 1):
        for sz in (-1, 1):
            mesh.box(palette, wood,
                     (W * 0.5 + sx * (W * 0.5 - 0.22), params["body_y0"] + 0.62, D * 0.5 + sz * (D * 0.5 - 0.24)),
                     (post, 1.24, post))
    mesh.box(palette, wood, (W * 0.5, params["body_y0"] + 0.22, D * 0.5 - D * 0.30),
             (W * 0.72, 0.42, 0.09))
    mesh.box(palette, wood, (W * 0.5, params["body_y0"] + 0.22, D * 0.5 + D * 0.30),
             (W * 0.72, 0.42, 0.09))
    # scaffold poles + a boom
    mesh.box(palette, "metal", (W * 0.5, 1.10, D * 0.50), (0.07, 2.0, 0.07))
    mesh.box(palette, "metal", (W * 0.5, 2.0, D * 0.50), (W * 0.70, 0.07, 0.07))
    mesh.box(palette, "metal", (W * 0.22, 1.55, D * 0.50), (0.06, 1.30, 0.06),
             rotation=((0.0, 1.0, 0.0), 0.30))
    mesh.box(palette, "metal", (W * 0.78, 1.55, D * 0.50), (0.06, 1.30, 0.06),
             rotation=((0.0, 1.0, 0.0), -0.30))
    return model


def build_construction_greybox_geometry(model, size, tier, faction):
    return model


# --------------------------------------------------------------------------- #
# write everything
# --------------------------------------------------------------------------- #
def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for chunk in iter(lambda: handle.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _seed_legacy_uv_cache():
    """Reproduce the P9 legacy UV-cache state for the untouched Mk.III/IV files.

    `artlib.Palette` caches UV patches by (faction, slot) instead of by material,
    so the shipped P9 files were produced *after* the T1 palette had seeded each
    (faction, slot) entry.  Mk.I/Mk.II now use `AllWoodPalette` with a separate
    cache, so without this seed the first legacy palette to run would be T3 and
    the Mk.III/IV UVs (and therefore their bytes) would drift.  Seeding the exact
    old state keeps T3/T4 byte-identical to their P9 delivery.
    """
    for faction in A.FACTIONS:
        seed = A.Palette("Small", "T1", faction)
        for slot in ("wood", "metal", "detail"):
            seed.uv_patch(slot)


def main():
    os.makedirs(ART_TIMBERMESH, exist_ok=True)
    os.makedirs(ART_LOD, exist_ok=True)
    os.makedirs(DATA_BOILER, exist_ok=True)
    _seed_legacy_uv_cache()
    manifest = {"generated_by": "mod/art/gen_models.py", "models": [], "lod_comparison": []}
    failures = []

    for size in A.SIZES:
        for tier in A.TIERS:
            for faction in A.FACTIONS:
                stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
                try:
                    lod_stats = {}
                    for lod in (0, 1, 2):
                        model = build_finished(size, tier, faction, lod)
                        dto = model.finish()
                        stats = A.model_stats(dto)
                        lod_stats[lod] = stats
                        if lod == 0:
                            path = os.path.join(ART_TIMBERMESH, stem + ".Model.timbermesh")
                            A.write_model(dto, path)
                            copy_target = os.path.join(DATA_BOILER, stem + ".Model.timbermesh")
                            with open(copy_target, "wb") as handle:
                                handle.write(open(path, "rb").read())
                            manifest["models"].append(_entry(stem, "finished", size, tier, faction, 0, stats, path))
                        else:
                            path = os.path.join(ART_LOD, stem + ".Model.LOD%d.timbermesh" % lod)
                            A.write_model(dto, path)
                    base = lod_stats[0]["triangles"]
                    manifest["lod_comparison"].append({
                        "stem": stem,
                        "lod0": base,
                        "lod1": lod_stats[1]["triangles"],
                        "lod2": lod_stats[2]["triangles"],
                        "lod1_ratio": round(lod_stats[1]["triangles"] / max(1, base), 4),
                        "lod2_ratio": round(lod_stats[2]["triangles"] / max(1, base), 4),
                    })
                    # construction greybox
                    construction = build_construction(size, tier, faction)
                    dto = construction.finish()
                    stats = A.model_stats(dto)
                    path = os.path.join(ART_TIMBERMESH, stem + ".ConstructionStage0.Model.timbermesh")
                    A.write_model(dto, path)
                    with open(os.path.join(DATA_BOILER, stem + ".ConstructionStage0.Model.timbermesh"), "wb") as handle:
                        handle.write(open(path, "rb").read())
                    manifest["models"].append(_entry(stem, "construction", size, tier, faction, 0, stats, path))
                except Exception as error:  # noqa: BLE001 - keep generating and report
                    import traceback
                    traceback.print_exc()
                    failures.append({"stem": stem, "error": "%s: %s" % (type(error).__name__, error)})
                    print("FAILED", stem, error)

    manifest["failures"] = failures
    manifest["model_count"] = len(manifest["models"])
    with open(os.path.join(ART_TIMBERMESH, "export-manifest.json"), "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, indent=2, ensure_ascii=False)
        handle.write("\n")
    print("=" * 78)
    print("finished + construction models: %d" % len(manifest["models"]))
    worst = max(manifest["lod_comparison"], key=lambda row: row["lod2_ratio"])
    print("worst LOD2 ratio: %s %.1f%%" % (worst["stem"], worst["lod2_ratio"] * 100))
    if failures:
        print("FAILURES: %d" % len(failures))
        return 1
    print("GENERATION PASS")
    return 0


def _entry(stem, family, size, tier, faction, lod, stats, path):
    return {
        "stem": stem,
        "family": family,
        "size": size,
        "tier": tier,
        "faction": faction,
        "lod": lod,
        "file": os.path.relpath(path, MOD_ROOT).replace("\\", "/"),
        "bytes": os.path.getsize(path),
        "sha256": sha256(path),
        "triangles": stats["triangles"],
        "vertices": stats["vertices"],
        "materials": stats["materials"],
        "bounds_min": stats["bounds_min"],
        "bounds_max": stats["bounds_max"],
        "nodes": stats["nodes"],
    }


if __name__ == "__main__":
    raise SystemExit(main())
