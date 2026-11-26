"""Draw the delivery sticker on the Night 2 milk tea cup (docs/kich-ban/dem-2.md, mục 5.4 bước 6).

    python -m pip install pillow==12.0.0
    python tools/make_milktea_sticker.py

Writes game/Assets/Art/Textures/T_MilkTeaSticker.png. Text is printed with Arial (Windows system font),
the font file is not copied into the repo. The shop name and order code are made up.
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "game" / "Assets" / "Art" / "Textures" / "T_MilkTeaSticker.png"
FONT = Path("C:/Windows/Fonts/arial.ttf")
FONT_BOLD = Path("C:/Windows/Fonts/arialbd.ttf")
W, H = 512, 512
NOTE = ["Ít đường.", "Trân châu trắng.", "Cảm ơn cháu.", "Mẹ Ngân"]


def main() -> None:
    image = Image.new("RGB", (W, H), (246, 244, 236))
    draw = ImageDraw.Draw(image)
    small = ImageFont.truetype(str(FONT), 26)
    bold = ImageFont.truetype(str(FONT_BOLD), 50)
    head = ImageFont.truetype(str(FONT_BOLD), 30)
    center = W // 2

    draw.rectangle((0, 0, W, 60), fill=(222, 120, 40))
    draw.text((center, 14), "TRÀ SỮA NHÀ GÓC · #0412", font=head, fill=(255, 255, 255), anchor="mt")
    draw.text((center, 76), "Size M · Giao: gửi chủ trọ", font=small, fill=(60, 60, 60), anchor="mt")
    draw.line((18, 118, W - 18, 118), fill=(170, 170, 170), width=2)
    draw.text((center, 128), "Ghi chú:", font=small, fill=(60, 60, 60), anchor="mt")
    y = 172
    # Center the note on the part of the curved sticker facing the player.
    for line in NOTE:
        draw.text((center, y), line, font=bold, fill=(20, 20, 20), anchor="mt")
        y += 82

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    image.save(OUTPUT, optimize=True)
    print(OUTPUT)


if __name__ == "__main__":
    main()
