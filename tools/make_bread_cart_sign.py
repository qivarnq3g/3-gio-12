import sys, random
from PIL import Image, ImageDraw, ImageFont, ImageFilter

W, H = 1024, 512
F = "C:/Windows/Fonts/"
im = Image.new("RGB", (W, H), (240, 240, 236))
d = ImageDraw.Draw(im)
d.rectangle([0, 0, W, 164], fill=(178, 28, 30))
d.rectangle([8, 8, W - 8, 156], outline=(250, 214, 90), width=5)


def center(text, font, y, fill):
    w = d.textlength(text, font=font)
    d.text(((W - w) / 2, y), text, font=font, fill=fill)


center("BÁNH MÌ", ImageFont.truetype(F + "arialbd.ttf", 92), 14, (255, 232, 120))
center("THỊT NGUỘI - PA TÊ - CHẢ LỤA", ImageFont.truetype(F + "arialbd.ttf", 30), 116, (255, 255, 255))
y0 = 180
d.rectangle([40, y0, 300, y0 + 236], fill=(255, 255, 255))
rng = random.Random(7)
q0x, q0y, cell, n = 70, y0 + 18, 8, 25
for i in range(n):
    for j in range(n):
        if rng.random() < 0.5:
            d.rectangle([q0x + i * cell, q0y + j * cell, q0x + i * cell + cell - 1, q0y + j * cell + cell - 1], fill=(15, 15, 15))
for fx, fy in [(0, 0), (n - 7, 0), (0, n - 7)]:
    x, y = q0x + fx * cell, q0y + fy * cell
    d.rectangle([x, y, x + 7 * cell - 1, y + 7 * cell - 1], fill=(15, 15, 15))
    d.rectangle([x + cell, y + cell, x + 6 * cell - 1, y + 6 * cell - 1], fill=(255, 255, 255))
    d.rectangle([x + 2 * cell, y + 2 * cell, x + 5 * cell - 1, y + 5 * cell - 1], fill=(15, 15, 15))
d.rectangle([300, y0, 1010, y0 + 236], fill=(28, 92, 168))
d.text((330, y0 + 36), "QUÉT MÃ", font=ImageFont.truetype(F + "arialbd.ttf", 64), fill=(255, 255, 255))
d.text((330, y0 + 126), "CHUYỂN KHOẢN", font=ImageFont.truetype(F + "arialbd.ttf", 30), fill=(255, 255, 255))
d.text((330, y0 + 168), "Không cần tiền lẻ", font=ImageFont.truetype(F + "arial.ttf", 30), fill=(210, 225, 245))
noise = Image.effect_noise((W, H), 10).convert("RGB")
im = Image.blend(im, noise, 0.04).filter(ImageFilter.GaussianBlur(0.6))
im.save(sys.argv[1])
