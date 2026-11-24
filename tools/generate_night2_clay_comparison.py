r"""Night 2 props: clay renders (Blender) + side-by-side comparison boards (Pillow).

Step 1, clay renders -> tools/out/clay/*_Clay.png:
  & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python tools/generate_night2_clay_comparison.py
Step 2, boards -> tools/out/comparison/*_Comparison.png + Night2_All_Props_Overview.png:
  py -3.14 tools/generate_night2_clay_comparison.py
"""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROPS = ROOT / "game/Assets/Art/Models/Props"
UNITY_SHOTS = ROOT / "game/Logs"
CLAY = ROOT / "tools/out/clay"
BOARDS = ROOT / "tools/out/comparison"
STEMS = ["Night2_BreadCart", "Night2_Shelf", "Night2_ClampLamp", "HomewareShop_ShelfKit_Box"]


def render_clay():
    import bpy
    from mathutils import Vector

    CLAY.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.resolution_x, scene.render.resolution_y = 800, 600
    scene.render.image_settings.file_format = "PNG"
    if scene.world is None:
        scene.world = bpy.data.worlds.new("World")
    scene.world.color = (0.18, 0.18, 0.2)

    clay = bpy.data.materials.new("Clay")
    clay.use_nodes = True
    bsdf = clay.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.78, 0.78, 0.78, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.65
    bsdf.inputs["Metallic"].default_value = 0.0
    spec = bsdf.inputs.get("Specular IOR Level") or bsdf.inputs.get("Specular")  # 4.x+ vs 3.x name
    spec.default_value = 0.2

    for stem in STEMS:
        for obj in list(bpy.data.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.ops.import_scene.fbx(filepath=str(PROPS / f"{stem}.fbx"))
        meshes = [o for o in scene.objects if o.type == "MESH"]
        assert meshes, f"No meshes in {stem}.fbx"
        for o in meshes:
            o.data.materials.clear()
            o.data.materials.append(clay)

        bpy.context.view_layer.update()
        pts = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
        lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
        hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
        center, size = (lo + hi) / 2, max((hi - lo).length, 0.01)

        cam = bpy.data.objects.new("ClayCam", bpy.data.cameras.new("ClayCam"))
        scene.collection.objects.link(cam)
        cam.location = center + Vector((0.75, -1.0, 0.6)).normalized() * size * 2.2
        cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.camera = cam

        # Key / fill (opposite side) / rim (behind) for silhouette and bevel definition.
        for name, kind, energy, offset in [
            ("Key", "SUN", 4.0, (1.0, -1.0, 1.4)),
            ("Fill", "SUN", 2.0, (-1.0, -0.6, 0.6)),
            ("Rim", "SUN", 3.0, (-0.3, 1.2, 1.0)),
        ]:
            light = bpy.data.objects.new(name, bpy.data.lights.new(name, kind))
            light.data.energy = energy
            light.location = center + Vector(offset) * size
            light.rotation_euler = (center - light.location).to_track_quat("-Z", "Y").to_euler()
            scene.collection.objects.link(light)

        scene.render.filepath = str(CLAY / f"{stem}_Clay.png")
        bpy.ops.render.render(write_still=True)
        assert (CLAY / f"{stem}_Clay.png").is_file()
        print(f"[CLAY] {stem}_Clay.png")


# ---------------------------------------------------------------- boards (Pillow)
REF_TEXT = {
    "Night2_BreadCart": "Xe bánh mì đầu hẻm\nkhung inox, tủ kính, mái tôn",
    "Night2_Shelf": "Kệ nhựa 4 tầng lắp ráp\nkhung nhựa xanh, thanh trắng",
    "Night2_ClampLamp": "Đèn kẹp bàn\nchao kim loại, tay gập, kẹp vít",
    "HomewareShop_ShelfKit_Box": "Hộp các-tông đựng kệ\nbăng keo, nhãn in",
}
REFERENCE_PHOTOS = {"Night2_ClampLamp": ROOT / "tools/out/reference/lamp_reference_L511S.jpg"}
HEADERS = ["Ảnh tham khảo thực tế", "Ảnh Clay Mesh (Blender)", "Ảnh Render Unity Ban Ngày"]
CELL_W, CELL_H, HEAD_H, TITLE_H, PAD = 640, 480, 44, 56, 16


def font(size):
    from PIL import ImageFont
    for name in ("arial.ttf", "segoeui.ttf", "DejaVuSans.ttf"):  # all carry Vietnamese glyphs
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def reference_card(stem):
    """Stylized placeholder for the real-world reference photo (no photos are shipped in the repo)."""
    from PIL import Image, ImageDraw, ImageOps
    photo = REFERENCE_PHOTOS.get(stem)
    if photo and photo.is_file():  # real photo when the user supplied one (tools/out is gitignored)
        return ImageOps.fit(Image.open(photo).convert("RGB"), (CELL_W, CELL_H))
    img = Image.new("RGB", (CELL_W, CELL_H), (236, 228, 212))
    d = ImageDraw.Draw(img)
    ink, accent = (60, 54, 48), (176, 96, 52)
    cx, base = CELL_W // 2, CELL_H - 150
    if stem == "Night2_BreadCart":
        d.polygon([(cx - 190, base - 230), (cx + 190, base - 230), (cx + 170, base - 260), (cx - 170, base - 260)], fill=(150, 90, 60))
        d.rectangle([cx - 170, base - 230, cx + 170, base - 130], outline=ink, width=4, fill=(205, 225, 230))
        d.rectangle([cx - 180, base - 130, cx + 180, base - 40], outline=ink, width=4, fill=(190, 192, 196))
        for x in (cx - 140, cx + 140):
            d.ellipse([x - 32, base - 60, x + 32, base + 4], outline=ink, width=6)
    elif stem == "Night2_Shelf":
        for x in (cx - 120, cx + 120):
            d.rectangle([x - 8, base - 280, x + 8, base], fill=(40, 90, 170))
        for i in range(4):
            y = base - 20 - i * 85
            d.rectangle([cx - 130, y - 10, cx + 130, y], fill=(235, 235, 235), outline=ink)
    elif stem == "Night2_ClampLamp":
        d.rectangle([cx - 90, base - 20, cx - 30, base], fill=ink)
        d.line([(cx - 60, base - 20), (cx + 10, base - 180), (cx + 110, base - 230)], fill=ink, width=8)
        d.pieslice([cx + 70, base - 260, cx + 190, base - 140], 200, 340, fill=(70, 70, 76))
        d.ellipse([cx + 115, base - 205, cx + 145, base - 175], fill=(255, 220, 140))
    else:
        d.polygon([(cx - 170, base - 100), (cx + 110, base - 100), (cx + 170, base - 150), (cx - 110, base - 150)], fill=(205, 165, 115), outline=ink)
        d.rectangle([cx - 170, base - 100, cx + 110, base], fill=(190, 145, 95), outline=ink, width=3)
        d.polygon([(cx + 110, base - 100), (cx + 170, base - 150), (cx + 170, base - 50), (cx + 110, base)], fill=(165, 125, 80), outline=ink)
        d.rectangle([cx - 80, base - 100, cx - 40, base], fill=(222, 200, 150))
    for i, line in enumerate(REF_TEXT[stem].split("\n")):
        d.text((cx, CELL_H - 100 + i * 32), line, font=font(26 if i == 0 else 20), fill=accent if i == 0 else ink, anchor="mm")
    d.text((12, 10), "minh hoạ tham khảo", font=font(16), fill=(130, 120, 110))
    return img


def fit(path):
    from PIL import Image, ImageDraw
    cell = Image.new("RGB", (CELL_W, CELL_H), (40, 40, 44))
    if not path.is_file():
        ImageDraw.Draw(cell).text((CELL_W // 2, CELL_H // 2), f"Thiếu ảnh:\n{path.name}", font=font(22), fill=(230, 90, 90), anchor="mm", align="center")
        return cell
    img = Image.open(path).convert("RGB")
    img.thumbnail((CELL_W, CELL_H))
    cell.paste(img, ((CELL_W - img.width) // 2, (CELL_H - img.height) // 2))
    return cell


def board(stem):
    from PIL import Image, ImageDraw
    cells = [reference_card(stem), fit(CLAY / f"{stem}_Clay.png"), fit(UNITY_SHOTS / f"{stem}.png")]
    w = PAD + 3 * (CELL_W + PAD)
    img = Image.new("RGB", (w, TITLE_H + HEAD_H + CELL_H + PAD), (24, 24, 28))
    d = ImageDraw.Draw(img)
    d.text((PAD, TITLE_H // 2), stem, font=font(30), fill=(245, 245, 245), anchor="lm")
    for i, (head, cell) in enumerate(zip(HEADERS, cells)):
        x = PAD + i * (CELL_W + PAD)
        d.text((x + CELL_W // 2, TITLE_H + HEAD_H // 2), head, font=font(24), fill=(250, 200, 120), anchor="mm")
        img.paste(cell, (x, TITLE_H + HEAD_H))
    return img


def build_boards():
    from PIL import Image
    BOARDS.mkdir(parents=True, exist_ok=True)
    boards = []
    for stem in STEMS:
        b = board(stem)
        b.save(BOARDS / f"{stem}_Comparison.png")
        boards.append(b)
        print(f"[BOARD] {stem}_Comparison.png")
    overview = Image.new("RGB", (boards[0].width, sum(b.height for b in boards)), (24, 24, 28))
    y = 0
    for b in boards:
        overview.paste(b, (0, y))
        y += b.height
    overview.save(BOARDS / "Night2_All_Props_Overview.png")
    assert all((BOARDS / f"{s}_Comparison.png").is_file() for s in STEMS)
    print("[BOARD] Night2_All_Props_Overview.png")


if __name__ == "__main__":
    try:
        import bpy  # noqa: F401
    except ImportError:
        build_boards()
    else:
        render_clay()
