import argparse
import hashlib
import json
import os
import re
import shutil
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

ROOT = Path(__file__).resolve().parent.parent
DESIGN_DIR = ROOT / "docs" / "thiet-ke"
MASTER = "thuyet-minh.md"
MEMBERS = DESIGN_DIR / "thanh-vien.local.txt"
COVER_TEMPLATE = DESIGN_DIR / "mau-bia.docx"
COVER_BLOCK = re.compile(r'^::: \{custom-style="(Cover \w+)"\}\n(.*?)\n:::', re.M | re.S)
PARTS = [
    ("luoc-do-use-case.md", 9, "Lược đồ use case"),
    ("kien-truc-he-thong.md", 10, "Kiến trúc hệ thống"),
    ("luoc-do-lop-game.md", 11, "Lược đồ lớp phần game"),
    ("luoc-do-lop-may-chu.md", 12, "Lược đồ lớp phần máy chủ"),
    ("database-design.md", 12, "Thiết kế cơ sở dữ liệu"),
    ("admin-ui-design.md", 27, "Thiết kế trang quản trị"),
    ("conclusion.md", 13, "Kết luận"),
]
FONT = "Times New Roman"
BROWSERS = [
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    "/usr/bin/google-chrome",
    "/usr/bin/chromium",
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
]
SCALE = 2
PORTRAIT = (15.0, 21.0)
TEXT_WIDTH = 16.0
TABLE_RULE = re.compile(r"^\|(\s*:?-+:?\s*\|)+\s*$")
TABLE_BORDER = '<w:{0} w:val="single" w:sz="4" w:space="0" w:color="000000"/>'
MERMAID_CONFIG = {
    "fontFamily": "Arial",
    **{kind: {"useMaxWidth": False} for kind in ("flowchart", "sequence", "class", "er", "state")},
}
PAGE_MARGINS = '<w:pgMar w:top="1134" w:right="1134" w:bottom="1134" w:left="1701" w:header="567" w:footer="567" w:gutter="0"/>'
HEADING = re.compile(r"^(#{1,6})\s+(?:\d+(?:\.\d+)*\.?\s+)?(.*?)\s*$")
FENCE = re.compile(r"^```\s*mermaid\s*$")
IMAGE = re.compile(r"^!\[[^\]]*\]\((?!https?://)([^)\s]+\.png)\)\s*$")
TOC_XML = """```{=openxml}
<w:p><w:r><w:br w:type="page"/></w:r></w:p>
<w:p><w:pPr><w:pStyle w:val="TOCHeading"/></w:pPr><w:r><w:t>Mục lục</w:t></w:r></w:p>
<w:p><w:r><w:fldChar w:fldCharType="begin" w:dirty="true"/></w:r><w:r><w:instrText xml:space="preserve"> TOC \\o "1-3" \\h \\z \\u </w:instrText></w:r><w:r><w:fldChar w:fldCharType="separate"/></w:r><w:r><w:t>Nhấn F9 để cập nhật mục lục.</w:t></w:r><w:r><w:fldChar w:fldCharType="end"/></w:r></w:p>
<w:p><w:r><w:br w:type="page"/></w:r></w:p>
```"""
ABBREVIATIONS = DESIGN_DIR / "abbreviations.md"
BIBLIOGRAPHY = DESIGN_DIR / "references.json"
CSL = DESIGN_DIR / "ieee.csl"
REFERENCES_TITLE = "Tài liệu tham khảo"
CAPTION = re.compile(r"^: (.+)$")


def list_xml(title, style):
    field = f'<w:p><w:r><w:fldChar w:fldCharType="begin" w:dirty="true"/></w:r><w:r><w:instrText xml:space="preserve"> TOC \\h \\z \\t "{style},1" </w:instrText></w:r><w:r><w:fldChar w:fldCharType="separate"/></w:r><w:r><w:t>Nhấn F9 để cập nhật danh mục.</w:t></w:r><w:r><w:fldChar w:fldCharType="end"/></w:r></w:p>'
    return f'<w:p><w:pPr><w:pStyle w:val="TOCHeading"/></w:pPr><w:r><w:t>{title}</w:t></w:r></w:p>{field}<w:p><w:r><w:br w:type="page"/></w:r></w:p>'


def front_lists():
    lists = list_xml("Danh mục hình", "Image Caption") + list_xml("Danh mục bảng", "Table Caption")
    heading = '<w:p><w:pPr><w:pStyle w:val="TOCHeading"/></w:pPr><w:r><w:t>Danh mục chữ viết tắt</w:t></w:r></w:p>'
    table = ABBREVIATIONS.read_text(encoding="utf-8").strip()
    page_break = '```{=openxml}\n<w:p><w:r><w:br w:type="page"/></w:r></w:p>\n```'
    return f"```{{=openxml}}\n{lists}{heading}\n```\n\n{table}\n\n{page_break}"


def number_tables(text):
    out, chapter, count, in_code = [], 0, 0, False
    lines = text.splitlines()
    for index, line in enumerate(lines):
        if line.startswith("```"):
            in_code = not in_code
        heading = None if in_code else re.match(r"^# (\d+)\. ", line)
        if heading:
            chapter, count = int(heading.group(1)), 0
        caption = None if in_code else CAPTION.match(line)
        if caption:
            before = next((lines[j] for j in range(index - 1, -1, -1) if lines[j].strip()), "")
            if not before.startswith("|"):
                sys.exit(f"Chú thích bảng không đứng ngay sau bảng: {line}")
            count += 1
            line = f": Bảng {chapter}.{count}. {caption.group(1)}"
        out.append(line)
    return "\n".join(out)


def split_cover(text):
    lines = text.splitlines()
    for i, line in enumerate(lines):
        if line.startswith("# "):
            return "\n".join(lines[:i]), "\n".join(lines[i:])
    return "", text


def read_members():
    if not MEMBERS.exists():
        return [("....................", "....................")] * 4, False
    members = [tuple(part.strip() for part in line.split(" - ", 1)) for line in MEMBERS.read_text(encoding="utf-8").splitlines() if " - " in line]
    return members, bool(members)


def cover_fields(cover):
    values = dict((style, text.strip()) for style, text in COVER_BLOCK.findall(cover))
    course = re.match(r"Học phần (.+) - lớp (\d+)$", values.get("Cover Course", ""))
    date = re.match(r".*?(tháng \d+ năm \d+)$", values.get("Cover Date", ""))
    if not (course and date and values.get("Cover Title") and values.get("Cover Subtitle")):
        sys.exit(f"Bìa trong {MASTER} cần đủ Cover Course, Cover Title, Cover Subtitle, Cover Date")
    return [
        ("BÁO CÁO ĐỀ TÀI", values["Cover Title"].upper()),
        ("&lt;Tên đề tài&gt;", values["Cover Subtitle"]),
        ("Môn: tên môn", f"Môn: {course.group(1)}"),
        ("Mã lớp học phần: mã lớp học phần", f"Mã lớp học phần: {course.group(2)}"),
        ("tháng &lt;thang&gt; năm 2025", date.group(1)),
    ]


def drop_intro(text):
    lines = text.splitlines()
    out, skipping = [], False
    for line in lines:
        if line.startswith("# "):
            out.append(line)
            skipping = True
            continue
        if skipping and line.startswith("## "):
            skipping = False
        if not skipping:
            out.append(line)
    return "\n".join(out)


def number_headings(text, chapter_start):
    counters = [chapter_start - 1, 0, 0, 0, 0, 0]
    out, in_code = [], False
    for line in text.splitlines():
        if line.startswith("```"):
            in_code = not in_code
        match = None if in_code else HEADING.match(line)
        if not match:
            out.append(line)
            continue
        level = len(match.group(1))
        counters[level - 1] += 1
        for k in range(level, len(counters)):
            counters[k] = 0
        label = ".".join(str(c) for c in counters[:level])
        out.append(f"{match.group(1)} {label}. {match.group(2)}")
    return "\n".join(out), counters[0]


def rewrite_references(text, chapter, chapters):
    for name, number in chapters.items():
        text = re.sub(r"\[`?" + re.escape(name) + r"`?\]\(" + re.escape(name) + r"[^)]*\)", f"chương {number}", text)
        text = text.replace(f"`{name}`", f"chương {number}")
    for name, task, _ in PARTS:
        text = re.sub(rf"task #{task}\b", f"chương {chapters[name]}", text)
    text = re.sub(r"\s*\((?:đã có từ |đã làm ở )?task #\d+(?:, #\d+)*\)", "", text)
    text = re.sub(r"chốt ở task #\d+, ", "", text)
    text = re.sub(r", quyết ở task #\d+", "", text)
    text = re.sub(r"(chương \d+), \1\b", r"\1", text)
    text = re.sub(r"(chương \d+) \(\1\)", r"\1", text)
    text = re.sub(r"(?<!!)\[([^\]]+)\]\((?!https?://)[^)]+\)", r"\1", text)
    architecture = chapters["kien-truc-he-thong.md"]

    def section(match):
        if match.group(2):
            return f"mục {architecture}.{match.group(1)}"
        return f"mục {chapter}.{match.group(1)}"

    return re.sub(r"(?<!bản 5, )mục (\d+(?:\.\d+)*)( của tài liệu kiến trúc)?", section, text)


def png_size(path):
    with open(path, "rb") as f:
        head = f.read(24)
    return struct.unpack(">II", head[16:24])


def figure_size(pixel_width, pixel_height):
    width = pixel_width / SCALE * 2.54 / 96
    height = pixel_height / SCALE * 2.54 / 96
    return width * min(1, PORTRAIT[0] / width, PORTRAIT[1] / height)


def browser_config(work):
    path = os.environ.get("MERMAID_BROWSER") or next((p for p in BROWSERS if Path(p).exists()), None)
    if not path:
        return None
    config = work / "puppeteer.json"
    config.write_text(json.dumps({"executablePath": path, "headless": "shell"}), encoding="utf-8")
    return config


def mermaid_command():
    found = shutil.which("mmdc")
    if found:
        return [found]
    npx = shutil.which("npx")
    if not npx:
        sys.exit("Cần Mermaid CLI: npm install -g @mermaid-js/mermaid-cli")
    return [npx, "-y", "@mermaid-js/mermaid-cli"]


def figure_lines(image, chapter, figure, title):
    size = figure_size(*png_size(image))
    return [f"![Hình {chapter}.{figure}. {title}]({image.as_posix()}){{width={size:.1f}cm}}"]


def render_diagrams(text, chapter, work, cache):
    out, block, figure, title = [], None, 0, ""
    for line in text.splitlines():
        heading = HEADING.match(line)
        if block is None and heading:
            title = re.sub(r"^\d+(?:\.\d+)*\.\s*", "", line.lstrip("#").strip())
        if block is None and FENCE.match(line):
            block = []
            continue
        if block is not None and line.startswith("```"):
            source = "\n".join(block) + "\n"
            settings = json.dumps(MERMAID_CONFIG, sort_keys=True)
            digest = hashlib.sha256((settings + source).encode("utf-8")).hexdigest()[:16]
            image = cache / f"{digest}.png"
            if not image.exists():
                src = work / f"{digest}.mmd"
                src.write_text(source, encoding="utf-8")
                config_file = work / "mermaid.json"
                config_file.write_text(settings, encoding="utf-8")
                cmd = mermaid_command() + ["-i", str(src), "-o", str(image), "-s", str(SCALE), "-b", "white", "-c", str(config_file)]
                browser = browser_config(work)
                if browser:
                    cmd += ["-p", str(browser)]
                subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL)
            figure += 1
            out.extend(figure_lines(image, chapter, figure, title))
            block = None
            continue
        picture = IMAGE.match(line) if block is None else None
        if picture:
            image = DESIGN_DIR / picture.group(1)
            if not image.exists():
                sys.exit(f"Thiếu ảnh {image}")
            figure += 1
            out.extend(figure_lines(image, chapter, figure, title))
            continue
        if block is not None:
            block.append(line)
        else:
            out.append(line)
    return "\n".join(out)


def placeholder(title):
    return f"# {title}\n\n## Nội dung\n\nPhần này đang được soạn.\n"


def build_markdown(work, cache):
    cover, body = split_cover((DESIGN_DIR / MASTER).read_text(encoding="utf-8"))
    chapters, missing, next_chapter = {}, [], None
    fields = cover_fields(cover)
    body_numbered, last = number_headings(body, 1)
    next_chapter = last + 1
    for name, _, _ in PARTS:
        chapters[name] = next_chapter
        next_chapter += 1
    pieces = [TOC_XML, front_lists(), body_numbered]
    for name, task, title in PARTS:
        path = DESIGN_DIR / name
        if path.exists():
            text = drop_intro(path.read_text(encoding="utf-8"))
        else:
            text = placeholder(title)
            missing.append(f"{name} (task #{task})")
        chapter = chapters[name]
        text, _ = number_headings(text, chapter)
        text = rewrite_references(text, chapter, chapters)
        text = render_diagrams(text, chapter, work, cache)
        pieces.append(text)
    combined = number_tables("\n\n".join(pieces)) + "\n"
    leftovers = sorted(set(re.findall(r"task #\d+", combined)) - {f"task #{t}" for n, t, _ in PARTS if f"{n} (task #{t})" in missing})
    prose = re.sub(r"^```.*?^```", "", combined, flags=re.M | re.S)
    leftovers += sorted(set(re.findall(r"(?<![\w&/#])#\d+\b", prose)) - set(leftovers))
    return combined, missing, leftovers, fields


def set_font(style, size=None, bold=None, color=True):
    style.font.name = FONT
    rpr = style.element.get_or_add_rPr()
    fonts = rpr.find(qn("w:rFonts"))
    if fonts is None:
        fonts = OxmlElement("w:rFonts")
        rpr.append(fonts)
    for attr in ("w:ascii", "w:hAnsi", "w:cs", "w:eastAsia"):
        fonts.set(qn(attr), FONT)
    for attr in ("w:asciiTheme", "w:hAnsiTheme", "w:cstheme", "w:eastAsiaTheme"):
        fonts.attrib.pop(qn(attr), None)
    if size:
        style.font.size = Pt(size)
    if bold is not None:
        style.font.bold = bold
    if color:
        style.font.color.rgb = RGBColor(0, 0, 0)
    style.font.italic = False


def make_reference(path):
    subprocess.run(["pandoc", "-o", str(path), "--print-default-data-file", "reference.docx"], check=True)
    doc = Document(str(path))
    styles = doc.styles
    for name in ("Normal", "Body Text", "First Paragraph", "Compact", "Table", "Caption", "Image Caption", "Table Caption", "Bibliography"):
        if name in [s.name for s in styles]:
            set_font(styles[name], 13)
    if "Table Caption" in [s.name for s in styles]:
        fmt = styles["Table Caption"].paragraph_format
        fmt.space_before, fmt.space_after, fmt.keep_with_next = Pt(6), Pt(3), True
    if "Bibliography" in [s.name for s in styles]:
        fmt = styles["Bibliography"].paragraph_format
        fmt.alignment = WD_ALIGN_PARAGRAPH.LEFT
        fmt.left_indent, fmt.first_line_indent = Cm(1.2), Cm(-1.2)
        fmt.space_before, fmt.space_after, fmt.line_spacing = Pt(0), Pt(6), 1.0
        fmt.tab_stops.add_tab_stop(Cm(1.2))
    for name, size in (("Heading 1", 16), ("Heading 2", 14), ("Heading 3", 13), ("Heading 4", 13), ("TOC Heading", 16)):
        if name in [s.name for s in styles]:
            set_font(styles[name], size, bold=True)
    for name in ("Body Text", "First Paragraph"):
        fmt = styles[name].paragraph_format
        fmt.space_before, fmt.space_after, fmt.line_spacing = Pt(0), Pt(6), 1.3
    for name in ("Heading 1", "Heading 2", "Heading 3"):
        styles[name].paragraph_format.keep_with_next = True
    styles["Heading 1"].paragraph_format.page_break_before = True
    for name in ("Caption", "Image Caption"):
        if name in [s.name for s in styles]:
            styles[name].paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    for section in doc.sections:
        section.page_width, section.page_height = Cm(21), Cm(29.7)
        section.top_margin = section.bottom_margin = Pt(56.7)
        section.left_margin = Pt(85)
        section.right_margin = Pt(56.7)
    doc.save(str(path))


def add_borders(tbl_pr):
    for old in tbl_pr.findall(qn("w:tblBorders")):
        tbl_pr.remove(old)
    xml = f'<w:tblBorders xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">{"".join(TABLE_BORDER.format(side) for side in ("top", "left", "bottom", "right", "insideH", "insideV"))}</w:tblBorders>'
    borders = parse_xml(xml)
    after = [tbl_pr.find(qn(f"w:{name}")) for name in ("tblInd", "jc", "tblW", "tblStyle")]
    anchor = next((node for node in after if node is not None), None)
    if anchor is None:
        tbl_pr.insert(0, borders)
    else:
        anchor.addnext(borders)


def word_finish(docx, pdf):
    if os.name != "nt":
        sys.exit("--word chỉ chạy trên Windows có Microsoft Word")
    script = (
        "$ErrorActionPreference='Stop';$w=New-Object -ComObject Word.Application;$w.Visible=$false;$w.DisplayAlerts=0;"
        f"try{{$d=$w.Documents.Open('{docx}');foreach($t in $d.TablesOfContents){{$t.Update()}};"
        f"$d.Save();$d.ExportAsFixedFormat('{pdf}',17);Write-Output ('pages=' + $d.ComputeStatistics(2));$d.Close(0)}}finally{{$w.Quit()}}"
    )
    result = subprocess.run(["powershell", "-NoProfile", "-Command", script], check=True, capture_output=True, text=True)
    print(result.stdout.strip())


def space_after_tables(doc):
    for table in doc.element.body.iter(qn("w:tbl")):
        follower = table.getnext()
        if follower is None or follower.tag != qn("w:p"):
            continue
        ppr = follower.get_or_add_pPr()
        spacing = ppr.find(qn("w:spacing"))
        if spacing is None:
            spacing = OxmlElement("w:spacing")
            ppr.append(spacing)
        spacing.set(qn("w:before"), "160")


def table_ratios(text):
    ratios = []
    for line in text.split("\n"):
        if TABLE_RULE.match(line):
            ratios.append([len(cell.strip(" :")) for cell in line.strip().strip("|").split("|")])
    return ratios


def fix_columns(table, ratio):
    total = TEXT_WIDTH * 567
    widths = [round(total * part / sum(ratio)) for part in ratio]
    tbl = table._tbl
    tbl_pr = tbl.tblPr
    for name in ("w:tblW", "w:tblLayout"):
        for old in tbl_pr.findall(qn(name)):
            tbl_pr.remove(old)
    tbl_w = OxmlElement("w:tblW")
    tbl_w.set(qn("w:w"), str(total))
    tbl_w.set(qn("w:type"), "dxa")
    layout = OxmlElement("w:tblLayout")
    layout.set(qn("w:type"), "fixed")
    tbl_pr.append(tbl_w)
    tbl_pr.append(layout)
    for col, width in zip(tbl.tblGrid.findall(qn("w:gridCol")), widths):
        col.set(qn("w:w"), str(width))
    for row in tbl.findall(qn("w:tr")):
        for tc, width in zip(row.findall(qn("w:tc")), widths):
            tc_pr = tc.get_or_add_tcPr()
            for old in tc_pr.findall(qn("w:tcW")):
                tc_pr.remove(old)
            tc_w = OxmlElement("w:tcW")
            tc_w.set(qn("w:w"), str(width))
            tc_w.set(qn("w:type"), "dxa")
            tc_pr.insert(0, tc_w)


def finish_docx(path, widths, ratios, update_on_open=True):
    doc = Document(str(path))
    shapes = doc.inline_shapes
    if len(shapes) != len(widths):
        sys.exit(f"Số hình trong bản Word ({len(shapes)}) khác số hình đã dựng ({len(widths)})")
    for shape, width in zip(shapes, widths):
        target = Cm(width)
        shape.height = int(shape.height * target / shape.width)
        shape.width = target
    if len(doc.tables) != len(ratios):
        sys.exit(f"Số bảng trong bản Word ({len(doc.tables)}) khác số bảng Markdown ({len(ratios)})")
    for table, ratio in zip(doc.tables, ratios):
        if len(set(ratio)) > 1 and len(ratio) == len(table.columns):
            fix_columns(table, ratio)
    for table in doc.tables:
        rows = table.rows
        if not rows:
            continue
        trpr = rows[0]._tr.get_or_add_trPr()
        if trpr.find(qn("w:tblHeader")) is None:
            trpr.append(OxmlElement("w:tblHeader"))
        for row in rows:
            row_pr = row._tr.get_or_add_trPr()
            if row_pr.find(qn("w:cantSplit")) is None:
                row_pr.insert(0, OxmlElement("w:cantSplit"))
        for cell in table._tbl.iter(qn("w:shd")):
            cell.getparent().remove(cell)
        add_borders(table._tbl.tblPr)
        for row_index, row in enumerate(rows):
            for cell in row.cells:
                for paragraph in cell.paragraphs:
                    paragraph.paragraph_format.keep_with_next = row_index == 0
                    for run in paragraph.runs:
                        run.font.bold = row_index == 0
    first = doc.sections[0]
    footer = first.footer.paragraphs[0]
    footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
    for kind, text in (("begin", None), (None, " PAGE "), ("end", None)):
        run = OxmlElement("w:r")
        if kind:
            node = OxmlElement("w:fldChar")
            node.set(qn("w:fldCharType"), kind)
        else:
            node = OxmlElement("w:instrText")
            node.set(qn("xml:space"), "preserve")
            node.text = text
        run.append(node)
        footer._p.append(run)
    space_after_tables(doc)
    settings = doc.settings.element
    if update_on_open and settings.find(qn("w:updateFields")) is None:
        update = OxmlElement("w:updateFields")
        update.set(qn("w:val"), "true")
        settings.append(update)
    doc.save(str(path))


def table_breaks(text):
    def code(match):
        return "`" + re.sub(r"([./,])(?=\S)", "\\1\u200b", match.group(1)) + "`"
    return "\n".join(re.sub(r"`([^`\n]+)`", code, line) if line.startswith("|") else line for line in text.split("\n"))


def main():
    parser = argparse.ArgumentParser(description="Ghép các phần thiết kế thành bản thuyết minh .docx")
    parser.add_argument("-o", "--output", default=str(DESIGN_DIR / "build" / "thuyet-minh-thiet-ke.docx"))
    parser.add_argument("--markdown", help="Ghi thêm bản Markdown đã ghép ra đường dẫn này")
    parser.add_argument("--word", action="store_true", help="Dùng Microsoft Word (Windows) cập nhật mục lục, danh mục và xuất PDF")
    args = parser.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    cache = output.parent / "diagrams"
    cache.mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        work = Path(tmp)
        combined, missing, leftovers, fields = build_markdown(work, cache)
        source = work / "combined.md"
        source.write_text(table_breaks(combined), encoding="utf-8")
        if args.markdown:
            Path(args.markdown).write_text(combined, encoding="utf-8")
        reference = work / "reference.docx"
        make_reference(reference)
        citations = ["--citeproc", "--bibliography", str(BIBLIOGRAPHY), "--csl", str(CSL), "-M", f"reference-section-title={REFERENCES_TITLE}"]
        subprocess.run(["pandoc", str(source), "-f", "markdown-auto_identifiers", "-o", str(output), "--reference-doc", str(reference), "--resource-path", str(ROOT), "--columns", "10000", *citations], check=True)
    finish_docx(output, [float(w) for w in re.findall(r"\{width=([\d.]+)cm\}", combined)], table_ratios(combined), update_on_open=not args.word)
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    from design_doc_cover import apply_cover
    members, named = read_members()
    if not named:
        missing.append(f"MSSV và họ tên ở bìa ({MEMBERS.relative_to(ROOT).as_posix()}, mỗi dòng: MSSV - Họ và tên)")
    apply_cover(output, COVER_TEMPLATE, members, fields)
    if args.word:
        word_finish(output, output.with_suffix(".pdf"))
    print(f"Đã ghi {output}")
    for item in missing:
        print(f"Thiếu phần: {item}")
    for item in leftovers:
        print(f"Còn nhắc tới {item} trong nội dung, cần sửa ở tệp nguồn")
    return 1 if leftovers else 0


if __name__ == "__main__":
    sys.exit(main())
