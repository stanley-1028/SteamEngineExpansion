"""SEE P9 - compose the 24-variant gallery into one contact sheet per faction.

Input : mod/art/preview/gallery/<stem>_iso.png  (rendered by render_gallery.py)
Output: mod/art/preview/gallery/Folktails_grid.png
        mod/art/preview/gallery/IronTeeth_grid.png

The sheet is 4 columns (T1..T4) x 3 rows (Small/Medium/Large), each cell is the
shipped LOD0 model rendered by Blender; labels are English ASCII only so no CJK
font is required.

Run:
  python mod/art/tools/make_contact_sheet.py
"""

from __future__ import annotations

import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.dirname(HERE)
GALLERY = os.path.join(ART, "preview", "gallery")

SIZES = ["Small", "Medium", "Large"]
TIERS = ["T1", "T2", "T3", "T4"]
FACTIONS = ["Folktails", "IronTeeth"]

CELL = (480, 360)
PAD = 12
LEFT = 118
TOP = 46
HEADER = 64
BACKGROUND = (28, 30, 33)
CELL_BACKGROUND = (42, 45, 49)
TEXT = (236, 238, 240)
ACCENT = (214, 178, 108)


def load_font(size, bold=False):
    candidates = [
        r"C:\Windows\Fonts\arialbd.ttf" if bold else r"C:\Windows\Fonts\arial.ttf",
        r"C:\Windows\Fonts\segoeuib.ttf" if bold else r"C:\Windows\Fonts\segoeui.ttf",
    ]
    for path in candidates:
        if os.path.isfile(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def make_sheet(faction):
    width = LEFT + 4 * CELL[0] + 5 * PAD
    height = HEADER + TOP + 3 * CELL[1] + 4 * PAD
    sheet = Image.new("RGB", (width, height), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    title_font = load_font(28, bold=True)
    label_font = load_font(20, bold=True)
    small_font = load_font(16)

    palette = {"Folktails": "T1-T2 all wood / T3-T4 metal", "IronTeeth": "T1-T2 all wood / T3-T4 metal"}[faction]
    draw.text((LEFT, 16), "SEE SteamBoiler - %s   (%s)" % (faction, palette),
              font=title_font, fill=ACCENT)

    for column, tier in enumerate(TIERS):
        x = LEFT + PAD + column * (CELL[0] + PAD)
        draw.text((x + CELL[0] // 2, HEADER + 12), tier, font=label_font,
                  fill=TEXT, anchor="mm")
    for row, size in enumerate(SIZES):
        y = HEADER + TOP + row * (CELL[1] + PAD)
        draw.text((PAD, y + CELL[1] // 2), size, font=label_font, fill=TEXT, anchor="lm")
        footprint = {"Small": "2x2 h3", "Medium": "3x3 h3", "Large": "4x4 h4"}[size]
        draw.text((PAD, y + CELL[1] // 2 + 26), footprint, font=small_font,
                  fill=(170, 174, 180), anchor="lm")

    for row, size in enumerate(SIZES):
        for column, tier in enumerate(TIERS):
            stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
            path = os.path.join(GALLERY, stem + "_iso.png")
            x = LEFT + PAD + column * (CELL[0] + PAD)
            y = HEADER + TOP + row * (CELL[1] + PAD)
            draw.rounded_rectangle((x, y, x + CELL[0], y + CELL[1]), radius=8,
                                   fill=CELL_BACKGROUND)
            if not os.path.isfile(path):
                draw.text((x + CELL[0] // 2, y + CELL[1] // 2), "missing",
                          font=label_font, fill=(200, 90, 90), anchor="mm")
                continue
            image = Image.open(path).convert("RGB").resize(CELL, Image.LANCZOS)
            sheet.paste(image, (x, y))
    return sheet


def main():
    os.makedirs(GALLERY, exist_ok=True)
    for faction in FACTIONS:
        sheet = make_sheet(faction)
        output = os.path.join(GALLERY, faction + "_grid.png")
        sheet.save(output)
        print("wrote", output, sheet.size, "%d bytes" % os.path.getsize(output))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
