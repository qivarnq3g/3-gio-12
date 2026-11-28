"""Dựng slide thuyết trình (.pptx) từ docs/slide/slides.md.

Cú pháp slides.md: các slide cách nhau bằng dòng `---`. Trong mỗi slide:
  # Tiêu đề slide
  ~ NHÃN NHỎ PHÍA TRÊN TIÊU ĐỀ              (tùy chọn)
  - ý chính                                  (thụt hai dấu cách là ý con)
  ![mô tả](đường/dẫn.png)                   ảnh bên phải chữ
  ![mô tả](đường/dẫn.png){full}             ảnh chiếm cả chiều rộng
  ![mô tả](đường/dẫn.png){pair}             các ảnh liên tiếp đặt cạnh nhau, mô tả làm chú thích
  > lời dẫn cho người trình bày (ghi chú, không hiện trên màn hình)
  Khối có cấu trúc (mỗi mục một dòng `- a | b | c`):
  ::: cards      thẻ: tiêu đề | mô tả | ký hiệu ngắn
  ::: stats      ô số liệu: số | nhãn | ghi chú
  ::: flow       chuỗi bước nối nhau: bước | ghi chú. Đặt `!` trước bước để tô đỏ (xấu), `+` để tô xanh (tốt)
  :::            (dòng đóng khối)
  `::: flow Tiêu đề hàng` đặt nhãn cho hàng.
Slide đầu (chỉ có `# Tiêu đề` và các `-`) là slide bìa; slide có `@bg ảnh` là slide kết có ảnh nền.
Mọi phần tử xuất hiện lần lượt (hiệu ứng mờ dần) khi vào slide; giữa các slide có chuyển cảnh mờ.
Đường dẫn ảnh tính từ thư mục gốc repo. Ảnh chưa có thì vẽ khung "Cần ảnh: ..." và liệt kê.

Chạy: python tools/build_slides.py   (ghi docs/slide/build/3-gio-12.pptx)
"""
import re
import sys
from pathlib import Path

from lxml import etree
from PIL import Image
from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.oxml.ns import qn
from pptx.oxml.xmlchemy import OxmlElement
from pptx.util import Inches, Pt

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "docs" / "slide" / "slides.md"
OUTPUT = ROOT / "docs" / "slide" / "build" / "3-gio-12.pptx"

BACKGROUND = RGBColor(0x0F, 0x13, 0x1A)
PANEL = RGBColor(0x1B, 0x21, 0x2C)
PANEL_LINE = RGBColor(0x2C, 0x35, 0x45)
TEXT = RGBColor(0xF4, 0xF4, 0xF4)
MUTED = RGBColor(0xA9, 0xB2, 0xC0)
ACCENT = RGBColor(0xE0, 0x4B, 0x4B)
GOOD = RGBColor(0x4C, 0xB5, 0x84)
FONT = "Segoe UI"
SLIDE_W, SLIDE_H = Inches(13.333), Inches(7.5)
MARGIN = Inches(0.7)
BODY_W = SLIDE_W - 2 * MARGIN
IMAGE_RE = re.compile(r"^!\[([^\]]*)\]\(([^)]+)\)(\{(full|pair)\})?\s*$")
P_NS = "http://schemas.openxmlformats.org/presentationml/2006/main"


# ---------- đọc nguồn ----------
def parse(text):
    slides = []
    for block in re.split(r"(?m)^---\s*$", text):
        lines = [l.rstrip() for l in block.strip("\n").splitlines()]
        if not any(l.strip() for l in lines):
            continue
        slide = {"title": "", "kicker": "", "bullets": [], "images": [], "blocks": [], "notes": [], "bg": ""}
        current = None
        for line in lines:
            stripped = line.strip()
            if stripped.startswith(":::"):
                rest = stripped[3:].strip()
                if rest and current is None:
                    kind, _, title = rest.partition(" ")
                    current = {"kind": kind, "title": title.strip(), "items": []}
                    slide["blocks"].append(current)
                else:
                    current = None
            elif current is not None:
                if stripped.startswith("- "):
                    current["items"].append([p.strip() for p in stripped[2:].split("|")])
            elif line.startswith("# "):
                slide["title"] = line[2:].strip()
            elif line.startswith("~ "):
                slide["kicker"] = line[2:].strip()
            elif line.startswith("@bg "):
                slide["bg"] = line[4:].strip()
            elif line.startswith(">"):
                slide["notes"].append(line[1:].strip())
            elif IMAGE_RE.match(stripped):
                alt, path, _, mode = IMAGE_RE.match(stripped).groups()
                slide["images"].append((alt, path, mode or "right"))
            elif re.match(r"^(\s*)- ", line):
                indent = len(re.match(r"^(\s*)", line).group(1))
                slide["bullets"].append((1 if indent >= 2 else 0, stripped[2:].strip()))
            elif stripped and not stripped.startswith("<!--"):
                slide["bullets"].append((0, stripped))
        slides.append(slide)
    return slides


# ---------- tiện ích vẽ ----------
def style_run(run, size, color=TEXT, bold=False):
    run.font.name = FONT
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.color.rgb = color


def textbox(slide, left, top, width, height, text, size, color=TEXT, bold=False, align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.TOP):
    box = slide.shapes.add_textbox(left, top, width, height)
    frame = box.text_frame
    frame.word_wrap = True
    frame.vertical_anchor = anchor
    frame.margin_left = frame.margin_right = frame.margin_top = frame.margin_bottom = 0
    paragraph = frame.paragraphs[0]
    paragraph.alignment = align
    run = paragraph.add_run()
    run.text = text
    style_run(run, size, color, bold)
    return box


def rectangle(slide, left, top, width, height, fill, shape=MSO_SHAPE.RECTANGLE, line=None, radius=None):
    shape = slide.shapes.add_shape(shape, left, top, width, height)
    shape.fill.solid()
    shape.fill.fore_color.rgb = fill
    if line is None:
        shape.line.fill.background()
    else:
        shape.line.color.rgb = line
        shape.line.width = Pt(1)
    shape.shadow.inherit = False
    if radius is not None and shape.adjustments:
        shape.adjustments[0] = radius
    return shape


def set_alpha(shape, percent):
    solid = shape.fill._xPr.find(qn("a:solidFill"))
    color = solid[0]
    for old in color.findall(qn("a:alpha")):
        color.remove(old)
    alpha = OxmlElement("a:alpha")
    alpha.set("val", str(int(percent * 1000)))
    color.append(alpha)


def bullets_box(slide, left, top, width, height, bullets, size):
    box = slide.shapes.add_textbox(left, top, width, height)
    frame = box.text_frame
    frame.word_wrap = True
    frame.margin_left = frame.margin_right = frame.margin_top = frame.margin_bottom = 0
    for index, (level, text) in enumerate(bullets):
        paragraph = frame.paragraphs[0] if index == 0 else frame.add_paragraph()
        paragraph.space_after = Pt(12 if level == 0 else 4)
        properties = paragraph._p.get_or_add_pPr()
        step = Inches(0.4)
        properties.set("marL", str(int(step * (level + 1))))
        properties.set("indent", str(-int(step)))
        colour = OxmlElement("a:buClr")
        srgb = OxmlElement("a:srgbClr")
        srgb.set("val", "E04B4B")
        colour.append(srgb)
        properties.append(colour)
        bullet = OxmlElement("a:buChar")
        bullet.set("char", "\u25A0" if level == 0 else "\u2013")
        properties.append(bullet)
        run = paragraph.add_run()
        run.text = text
        style_run(run, size if level == 0 else size - 3, TEXT if level == 0 else MUTED)
    return box


def fit_picture(slide, path, left, top, max_w, max_h, border=True):
    with Image.open(path) as image:
        width, height = image.size
    scale = min(max_w / width, max_h / height)
    w, h = int(width * scale), int(height * scale)
    pic = slide.shapes.add_picture(str(path), left + (max_w - w) // 2, top + (max_h - h) // 2, w, h)
    if border:
        pic.line.color.rgb = PANEL_LINE
        pic.line.width = Pt(1.25)
    return pic


def missing_box(slide, left, top, width, height, label):
    shape = rectangle(slide, left, top, width, height, PANEL, MSO_SHAPE.ROUNDED_RECTANGLE, ACCENT, 0.05)
    shape.line.dash_style = 4
    frame = shape.text_frame
    frame.word_wrap = True
    frame.vertical_anchor = MSO_ANCHOR.MIDDLE
    paragraph = frame.paragraphs[0]
    paragraph.alignment = PP_ALIGN.CENTER
    run = paragraph.add_run()
    run.text = "Cần ảnh: " + label
    style_run(run, 16, MUTED)
    return shape


def crop_to(path, ratio, out, focus=0.4):
    """Cắt ảnh về tỉ lệ rộng/cao = ratio, giữ vùng quanh `focus` theo chiều cắt."""
    with Image.open(path) as image:
        image = image.convert("RGB")
        w, h = image.size
        if w / h > ratio:
            new_w = int(h * ratio)
            left = int((w - new_w) * focus)
            image = image.crop((left, 0, left + new_w, h))
        else:
            new_h = int(w / ratio)
            top = int((h - new_h) * focus)
            image = image.crop((0, top, w, top + new_h))
        out.parent.mkdir(parents=True, exist_ok=True)
        image.save(out, quality=88)
    return out


# ---------- khối có cấu trúc ----------
def cards_block(slide, block, top, height, groups):
    items = block["items"]
    gap = Inches(0.3)
    count = len(items)
    width = (BODY_W - gap * (count - 1)) // count
    for index, item in enumerate(items):
        title = item[0]
        desc = item[1] if len(item) > 1 else ""
        mark = item[2] if len(item) > 2 else str(index + 1)
        left = MARGIN + (width + gap) * index
        card = rectangle(slide, left, top, width, height, PANEL, MSO_SHAPE.ROUNDED_RECTANGLE, PANEL_LINE, 0.04)
        badge = rectangle(slide, left + Inches(0.3), top + Inches(0.3), Inches(0.62), Inches(0.62), ACCENT, MSO_SHAPE.OVAL)
        frame = badge.text_frame
        frame.margin_left = frame.margin_right = 0
        frame.vertical_anchor = MSO_ANCHOR.MIDDLE
        paragraph = frame.paragraphs[0]
        paragraph.alignment = PP_ALIGN.CENTER
        run = paragraph.add_run()
        run.text = mark
        style_run(run, 18, TEXT, True)
        head = textbox(slide, left + Inches(0.3), top + Inches(1.15), width - Inches(0.6), Inches(0.9), title, 22, TEXT, True)
        body = textbox(slide, left + Inches(0.3), top + Inches(2.25), width - Inches(0.6), height - Inches(2.4), desc, 18, MUTED)
        groups.append([card, badge, head, body])


def stats_block(slide, block, top, height, groups):
    items = block["items"]
    gap = Inches(0.3)
    count = len(items)
    width = (BODY_W - gap * (count - 1)) // count
    for index, item in enumerate(items):
        number, label = item[0], item[1] if len(item) > 1 else ""
        note = item[2] if len(item) > 2 else ""
        left = MARGIN + (width + gap) * index
        tile = rectangle(slide, left, top, width, height, PANEL, MSO_SHAPE.ROUNDED_RECTANGLE, PANEL_LINE, 0.05)
        bar = rectangle(slide, left, top + Inches(0.35), Inches(0.09), Inches(0.9), ACCENT)
        big = textbox(slide, left + Inches(0.35), top + Inches(0.25), width - Inches(0.5), Inches(1.2), number, 54, ACCENT, True, anchor=MSO_ANCHOR.MIDDLE)
        name = textbox(slide, left + Inches(0.35), top + Inches(1.5), width - Inches(0.6), Inches(0.85), label, 19, TEXT, True)
        extra = textbox(slide, left + Inches(0.35), top + Inches(2.4), width - Inches(0.6), height - Inches(2.5), note, 14, MUTED)
        groups.append([tile, bar, big, name, extra])


def flow_block(slide, block, top, height, groups):
    items = block["items"]
    if block["title"]:
        label = textbox(slide, MARGIN, top, BODY_W, Inches(0.4), block["title"].upper(), 14, MUTED, True)
        groups.append([label])
        top += Inches(0.5)
        height -= Inches(0.5)
    count = len(items)
    gap = Inches(0.12)
    width = (BODY_W - gap * (count - 1)) // count
    for index, item in enumerate(items):
        text = item[0]
        tone = ACCENT if text.startswith("!") else GOOD if text.startswith("+") else None
        text = text.lstrip("!+ ")
        left = MARGIN + (width + gap) * index
        shape = rectangle(slide, left, top, width, height, PANEL, MSO_SHAPE.PENTAGON if index < count - 1 else MSO_SHAPE.ROUNDED_RECTANGLE, tone or PANEL_LINE)
        if tone:
            shape.line.width = Pt(2.25)
        frame = shape.text_frame
        frame.word_wrap = True
        frame.vertical_anchor = MSO_ANCHOR.MIDDLE
        frame.margin_left = Inches(0.2)
        frame.margin_right = Inches(0.35)
        paragraph = frame.paragraphs[0]
        paragraph.alignment = PP_ALIGN.LEFT
        run = paragraph.add_run()
        run.text = text
        style_run(run, 17, TEXT, True)
        if len(item) > 1:
            sub = frame.add_paragraph()
            sub.space_before = Pt(4)
            sub_run = sub.add_run()
            sub_run.text = item[1]
            style_run(sub_run, 13, MUTED)
        groups.append([shape])


# ---------- chuyển cảnh và hiệu ứng ----------
def add_transition(slide, kind="fade"):
    element = slide._element
    for old in element.findall(qn("p:transition")):
        element.remove(old)
    xml = f'<p:transition xmlns:p="{P_NS}" spd="med"><p:{kind}/></p:transition>'
    node = etree.fromstring(xml)
    clr = element.find(qn("p:clrMapOvr"))
    (clr if clr is not None else element.find(qn("p:cSld"))).addnext(node)


def add_animation(slide, groups, step_ms=350, duration_ms=500):
    """Mỗi nhóm hình mờ dần vào, nhóm sau nối tiếp nhóm trước; tự chạy khi slide hiện."""
    groups = [g for g in groups if g]
    if not groups:
        return
    counter = [2]

    def next_id():
        counter[0] += 1
        return counter[0]

    def effect(shape_id, node_type, delay):
        a, b, c = next_id(), next_id(), next_id()
        return (
            f'<p:par><p:cTn id="{a}" presetID="10" presetClass="entr" presetSubtype="0" fill="hold" nodeType="{node_type}">'
            f'<p:stCondLst><p:cond delay="{delay}"/></p:stCondLst><p:childTnLst>'
            f'<p:set><p:cBhvr><p:cTn id="{b}" dur="1" fill="hold"><p:stCondLst><p:cond delay="0"/></p:stCondLst></p:cTn>'
            f'<p:tgtEl><p:spTgt spid="{shape_id}"/></p:tgtEl><p:attrNameLst><p:attrName>style.visibility</p:attrName></p:attrNameLst></p:cBhvr>'
            f'<p:to><p:strVal val="visible"/></p:to></p:set>'
            f'<p:animEffect transition="in" filter="fade"><p:cBhvr><p:cTn id="{c}" dur="{duration_ms}"/>'
            f'<p:tgtEl><p:spTgt spid="{shape_id}"/></p:tgtEl></p:cBhvr></p:animEffect>'
            f'</p:childTnLst></p:cTn></p:par>'
        )

    steps = []
    for index, group in enumerate(groups):
        step_id = next_id()
        inner = ""
        for position, shape in enumerate(group):
            node_type = "afterEffect" if position == 0 else "withEffect"
            inner += effect(shape.shape_id, node_type, 0)
        steps.append(
            f'<p:par><p:cTn id="{step_id}" fill="hold"><p:stCondLst><p:cond delay="{index * step_ms}"/></p:stCondLst>'
            f'<p:childTnLst>{inner}</p:childTnLst></p:cTn></p:par>'
        )
    outer_id = next_id()
    xml = (
        f'<p:timing xmlns:p="{P_NS}"><p:tnLst><p:par><p:cTn id="1" dur="indefinite" restart="never" nodeType="tmRoot"><p:childTnLst>'
        f'<p:seq concurrent="1" nextAc="seek"><p:cTn id="2" dur="indefinite" nodeType="mainSeq"><p:childTnLst>'
        f'<p:par><p:cTn id="{outer_id}" fill="hold"><p:stCondLst><p:cond delay="indefinite"/><p:cond evt="onBegin" delay="0"><p:tn val="2"/></p:cond></p:stCondLst>'
        f'<p:childTnLst>{"".join(steps)}</p:childTnLst></p:cTn></p:par>'
        f'</p:childTnLst></p:cTn><p:prevCondLst><p:cond evt="onPrev" delay="0"><p:tgtEl><p:sldTgt/></p:tgtEl></p:cond></p:prevCondLst>'
        f'<p:nextCondLst><p:cond evt="onNext" delay="0"><p:tgtEl><p:sldTgt/></p:tgtEl></p:cond></p:nextCondLst></p:seq>'
        f'</p:childTnLst></p:cTn></p:par></p:tnLst></p:timing>'
    )
    element = slide._element
    for old in element.findall(qn("p:timing")):
        element.remove(old)
    element.append(etree.fromstring(xml))


# ---------- dựng slide ----------
def background(slide):
    slide.background.fill.solid()
    slide.background.fill.fore_color.rgb = BACKGROUND


def footer(slide, number):
    textbox(slide, MARGIN, Inches(7.05), Inches(4), Inches(0.3), "3 GIỜ 12", 11, MUTED)
    textbox(slide, SLIDE_W - MARGIN - Inches(1), Inches(7.05), Inches(1), Inches(0.3), str(number), 11, MUTED, align=PP_ALIGN.RIGHT)


def title_slide(slide, data, work):
    background(slide)
    picture = ROOT / "docs" / "slide" / "anh" / "hem-3am.png"
    groups = []
    right_w = Inches(5.2)
    if picture.exists():
        cropped = crop_to(picture, right_w / SLIDE_H, work / "bia.jpg", 0.0)
        slide.shapes.add_picture(str(cropped), SLIDE_W - right_w, 0, right_w, SLIDE_H)
        veil = rectangle(slide, SLIDE_W - right_w, 0, right_w, SLIDE_H, BACKGROUND)
        set_alpha(veil, 18)
    clock = textbox(slide, MARGIN, Inches(0.7), Inches(4), Inches(0.5), "03:12", 22, ACCENT, True)
    bar = rectangle(slide, MARGIN, Inches(2.15), Inches(0.12), Inches(2.0), ACCENT)
    title = textbox(slide, MARGIN + Inches(0.4), Inches(2.0), Inches(7), Inches(1.4), data["title"], 72, TEXT, True)
    body = [text for _, text in data["bullets"]]
    sub = textbox(slide, MARGIN + Inches(0.4), Inches(3.35), Inches(6.6), Inches(1.0), body[0] if body else "", 22, MUTED)
    info = textbox(slide, MARGIN + Inches(0.4), Inches(5.0), Inches(6.6), Inches(1.6), "\n".join(body[1:]), 16, MUTED)
    groups = [[clock], [bar, title], [sub], [info]]
    return groups


def closing_slide(slide, data, work):
    background(slide)
    picture = ROOT / data["bg"]
    if picture.exists():
        cropped = crop_to(picture, SLIDE_W / SLIDE_H, work / "ket.jpg", 0.55)
        slide.shapes.add_picture(str(cropped), 0, 0, SLIDE_W, SLIDE_H)
        veil = rectangle(slide, 0, 0, SLIDE_W, SLIDE_H, BACKGROUND)
        set_alpha(veil, 72)
    bar = rectangle(slide, MARGIN, Inches(2.2), Inches(0.12), Inches(1.5), ACCENT)
    title = textbox(slide, MARGIN + Inches(0.4), Inches(2.1), Inches(11), Inches(1.0), data["title"], 54, TEXT, True)
    lines = [text for _, text in data["bullets"]]
    body = textbox(slide, MARGIN + Inches(0.4), Inches(3.3), Inches(11), Inches(2.5), "\n".join(lines), 24, MUTED)
    return [[bar, title], [body]]


def content_slide(slide, number, data, missing):
    background(slide)
    groups = []
    if data["kicker"]:
        groups.append([textbox(slide, MARGIN, Inches(0.42), Inches(8), Inches(0.3), data["kicker"].upper(), 13, ACCENT, True)])
    title = textbox(slide, MARGIN, Inches(0.72), BODY_W, Inches(0.8), data["title"], 34, TEXT, True, anchor=MSO_ANCHOR.MIDDLE)
    groups.append([title])
    top = Inches(1.85)
    bottom = Inches(6.85)
    images = data["images"]
    has_text = bool(data["bullets"])
    blocks = data["blocks"]
    side_images = [i for i in images if i[2] == "right"]
    full_images = [i for i in images if i[2] == "full"]
    pair_images = [i for i in images if i[2] == "pair"]

    left_w = BODY_W
    if side_images:
        left_w = Inches(5.4)
    if has_text:
        count = len(data["bullets"])
        size = 22 if side_images or blocks else 26
        height = Inches(min(4.9, 0.62 * count + 0.2)) if (blocks or full_images or pair_images) else bottom - top
        groups.append([bullets_box(slide, MARGIN, top, left_w, height, data["bullets"], size)])
        if blocks or full_images or pair_images:
            top += height + Inches(0.1)
    for block in blocks:
        height = bottom - top
        if block["kind"] == "cards":
            cards_block(slide, block, top, min(height, Inches(4.2)), groups)
        elif block["kind"] == "stats":
            stats_block(slide, block, top, min(height, Inches(3.7)), groups)
        elif block["kind"] == "flow":
            row_h = Inches(1.45)
            flow_block(slide, block, top, row_h, groups)
            top += row_h + Inches(0.75 if block["title"] else 0.25)
    pictures = []
    if side_images:
        left = MARGIN + Inches(5.8)
        width = SLIDE_W - MARGIN - left
        slot = (bottom - top) // len(side_images)
        for index, (alt, path, _) in enumerate(side_images):
            file = ROOT / path
            slot_top = top + slot * index
            if file.exists():
                pictures.append(fit_picture(slide, file, left, slot_top, width, slot - Inches(0.1)))
            else:
                pictures.append(missing_box(slide, left, slot_top, width, slot - Inches(0.1), alt))
                missing.append(f"slide {number}: {path} ({alt})")
    for alt, path, _ in full_images:
        file = ROOT / path
        if file.exists():
            pictures.append(fit_picture(slide, file, MARGIN, top, BODY_W, bottom - top))
        else:
            pictures.append(missing_box(slide, MARGIN, top, BODY_W, bottom - top, alt))
            missing.append(f"slide {number}: {path} ({alt})")
    if pair_images:
        gap = Inches(0.35)
        width = (BODY_W - gap * (len(pair_images) - 1)) // len(pair_images)
        for index, (alt, path, _) in enumerate(pair_images):
            left = MARGIN + (width + gap) * index
            file = ROOT / path
            caption = textbox(slide, left, bottom - Inches(0.35), width, Inches(0.35), alt, 14, MUTED, align=PP_ALIGN.CENTER)
            if file.exists():
                pictures.append(fit_picture(slide, file, left, top, width, bottom - top - Inches(0.5)))
            else:
                pictures.append(missing_box(slide, left, top, width, bottom - top - Inches(0.5), alt))
                missing.append(f"slide {number}: {path} ({alt})")
            pictures.append(caption)
    for picture in pictures:
        groups.append([picture])
    footer(slide, number)
    return groups


def build():
    sys.stdout.reconfigure(encoding="utf-8")
    slides = parse(SOURCE.read_text(encoding="utf-8"))
    deck = Presentation()
    deck.slide_width, deck.slide_height = SLIDE_W, SLIDE_H
    blank = deck.slide_layouts[6]
    missing = []
    work = OUTPUT.parent / "tmp"
    for number, data in enumerate(slides, start=1):
        slide = deck.slides.add_slide(blank)
        if number == 1:
            groups = title_slide(slide, data, work)
        elif data["bg"]:
            groups = closing_slide(slide, data, work)
        else:
            groups = content_slide(slide, number, data, missing)
        if number > 1:
            add_transition(slide, "fade")
        add_animation(slide, groups)
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
