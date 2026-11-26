"""Dựng báo cáo cuối kỳ (.docx) theo hướng dẫn trình bày BM-BTL-07 của trường (docs/report/formatting-guide.pdf).

Dùng lại các hàm của build_design_doc.py (bản thuyết minh thiết kế đã nộp, không đổi) và thay phần định dạng:
lề 3,5 / 3 / 3,5 / 2 cm, Times New Roman 13, giãn dòng 1,5, tên chương in hoa, đánh số trang La Mã từ Mục lục
đến hết Danh mục, số thường từ Lời mở đầu, tài liệu tham khảo đứng trước phụ lục.

Cách dùng và cấu trúc tệp: docs/report/README.md.
"""
import argparse
import copy
import re
import subprocess
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_design_doc as bdd  # noqa: E402
from design_doc_cover import apply_cover  # noqa: E402
from docx import Document  # noqa: E402
from docx.enum.text import WD_ALIGN_PARAGRAPH  # noqa: E402
from docx.oxml import OxmlElement, parse_xml  # noqa: E402
from docx.oxml.ns import qn  # noqa: E402
from docx.shared import Cm, Pt  # noqa: E402

ROOT = bdd.ROOT
REPORT_DIR = ROOT / "docs" / "report"
DESIGN_DIR = bdd.DESIGN_DIR
MASTER = "bao-cao.md"
REPORT_CSL = REPORT_DIR / "ieee-sap-xep.csl"
FONT_SIZE = 13
LINE_SPACING = 1.5
MARGINS_CM = {"top": 3.5, "bottom": 3.0, "left": 3.5, "right": 2.0}
TEXT_WIDTH_CM = 21.0 - MARGINS_CM["left"] - MARGINS_CM["right"]
PORTRAIT_CM = (TEXT_WIDTH_CM, 29.7 - MARGINS_CM["top"] - MARGINS_CM["bottom"] - 1.5)

# Chương 2 ghép từ các phần của thuyết minh thiết kế (docs/thiet-ke/).
DESIGN_PARTS = [
    ("luoc-do-use-case.md", "Lược đồ use case"),
    ("kien-truc-he-thong.md", "Kiến trúc hệ thống"),
    ("luoc-do-lop-game.md", "Lược đồ lớp phần game"),
    ("luoc-do-lop-may-chu.md", "Lược đồ lớp phần máy chủ"),
    ("database-design.md", "Thiết kế cơ sở dữ liệu"),
    ("admin-ui-design.md", "Thiết kế trang quản trị"),
]
# (tệp trong docs/report/ hoặc "@thiet-ke", tên chương)
CHAPTERS = [
    ("chuong-1-tong-quan.md", "Tổng quan đề tài"),
    ("@thiet-ke", "Phân tích và thiết kế"),
    ("chuong-3-hien-thuc-kiem-thu.md", "Hiện thực và kiểm thử"),
    ("ke-hoach-khoi-nghiep.md", "Kế hoạch khởi nghiệp"),
]
NOTE_START = "Ghi chú cho nhóm"
FENCE = re.compile(r"^```")
HEADING = bdd.HEADING


def read_text(path):
    return path.read_text(encoding="utf-8")


def placeholder(title):
    return f"# {title}\n\nPhần này đang được soạn.\n"


def split_fences(text):
    """Trả về (dòng, có_nằm_trong_khối_mã) để không đụng vào mã và sơ đồ."""
    in_code, result = False, []
    for line in text.splitlines():
        if FENCE.match(line):
            in_code = not in_code
            result.append((line, True))
            continue
        result.append((line, in_code))
    return result


def strip_title_and_note(text):
    """Bỏ dòng `# Tiêu đề` đầu tệp và đoạn ghi chú cho nhóm ngay sau nó (script không đưa vào bản nộp)."""
    lines = text.splitlines()
    out, seen_title, i = [], False, 0
    while i < len(lines):
        line = lines[i]
        if not seen_title and line.startswith("# "):
            seen_title = True
            i += 1
            while i < len(lines) and not lines[i].strip():
                i += 1
            if i < len(lines) and lines[i].startswith(NOTE_START):
                while i < len(lines) and lines[i].strip():
                    i += 1
            continue
        out.append(line)
        i += 1
    return "\n".join(out).strip("\n") + "\n"


def shift_headings(text, delta):
    out = []
    for line, code in split_fences(text):
        match = None if code else re.match(r"^(#{1,6})(\s+.*)$", line)
        if match:
            line = "#" * min(6, len(match.group(1)) + delta) + match.group(2)
        out.append(line)
    return "\n".join(out)


def number_headings(text, chapter):
    """Đánh số mục 2.1, 2.1.1, 2.1.1.1 từ các dòng `##` trở xuống; dòng `#` là tên chương, giữ nguyên."""
    counters = [0, 0, 0]
    out = []
    for line, code in split_fences(text):
        match = None if code else HEADING.match(line)
        if not match or len(match.group(1)) == 1:
            out.append(line)
            continue
        level = len(match.group(1)) - 1
        if level > len(counters):
            out.append(f"**{match.group(2)}**")  # sâu hơn 4 cấp: hướng dẫn của trường chỉ có tới 1.1.1.1
            continue
        counters[level - 1] += 1
        for k in range(level, len(counters)):
            counters[k] = 0
        label = ".".join([str(chapter)] + [str(c) for c in counters[:level]])
        out.append(f"{match.group(1)} {label}. {match.group(2)}")
    return "\n".join(out)


def rewrite_design_references(text, section_label, labels):
    """Giống bdd.rewrite_references nhưng tham chiếu tới phần khác viết là "mục 2.k" thay vì "chương N"."""
    for name, label in labels.items():
        text = re.sub(r"\[`?" + re.escape(name) + r"`?\]\(" + re.escape(name) + r"[^)]*\)", label, text)
        text = text.replace(f"`{name}`", label)
    for name, task, _ in bdd.PARTS:
        if name in labels:
            text = re.sub(rf"task #{task}\b", labels[name], text)
    text = re.sub(r"\s*\((?:đã có từ |đã làm ở )?task #\d+(?:, #\d+)*\)", "", text)
    text = re.sub(r"chốt ở task #\d+, ", "", text)
    text = re.sub(r", quyết ở task #\d+", "", text)
    text = re.sub(r"(mục \d+(?:\.\d+)*), \1\b", r"\1", text)
    text = re.sub(r"(mục \d+(?:\.\d+)*) \(\1\)", r"\1", text)
    text = re.sub(r"(?<!!)\[([^\]]+)\]\((?!https?://)[^)]+\)", r"\1", text)
    architecture = labels["kien-truc-he-thong.md"].replace("mục ", "")

    def section(match):
        if match.group(2):
            return f"mục {architecture}.{match.group(1)}"
        return f"mục {section_label}.{match.group(1)}"

    return re.sub(r"(?<!bản 5, )mục (\d+(?:\.\d+)*)( của tài liệu kiến trúc)?", section, text)


def rewrite_local_sections(text, chapter):
    """Với tệp chương viết riêng: "mục 5" trong tệp thành "mục <chương>.5"."""
    text = re.sub(r"(?<!!)\[([^\]]+)\]\((?!https?://)[^)]+\)", r"\1", text)
    return re.sub(r"(?<![\w.])mục (\d+(?:\.\d+)*)", lambda m: f"mục {chapter}.{m.group(1)}", text)


def render_diagrams(text, chapter, base_dirs, work, cache):
    """Như bdd.render_diagrams nhưng đánh số hình liên tục trong cả chương và tìm ảnh PNG ở nhiều thư mục."""
    out, block, figure, title = [], None, 0, ""
    for line in text.splitlines():
        heading = HEADING.match(line)
        if block is None and heading and not line.startswith("# "):
            title = re.sub(r"^\d+(?:\.\d+)*\.\s*", "", line.lstrip("#").strip())
        if block is None and bdd.FENCE.match(line):
            block = []
            continue
        if block is not None and line.startswith("```"):
            source = "\n".join(block) + "\n"
            settings = bdd.json.dumps(bdd.MERMAID_CONFIG, sort_keys=True)
            digest = bdd.hashlib.sha256((settings + source).encode("utf-8")).hexdigest()[:16]
            image = cache / f"{digest}.png"
            if not image.exists():
                src = work / f"{digest}.mmd"
                src.write_text(source, encoding="utf-8")
                config_file = work / "mermaid.json"
                config_file.write_text(settings, encoding="utf-8")
                cmd = bdd.mermaid_command() + ["-i", str(src), "-o", str(image), "-s", str(bdd.SCALE), "-b", "white", "-c", str(config_file)]
                browser = bdd.browser_config(work)
                if browser:
                    cmd += ["-p", str(browser)]
                subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL)
            figure += 1
            out.extend(bdd.figure_lines(image, chapter, figure, title))
            block = None
            continue
        picture = bdd.IMAGE.match(line) if block is None else None
        if picture:
            image = next((d / picture.group(1) for d in base_dirs if (d / picture.group(1)).exists()), None)
            if image is None:
                sys.exit(f"Thiếu ảnh {picture.group(1)}")
            figure += 1
            out.extend(bdd.figure_lines(image, chapter, figure, title))
            continue
        if block is not None:
            block.append(line)
        else:
            out.append(line)
    return "\n".join(out)


def number_tables(text):
    """Chú thích bảng `: Tên bảng` đứng ngay sau bảng; đánh số theo chương: Bảng 2.4."""
    out, chapter, count, in_code = [], 0, 0, False
    lines = text.splitlines()
    for index, line in enumerate(lines):
        if line.startswith("```"):
            in_code = not in_code
        heading = None if in_code else re.match(r"^# CHƯƠNG (\d+)\. ", line)
        if heading:
            chapter, count = int(heading.group(1)), 0
        caption = None if in_code else bdd.CAPTION.match(line)
        if caption:
            before = next((lines[j] for j in range(index - 1, -1, -1) if lines[j].strip()), "")
            if not before.startswith("|"):
                sys.exit(f"Chú thích bảng không đứng ngay sau bảng: {line}")
            count += 1
            line = f": Bảng {chapter}.{count}. {caption.group(1)}"
        out.append(line)
    return "\n".join(out)


def page_break():
    return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>'


def front_matter():
    toc = (
        '<w:p><w:pPr><w:pStyle w:val="TOCHeading"/></w:pPr><w:r><w:t>MỤC LỤC</w:t></w:r></w:p>'
        '<w:p><w:r><w:fldChar w:fldCharType="begin" w:dirty="true"/></w:r><w:r><w:instrText xml:space="preserve"> TOC \\o "1-3" \\h \\z \\u </w:instrText></w:r>'
        '<w:r><w:fldChar w:fldCharType="separate"/></w:r><w:r><w:t>Nhấn F9 để cập nhật mục lục.</w:t></w:r><w:r><w:fldChar w:fldCharType="end"/></w:r></w:p>'
    )
    abbreviations_heading = '<w:p><w:pPr><w:pStyle w:val="TOCHeading"/></w:pPr><w:r><w:t>DANH MỤC CÁC TỪ VIẾT TẮT</w:t></w:r></w:p>'
    lists = bdd.list_xml("DANH MỤC BẢNG", "Table Caption") + bdd.list_xml("DANH MỤC HÌNH", "Image Caption")
    table = bdd.ABBREVIATIONS.read_text(encoding="utf-8").strip()
    return (
        f"```{{=openxml}}\n{page_break()}{toc}{page_break()}{abbreviations_heading}\n```\n\n{table}\n\n"
        f"```{{=openxml}}\n{page_break()}{lists}\n```"
    )


def read_chapter_file(name, title, missing):
    path = REPORT_DIR / name
    if path.exists():
        return strip_title_and_note(read_text(path))
    missing.append(f"docs/report/{name}")
    return "Phần này đang được soạn.\n"


def build_design_chapter(missing):
    labels = {name: f"mục 2.{index}" for index, (name, _) in enumerate(DESIGN_PARTS, start=1)}
    chunks = []
    for index, (name, title) in enumerate(DESIGN_PARTS, start=1):
        path = DESIGN_DIR / name
        if path.exists():
            body = bdd.drop_intro(read_text(path))
            body = "\n".join(body.splitlines()[1:])  # bỏ dòng `# Tiêu đề` của phần cũ
        else:
            body = "Phần này đang được soạn.\n"
            missing.append(f"docs/thiet-ke/{name}")
        body = shift_headings(body, 1)
        body = rewrite_design_references(body, f"2.{index}", labels)
        chunks.append(f"## {title}\n\n{body.strip(chr(10))}\n")
    return "\n".join(chunks)


def build_markdown(work, cache):
    missing = []
    master = read_text(REPORT_DIR / MASTER)
    cover, body = bdd.split_cover(master)
    fields = bdd.cover_fields(cover)
    intro = "\n".join(line for line in strip_title_and_note(body).splitlines())
    pieces = [front_matter(), f"# LỜI MỞ ĐẦU\n\n{intro.strip(chr(10))}\n"]
    for number, (name, title) in enumerate(CHAPTERS, start=1):
        if name == "@thiet-ke":
            text = build_design_chapter(missing)
        else:
            text = read_chapter_file(name, title, missing)
            text = rewrite_local_sections(text, number)
        text = f"# CHƯƠNG {number}. {title.upper()}\n\n{text}"
        text = number_headings(text, number)
        text = render_diagrams(text, number, [REPORT_DIR, DESIGN_DIR], work, cache)
        pieces.append(text)
    conclusion = read_chapter_file("ket-luan.md", "Kết luận", missing)
    pieces.append(f"# KẾT LUẬN\n\n{conclusion}")
    pieces.append("# TÀI LIỆU THAM KHẢO\n\n::: {#refs}\n:::\n")
    appendix = read_chapter_file("phu-luc.md", "Phụ lục", missing)
    pieces.append(f"# PHỤ LỤC\n\n{appendix}")
    combined = number_tables("\n\n".join(pieces)) + "\n"
    waiting = combined.count("\nPhần này đang được soạn.")
    if waiting:
        missing.append(f"{waiting} mục còn dòng 'Phần này đang được soạn.' (tìm trong docs/report/)")
    prose = re.sub(r"^```.*?^```", "", combined, flags=re.M | re.S)
    leftovers = sorted(set(re.findall(r"task #\d+", prose)) | set(re.findall(r"(?<![\w&/#])#\d+\b", prose)))
    return combined, missing, leftovers, fields


def make_reference(path):
    subprocess.run(["pandoc", "-o", str(path), "--print-default-data-file", "reference.docx"], check=True)
    doc = Document(str(path))
    styles = doc.styles
    names = {s.name for s in styles}
    for name in ("Normal", "Body Text", "First Paragraph", "Compact", "Table", "Caption", "Image Caption", "Table Caption", "Bibliography"):
        if name in names:
            bdd.set_font(styles[name], FONT_SIZE)
    for name in ("Body Text", "First Paragraph", "Compact"):
        fmt = styles[name].paragraph_format
        fmt.space_before, fmt.space_after, fmt.line_spacing = Pt(0), Pt(6), LINE_SPACING
    for name in ("Caption", "Image Caption", "Table Caption"):
        if name in names:
            fmt = styles[name].paragraph_format
            fmt.line_spacing = 1.0
            fmt.alignment = WD_ALIGN_PARAGRAPH.CENTER
    fmt = styles["Table Caption"].paragraph_format
    fmt.space_before, fmt.space_after, fmt.keep_with_next = Pt(6), Pt(3), True
    if "Bibliography" in names:
        fmt = styles["Bibliography"].paragraph_format
        fmt.alignment = WD_ALIGN_PARAGRAPH.LEFT
        fmt.left_indent, fmt.first_line_indent = Cm(1.2), Cm(-1.2)
        fmt.space_before, fmt.space_after, fmt.line_spacing = Pt(0), Pt(6), LINE_SPACING
        fmt.tab_stops.add_tab_stop(Cm(1.2))
    # Tiêu đề: chương in hoa đậm (chữ đã viết hoa trong nguồn); mục đậm; nhóm tiểu mục đậm nghiêng; tiểu mục nghiêng.
    for name, bold, italic in (("Heading 1", True, False), ("Heading 2", True, False), ("Heading 3", True, True), ("Heading 4", False, True), ("TOC Heading", True, False)):
        if name in names:
            bdd.set_font(styles[name], FONT_SIZE, bold=bold)
            styles[name].font.italic = italic
            fmt = styles[name].paragraph_format
            fmt.space_before, fmt.space_after, fmt.line_spacing = Pt(12), Pt(6), LINE_SPACING
            fmt.keep_with_next = True
    styles["Heading 1"].paragraph_format.page_break_before = True
    styles["TOC Heading"].paragraph_format.page_break_before = False
    for section in doc.sections:
        section.page_width, section.page_height = Cm(21), Cm(29.7)
        section.top_margin, section.bottom_margin = Cm(MARGINS_CM["top"]), Cm(MARGINS_CM["bottom"])
        section.left_margin, section.right_margin = Cm(MARGINS_CM["left"]), Cm(MARGINS_CM["right"])
    doc.save(str(path))


def add_page_number_footer(section):
    footer = section.footer.paragraphs[0]
    for run in list(footer.runs):
        run._r.getparent().remove(run._r)
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


def set_page_numbering(sect_pr, fmt):
    for old in sect_pr.findall(qn("w:pgNumType")):
        sect_pr.remove(old)
    node = OxmlElement("w:pgNumType")
    node.set(qn("w:fmt"), fmt)
    node.set(qn("w:start"), "1")
    sect_pr.append(node)


def split_page_numbering(doc):
    """Mục lục tới hết danh mục: số La Mã thường; từ Lời mở đầu: số thường bắt đầu từ 1."""
    body_sect = doc.sections[-1]._sectPr
    add_page_number_footer(doc.sections[-1])
    target = next((p for p in doc.paragraphs if p.text.strip() == "LỜI MỞ ĐẦU" and p.style.name == "Heading 1"), None)
    if target is None:
        sys.exit("Không tìm thấy tiêu đề LỜI MỞ ĐẦU để chia phần đánh số trang")
    front = copy.deepcopy(body_sect)
    set_page_numbering(front, "lowerRoman")
    for node in front.findall(qn("w:type")):
        front.remove(node)
    kind = OxmlElement("w:type")
    kind.set(qn("w:val"), "nextPage")
    front.insert(0, kind)
    carrier = parse_xml('<w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr/></w:p>')
    carrier.find(qn("w:pPr")).append(front)
    target._p.addprevious(carrier)
    set_page_numbering(body_sect, "decimal")


def tidy_tables(doc):
    for table in doc.tables:
        for row in table.rows:
            for cell in row.cells:
                for paragraph in cell.paragraphs:
                    paragraph.paragraph_format.line_spacing = 1.0


def finish_docx(path, widths, ratios):
    bdd.TEXT_WIDTH = TEXT_WIDTH_CM
    bdd.PORTRAIT = PORTRAIT_CM
    bdd.finish_docx(path, widths, ratios, update_on_open=True)
    doc = Document(str(path))
    split_page_numbering(doc)
    tidy_tables(doc)
    doc.save(str(path))


def main():
    parser = argparse.ArgumentParser(description="Dựng báo cáo cuối kỳ .docx theo hướng dẫn trình bày của trường")
    parser.add_argument("-o", "--output", default=str(REPORT_DIR / "build" / "bao-cao-cuoi-ky.docx"))
    parser.add_argument("--markdown", help="Ghi thêm bản Markdown đã ghép ra đường dẫn này")
    parser.add_argument("--word", action="store_true", help="Dùng Microsoft Word (Windows) cập nhật mục lục, danh mục và xuất PDF")
    args = parser.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    bdd.PORTRAIT = PORTRAIT_CM
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    cache = output.parent / "diagrams"
    cache.mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        work = Path(tmp)
        combined, missing, leftovers, fields = build_markdown(work, cache)
        source = work / "combined.md"
        source.write_text(bdd.table_breaks(combined), encoding="utf-8")
        if args.markdown:
            Path(args.markdown).write_text(combined, encoding="utf-8")
        reference = work / "reference.docx"
        make_reference(reference)
        subprocess.run(
            ["pandoc", str(source), "-f", "markdown-auto_identifiers", "-o", str(output), "--reference-doc", str(reference),
             "--resource-path", str(ROOT), "--columns", "10000",
             "--citeproc", "--bibliography", str(bdd.BIBLIOGRAPHY), "--csl", str(REPORT_CSL)],
            check=True,
        )
    finish_docx(output, [float(w) for w in re.findall(r"\{width=([\d.]+)cm\}", combined)], bdd.table_ratios(combined))
    members, named = bdd.read_members()
    if not named:
        missing.append(f"MSSV và họ tên ở bìa ({bdd.MEMBERS.relative_to(ROOT).as_posix()}, mỗi dòng: MSSV - Họ và tên)")
    apply_cover(output, bdd.COVER_TEMPLATE, members, fields)
    if args.word:
        bdd.word_finish(output, output.with_suffix(".pdf"))
    print(f"Đã ghi {output}")
    for item in missing:
        print(f"Thiếu phần: {item}")
    for item in leftovers:
        print(f"Còn nhắc tới {item} trong nội dung, cần sửa ở tệp nguồn")
    return 1 if leftovers else 0


if __name__ == "__main__":
    sys.exit(main())
