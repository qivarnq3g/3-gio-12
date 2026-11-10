import re
import zipfile
from pathlib import Path

PAGE_SIZE = '<w:pgSz w:w="11906" w:h="16838"/>'
PAGE_WIDTH = 11906
LOGO_TARGET = "media/cover-logo.png"
LOGO_ID = "rIdCoverLogo"
DEFAULT_SPACING = '<w:spacing w:after="160" w:line="259" w:lineRule="auto"/>'
CELL_SPACING = '<w:spacing w:before="60" w:after="60" w:line="259" w:lineRule="auto"/>'
PARAGRAPH = re.compile(r"<w:p(?:\s[^>]*)?>.*?</w:p>", re.S)
TEXT = re.compile(r"<w:t(?:\s[^>]*)?>[^<]")


def escape(text):
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def template_cover(template_doc):
    body = template_doc[template_doc.index("<w:body>") + len("<w:body>"):]
    stop = body.find("MỤC LỤC")
    if stop == -1:
        raise SystemExit("Mẫu bìa không có dòng MỤC LỤC để cắt")
    start = max(body.rfind("<w:p ", 0, stop), body.rfind("<w:p>", 0, stop))
    cover = body[:start]
    breaks = [m.start() for m in re.finditer(r'<w:br w:type="page"/>', cover)]
    if not breaks:
        raise SystemExit("Mẫu bìa không có ngắt trang sau bìa chính")
    cut = breaks[0]
    paragraph_end = cover.index("</w:p>", cut) + len("</w:p>")
    tail = cover[cut + len('<w:br w:type="page"/>'):paragraph_end]
    if TEXT.search(tail):
        raise SystemExit("Mẫu bìa có chữ sau ngắt trang trong cùng đoạn")
    cover = cover[:cut] + tail
    while True:
        blocks = list(PARAGRAPH.finditer(cover))
        tail = blocks[-1]
        if cover[tail.end():].strip() or TEXT.search(tail.group(0)) or "<w:drawing>" in tail.group(0):
            break
        cover = cover[:tail.start()] + cover[tail.end():]
    return cover


def insert_props(block, props):
    existing = re.search(r"<w:pPr>(.*?)</w:pPr>", block, re.S)
    if existing:
        inner = existing.group(1)
        style = re.match(r"\s*<w:pStyle[^>]*/>", inner)
        merged = style.group(0) + props + inner[style.end():] if style else props + inner
        return block.replace(existing.group(0), "<w:pPr>" + merged + "</w:pPr>", 1)
    opening = re.match(r"<w:p(?:\s[^>]*)?>", block).group(0)
    return block.replace(opening, opening + "<w:pPr>" + props + "</w:pPr>", 1)


def fix_paragraph(match):
    block = match.group(0)
    listed = "<w:numPr>" in block
    block = re.sub(r"<w:numPr>.*?</w:numPr>", "", block, flags=re.S)
    block = re.sub(r'<w:pStyle w:val="ListParagraph"\s*/>', "", block)
    props = "" if "<w:spacing" in block else DEFAULT_SPACING
    if listed:
        props = '<w:spacing w:after="0" w:line="259" w:lineRule="auto"/><w:ind w:left="720" w:hanging="360"/>'
    return insert_props(block, props) if props else block


def student_rows(table, members):
    rows = re.findall(r"<w:tr[ >].*?</w:tr>", table, re.S)
    sample = next(row for row in rows if "&lt;Tên sv 1&gt;" in row)
    head = rows[0]
    body = []
    for index, (student_id, name) in enumerate(members, start=1):
        row = sample.replace("&lt;Tên sv 1&gt;", f"{index}.</w:t></w:r><w:r><w:rPr><w:rFonts w:ascii=\"Times New Roman\" w:hAnsi=\"Times New Roman\" w:cs=\"Times New Roman\"/><w:b/><w:bCs/><w:sz w:val=\"28\"/><w:szCs w:val=\"28\"/></w:rPr><w:tab/><w:t>{escape(name)}")
        row = row.replace("&lt;MSSV 1&gt;", escape(student_id))
        body.append(row)
    start = table.index(rows[0])
    end = table.index(rows[-1]) + len(rows[-1])
    return table[:start] + head + "".join(body) + table[end:]


def fit_table(table, available):
    half = available // 2
    widths = (half, available - half)
    table = re.sub(r'<w:tblW [^>]*/>', f'<w:tblW w:w="{available}" w:type="dxa"/>', table, count=1)
    if "<w:tblLayout" not in table:
        table = table.replace("<w:tblLook", '<w:tblLayout w:type="fixed"/><w:tblLook', 1)
    table = re.sub(r"<w:tblGrid>.*?</w:tblGrid>", "<w:tblGrid>" + "".join(f'<w:gridCol w:w="{w}"/>' for w in widths) + "</w:tblGrid>", table, flags=re.S)
    table = re.sub(r'<w:tblStyle w:val="[^"]*"\s*/>', "", table)
    line = '<w:{0} w:val="single" w:sz="4" w:space="0" w:color="000000"/>'
    table = re.sub(r"<w:tblBorders>.*?</w:tblBorders>", "<w:tblBorders>" + "".join(line.format(side) for side in ("top", "left", "bottom", "right", "insideH", "insideV")) + "</w:tblBorders>", table, count=1, flags=re.S)
    edges = "".join(line.format(side) for side in ("top", "left", "bottom", "right"))
    table = table.replace('<w:vAlign w:val="bottom"/>', '<w:vAlign w:val="center"/>').replace('<w:jc w:val="right"/>', '<w:jc w:val="center"/>')
    out = []
    for row in re.split(r"(?=<w:tr[ >])", table):
        column = iter(widths)
        row = re.sub(r'<w:tcW [^>]*/>', lambda m: f'<w:tcW w:w="{next(column)}" w:type="dxa"/><w:tcBorders>{edges}</w:tcBorders>', row)
        row = PARAGRAPH.sub(lambda m: insert_props(re.sub(r"<w:spacing[^>]*/>", "", m.group(0)), CELL_SPACING), row)
        out.append(row)
    return "".join(out)


def merge_namespaces(document, template_doc):
    tag = re.search(r"<w:document\b[^>]*>", document).group(0)
    donor = re.search(r"<w:document\b[^>]*>", template_doc).group(0)
    have = set(re.findall(r"xmlns:(\w+)=", tag))
    extra = [f'xmlns:{p}="{u}"' for p, u in re.findall(r'xmlns:(\w+)="([^"]+)"', donor) if p not in have]
    merged = tag[:-1].rstrip() + (" " + " ".join(extra) if extra else "") + ">"
    donor_ignorable = re.search(r'mc:Ignorable="([^"]*)"', donor)
    if donor_ignorable:
        own = re.search(r'mc:Ignorable="([^"]*)"', merged)
        names = " ".join(sorted(set(donor_ignorable.group(1).split()) | set(own.group(1).split() if own else [])))
        merged = merged.replace(own.group(0), f'mc:Ignorable="{names}"') if own else merged[:-1] + f' mc:Ignorable="{names}">'
    return document.replace(tag, merged, 1)


def apply_cover(docx, template, members, fields):
    with zipfile.ZipFile(template) as source:
        template_doc = source.read("word/document.xml").decode("utf-8")
        logo = source.read("word/media/image1.png")
    cover = template_cover(template_doc)
    for old, new in fields:
        if old not in cover:
            raise SystemExit(f"Mẫu bìa không còn chuỗi {old!r}")
        cover = cover.replace(old, escape(new))
    margins = re.search(r"<w:pgMar[^>]*/>", template_doc).group(0)
    borders = re.search(r"<w:pgBorders.*?</w:pgBorders>", template_doc, re.S).group(0)
    left = int(re.search(r'w:left="(\d+)"', margins).group(1))
    right = int(re.search(r'w:right="(\d+)"', margins).group(1))
    cover = PARAGRAPH.sub(fix_paragraph, cover)
    cover = re.sub(r"<w:tbl>.*?</w:tbl>", lambda m: fit_table(student_rows(m.group(0), members), PAGE_WIDTH - left - right), cover, flags=re.S)
    embed = re.search(r'r:embed="([^"]+)"', cover).group(1)
    cover = cover.replace(f'r:embed="{embed}"', f'r:embed="{LOGO_ID}"')
    section = "<w:p><w:pPr><w:sectPr>" + PAGE_SIZE + margins + borders + "</w:sectPr></w:pPr></w:p>"

    with zipfile.ZipFile(docx) as built:
        items = {name: built.read(name) for name in built.namelist()}
    document = items["word/document.xml"].decode("utf-8")
    body_at = document.index("<w:body>") + len("<w:body>")
    first = PARAGRAPH.match(document, body_at)
    if first and '<w:br w:type="page"/>' in first.group(0) and not TEXT.search(first.group(0)):
        document = document[:first.start()] + document[first.end():]
    document = merge_namespaces(document, template_doc)
    body_at = document.index("<w:body>") + len("<w:body>")
    document = document[:body_at] + cover + section + document[body_at:]
    items["word/document.xml"] = document.encode("utf-8")

    rels = items["word/_rels/document.xml.rels"].decode("utf-8")
    if LOGO_ID not in rels:
        rels = rels.replace("</Relationships>", f'<Relationship Id="{LOGO_ID}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="{LOGO_TARGET}"/></Relationships>')
        items["word/_rels/document.xml.rels"] = rels.encode("utf-8")
    items[f"word/{LOGO_TARGET}"] = logo
    types = items["[Content_Types].xml"].decode("utf-8")
    if 'Extension="png"' not in types:
        first_override = types.find("<Override")
        types = types[:first_override] + '<Default Extension="png" ContentType="image/png"/>' + types[first_override:]
        items["[Content_Types].xml"] = types.encode("utf-8")

    temp = Path(str(docx) + ".tmp")
    with zipfile.ZipFile(temp, "w", zipfile.ZIP_DEFLATED) as out:
        for name, data in items.items():
            out.writestr(name, data)
    temp.replace(docx)
