r"""Generate the Night 2 evening props for issue #129:
1. Night2_RiceShop  (nồi canh trên bếp ga, nồi cơm điện, chồng đĩa: phần env_rice_shop còn thiếu)
2. Night2_MilkTea   (ly trà sữa nhựa có nắp, ống hút, giấy dán T_MilkTeaSticker.png)
3. Night2_LienTable (bàn nhựa thấp cạnh cô Liên ở cổng nhà trọ, chỗ đặt ly trà sữa)

Reuses the helpers of tools/generate_night2_props.py. Run with Blender 5.2+:
  & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python tools/generate_night2_evening_props.py -- --render

Blender axes: X right, -Y towards the customer (Unity +Z after FBX export), Z up. The rice shop front, food counter,
tables and stools already exist in env_rice_shop (#23); Night2_RiceShop only adds what the evening scene still lacks.
"""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_night2_props as base  # noqa: E402

box, cylinder, lathe, join_objects = base.box, base.cylinder, base.lathe, base.join_objects
LIMITS = {"Night2_RiceShop": 10000, "Night2_MilkTea": 3000, "Night2_LienTable": 3000}


def build_rice_shop():
    """Nồi canh trên bếp ga, nồi cơm điện trên kệ inox, chồng đĩa: phần env_rice_shop (#23) còn thiếu.

    Gốc tọa độ ở giữa đáy bếp ga. Đặt cạnh người bán, giữa quầy món có sẵn và bình gas của env_rice_shop.
    """
    base.clear_scene()
    parts = []
    # Bếp ga đơn trên chân sắt, mặt men đen, hai núm vặn phía trước.
    parts.append(box("Stove_Frame", (0, 0, 0.36), (0.46, 0.40, 0.72), "M_SteelDark", edge=0.01))
    parts.append(box("Stove_Top", (0, 0, 0.735), (0.48, 0.42, 0.03), "M_ScreenBlack", edge=0.006))
    for dx in (-0.08, 0.08):
        parts.append(cylinder("Stove_Knob", (dx, -0.205, 0.66), (dx, -0.235, 0.66), 0.018, "M_PlasticWhite", sides=10))
    parts.append(lathe("Stove_Burner", (0, 0, 0.75), (0, 0, 1), [(0, 0.10), (0.025, 0.10), (0.025, 0.06), (0.035, 0.06), (0.035, 0)], "M_SteelDark", sides=16))
    for k in range(4):
        a = math.radians(45 + 90 * k)
        parts.append(box(f"Stove_Grate{k}", (0.09 * math.cos(a), 0.09 * math.sin(a), 0.79), (0.03, 0.03, 0.025), "M_SteelDark"))
    parts.append(cylinder("Gas_Hose", (0.20, 0.12, 0.62), (0.33, 0.35, 0.10), 0.009, "M_Rubber", sides=6))
    # Nồi canh inox lớn có nắp, quai và vá múc.
    parts.append(lathe("SoupPot", (0, 0, 0.80), (0, 0, 1),
                       [(0, 0), (0, 0.17), (0.28, 0.17), (0.29, 0.18), (0.29, 0.155), (0.02, 0.155), (0.02, 0)], "M_StainlessSteel", sides=22))
    parts.append(lathe("SoupPot_Lid", (0, 0, 1.09), (0, 0, 1), [(0, 0.175), (0.03, 0.15), (0.05, 0.03), (0.07, 0.03), (0.07, 0)], "M_StainlessSteel", sides=22))
    for sgn in (-1, 1):
        parts.append(box("SoupPot_Handle", (sgn * 0.195, 0, 1.03), (0.05, 0.03, 0.02), "M_SteelDark"))
    parts.append(cylinder("Ladle_Handle", (0.06, -0.05, 1.08), (0.12, -0.10, 1.34), 0.008, "M_StainlessSteel", sides=6))
    # Kệ inox thấp phía sau với nồi cơm điện công nghiệp.
    sx, sy = 0, 0.62
    parts.append(box("RiceStand_Top", (sx, sy, 0.62), (0.52, 0.48, 0.025), "M_StainlessSteel", edge=0.006))
    parts.append(box("RiceStand_Shelf", (sx, sy, 0.18), (0.48, 0.44, 0.015), "M_StainlessSteel"))
    for dx in (-0.23, 0.23):
        for dy in (-0.20, 0.20):
            parts.append(box("RiceStand_Leg", (sx + dx, sy + dy, 0.30), (0.03, 0.03, 0.60), "M_StainlessSteel"))
    parts.append(lathe("RiceCooker", (sx, sy, 0.635), (0, 0, 1),
                       [(0, 0), (0, 0.19), (0.02, 0.20), (0.28, 0.20), (0.31, 0.18), (0.34, 0.09), (0.35, 0)], "M_StainlessSteel", sides=20))
    parts.append(box("RiceCooker_Panel", (sx, sy - 0.20, 0.72), (0.12, 0.012, 0.07), "M_ScreenBlack"))
    parts.append(lathe("RiceCooker_Knob", (sx, sy, 0.985), (0, 0, 1), [(0, 0.03), (0.03, 0.03), (0.03, 0)], "M_SteelDark", sides=10))
    # Chồng đĩa nhựa trắng và rổ muỗng trên tầng dưới của kệ.
    for k in range(8):
        parts.append(lathe(f"Plate{k}", (sx - 0.1, sy, 0.19 + k * 0.012), (0, 0, 1), [(0, 0), (0, 0.10), (0.012, 0.11), (0.012, 0)], "M_PlasticWhite", sides=16))
    parts.append(box("SpoonBasket", (sx + 0.13, sy, 0.24), (0.16, 0.12, 0.10), "M_PlasticGreen", edge=0.01))

    shop = join_objects(parts, "Night2_RiceShop")
    base.finalize_asset(shop, pivot_at=(0, 0, 0))
    return shop


def build_milk_tea():
    base.clear_scene()
    r0, r1, h = 0.030, 0.045, 0.160  # ly 700 ml: đáy 6 cm, miệng 9 cm, cao 16 cm
    cup = lathe("Cup", (0, 0, 0), (0, 0, 1), [(0, 0), (0, r0), (h, r1), (h, r1 - 0.002), (0.004, r0 - 0.002), (0.004, 0)], "M_Night2CartGlass", sides=24)
    tea = lathe("Tea", (0, 0, 0.004), (0, 0, 1), [(0, 0), (0, r0 - 0.003), (0.136, r1 - 0.005), (0.136, 0)], "M_MilkTeaLiquid", sides=24)
    pearls = []
    for k in range(14):
        a = k * 2.399
        rr = 0.004 + 0.016 * ((k * 7) % 5) / 4  # stays inside the 3 cm bottom radius
        pearls.append(lathe(f"Pearl{k}", (rr * math.cos(a), rr * math.sin(a), 0.010 + 0.007 * (k % 3)), (0, 0, 1),
                            [(0, 0), (0, 0.004), (0.006, 0.0065), (0.012, 0.004), (0.013, 0)], "M_PaperWhite", sides=8))
    lid = lathe("Lid", (0, 0, h), (0, 0, 1), [(0, r1 + 0.003), (0.006, r1 + 0.002), (0.012, r1 - 0.006), (0.016, 0.02), (0.018, 0)], "M_Night2CartGlass", sides=24)
    straw = cylinder("Straw", (0.012, 0.004, 0.02), (0.022, 0.008, h + 0.075), 0.0055, "M_PlasticRed", sides=10)
    body = join_objects([cup, tea, *pearls, lid, straw], "Night2_MilkTea")
    base.finalize_asset(body, pivot_at=(0, 0, 0))

    # Giấy dán cong theo thân ly, hướng ra phía người chơi (-Y Blender = +Z Unity), UV phủ đủ ảnh 512 x 512.
    cols, arc, z0, z1 = 12, math.radians(90), 0.045, 0.110
    verts, faces, uvs = [], [], []
    for j, z in enumerate((z0, z1)):
        rad = r0 + (r1 - r0) * z / h + 0.0012
        for i in range(cols + 1):
            t = i / cols
            a = -math.pi / 2 - arc / 2 + arc * t
            verts.append((rad * math.cos(a), rad * math.sin(a), z))
            uvs.append((t, j))
    for i in range(cols):
        faces.append((i, i + 1, cols + 2 + i, cols + 1 + i))
    sticker = base.mesh("Night2_MilkTea_Sticker", verts, faces, "M_PaperWhite")
    layer = sticker.data.uv_layers.new(name="UVMap")
    for poly in sticker.data.polygons:
        for li, vi in zip(poly.loop_indices, poly.vertices):
            layer.data[li].uv = uvs[vi]
    sticker.data.materials.clear()
    sticker.data.materials.append(bpy.data.materials.get("M_MilkTeaSticker") or bpy.data.materials.new("M_MilkTeaSticker"))
    sticker.parent = body
    return body


def build_lien_table():
    base.clear_scene()
    parts = [box("Top", (0, 0, 0.445), (0.48, 0.48, 0.03), "M_PlasticBlue", edge=0.01)]
    parts.append(box("Apron", (0, 0, 0.41), (0.44, 0.44, 0.05), "M_PlasticBlue", edge=0.006))
    for dx in (-0.2, 0.2):
        for dy in (-0.2, 0.2):
            parts.append(cylinder("Leg", (dx * 1.1, dy * 1.1, 0.0), (dx, dy, 0.40), 0.018, "M_PlasticBlue", sides=10))
            parts.append(cylinder("Foot", (dx * 1.1, dy * 1.1, 0.0), (dx * 1.1, dy * 1.1, 0.012), 0.022, "M_Rubber", sides=10))
    parts.append(box("Brace", (0, 0, 0.12), (0.44, 0.02, 0.02), "M_PlasticBlue"))
    parts.append(box("Brace2", (0, 0, 0.12), (0.02, 0.44, 0.02), "M_PlasticBlue"))
    table = join_objects(parts, "Night2_LienTable")
    base.finalize_asset(table)
    return table


def triangles(obj):
    return sum(len(p.vertices) - 2 for o in [obj, *obj.children] for p in o.data.polygons)


def main():
    render = "--render" in sys.argv
    for stem, builder in (("Night2_RiceShop", build_rice_shop), ("Night2_MilkTea", build_milk_tea), ("Night2_LienTable", build_lien_table)):
        obj = builder()
        tris = triangles(obj)
        assert tris <= LIMITS[stem], f"{stem} has {tris} triangles, limit {LIMITS[stem]}"
        dims = obj.dimensions
        print(f"[MODEL] {stem} tris={tris} size=({dims.x:.3f}, {dims.z:.3f}, {dims.y:.3f})")
        base.export_fbx(obj, base.DEST / f"{stem}.fbx")
        if render:
            base.render_preview(obj, stem)
    print("[DONE] Night 2 evening props")


if __name__ == "__main__":
    main()
