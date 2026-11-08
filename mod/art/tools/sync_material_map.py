"""SEE P9 - vanilla material patch map (derived, read-only source).

The 17 vanilla `MaterialCollection` materials can only be referenced by their
asset name in a `.timbermesh`; the game resolves them per faction.  The UV
patch rectangles below are the *real* per-material texture patches measured by
the sister project HDBF (ART-05/ART-07): each material resolves to its own
1024x1024 `_D` albedo PNG plus a `_Color` tint, and the game's runtime
AutoAtlaser preserves the normalised position inside that texture.  So a UV
inside `uv_rect` samples the curated coherent region of the material's own
texture, in-game, with no custom atlas / AssetBundle.

Source of truth (read-only): HDBF `art/material_texture_map.json`
(records the texture file, sha256 and the patch selection evidence).
This script only extracts the subset SEE uses and keeps the provenance.
"""

from __future__ import annotations

import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.dirname(HERE)
OUT = os.path.join(ART, "material_patches.json")

HDBF_MAP = (r"C:\Users\user\Documents\Timberborn\Mods\HighDensityBoxFarm"
            r"\art\material_texture_map.json")

#: materials SEE actually uses, grouped by logical slot role.
USED = {
    "wood": [
        "BaseWood_Brown.Folktails",
        "BaseWood_LightBrown.Folktails",
        "BaseWood_White.Folktails",
        "BaseWood_DarkBrown.IronTeeth",
        "BaseWood_Grey.IronTeeth",
        "IrregularPlanks_MetalForged.IronTeeth",
        "RoofPlanks.IronTeeth",
    ],
    "metal": [
        "BaseMetal.Folktails",
        "BaseMetal.IronTeeth",
    ],
    "detail": [
        "Details.Folktails",
        "Details.IronTeeth",
    ],
    "glass": [
        "WindowsAtlas.Folktails",
        "WindowsAtlas.IronTeeth",
    ],
}


def main():
    with open(HDBF_MAP, "r", encoding="utf-8") as handle:
        source = json.load(handle)
    materials = source["materials"]
    subset = {}
    missing = []
    for role, names in USED.items():
        for name in names:
            entry = materials.get(name)
            if entry is None:
                missing.append(name)
                continue
            patch = entry["patch"]
            subset[name] = {
                "role": role,
                "patch_class": entry["patch_class"],
                "texture": entry["texture"],
                "texture_path": entry["texture_path"],
                "texture_size": entry["texture_size"],
                "texture_sha256": entry["texture_sha256"],
                "uv_rect": [round(v, 8) for v in patch["uv_rect"]],
                "pixel_rect": patch["pixel_rect"],
                "material_color_tint": [round(v, 6) for v in entry["material_color_tint"]],
                "mean_rgb": [round(v, 3) for v in patch["mean_rgb"]],
                "reason": patch["reason"],
            }
    if missing:
        raise SystemExit("missing materials in HDBF map: %r" % missing)

    document = {
        "schema": "see.material_patches/v1",
        "generated_by": "mod/art/tools/sync_material_map.py",
        "source": {
            "project": "HighDensityBoxFarm (sister project, read-only)",
            "file": "art/material_texture_map.json",
            "schema": source.get("schema"),
            "note": "HDBF ART-05/ART-07 real per-material patch map; SEE reuses the "
                    "vanilla material asset names and the measured UV patches only.",
        },
        "uv_convention": "UV space of the material's own 1024x1024 _D albedo texture; "
                         "AutoAtlaser preserves the normalised position inside the atlas cell.",
        "material_count": len(subset),
        "materials": subset,
    }
    with open(OUT, "w", encoding="utf-8") as handle:
        json.dump(document, handle, indent=2, ensure_ascii=False)
        handle.write("\n")
    print("wrote %s (%d materials)" % (OUT, len(subset)))


if __name__ == "__main__":
    main()
