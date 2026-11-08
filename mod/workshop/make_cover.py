"""Generate the Steam Workshop cover assets for Steam Engine Expansion.

Input : mod/workshop/cover/ai-raw-2732x1535.jpg  (Seedream output, carries a
        "豆包AI生成" watermark in the bottom-right corner)
Output: clean 16:9 image without the watermark, titled cover, 1280x720 and
        322x181 thumbnails, square-crop QA image.

Run: python make_cover.py  (Pillow required; the DSH bundled python has it)
"""

from __future__ import annotations

import os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
COVER = os.path.join(HERE, "cover")
RAW = os.path.join(COVER, "ai-raw-2732x1535.jpg")

FONT_IMPACT = r"C:\Windows\Fonts\impact.ttf"
CREAM = (247, 236, 208)
DARK = (36, 22, 16)
GOLD = (255, 226, 160)

# The watermark sits in the bottom-right corner. Cropping the right edge to 2516 px
# removes it completely; cropping the bottom to 16:9 keeps the composition legal.
CROP_RIGHT = 2516


def load_clean() -> Image.Image:
    image = Image.open(RAW).convert("RGB")
    width, height = image.size
    height = int(round(CROP_RIGHT * 9 / 16))
    image = image.crop((0, 0, CROP_RIGHT, height))
    return image.resize((1920, 1080), Image.LANCZOS)


def draw_titled(image: Image.Image) -> Image.Image:
    canvas = image.copy()
    draw = ImageDraw.Draw(canvas)
    title = ImageFont.truetype(FONT_IMPACT, 112)
    subtitle = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 40)

    def outlined(text: str, xy: tuple[int, int], font, fill, outline, stroke: int) -> None:
        x, y = xy
        draw.text((x + 6, y + 6), text, font=font, fill=(0, 0, 0))
        draw.text((x, y), text, font=font, fill=fill,
                  stroke_width=stroke, stroke_fill=outline)

    outlined("STEAM ENGINE", (64, 46), title, CREAM, DARK, 7)
    outlined("EXPANSION", (64, 152), title, CREAM, DARK, 7)
    outlined("12 BOILERS  ·  2 FACTIONS  ·  MAINTENANCE", (68, 292), subtitle, GOLD, DARK, 4)
    return canvas


def save_jpeg(image: Image.Image, path: str, quality: int = 90) -> None:
    image.save(path, "JPEG", quality=quality, optimize=True, progressive=True)
    print(f"{os.path.basename(path):32s} {os.path.getsize(path):>8d} B")


def main() -> None:
    clean = load_clean()
    save_jpeg(clean, os.path.join(COVER, "clean-1920x1080.jpg"), 92)

    titled = draw_titled(clean)
    save_jpeg(titled, os.path.join(COVER, "mockup-1920x1080.jpg"), 90)

    thumb = titled.resize((1280, 720), Image.LANCZOS)
    thumb.save(os.path.join(COVER, "thumbnail-1280x720.png"), "PNG", optimize=True)
    thumb.save(os.path.join(HERE, "thumbnail.png"), "PNG", optimize=True)
    thumb.resize((322, 181), Image.LANCZOS).save(os.path.join(COVER, "qa-322x181.png"), "PNG")

    # Square crop QA (Timberborn Workshop uses square thumbnails in some lists).
    side = min(titled.size)
    left = (titled.width - side) // 2
    top = (titled.height - side) // 2
    titled.crop((left, top, left + side, top + side)).resize((1080, 1080), Image.LANCZOS).save(
        os.path.join(COVER, "qa-square-1080.jpg"), "JPEG", quality=90, optimize=True)

    with open(os.path.join(HERE, "thumbnail.png.meta.json"), "w", encoding="utf-8", newline="\n") as handle:
        handle.write('{"isSprite": true}\n')
    print("done")


if __name__ == "__main__":
    main()
