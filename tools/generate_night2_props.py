r"""Generate realistic props for Night 2:
1. Night2_BreadCart (Xe bánh mì đầu hẻm của chú Hùng)
2. Night2_Shelf (Kệ nhựa 4 tầng lắp ráp)
3. HomewareShop_ShelfKit_Box (Hộp đóng gói kệ nhựa)
4. Night2_ClampLamp (Đèn bàn kẹp có bóng phát sáng)

Run with Blender 5.2+:
  & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python tools/generate_night2_props.py -- --render
"""

import argparse
import hashlib
import json
import math
import os
import re
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "game/Assets/Art/Models/Props"
MATERIALS = ROOT / "game/Assets/Art/Materials"
QA = ROOT / "tools/out/night2_props"
TAU = math.tau
REPORT = []


def material(name):
    """Read existing Unity colour; never rename or break Unity material mapping."""
    old = bpy.data.materials.get(name)
    if old:
        return old
    path = MATERIALS / (name + ".mat")
    assert path.is_file(), f"Missing material: {path}"
    content = path.read_text(encoding="utf-8")
    match = re.search(r"_BaseColor: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)", content)
    assert match, f"Cannot parse _BaseColor in {path}"
    color = tuple(float(v) for v in match.groups())
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    if shader:
        shader.inputs["Base Color"].default_value = (*color, 1)
        metal = name in {"M_StainlessSteel", "M_SteelDark", "M_AluminumFrame", "M_MetalHandle"}
        shader.inputs["Metallic"].default_value = 0.85 if metal else 0.0
        shader.inputs["Roughness"].default_value = 0.28 if metal else 0.72
        if name == "M_Night2CartGlass":
            # Match the Toon shader's constant clipping mask + transparency tweak.
            tweak = re.search(r"_Tweak_transparency: ([-\d.]+)", content)
            assert tweak, "Cart glass is missing its transparency setting"
            opacity = 1 + float(tweak.group(1))
            assert 0 < opacity < 0.3, "Cart glass must remain see-through"
            mat.diffuse_color = (*color, opacity)
            shader.inputs["Alpha"].default_value = opacity
            shader.inputs["Roughness"].default_value = 0.15
            mat.surface_render_method = "DITHERED"
        elif name in {"M_GlassFrosted", "M_GlassDark"}:
            shader.inputs["Roughness"].default_value = 0.15
            shader.inputs["Transmission Weight"].default_value = 0.9 if "Transmission Weight" in shader.inputs else 0.0
        if name in {"M_BulbWarmGlow", "M_TubeLightGlow"}:
            if "Emission Color" in shader.inputs:
                shader.inputs["Emission Color"].default_value = (1.0, 0.75, 0.38, 1.0)
            if "Emission Strength" in shader.inputs:
                shader.inputs["Emission Strength"].default_value = 3.0
    return mat


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.textures, bpy.data.images):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def mesh(name, vertices, faces, mat_name, smooth=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(material(mat_name))
    for p in data.polygons:
        p.use_smooth = smooth
    return obj


def activate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def bevel(obj, width, segments=1):
    activate(obj)
    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def box(name, center, size, mat_name, edge=0):
    x, y, z = center
    a, b, c = [v / 2 for v in size]
    verts = [
        (x + i * a, y + j * b, z + k * c)
        for k in (-1, 1)
        for j in (-1, 1)
        for i in (-1, 1)
    ]
    faces = [
        (0, 2, 3, 1), (4, 5, 7, 6), (0, 1, 5, 4),
        (2, 6, 7, 3), (0, 4, 6, 2), (1, 3, 7, 5)
    ]
    obj = mesh(name, verts, faces, mat_name)
    if edge > 0:
        bevel(obj, min(edge, min(size) * 0.45))
    return obj


def cylinder(name, a, b, radius, mat_name, sides=12):
    a, b = Vector(a), Vector(b)
    d = b - a
    length = d.length
    if length < 1e-6:
        return None
    d_norm = d.normalized()
    u = Vector((0, 0, 1))
    if abs(d_norm.dot(u)) > 0.99:
        u = Vector((0, 1, 0))
    v1 = d_norm.cross(u).normalized() * radius
    v2 = d_norm.cross(v1).normalized() * radius

    verts = []
    # Ring A
    for i in range(sides):
        angle = TAU * i / sides
        p = a + v1 * math.cos(angle) + v2 * math.sin(angle)
        verts.append(tuple(p))
    # Ring B
    for i in range(sides):
        angle = TAU * i / sides
        p = b + v1 * math.cos(angle) + v2 * math.sin(angle)
        verts.append(tuple(p))

    faces = []
    # Side quads
    for i in range(sides):
        nxt = (i + 1) % sides
        faces.append((i, nxt, sides + nxt, sides + i))
    # Cap A
    faces.append(tuple(reversed(range(sides))))
    # Cap B
    faces.append(tuple(range(sides, 2 * sides)))

    return mesh(name, verts, faces, mat_name)


def join_objects(objs, new_name):
    valid = [o for o in objs if o is not None]
    if not valid:
        return None
    activate(valid[0])
    for o in valid[1:]:
        o.select_set(True)
    bpy.ops.object.join()
    res = bpy.context.view_layer.objects.active
    res.name = new_name
    return res


def finalize_asset(root_obj, ensure_floor_zero=True, center_xz=True, pivot_at=None):
    """Clean normals, triangulate n-gons, UV unwrap, adjust pivot, check constraints."""
    activate(root_obj)
    
    # Triangulate to avoid n-gons and ensure clean tangents
    mod_tri = root_obj.modifiers.new("Triangulate", "TRIANGULATE")
    mod_tri.keep_custom_normals = True
    bpy.ops.object.modifier_apply(modifier=mod_tri.name)
    
    # Apply all transforms
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # UV Unwrap
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.008)
    bpy.ops.object.mode_set(mode="OBJECT")
    root_obj.data.update()

    # Bounding box in world coordinates
    verts = [root_obj.matrix_world @ v.co for v in root_obj.data.vertices]
    min_x = min(v.x for v in verts)
    max_x = max(v.x for v in verts)
    min_y = min(v.y for v in verts)
    max_y = max(v.y for v in verts)
    min_z = min(v.z for v in verts)
    max_z = max(v.z for v in verts)

    offset = Vector((0, 0, 0))
    if pivot_at is not None:
        offset = Vector(pivot_at)
    else:
        if center_xz:
            offset.x = (min_x + max_x) / 2
            offset.y = (min_y + max_y) / 2
        if ensure_floor_zero:
            offset.z = min_z  # In Blender Z is up

    if offset.length > 1e-6:
        for v in root_obj.data.vertices:
            v.co -= offset
        root_obj.data.update()

    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return root_obj


# ==============================================================================
# Model 1: Night2_BreadCart (Xe bánh mì Sài Gòn đầu hẻm)
# ==============================================================================
def build_bread_cart():
    clear_scene()
    parts = []

    # Overall size: X=1.36m, Y=0.66m, Z=1.86m (Blender coords: X=width, Y=depth, Z=height)
    # Bottom chassis / cabinet: Z from 0.18 to 0.88 (Height 0.70m, width 1.30m, depth 0.62m)
    body = box("Cart_Chassis", (0, 0, 0.53), (1.30, 0.62, 0.70), "M_StainlessSteel", edge=0.012)
    parts.append(body)

    # Cabinet front panel trim / seams
    panel_left = box("Panel_L", (-0.32, -0.312, 0.53), (0.60, 0.008, 0.64), "M_AluminumFrame")
    panel_right = box("Panel_R", (0.32, -0.312, 0.53), (0.60, 0.008, 0.64), "M_AluminumFrame")
    parts.extend([panel_left, panel_right])

    # Countertop overhang rim at Z = 0.88: thickness 0.03m, width 1.36m, depth 0.68m
    countertop = box("Countertop", (0, 0, 0.895), (1.36, 0.68, 0.03), "M_StainlessSteel", edge=0.008)
    parts.append(countertop)

    # Glass display showcase on top: Z from 0.91 to 1.56 (Height 0.65m, width 1.26m, depth 0.44m)
    # Placed slightly towards front (-Y in Blender, Y in Unity)
    # Aluminum corner posts
    cx, cy, cz = 0, -0.08, 1.235
    gw, gd, gh = 1.26, 0.44, 0.65
    for dx in (-gw / 2 + 0.015, gw / 2 - 0.015):
        for dy in (-gd / 2 + 0.015, gd / 2 - 0.015):
            parts.append(box("Glass_Post", (cx + dx, cy + dy, cz), (0.025, 0.025, gh), "M_AluminumFrame"))

    # Top aluminum frame of showcase
    parts.append(box("Glass_TopFrame", (cx, cy, cz + gh / 2 - 0.01), (gw, gd, 0.02), "M_AluminumFrame"))
    # Middle glass shelf
    parts.append(box("Glass_MidShelf", (cx, cy, cz - 0.05), (gw - 0.04, gd - 0.04, 0.008), "M_Night2CartGlass"))

    # Glass panes (Front, Left, Right, Top)
    parts.append(box("Glass_Front", (cx, cy - gd / 2 + 0.005, cz), (gw - 0.04, 0.006, gh - 0.03), "M_Night2CartGlass"))
    parts.append(box("Glass_Left", (cx - gw / 2 + 0.005, cy, cz), (0.006, gd - 0.04, gh - 0.03), "M_Night2CartGlass"))
    parts.append(box("Glass_Right", (cx + gw / 2 - 0.005, cy, cz), (0.006, gd - 0.04, gh - 0.03), "M_Night2CartGlass"))
    parts.append(box("Glass_Top", (cx, cy, cz + gh / 2 - 0.015), (gw - 0.04, gd - 0.04, 0.006), "M_Night2CartGlass"))

    # Overhead curved canopy roof: Z from 1.75 to 1.86, arched
    # Roof posts (4 vertical tubular struts from chassis/countertop to canopy)
    for px in (-0.62, 0.62):
        for py in (-0.28, 0.28):
            parts.append(cylinder("Roof_Strut", (px, py, 0.91), (px, py, 1.76), 0.014, "M_StainlessSteel", sides=8))

    # Curved canopy roof sheet
    canopy = box("Roof_Canopy", (0, 0, 1.80), (1.42, 0.74, 0.025), "M_TinRoof", edge=0.006)
    parts.append(canopy)
    canopy_trim = box("Roof_Trim", (0, 0, 1.79), (1.44, 0.76, 0.012), "M_StainlessSteel")
    parts.append(canopy_trim)

    # Push handlebar at back (+Y side)
    parts.append(cylinder("Handle_Bar", (-0.55, 0.38, 0.88), (0.55, 0.38, 0.88), 0.015, "M_StainlessSteel", sides=10))
    parts.append(cylinder("Handle_L", (-0.55, 0.31, 0.88), (-0.55, 0.38, 0.88), 0.014, "M_StainlessSteel", sides=8))
    parts.append(cylinder("Handle_R", (0.55, 0.31, 0.88), (0.55, 0.38, 0.88), 0.014, "M_StainlessSteel", sides=8))

    # Wheels at bottom:
    # 2 Large spoke front wheels at Z=0.15 (diameter 0.30m, thickness 0.04m, placed at X=-0.58 and +0.58, Y=-0.18)
    for wx in (-0.58, 0.58):
        parts.append(cylinder("Wheel_Tire", (wx, -0.18, 0.15), (wx + 0.035, -0.18, 0.15), 0.14, "M_Rubber", sides=16))
        parts.append(cylinder("Wheel_Rim", (wx - 0.002, -0.18, 0.15), (wx + 0.037, -0.18, 0.15), 0.10, "M_StainlessSteel", sides=12))
        parts.append(cylinder("Wheel_Hub", (wx - 0.01, -0.18, 0.15), (wx + 0.045, -0.18, 0.15), 0.025, "M_StainlessSteel", sides=8))
    # Axle between large wheels
    parts.append(cylinder("Front_Axle", (-0.57, -0.18, 0.15), (0.57, -0.18, 0.15), 0.012, "M_StainlessSteel", sides=8))

    # 2 Caster swivel wheels at rear (Y=+0.20, Z=0.08, diameter 0.10m)
    for rx in (-0.48, 0.48):
        parts.append(cylinder("Caster_Mount", (rx, 0.20, 0.18), (rx, 0.20, 0.12), 0.018, "M_StainlessSteel", sides=8))
        parts.append(cylinder("Caster_Wheel", (rx - 0.02, 0.20, 0.06), (rx + 0.02, 0.20, 0.06), 0.055, "M_Rubber", sides=12))

    # Food props inside glass showcase:
    # 3 French baguettes (Bánh mì vàng giòn)
    for bi, bx in enumerate((-0.35, -0.20, -0.05)):
        parts.append(cylinder(f"Baguette_{bi}", (bx, -0.08, 0.96), (bx + 0.12, -0.04, 0.99), 0.032, "M_FoodBrown", sides=10))

    # Pork bologna roll (Cây chả lụa lá chuối xanh)
    parts.append(cylinder("Cha_Lua", (0.22, -0.12, 0.94), (0.22, 0.06, 0.94), 0.048, "M_FoodGreen", sides=12))

    # Condiment glass jars with red/yellow lids
    parts.append(cylinder("Sauce_Jar_1", (0.42, -0.12, 0.93), (0.42, -0.12, 1.05), 0.038, "M_Night2CartGlass", sides=12))
    parts.append(cylinder("Sauce_Lid_1", (0.42, -0.12, 1.05), (0.42, -0.12, 1.08), 0.040, "M_PlasticRed", sides=12))
    parts.append(cylinder("Sauce_Jar_2", (0.42, 0.02, 0.93), (0.42, 0.02, 1.04), 0.035, "M_Night2CartGlass", sides=12))
    parts.append(cylinder("Sauce_Lid_2", (0.42, 0.02, 1.04), (0.42, 0.02, 1.07), 0.037, "M_FoodYellow", sides=12))

    cart = join_objects(parts, "Night2_BreadCart")
    finalize_asset(cart, ensure_floor_zero=True, center_xz=True)
    return cart


# ==============================================================================
# Model 2: Night2_Shelf (Kệ nhựa ráp 4 tầng phòng 7)
# ==============================================================================
def build_shelf():
    clear_scene()
    tiers = [[] for _ in range(4)]

    # Dimensions: Width 0.48m, Depth 0.32m, Total height 1.02m (4 tiers)
    tier_height = 0.25
    tw, td, th = 0.48, 0.32, 0.045  # tray size
    leg_r = 0.016

    for tier in range(4):
        parts = tiers[tier]
        tz = tier * tier_height + 0.06
        # Tray base
        tray_bottom = box(f"Tray_Bottom_{tier}", (0, 0, tz), (tw - 0.02, td - 0.02, 0.012), "M_PlasticBlue")
        parts.append(tray_bottom)
        # Perimeter lip walls (Front, Back, Left, Right)
        parts.append(box(f"Lip_F_{tier}", (0, -td / 2 + 0.008, tz + th / 2), (tw, 0.016, th), "M_PlasticBlue", edge=0.003))
        parts.append(box(f"Lip_B_{tier}", (0, td / 2 - 0.008, tz + th / 2), (tw, 0.016, th), "M_PlasticBlue", edge=0.003))
        parts.append(box(f"Lip_L_{tier}", (-tw / 2 + 0.008, 0, tz + th / 2), (0.016, td - 0.032, th), "M_PlasticBlue", edge=0.003))
        parts.append(box(f"Lip_R_{tier}", (tw / 2 - 0.008, 0, tz + th / 2), (0.016, td - 0.032, th), "M_PlasticBlue", edge=0.003))

        # Corner leg sleeves
        for lx in (-tw / 2 + 0.025, tw / 2 - 0.025):
            for ly in (-td / 2 + 0.025, td / 2 - 0.025):
                parts.append(cylinder(f"Sleeve_{tier}", (lx, ly, tz - 0.01), (lx, ly, tz + th + 0.01), leg_r + 0.004, "M_PlasticBlue", sides=10))

        # Connecting legs to next tier or feet
        if tier < 3:
            for lx in (-tw / 2 + 0.025, tw / 2 - 0.025):
                for ly in (-td / 2 + 0.025, td / 2 - 0.025):
                    parts.append(cylinder(f"Leg_{tier}", (lx, ly, tz + th), (lx, ly, tz + tier_height), leg_r, "M_PlasticWhite", sides=10))

    # Base feet belong to the bottom tier
    parts = tiers[0]
    for lx in (-tw / 2 + 0.025, tw / 2 - 0.025):
        for ly in (-td / 2 + 0.025, td / 2 - 0.025):
            parts.append(cylinder("Foot", (lx, ly, 0.0), (lx, ly, 0.06), leg_r + 0.003, "M_Rubber", sides=10))

    root = bpy.data.objects.new("Night2_Shelf", None)
    bpy.context.collection.objects.link(root)
    # Geometry is already centred on XY with feet at Z=0, so a zero pivot keeps tiers aligned.
    for i, parts in enumerate(tiers):
        tier_obj = join_objects(parts, f"Tier_{i + 1}")
        finalize_asset(tier_obj, pivot_at=(0, 0, 0))
        tier_obj.parent = root
    return root


# ==============================================================================
# Model 3: HomewareShop_ShelfKit_Box (Hộp đóng gói kệ nhựa)
# ==============================================================================
def build_shelf_box():
    clear_scene()
    parts = []

    # Box dimensions: 0.50m x 0.35m x 0.22m
    carton = box("Carton_Body", (0, 0, 0.11), (0.50, 0.35, 0.22), "M_Cardboard", edge=0.008)
    parts.append(carton)

    # Sealing packing tape across top and flaps
    tape_top = box("Tape_Top", (0, 0, 0.221), (0.502, 0.05, 0.002), "M_TapeClear")
    parts.append(tape_top)
    tape_side1 = box("Tape_Side1", (-0.251, 0, 0.17), (0.002, 0.05, 0.10), "M_TapeClear")
    tape_side2 = box("Tape_Side2", (0.251, 0, 0.17), (0.002, 0.05, 0.10), "M_TapeClear")
    parts.extend([tape_side1, tape_side2])

    # Shipping product illustration label on top
    label = box("Product_Label", (0.08, -0.04, 0.2215), (0.22, 0.16, 0.001), "M_PaperWhite")
    parts.append(label)

    box_obj = join_objects(parts, "HomewareShop_ShelfKit_Box")
    finalize_asset(box_obj, ensure_floor_zero=True, center_xz=True)
    return box_obj


# ==============================================================================
# Model 4: Night2_ClampLamp (Đèn bàn kẹp tay gập kiểu L511S: chao chuông đen, tay đôi, lò xo)
# ==============================================================================
def lathe(name, origin, axis, profile, mat_name, sides=20, smooth=True):
    """Revolve (distance along axis, radius) points; radius 0 collapses to a pole vertex."""
    axis = Vector(axis).normalized()
    u = axis.cross(Vector((1, 0, 0)) if abs(axis.x) < 0.9 else Vector((0, 1, 0))).normalized()
    v = axis.cross(u)
    verts, rings = [], []
    for d, r in profile:
        c = Vector(origin) + axis * d
        if r < 1e-6:
            rings.append([len(verts)])
            verts.append(tuple(c))
            continue
        rings.append(list(range(len(verts), len(verts) + sides)))
        verts += [tuple(c + (u * math.cos(TAU * s / sides) + v * math.sin(TAU * s / sides)) * r) for s in range(sides)]
    faces = []
    for a, b in zip(rings, rings[1:]):
        for s in range(sides):
            n = (s + 1) % sides
            if len(a) == 1:
                faces.append((a[0], b[s], b[n]))
            elif len(b) == 1:
                faces.append((a[s], b[0], a[n]))
            else:
                faces.append((a[s], b[s], b[n], a[n]))
    return mesh(name, verts, faces, mat_name, smooth=smooth)


def side_plate(name, points_yz, x, thick, mat_name):
    """Flat convex bracket plate in the arm plane (YZ), extruded along X."""
    cy = sum(p[0] for p in points_yz) / len(points_yz)
    cz = sum(p[1] for p in points_yz) / len(points_yz)
    pts = sorted(points_yz, key=lambda p: math.atan2(p[1] - cz, p[0] - cy))
    n = len(pts)
    verts = [(x - thick / 2, y, z) for y, z in pts] + [(x + thick / 2, y, z) for y, z in pts]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    return mesh(name, verts, faces, mat_name)


def polyline(name, points, radius, mat_name, sides=6):
    return [cylinder(f"{name}_{i}", a, b, radius, mat_name, sides=sides) for i, (a, b) in enumerate(zip(points, points[1:]))]


def helix_spring(name, a, b, coil_r, wire_r, turns, x_off):
    """Tension spring between a and b (offset beside the arm along X) with straight end hooks."""
    a, b = Vector(a) + Vector((x_off, 0, 0)), Vector(b) + Vector((x_off, 0, 0))
    axis = (b - a).normalized()
    u = Vector((1, 0, 0))
    v = axis.cross(u).normalized()
    s0, s1 = a + axis * 0.010, b - axis * 0.010
    steps = turns * 8
    pts = [s0 + (s1 - s0) * (i / steps) + (u * math.cos(TAU * i / 8) + v * math.sin(TAU * i / 8)) * coil_r for i in range(steps + 1)]
    return polyline(name, [a, *pts, b], wire_r, "M_StainlessSteel", sides=5)


def arm_pair(name, p, q, gap, rod_r):
    """Two parallel rods p->q offset in the arm plane (side-view parallelogram like the photo)."""
    p, q = Vector(p), Vector(q)
    d = (q - p).normalized()
    n = Vector((0, -d.z, d.y)) * (gap / 2)
    return [cylinder(f"{name}_RodA", p + n, q + n, rod_r, "M_SteelDark", sides=10),
            cylinder(f"{name}_RodB", p - n, q - n, rod_r, "M_SteelDark", sides=10)], n


def joint(name, center, ends, half_w=0.007):
    """Bracket plates on both X sides of a joint plus one chrome pivot pin per rod end."""
    c = Vector(center)
    pad = [c + (e - c).normalized() * ((e - c).length + 0.007) for e in ends]
    yz = [(p.y, p.z) for p in pad] + [(c.y, c.z + 0.012), (c.y, c.z - 0.012)]
    parts = [side_plate(f"{name}_Plate{i}", yz, s * half_w, 0.003, "M_SteelDark") for i, s in enumerate((-1, 1))]
    for i, e in enumerate(ends):
        parts.append(cylinder(f"{name}_Pin{i}", (-half_w - 0.003, e.y, e.z), (half_w + 0.003, e.y, e.z), 0.0024, "M_StainlessSteel", sides=8))
    return parts


ROD_GAP, ROD_R = 0.022, 0.0035
LAMP_BASE = Vector((0, 0.028, 0.050))
LAMP_ELBOW = Vector((0, -0.02, 0.33))
LAMP_HEAD = Vector((0, 0.20, 0.47))
SHADE_DIR = Vector((0, math.cos(math.radians(40)), -math.sin(math.radians(40))))
SHADE_ORIGIN = LAMP_HEAD + Vector((0, 0.035, -0.030))  # where the rear cap meets the bell
BULB_TIP_D = 0.070
BULB_TIP = SHADE_ORIGIN + SHADE_DIR * BULB_TIP_D


def build_clamp_lamp():
    clear_scene()
    parts = []

    # PIVOT AT NGÀM KẸP: upper jaw top-inner surface (0, 0.028, 0) -> (0, 0, 0) after finalize.
    parts.append(box("Clamp_UpperJaw", (0, 0.028, 0.004), (0.045, 0.055, 0.008), "M_SteelDark", edge=0.002))
    parts.append(box("Clamp_Spine", (0, 0.004, -0.024), (0.045, 0.008, 0.056), "M_SteelDark", edge=0.002))
    parts.append(box("Clamp_LowerJaw", (0, 0.025, -0.051), (0.045, 0.050, 0.008), "M_SteelDark", edge=0.002))
    parts.append(cylinder("Clamp_Screw", (0, 0.028, -0.085), (0, 0.028, -0.025), 0.006, "M_StainlessSteel", sides=10))
    parts.append(box("Clamp_Wingnut", (0, 0.028, -0.088), (0.038, 0.012, 0.008), "M_StainlessSteel", edge=0.002))
    parts.append(cylinder("Clamp_Disc", (0, 0.028, -0.025), (0, 0.028, -0.022), 0.014, "M_StainlessSteel", sides=12))
    # Swivel socket boss + pin (the short post on the photo's base).
    parts.append(cylinder("Clamp_Boss", (0, 0.028, 0.008), (0, 0.028, 0.030), 0.012, "M_SteelDark", sides=14))
    parts.append(cylinder("Clamp_SwivelPin", (0, 0.028, 0.030), (0, 0.028, 0.040), 0.007, "M_StainlessSteel", sides=10))

    # Scissor arm: base -> elbow (leans back) -> head (reaches forward over the desk).
    lower, n1 = arm_pair("LowerArm", LAMP_BASE, LAMP_ELBOW, ROD_GAP, ROD_R)
    upper, n2 = arm_pair("UpperArm", LAMP_ELBOW, LAMP_HEAD, ROD_GAP, ROD_R)
    parts += lower + upper
    parts += joint("BaseHinge", LAMP_BASE, [LAMP_BASE + n1, LAMP_BASE - n1])
    parts.append(side_plate("BaseHinge_Fork", [(0.016, 0.036), (0.040, 0.036), (0.040, 0.058), (0.016, 0.058)], 0, 0.012, "M_SteelDark"))
    parts += joint("Elbow", LAMP_ELBOW, [LAMP_ELBOW + n1, LAMP_ELBOW - n1, LAMP_ELBOW + n2, LAMP_ELBOW - n2])
    parts += joint("HeadHinge", LAMP_HEAD, [LAMP_HEAD + n2, LAMP_HEAD - n2])
    # Butterfly tension knob on the base hinge.
    parts.append(cylinder("BaseKnob_Stem", (0.008, LAMP_BASE.y, LAMP_BASE.z), (0.022, LAMP_BASE.y, LAMP_BASE.z), 0.003, "M_StainlessSteel", sides=8))
    parts.append(box("BaseKnob_Wings", (0.024, LAMP_BASE.y, LAMP_BASE.z), (0.004, 0.026, 0.008), "M_StainlessSteel", edge=0.001))

    # Tension springs running diagonally across each arm (as on the photo).
    d1 = (LAMP_ELBOW - LAMP_BASE).normalized()
    parts += helix_spring("LowerSpring", LAMP_BASE - n1 + d1 * 0.03, LAMP_BASE + n1 + d1 * 0.15, 0.0035, 0.0008, 14, 0.012)
    d2 = (LAMP_HEAD - LAMP_ELBOW).normalized()
    parts += helix_spring("UpperSpring", LAMP_ELBOW + n2 + d2 * 0.03, LAMP_ELBOW - n2 + d2 * 0.13, 0.0030, 0.0008, 12, -0.012)

    # Head swivel: short neck from the head hinge into the rear cap, with a thumb screw.
    cap_mid = SHADE_ORIGIN - SHADE_DIR * 0.022
    parts.append(cylinder("Head_Neck", LAMP_HEAD, cap_mid, 0.0045, "M_StainlessSteel", sides=10))
    parts.append(cylinder("Head_ThumbScrew", (0.010, LAMP_HEAD.y, LAMP_HEAD.z), (0.022, LAMP_HEAD.y, LAMP_HEAD.z), 0.005, "M_StainlessSteel", sides=10))

    # Rear socket cap: domed cylinder, r=22mm, ~45mm long.
    parts.append(lathe("Lamp_RearCap", SHADE_ORIGIN, SHADE_DIR,
                       [(-0.052, 0.0), (-0.050, 0.010), (-0.046, 0.016), (-0.040, 0.020), (-0.032, 0.022), (0.0, 0.022), (0.0, 0.0)],
                       "M_SteelDark", sides=20))
    # Bell shade, double-walled (1.5mm) so the inside still renders under back-face culling.
    outer = [(0.0, 0.022), (0.015, 0.024), (0.035, 0.030), (0.060, 0.042), (0.085, 0.056), (0.105, 0.065)]
    inner = [(d, r - 0.0015) for d, r in reversed(outer)]
    parts.append(lathe("Lamp_Shade", SHADE_ORIGIN, SHADE_DIR, outer + inner, "M_SteelDark", sides=24))
    bpy.ops.mesh.primitive_torus_add(major_segments=24, minor_segments=6, major_radius=0.0645, minor_radius=0.0022,
                                     location=SHADE_ORIGIN + SHADE_DIR * 0.105)
    rim = bpy.context.object
    rim.name = "Shade_Rim"
    rim.rotation_euler = Vector((0, 0, 1)).rotation_difference(SHADE_DIR).to_euler()
    rim.data.materials.append(material("M_SteelDark"))
    parts.append(rim)

    # Bulb: separate emissive child, A-shape so it reads as a bulb through the opening.
    bulb = lathe("Night2_ClampLamp_Bulb", SHADE_ORIGIN, SHADE_DIR,
                 [(0.004, 0.0), (0.004, 0.011), (0.022, 0.012), (0.040, 0.022), (0.058, 0.021), (0.066, 0.013), (BULB_TIP_D, 0.0)],
                 "M_BulbWarmGlow", sides=16)

    # Black cord: leaves the rear cap, rides along the arms with loose loops at each joint.
    x = -0.014
    cord = [cap_mid + Vector((x, -0.010, 0.012)), LAMP_HEAD + Vector((x, 0.015, 0.035)), LAMP_HEAD + Vector((x, -0.020, 0.025)),
            LAMP_HEAD + Vector((x, -0.030, -0.010)), LAMP_ELBOW + Vector((x, 0.030, 0.035)), LAMP_ELBOW + Vector((x, -0.030, 0.025)),
            LAMP_ELBOW + Vector((x, -0.035, -0.020)), LAMP_ELBOW + Vector((x, -0.010, -0.050)), LAMP_BASE + Vector((x, -0.020, 0.040)),
            Vector((x, 0.012, 0.020)), Vector((-0.02, -0.02, -0.04)), Vector((-0.03, -0.05, -0.15)), Vector((-0.02, -0.06, -0.30))]
    parts += polyline("Cord", cord, 0.0022, "M_WireBlack", sides=6)

    lamp = join_objects(parts, "Night2_ClampLamp")
    finalize_asset(lamp, ensure_floor_zero=False, center_xz=False, pivot_at=(0, 0.028, 0.0))
    finalize_asset(bulb, ensure_floor_zero=False, center_xz=False, pivot_at=(0, 0.028, 0.0))
    bulb.parent = lamp  # both have identity transforms after finalize, so no offset needed
    validate_lamp_opening(lamp)
    tip = BULB_TIP - Vector((0, 0.028, 0))
    print(f"[LAMP] bulb tip Blender {tuple(round(c, 3) for c in tip)} -> Unity ({tip.x:.3f}, {tip.z:.3f}, {-tip.y:.3f})")
    return lamp


def validate_lamp_opening(lamp):
    """The bulb must be visible from outside the shade after joining the mesh."""
    direction = SHADE_DIR
    bulb_tip = BULB_TIP - Vector((0, 0.028, 0))
    # One BVH over lamp body + child bulb; remember which object each polygon came from.
    verts, polys, owners = [], [], []
    for obj in [lamp, *lamp.children]:
        base = len(verts)
        verts += [obj.matrix_world @ v.co for v in obj.data.vertices]
        for p in obj.data.polygons:
            polys.append([base + i for i in p.vertices])
            owners.append((obj, p.material_index))
    tree = BVHTree.FromPolygons(verts, polys)
    # Sample across the opening, not only along its center line.
    for x in (-0.012, 0, 0.012):
        origin = bulb_tip + Vector((x, 0, 0)) + direction * 0.12
        _, _, index, _ = tree.ray_cast(origin, -direction)
        assert index is not None, "Lamp bulb is missing from the shade opening"
        obj, mat_index = owners[index]
        mat = obj.data.materials[mat_index]
        assert obj.name == "Night2_ClampLamp_Bulb", f"Lamp bulb obstructed by {obj.name}"
        assert mat.name == "M_BulbWarmGlow", f"Lamp bulb obstructed by {mat.name}"
    print("[CHECK] Lamp bulb visible through all three opening rays")


# ==============================================================================
# Main Execution and FBX Export
# ==============================================================================
def export_fbx(obj, path):
    DEST.mkdir(parents=True, exist_ok=True)
    activate(obj)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_tspace=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
        embed_textures=False,
    )
    print(f"[EXPORT] Saved {path.name} ({path.stat().st_size:,} bytes)")


def render_preview(obj, stem):
    QA.mkdir(parents=True, exist_ok=True)
    activate(obj)
    # Add temporary camera and light
    cam_data = bpy.data.cameras.new("QACam")
    cam_obj = bpy.data.objects.new("QACam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj

    verts = [m.matrix_world @ v.co for m in [obj, *obj.children_recursive] if m.type == "MESH" for v in m.data.vertices]
    center = sum(verts, Vector((0, 0, 0))) / len(verts)
    max_dim = max(max(v.x for v in verts) - min(v.x for v in verts),
                  max(v.y for v in verts) - min(v.y for v in verts),
                  max(v.z for v in verts) - min(v.z for v in verts))

    cam_obj.location = center + Vector((max_dim * 1.6, -max_dim * 2.0, max_dim * 1.2))
    direction = center - cam_obj.location
    rot_quat = direction.to_track_quat("-Z", "Y")
    cam_obj.rotation_euler = rot_quat.to_euler()

    # Sun light
    light_data = bpy.data.lights.new("QALight", type="SUN")
    light_data.energy = 3.5
    light_obj = bpy.data.objects.new("QALight", light_data)
    light_obj.location = center + Vector((2, -3, 5))
    bpy.context.collection.objects.link(light_obj)

    bpy.context.scene.render.resolution_x = 800
    bpy.context.scene.render.resolution_y = 600
    out_file = QA / f"{stem}.png"
    bpy.context.scene.render.filepath = str(out_file)
    bpy.ops.render.render(write_still=True)
    print(f"[RENDER] Rendered preview {out_file}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--render", action="store_true", help="Render preview images")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    models_to_build = [
        ("Night2_BreadCart", build_bread_cart),
        ("Night2_Shelf", build_shelf),
        ("HomewareShop_ShelfKit_Box", build_shelf_box),
        ("Night2_ClampLamp", build_clamp_lamp),
    ]

    for stem, builder in models_to_build:
        print(f"\n--- Generating {stem} ---")
        obj = builder()
        fbx_path = DEST / f"{stem}.fbx"
        export_fbx(obj, fbx_path)
        if args.render:
            render_preview(obj, stem)

    print("\n[DONE] All Night 2 models generated successfully!")


if __name__ == "__main__":
    main()
