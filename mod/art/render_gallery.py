"""SEE P9 - render a full 24-variant gallery (all sizes x tiers x factions).

Optional extra (not part of the §7.2 six-preview deliverable): renders one
640x480 still per shipped model into `mod/art/preview/gallery/` so a human can
compare all 12 designs x 2 faction palettes at a glance. Uses the same shipped
`.Model.timbermesh` files and the same camera/light/material setup as
`render_previews.py`.

Run (after python mod/art/gen_models.py):
  blender.exe -b --factory-startup -noaudio --python mod/art/render_gallery.py
Optional:
  ... -- --tiers T1,T2         (only render the T1/T2 gallery images;
                                --tiers=T1,T2 is accepted as well)

With `--tiers` the images outside the filter are left untouched.
"""

from __future__ import annotations

import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import render_previews as R  # noqa: E402  (bpy/artlib paths are set up there)

GALLERY_DIR = os.path.join(R.PREVIEW_DIR, "gallery")
SAMPLES = 40
RESOLUTION = (640, 480)


def gallery_jobs(tiers=None):
    """(stem, mesh_path) list for the requested tiers (None = all 24)."""
    jobs = []
    for size in R.A.SIZES:
        for tier in R.A.TIERS:
            if tiers and tier not in tiers:
                continue
            for faction in R.A.FACTIONS:
                stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
                mesh_path = os.path.join(R.ART_TIMBERMESH, stem + ".Model.timbermesh")
                if not os.path.isfile(mesh_path):
                    raise SystemExit("missing shipped model: " + mesh_path)
                jobs.append((stem, mesh_path))
    return jobs


def main():
    arguments = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    tiers = R.parse_tier_filter(arguments)
    if tiers:
        print("tier filter:", ", ".join(tiers))
    jobs = gallery_jobs(tiers)
    os.makedirs(GALLERY_DIR, exist_ok=True)
    count = 0
    total = len(jobs)
    for stem, mesh_path in jobs:
        output = os.path.join(GALLERY_DIR, stem + "_iso.png")
        R.render_model(mesh_path, stem, output, RESOLUTION, SAMPLES, False)
        count += 1
        print("gallery %d/%d %s" % (count, total, output))
    print("gallery done:", count, "images ->", GALLERY_DIR)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
