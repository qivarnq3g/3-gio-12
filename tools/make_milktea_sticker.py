"""Draw the order label and thank-you note for the Night 2 milk tea cup (section 5.4 of dem-2.md).

    python -m pip install pillow==12.0.0
    python tools/make_milktea_sticker.py

Writes game/Assets/Art/Textures/T_MilkTeaSticker.png. The top half is the order label
and the bottom half is the note. The Windows font files stay outside the repo.
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "game" / "Assets" / "Art" / "Textures" / "T_MilkTeaSticker.png"
FONT = Path("C:/Windows/Fonts/arial.ttf")
FONT_BOLD = Path("C:/Windows/Fonts/arialbd.ttf")
FONT_NOTE = Path("C:/Windows/Fonts/ariali.ttf")
W, H = 512, 512


def main() -> None:
    image = Image.new("RGB", (W, H), (246, 244, 236))
    draw = ImageDraw.Draw(image)
    small = ImageFont.truetype(str(FONT), 33)
    bold = ImageFont.truetype(str(FONT_BOLD), 49)
    head = ImageFont.truetype(str(FONT_BOLD), 28)
    note = ImageFont.truetype(str(FONT_NOTE), 60)
    signature = ImageFont.truetype(str(FONT_NOTE), 48)
    center = W // 2

    draw.rectangle((0, 0, W, 61), fill=(222, 120, 40))
    draw.text((center, 19), "TRÀ SỮA NHÀ GÓC · #0412", font=head, fill=(255, 255, 255), anchor="mt")
    draw.text((center, 77), "Trà sữa trân châu", font=bold, fill=(20, 20, 20), anchor="mt")
    draw.text((center, 142), "Ít đường · Trân châu trắng", font=small, fill=(60, 60, 60), anchor="mt")
    draw.line((18, 188, W - 18, 188), fill=(170, 170, 170), width=2)
    draw.text((center, 202), "Size M · Giao: gửi chủ trọ", font=small, fill=(60, 60, 60), anchor="mt")

    draw.rectangle((0, H // 2, W, H), fill=(255, 234, 176))
    draw.text((center, 316), "Cảm ơn cháu.", font=note, fill=(51, 37, 27), anchor="mt")
    draw.text((center, 407), "Mẹ Ngân", font=signature, fill=(51, 37, 27), anchor="mt")

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    image.save(OUTPUT, optimize=True)
    print(OUTPUT)


if __name__ == "__main__":
    main()
