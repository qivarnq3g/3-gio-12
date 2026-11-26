"""Dựng slide thuyết trình (.pptx) từ docs/slide/slides.md.

Định dạng slides.md: các slide cách nhau bằng dòng `---`. Trong mỗi slide:
  # Tiêu đề slide
  - ý chính            (thụt hai dấu cách là ý con)
  ![mô tả](đường/dẫn.png)        ảnh cạnh chữ; thêm {full} ngay sau để ảnh chiếm cả chiều rộng
  > lời dẫn cho người trình bày (ghi chú của slide, không hiện trên màn hình)
Đường dẫn ảnh tính từ thư mục gốc repo. Ảnh chưa có thì slide vẽ khung ghi "Cần ảnh: <mô tả>" và script liệt kê.

Chạy: python tools/build_slides.py   (ghi docs/slide/build/3-gio-12.pptx)
"""
import re
import sys
from pathlib import Path

from PIL import Image
from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.oxml.ns import qn
from pptx.oxml.xmlchemy import OxmlElement
from pptx.util import Emu, Inches, Pt

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "docs" / "slide" / "slides.md"
OUTPUT = ROOT / "docs" / "slide" / "build" / "3-gio-12.pptx"

BACKGROUND = RGBColor(0x12, 0x16, 0x1E)
PANEL = RGBColor(0x1E, 0x24, 0x30)
TEXT = RGBColor(0xF2, 0xF2, 0xF2)
MUTED = RGBColor(0xA8, 0xB0, 0xBC)
ACCENT = RGBColor(0xD6, 0x45, 0x45)
FONT = "Segoe UI"
SLIDE_W, SLIDE_H = Inches(13.333), Inches(7.5)
MARGIN = Inches(0.6)
IMAGE_RE = re.compile(r"^!\[([^\]]*)\]\(([^)]+)\)(\{full\})?\s*$")


def parse(text):
    slides = []
    for block in re.split(r"(?m)^---\s*$", text):
        lines = [l.rstrip() for l in block.strip("\n").splitlines()]
        if not any(l.strip() for l in lines):
            continue
        slide = {"title": "", "bullets": [], "images": [], "notes": []}
        for line in lines:
            if line.startswith("# "):
                slide["title"] = line[2:].strip()
            elif line.startswith(">"):
                slide["notes"].append(line[1:].strip())
            elif IMAGE_RE.match(line.strip()):
                alt, path, full = IMAGE_RE.match(line.strip()).groups()
                slide["images"].append((alt, path, bool(full)))
            elif re.match(r"^(\s*)- ", line):
                indent = len(re.match(r"^(\s*)", line).group(1))
                slide["bullets"].append((1 if indent >= 2 else 0, line.strip()[2:].strip()))
            elif line.strip() and not line.startswith("<!--"):
                slide["bullets"].append((0, line.strip()))
        slides.append(slide)
    return slides


def style_run(run, size, color=TEXT, bold=False):
    run.font.name = FONT
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.color.rgb = color


def add_text(slide, left, top, width, height, text, size, color=TEXT, bold=False, align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.TOP):
    box = slide.shapes.add_textbox(left, top, width, height)
    frame = box.text_frame
    frame.word_wrap = True
    frame.vertical_anchor = anchor
    frame.margin_left = frame.margin_right = 0
    paragraph = frame.paragraphs[0]
    paragraph.alignment = align
    style_run(paragraph.add_run(), size, color, bold)
    paragraph.runs[0].text = text
    return box


def add_bullets(slide, left, top, width, height, bullets, size):
    box = slide.shapes.add_textbox(left, top, width, height)
    frame = box.text_frame
    frame.word_wrap = True
    frame.margin_left = frame.margin_right = 0
    for index, (level, text) in enumerate(bullets):
        paragraph = frame.paragraphs[0] if index == 0 else frame.add_paragraph()
        paragraph.space_after = Pt(10 if level == 0 else 4)
        properties = paragraph._p.get_or_add_pPr()
        step = Inches(0.4)
        properties.set("marL", str(int(step * (level + 1))))
        properties.set("indent", str(-int(step)))
        for old in properties.findall(qn("a:buNone")) + properties.findall(qn("a:buChar")):
            properties.remove(old)
        bullet = OxmlElement("a:buChar")
        bullet.set("char", "\u2022" if level == 0 else "\u2013")
        properties.append(bullet)
        run = paragraph.add_run()
        run.text = text
        style_run(run, size if level == 0 else size - 3, TEXT if level == 0 else MUTED)
    return box


def place_image(slide, path, left, top, max_w, max_h):
    with Image.open(path) as image:
        width, height = image.size
    scale = min(max_w / width, max_h / height)
    w, h = int(width * scale), int(height * scale)
    slide.shapes.add_picture(str(path), left + (max_w - w) // 2, top + (max_h - h) // 2, w, h)


def placeholder(slide, left, top, width, height, label):
    shape = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
    shape.fill.solid()
    shape.fill.fore_color.rgb = PANEL
    shape.line.color.rgb = ACCENT
    shape.line.dash_style = 4
    shape.line.width = Pt(1.5)
    frame = shape.text_frame
    frame.word_wrap = True
    frame.vertical_anchor = MSO_ANCHOR.MIDDLE
    paragraph = frame.paragraphs[0]
    paragraph.alignment = PP_ALIGN.CENTER
    run = paragraph.add_run()
    run.text = "Cần ảnh: " + label
    style_run(run, 16, MUTED)


def build():
    sys.stdout.reconfigure(encoding="utf-8")
    slides = parse(SOURCE.read_text(encoding="utf-8"))
    deck = Presentation()
    deck.slide_width, deck.slide_height = SLIDE_W, SLIDE_H
    blank = deck.slide_layouts[6]
    missing = []
    for number, data in enumerate(slides, start=1):
        slide = deck.slides.add_slide(blank)
        slide.background.fill.solid()
        slide.background.fill.fore_color.rgb = BACKGROUND
        is_title = number == 1
        if is_title:
            bar = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, MARGIN, Inches(2.2), Inches(0.12), Inches(1.9))
            bar.fill.solid()
            bar.fill.fore_color.rgb = ACCENT
            bar.line.fill.background()
            add_text(slide, MARGIN + Inches(0.4), Inches(2.1), Inches(11.5), Inches(1.2), data["title"], 60, bold=True)
            body = [text for _, text in data["bullets"]]
            if body:
                add_text(slide, MARGIN + Inches(0.4), Inches(3.4), Inches(11.5), Inches(1.0), body[0], 24, MUTED)
            if len(body) > 1:
                add_text(slide, MARGIN + Inches(0.4), Inches(4.7), Inches(11.5), Inches(1.6), "\n".join(body[1:]), 18, MUTED)
        else:
            add_text(slide, MARGIN, Inches(0.4), Inches(12.1), Inches(0.9), data["title"], 32, bold=True, anchor=MSO_ANCHOR.MIDDLE)
            line = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, MARGIN, Inches(1.3), Inches(1.2), Inches(0.06))
            line.fill.solid()
            line.fill.fore_color.rgb = ACCENT
            line.line.fill.background()
            top, area_h = Inches(1.65), Inches(5.0)
            images = data["images"]
            has_text = bool(data["bullets"])
            full = bool(images) and (images[0][2] or not has_text)
            if has_text and not full:
                text_w = Inches(5.5) if images else Inches(12.1)
                size = 22 if images else 26
                add_bullets(slide, MARGIN, top, text_w, area_h, data["bullets"], size)
            elif has_text and full:
                add_bullets(slide, MARGIN, top, Inches(12.1), Inches(1.2), data["bullets"], 20)
                top, area_h = Inches(2.9), Inches(3.8)
            if images:
                if full:
                    left, width = MARGIN, Inches(12.1)
                else:
                    left, width = MARGIN + Inches(5.9), Inches(6.2)
                slots = len(images)
                slot_h = area_h // slots
                for index, (alt, path, _) in enumerate(images):
                    file = ROOT / path
                    slot_top = top + slot_h * index
                    if file.exists():
                        place_image(slide, file, left, slot_top, width, slot_h - Inches(0.1))
                    else:
                        placeholder(slide, left, slot_top, width, slot_h - Inches(0.1), alt)
                        missing.append(f"slide {number}: {path} ({alt})")
        add_text(slide, MARGIN, Inches(7.05), Inches(4), Inches(0.3), "3 GIỜ 12", 11, MUTED)
        add_text(slide, SLIDE_W - MARGIN - Inches(1), Inches(7.05), Inches(1), Inches(0.3), str(number), 11, MUTED, align=PP_ALIGN.RIGHT)
        if data["notes"]:
            slide.notes_slide.notes_text_frame.text = "\n".join(data["notes"])
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    deck.save(str(OUTPUT))
    print(f"Đã ghi {OUTPUT} ({len(slides)} slide)")
    for item in missing:
        print(f"Thiếu ảnh: {item}")
    return 0


if __name__ == "__main__":
    sys.exit(build())
