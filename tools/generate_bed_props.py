r"""Generate 3 boarding house bed models based on real reference photos.

Run with Blender 5.2:
  & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python tools/generate_bed_props.py -- --render

Generates:
  1. prop_bed_bunk_iron.fbx       (Mẫu 1: Giường tầng sắt sinh viên / KTX)
  2. prop_bed_wood_slats.fbx       (Mẫu 2: Giường gỗ dát nan mộc phòng trọ)
  3. prop_bed_mattress_bamboo.fbx  (Mẫu 3: Nệm đặt sàn phủ chiếu trúc hạt nâu)
"""

import argparse
import hashlib
import json
import math
import random
import re
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Matrix, Vector
from mathutils.geometry import delaunay_2d_cdt

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "game/Assets/Art/Models/Props"
MATERIALS = ROOT / "game/Assets/Art/Materials"
QA = ROOT / "tools/out/beds"
SOURCE = ROOT / "game/Assets/Art/_Source"
TAU = math.tau
REPORT = []
RUST = "M_TinRust"
STEEL = "M_SteelDark"
MOLD = "M_WallMoldStain"
STAIN = "M_WallStainYellow"
BARK = "M_TreeBark"
WOOD_PALETTE = ("M_DoorWoodDark", "M_DoorWoodBrown", "M_FoodBrown",
                "M_ShutterGray", BARK, MOLD)


def material(name):
    """Read existing Unity colour; never create/rename a Unity material."""
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
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    metal = name in {"M_StainlessSteel", "M_SteelDark"}
    shader.inputs["Metallic"].default_value = 0.8 if metal else 0.0
    shader.inputs["Roughness"].default_value = 0.35 if metal else 0.75
    return mat


def mesh(name, vertices, faces, mat, smooth=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(material(mat))
    for p in data.polygons:
        p.use_smooth = smooth
    return obj


def activate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def bevel(obj, width, segments=1):
    activate(obj)
    mod = obj.modifiers.new("Physical edge radius", "BEVEL")
    mod.width, mod.segments = width, segments
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def box(name, center, size, mat, edge=0):
    x, y, z = center
    a, b, c = [v / 2 for v in size]
    verts = [(x+i*a, y+j*b, z+k*c) for k in (-1, 1)
             for j in (-1, 1) for i in (-1, 1)]
    obj = mesh(name, verts, [(0, 2, 3, 1), (4, 5, 7, 6), (0, 1, 5, 4),
                             (2, 6, 7, 3), (0, 4, 6, 2), (1, 3, 7, 5)], mat)
    return bevel(obj, min(edge, min(size)*0.45)) if edge else obj


def rectangular_frame(name, center, outer_size, border_width, height, mat):
    cx, cy, cz = center
    ow, od = outer_size
    iw, id_ = ow - 2 * border_width, od - 2 * border_width
    z0, z1 = cz - height / 2, cz + height / 2
    rings = []
    for z in (z0, z1):
        rings.extend([
            (cx - ow / 2, cy - od / 2, z),
            (cx + ow / 2, cy - od / 2, z),
            (cx + ow / 2, cy + od / 2, z),
            (cx - ow / 2, cy + od / 2, z),
            (cx - iw / 2, cy - id_ / 2, z),
            (cx + iw / 2, cy - id_ / 2, z),
            (cx + iw / 2, cy + id_ / 2, z),
            (cx - iw / 2, cy + id_ / 2, z),
        ])
    faces = []
    for start in (0, 8):
        for i in range(4):
            nxt = (i + 1) % 4
            faces.append((start + i, start + nxt, start + 4 + nxt, start + 4 + i))
    for i in range(4):
        nxt = (i + 1) % 4
        faces.append((i, 8 + i, 8 + nxt, nxt))
        faces.append((4 + i, 4 + nxt, 12 + nxt, 12 + i))
    return mesh(name, rings, faces, mat)


def beam(name, a, b, width, depth, mat):
    """Rectangular beam between points a and b."""
    a, b = Vector(a), Vector(b)
    d = (b - a).normalized()
    u = Vector((0, 1, 0))
    if abs(d.dot(u)) > 0.99:
        u = Vector((1, 0, 0))
    v = d.cross(u).normalized() * (width / 2)
    u = v.normalized().cross(d).normalized() * (depth / 2)
    verts = [tuple(p + i*u + j*v) for p in (a, b) for i, j in
             [(-1, -1), (1, -1), (1, 1), (-1, 1)]]
    return mesh(name, verts, [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4),
                              (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], mat)


def cylinder(name, a, b, radius, mat, sides=12, wear=(), rng=None):
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

    rings = [[0] * sides]
    worn_rings = {}
    for center, half_span, coverage in wear:
        worn_rings[len(rings)] = coverage
        rings.extend([[center + sign * half_span * rng.uniform(0.25, 1.0) for _ in range(sides)]
                      for sign in (-1, 1)])
    rings.append([1] * sides)
    verts = [tuple(p + math.cos(TAU * j / sides) * v1 + math.sin(TAU * j / sides) * v2)
             for ring in rings for j, t in enumerate(ring)
             for p in [a if t == 0 else b if t == 1 else a + d * t]]

    faces, slots = [], []
    for ring in range(len(rings) - 1):
        for j in range(sides):
            nxt = (j + 1) % sides
            i = ring * sides
            faces.append((i + j, i + nxt, i + sides + nxt, i + sides + j))
            slot = 0
            if ring in worn_rings and rng.random() < worn_rings[ring]:
                slot = rng.choices((1, 2), weights=(3, 2))[0]
            slots.append(slot)
    faces.append(tuple(reversed(range(sides))))
    faces.append(tuple(len(verts) - sides + j for j in range(sides)))
    slots.extend((0, 0))
    obj = mesh(name, verts, faces, mat, smooth=True)
    return surface_materials(obj, (mat, RUST, STEEL), slots) if wear else obj


def rounded_rect(w, d, r, z, sides=8):
    """Points of a rounded rectangle at height z with corner radius r."""
    a, b = w / 2 - r, d / 2 - r
    corners = [(a, b), (-a, b), (-a, -b), (a, -b)]
    points = []
    for idx, (cx, cy) in enumerate(corners):
        for step in range(sides):
            theta = idx * math.pi / 2 + step * (math.pi / 2) / sides
            points.append(Vector((cx + r * math.cos(theta), cy + r * math.sin(theta), z)))
    return points


def tube(name, points, radius, mat, sides=8, closed=False, reference=None):
    points = [Vector(p) for p in points]
    vertices, faces = [], []
    for i, p in enumerate(points):
        prev = points[(i - 1) % len(points)] if closed or i else points[0]
        nxt = points[(i + 1) % len(points)] if closed or i < len(points) - 1 else p
        tangent = (nxt - prev).normalized()
        normal = tangent.cross(Vector(reference or (0, 0, 1)))
        if normal.length < 0.01:
            normal = tangent.cross(Vector((0, 1, 0)))
        normal.normalize()
        other = tangent.cross(normal).normalized()
        for j in range(sides):
            vertices.append(tuple(p + radius * (normal * math.cos(TAU * j / sides)
                                                + other * math.sin(TAU * j / sides))))
    for i in range(len(points) if closed else len(points) - 1):
        k = (i + 1) % len(points)
        for j in range(sides):
            n = (j + 1) % sides
            faces.append((i * sides + j, i * sides + n, k * sides + n, k * sides + j))
    if not closed:
        faces += [tuple(reversed(range(sides))),
                  tuple((len(points) - 1) * sides + j for j in range(sides))]
    return mesh(name, vertices, faces, mat, smooth=True)


def objects():
    return [o for o in bpy.context.scene.objects if o.type == "MESH"]


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for item in list(block):
            block.remove(item)


def surface_materials(obj, names, face_slots):
    used_slots = sorted(set(face_slots))
    remap = {slot: i for i, slot in enumerate(used_slots)}
    obj.data.materials.clear()
    for slot in used_slots:
        obj.data.materials.append(material(names[slot]))
    for face, slot in zip(obj.data.polygons, face_slots, strict=True):
        face.material_index = remap[slot]
    return obj


def triangulate_inlays(vertices, corners, points, outlines, project):
    points, _, triangles, sources, _, regions = delaunay_2d_cdt(points, [], outlines, 1, 1e-7)
    indices = []
    for point, source in zip(points, sources, strict=True):
        if source[0] < len(corners):
            indices.append(corners[source[0]])
        else:
            indices.append(len(vertices))
            vertices.append(tuple(project(point)))
    return [tuple(indices[i] for i in tri) for tri in triangles], regions


def circular_arc(center, axis_u, axis_v, radius, start, end, steps=5):
    center, axis_u, axis_v = map(Vector, (center, axis_u, axis_v))
    return [center + radius * (axis_u * math.cos(angle) + axis_v * math.sin(angle))
            for angle in (math.radians(start + (end - start) * i / steps) for i in range(steps + 1))]


def fabric_grid(name, columns, rows, sample, thickness, names, face_slot=None, hem_mat=None):
    """Make a thin closed cloth shell, with optional material bands and a bound perimeter."""
    front = [Vector(sample(i / columns, j / rows)) for j in range(rows + 1) for i in range(columns + 1)]
    width = columns + 1
    count = len(front)
    back = []
    for j in range(rows + 1):
        for i in range(columns + 1):
            du = front[j * width + min(i + 1, columns)] - front[j * width + max(0, i - 1)]
            dv = front[min(j + 1, rows) * width + i] - front[max(0, j - 1) * width + i]
            back.append(front[j * width + i] - thickness * du.cross(dv).normalized())

    faces, slots = [], []
    border = [*range(width), *(j * width + columns for j in range(1, rows + 1)),
              *(rows * width + i for i in range(columns - 1, -1, -1)),
              *(j * width for j in range(rows - 1, 0, -1))]
    for j in range(rows):
        for i in range(columns):
            a = j * width + i
            quad = (a, a + 1, a + 1 + width, a + width)
            faces.extend((quad, tuple(k + count for k in reversed(quad))))
            slots.extend([face_slot(i, j) if face_slot else 0] * 2)
    for a, b in zip(border, border[1:] + border[:1]):
        faces.append((b, a, a + count, b + count))
        slots.append(0)
    obj = surface_materials(mesh(name, front + back, faces, names[0], smooth=True), names, slots)
    if hem_mat:
        corners = {0, columns, rows * width + columns, rows * width}
        outline = [front[a] for i, a in enumerate(border) if i % 2 == 0 or a in corners]
        tube(name + "_StitchedHem", outline, thickness * 1.2,
             hem_mat, sides=4, closed=True, reference=(0, 0, 1))
    return obj


def folded_linen(name, center, size, mat, phase, yaw=0, worn_mat=None):
    """Lobed perimeter, compressed lower fold and rounded upper crown."""
    cx, cy, cz = center
    w, length, height = size
    vertices, faces, slots = [], [], []
    cosine, sine = math.cos(yaw), math.sin(yaw)
    profiles = ((0.84, -0.5), (0.98, -0.29), (1.0, 0.12), (0.88, 0.5))
    for scale, level in profiles:
        outline = rounded_rect(w * scale, length * scale, min(w, length) * 0.16, 0, sides=3)
        for i, p in enumerate(outline):
            angle = TAU * i / len(outline)
            wrinkle = math.sin(3 * angle + phase) + 0.45 * math.sin(7 * angle - phase)
            bulge = 1 + 0.015 * wrinkle
            z = cz + height * (level + (0.09 * wrinkle if level > -0.5 else 0))
            px, py = p.x * bulge + 0.01 * level, p.y * bulge
            vertices.append((cx + cosine * px - sine * py, cy + sine * px + cosine * py, z))
    n = len(outline)
    for ring in range(len(profiles) - 1):
        for i in range(n):
            a, b = ring * n + i, ring * n + (i + 1) % n
            faces.append((a, b, b + n, a + n))
            slots.append(1 if ring == 1 and i % 5 == 1 else 0)
    faces.extend((tuple(reversed(range(n))), tuple(range(len(vertices) - n, len(vertices)))))
    obj = mesh(name, vertices, faces, mat, smooth=True)
    return surface_materials(obj, (mat, worn_mat), slots + [0, 0]) if worn_mat else obj


def worn_foot(x, y, rng):
    sides = 8
    angles = [TAU * i / sides for i in range(sides)]
    radius = [rng.uniform(0.022, 0.024) for _ in angles]
    heights = [rng.uniform(0.012, 0.033) for _ in angles]
    vertices = [(x + r * math.cos(a), y + r * math.sin(a), z)
                for zs in ([0] * sides, heights)
                for a, r, z in zip(angles, radius, zs)]
    faces = [(i, (i + 1) % sides, (i + 1) % sides + sides, i + sides) for i in range(sides)]
    faces.append(tuple(reversed(range(sides))))
    vertices.append((x, y, 0.018))
    faces.extend((i + sides, (i + 1) % sides + sides, len(vertices) - 1) for i in range(sides))
    mesh("SplitRubberFoot", vertices, faces, "M_Rubber")


def surface_chips(name, specs, rng, names):
    vertices, faces, slots = [], [], []
    for center, axis_u, axis_v in specs:
        start = len(vertices)
        axis_u, axis_v = Vector(axis_u), Vector(axis_v)
        steps = rng.choice((6, 7, 8))
        vertices.append(center)
        for i in range(steps):
            angle = TAU * i / steps
            shape = (axis_u * math.cos(angle) + axis_v * math.sin(angle)) * rng.uniform(0.52, 1)
            vertices.append(Vector(center) + shape)
            faces.append((start, start + i + 1, start + (i + 1) % steps + 1))
            slots.append(rng.choices((0, 1), weights=(4, 1))[0])
    return surface_materials(mesh(name, vertices, faces, names[0]), names, slots)


# ---------------------------------------------------------------------------
# MODEL 1: Giường tầng sắt sinh viên / KTX
# ---------------------------------------------------------------------------
def build_bunk_bed():
    """Dorm bunk with welded round posts, hooked ladder and lived-in textiles."""
    width, length, height = 0.94, 2.00, 1.70
    post_r = 0.021
    hx, hy = width / 2 - 0.023, length / 2 - 0.023
    low, high = 0.375, 1.345
    paint, repair_paint = "M_WallPlinthGray", "M_ShutterGray"
    red, blue, faded = "M_CanvasRed", "M_CanvasBlue", "M_ClothBlue"
    rng = random.Random(1203)

    for x in (-hx, hx):
        for y in (-hy, hy):
            cylinder("Upright", (x, y, 0.038), (x, y, height - 0.004), post_r, paint, 12,
                     wear=((0.014, 0.011, 0.9), (0.19, 0.023, 0.65), (0.785, 0.024, 0.75),
                           (0.948, 0.025, 0.5)), rng=rng)
            cylinder("RustExposedAboveFoot", (x, y, 0.006), (x, y, 0.038), post_r, RUST, 12)
            cylinder("UprightTopPlug", (x, y, height - 0.007), (x, y, height), 0.020, STEEL, 8)
            worn_foot(x, y, rng)

    for deck, z in (("Lower", low), ("Upper", high)):
        for x in (-hx, hx):
            box(deck + "LongBoxTube", (x, 0, z), (0.035, 2 * hy, 0.054), paint, edge=0.0016)
            for y in (-hy, hy):
                sign = math.copysign(1, x)
                plate_y = y - math.copysign(0.043, y)
                box(deck + "WeldedTab", (x + sign * 0.017, plate_y, z),
                    (0.004, 0.064, 0.039), RUST)
                cylinder(deck + "HexBolt", (x + sign * 0.018, plate_y, z),
                         (x + sign * 0.024, plate_y, z), 0.007, rng.choice((RUST, STEEL)), 6)
                beam(deck + "WeldBead", (x + sign * 0.017, y + 0.007, z - 0.021),
                     (x + sign * 0.020, y - 0.005, z + 0.021), 0.013, 0.007, RUST)
        for y in (-hy, hy):
            box(deck + "EndBoxTube", (0, y, z), (2 * hx, 0.033, 0.054), paint, edge=0.0016)
        slat_count = 9 if deck == "Lower" else 5
        for i in range(slat_count):
            y = -0.788 + 1.576 * i / (slat_count - 1)
            box(deck + "SteelBoxSlat", (0, y, z + 0.005), (2 * hx, 0.024, 0.038), repair_paint)

    for y in (-hy, hy):
        for deck, z, rise in (("Lower", low, 0.225), ("Upper", high, 0.277)):
            cylinder(deck + "EndRail", (-hx, y, z + rise), (hx, y, z + rise), 0.0115, paint, 10,
                     wear=((0.05, 0.03, 0.65), (0.44, 0.05, 0.45), (0.96, 0.03, 0.75)), rng=rng)
            for x in (-0.22, 0, 0.22):
                cylinder(deck + "EndUpright", (x, y, z + 0.026), (x, y, z + rise), 0.0085, paint, 6)
        for x in (-hx, hx):
            beam("LowerCornerBrace", (x, y - math.copysign(0.19, y), low - 0.025),
                 (x, y, low - 0.18), 0.023, 0.011, paint)

    # Ladder opening at the near end of the aisle-side guard.
    rail_top, bend = 1.627, 0.049
    for x, start in ((hx, -0.31), (-hx, -0.88)):
        end = 0.917
        rail_path = [
            (x, start, high + 0.026),
            *circular_arc((x, start + bend, rail_top - bend), (0, 1, 0), (0, 0, 1), bend, 180, 90, steps=3),
            *circular_arc((x, end - bend, rail_top - bend), (0, 1, 0), (0, 0, 1), bend, 90, 0, steps=3),
            (x, end, high + 0.026),
        ]
        tube("RoundedSafetyGuard", rail_path, 0.013, paint, sides=8, reference=(1, 0, 0))
        for y in (start + (end - start) * 0.37, start + (end - start) * 0.72):
            cylinder("GuardStanchion", (x, y, high + 0.026), (x, y, rail_top), 0.010, repair_paint, 8,
                     wear=((0.10, 0.08, 0.70), (0.93, 0.04, 0.6)), rng=rng)

    ladder_y = (-0.865, -0.515)
    x_foot = hx + 0.091
    x_ladder = hx + 0.054
    hook_center = hx + 0.012
    hook_z = high + 0.027
    for y in ladder_y:
        hook = circular_arc((hook_center, y, hook_z), (1, 0, 0), (0, 0, 1), 0.042, 0, 180, steps=7)
        tube("HookedLadderStringer", [(x_foot, y, 0.025), *hook, (hook_center - 0.042, y, high - 0.009)],
             0.014, paint, sides=8, reference=(0, 1, 0))
        cylinder("LadderRubberShoe", (x_foot, y, 0), (x_foot, y, 0.034), 0.0165, "M_Rubber", 8)
        cylinder("LadderHookGrip", (hook_center - 0.042, y, high - 0.012),
                 (hook_center - 0.042, y, high + 0.011), 0.015, STEEL, 8)
    for z in (0.285, 0.575, 0.865, 1.155):
        x = x_foot + (x_ladder - x_foot) * ((z - 0.025) / (hook_z - 0.025))
        cylinder("LadderRung", (x, ladder_y[0], z), (x, ladder_y[1], z), 0.0125, repair_paint, 8,
                 wear=((0.12, 0.08, 0.72), (rng.uniform(0.43, 0.58), 0.20, 0.74),
                       (0.90, 0.07, 0.82)), rng=rng)
        for y, inward in zip(ladder_y, (1, -1)):
            cylinder("LadderRungSocket", (x, y + inward * 0.010, z),
                     (x, y + inward * 0.025, z), 0.0155, RUST, 6)

    # Exposed chips on the enamel. The silhouette remains that of the steel tube.
    chips = []
    for z in (low, high):
        for sign in (-1, 1):
            centers = (-0.881, -0.752, -0.485, -0.276, -0.045, 0.122, 0.408, 0.660, 0.815)
            for y in centers:
                cz = z + rng.uniform(-0.011, 0.011)
                chips.append(((sign * (hx + 0.0178), y + rng.uniform(-0.008, 0.008), cz),
                              (0, sign * rng.uniform(0.019, 0.053), 0),
                              (0, 0, rng.uniform(0.004, 0.013))))
        for sign in (-1, 1):
            for x in (-0.345, -0.228, 0.077, 0.281, 0.367):
                chips.append(((x, sign * (hy + 0.0168), z + rng.uniform(-0.01, 0.01)),
                              (-sign * rng.uniform(0.021, 0.051), 0, 0),
                              (0, 0, rng.uniform(0.003, 0.012))))
    surface_chips("WornEnamelAlongDeckRails", chips, rng, (RUST, STEEL))

    # Blue polypropylene dust sheet suspended below the upper mattress tray.
    tarp_w, tarp_l, tie_z = 0.828, 1.86, high - 0.025

    def tarp(u, v):
        s, t = math.sin(math.pi * u), math.sin(math.pi * v)
        x = (u - 0.5) * (tarp_w - 0.046 * t) + 0.007 * t * math.sin(TAU * v)
        y = (v - 0.5) * (tarp_l - 0.023 * s)
        sag = 0.094 * s**1.35 * t**0.85 + 0.044 * t + 0.007 * s
        wrinkles = (0.012 * math.sin(TAU * (1.8 * u + 2.1 * v))
                    + 0.006 * math.sin(TAU * (2.4 * u - 4.0 * v))) * s * t
        return x, y, tie_z - sag + wrinkles

    tarp_columns, tarp_rows = 8, 13

    def tarp_slot(i, j):
        if (i == 0 and j in (4, 5, 6)) or (i in (5, 6) and j in (9, 10)):
            return 1
        return 2 if i in (0, 7) and j in (0, 8, 12) else 0

    fabric_grid("BlueDustTarp", tarp_columns, tarp_rows, tarp, 0.0016,
                (blue, faded, "M_ShutterGray"), face_slot=tarp_slot, hem_mat=faded)
    for u, rows in ((0.0, range(4, 7)), (0.625, range(9, 11)), (0.875, range(9, 11))):
        for row in rows:
            a = Vector(tarp(u, row / tarp_rows))
            b = Vector(tarp(u, (row + 1) / tarp_rows))
            for step in (0.30, 0.66):
                stitch = a.lerp(b, step)
                stitch.z -= 0.0025
                beam("TarpRepairStitch", stitch + Vector((-0.004, -0.007, 0)),
                     stitch + Vector((0.004, 0.007, 0)), 0.0025, 0.0025, STAIN)
    for u in (0, 1):
        for v in (0, 1):
            p = Vector(tarp(u, v))
            sign_x, sign_y = 2 * u - 1, 2 * v - 1
            knot = (p.x + sign_x * 0.011, p.y + sign_y * 0.012, tie_z + 0.003)
            tie_path = [p, knot, (sign_x * (hx - 0.007), p.y + sign_y * 0.016, high + 0.030),
                        (sign_x * (hx + 0.018), p.y + sign_y * 0.022, high + 0.024),
                        (sign_x * (hx + 0.020), p.y + sign_y * 0.027, tie_z - 0.006),
                        knot, (p.x + sign_x * 0.005, p.y + sign_y * 0.025, tie_z - 0.033)]
            tube("TarpCornerTie", tie_path, 0.0018, STAIN, sides=4, reference=(0, 1, 0))

    # The woven color bands share the same surface as the straw ribs and red binding.
    mat_w, mat_l, mat_z = 0.868, 1.922, low + 0.037
    straw, pale, green = "M_FoodBrown", "M_DoorWoodBrown", "M_FoodGreen"
    mat_names = (straw, pale, red, green, STAIN, BARK)
    y0, y1 = -mat_l / 2, mat_l / 2
    bands = [(y0, y0 + 0.022, 2)]
    cursor = bands[-1][1]
    for center in (-0.716, -0.431, -0.145, 0.141, 0.426, 0.712):
        y = center - 0.043
        bands.append((cursor, y, 0))
        for span, slot in ((0.016, 2), (0.009, 1), (0.023, 3), (0.008, 1), (0.015, 2)):
            bands.append((y, y + span, slot))
            y += span
        cursor = y
    bands.extend(((cursor, y1 - 0.022, 0), (y1 - 0.022, y1, 2)))
    x_inner = mat_w / 2 - 0.020
    x_steps = [-mat_w / 2, -x_inner, -0.251, -0.245, 0.071, 0.078, x_inner, mat_w / 2]
    y_steps = [bands[0][0], *(b[1] for b in bands)]
    n = len(x_steps)
    verts = [(x + (rng.uniform(-0.006, 0.004) if i in (0, n - 1) else 0), y,
              mat_z + 0.0017 * math.sin(i * 1.9 + j * 0.83)
              + (0.004 * math.sin(j * 1.4) if i in (0, n - 1) else 0))
             for j, y in enumerate(y_steps) for i, x in enumerate(x_steps)]
    faces, slots = [], []
    for j, (_, _, slot) in enumerate(bands):
        for i in range(n - 1):
            a = j * n + i
            faces.append((a, a + 1, a + 1 + n, a + n))
            if i in (0, n - 2):
                slots.append(4 if (i, j) in ((0, 6), (0, 29), (n - 2, 4), (n - 2, 30)) else 2)
            elif slot == 0 and i in (2, 4):
                slots.append(5 if j % 5 == 1 else 1)
            else:
                slots.append(slot)
    border = [*range(n), *(j * n + n - 1 for j in range(1, len(y_steps))),
              *((len(y_steps) - 1) * n + i for i in range(n - 2, -1, -1)),
              *(j * n for j in range(len(y_steps) - 2, 0, -1))]
    bottom = len(verts)
    verts.extend((verts[i][0], verts[i][1], low + 0.023) for i in border)
    for i, a in enumerate(border):
        k = (i + 1) % len(border)
        faces.append((border[k], a, bottom + i, bottom + k))
        slots.append(2)
    faces.append(tuple(reversed(range(bottom, len(verts)))))
    slots.append(0)
    surface_materials(mesh("WovenSedgeMatAndBinding", verts, faces, straw), mat_names, slots)
    for i, j in ((0, 6), (0, 29), (n - 1, 4), (n - 1, 30)):
        for _ in range(4):
            x, y, z = verts[j * n + i]
            y += rng.uniform(-0.012, 0.012)
            x += 0.004 if i == 0 else -0.004
            tail = (x + (-1 if i == 0 else 1) * rng.uniform(0.009, 0.023),
                    y + rng.uniform(-0.007, 0.007), z - rng.uniform(0.001, 0.007))
            cylinder("LooseSedgeFiber", (x, y, z - 0.001), tail, 0.0008, rng.choice((red, pale, STAIN)), 4)

    box("UpperPad", (0, 0, high + 0.041), (0.862, 1.908, 0.031), STAIN, edge=0.007)
    for z, radius in ((high + 0.032, 0.006), (high + 0.054, 0.004)):
        tube("UpperPadRedPiping", rounded_rect(0.854, 1.900, 0.036, z, sides=3),
             radius, red, sides=4, closed=True, reference=(0, 0, 1))
    folded_linen("UpperFoldedParachuteBlanket", (0.065, 0.674, high + 0.084), (0.57, 0.43, 0.065),
                 repair_paint, 1.0, yaw=-0.17, worn_mat=faded)
    folded_linen("UpperWindbreakerBody", (0.105, 0.629, high + 0.135), (0.37, 0.36, 0.068),
                 faded, 2.7, yaw=0.26, worn_mat=blue)
    for x, y, yaw in ((-0.062, 0.570, -0.81), (0.228, 0.524, 0.68)):
        folded_linen("UpperWindbreakerSleeve", (x, y, high + 0.126), (0.108, 0.29, 0.058),
                     faded, x * 6, yaw=yaw, worn_mat=blue)
    tube("UpperWindbreakerZipper", [(0.155, 0.51, high + 0.165), (0.121, 0.64, high + 0.170),
                                   (0.088, 0.761, high + 0.166)], 0.0028, STEEL, 4)

    for index, (x, y, height_, linen) in enumerate((
        (0.125, -0.658, 0.061, BARK),
        (0.112, -0.647, 0.048, faded),
        (0.155, -0.677, 0.055, STAIN),
    )):
        z = (0.031, 0.082, 0.127)[index]
        folded_linen("UnderbedFoldedLinen", (x, y, z), (0.55 - 0.032 * index, 0.42 - 0.02 * index, height_),
                     linen, 1.2 + index, yaw=(index - 1) * 0.14, worn_mat=repair_paint if index else None)

    # Hook curls over the rail; the towel passes over the wire and hangs down both sides.
    hanger_x, hanger_y = hx + 0.025, 0.443
    shoulder_z, bar_z = high - 0.084, high - 0.215
    silver = "M_StainlessSteel"
    hook = circular_arc((hx + 0.002, hanger_y, high + 0.006), (1, 0, 0), (0, 0, 1),
                        0.024, 197, 0, steps=8)
    tube("HangerHook", [*hook, (hanger_x, hanger_y, shoulder_z)], 0.0021, silver,
         sides=6, reference=(0, 1, 0))
    triangle = [(hanger_x + dx, hanger_y + dy, z) for dx, dy, z in
                ((0, 0, shoulder_z), (-0.012, -0.195, bar_z + 0.031), (-0.007, -0.205, bar_z + 0.005),
                 (0, -0.089, bar_z - 0.006), (0, 0.091, bar_z + 0.005),
                 (0.013, 0.218, bar_z - 0.011), (0.014, 0.219, bar_z + 0.012))]
    tube("HangerTriangle", triangle, 0.0021, silver, sides=6, closed=True, reference=(1, 0, 0))

    def towel(u, v):
        across = u - 0.5
        turn = 0.365
        phase = (v - turn) / (1 - turn if v >= turn else turn)
        drape = abs(phase)
        y = (hanger_y + 0.326 * across * (0.93 + 0.10 * drape) + 0.010 * phase * phase
             + 0.006 * math.sin(20 * v) * drape * abs(across))
        x = (hanger_x + 0.008 * math.tanh(phase * 15) + 0.010 * phase
             + (0.004 + 0.013 * drape) * math.sin(TAU * (2.6 * u + 0.22 * phase)))
        z = (bar_z + 0.008 - (0.275 if phase >= 0 else 0.172) * drape
             + drape**3 * (0.053 * across + 0.020 * math.sin(TAU * 2.1 * u)
                           + 0.006 * math.cos(18 * u)))
        return x, y, z

    fabric_grid("DrapedCottonTowel", 8, 13, towel, 0.0015, (STAIN, BARK, "M_DoorWoodBrown"),
                face_slot=lambda i, j: 2 if j in (0, 11) else 1 if j == 12 and i < 5 else 0)

    return {
        "nominal_m": [width, height, length],
        "style": "Dormitory steel bunk",
        "tarp_drop_m": round(tie_z - min(tarp(i / tarp_columns, j / tarp_rows)[2]
                                        for i in range(tarp_columns + 1) for j in range(tarp_rows + 1)), 4),
        "ladder_rungs": 4,
        "lower_support_slats": 9,
    }


def damp_wood_face(vertices, quad, base_slot, rng):
    faces, slots = [], []
    for i, triangle in enumerate(((quad[0], quad[1], quad[2]), (quad[0], quad[2], quad[3]))):
        a, b, c = (Vector(vertices[j]) for j in triangle)
        center = Vector((rng.uniform(0.24, 0.39), rng.uniform(0.19, 0.27)))
        points = [Vector(p) for p in ((0, 0), (1, 0), (0, 1))]
        outlines = [(0, 1, 2)]
        for scale in (1.0, 0.44):
            start = len(points)
            for k in range(9):
                angle = TAU * k / 9
                radius = rng.uniform(0.66, 1.0) * scale
                points.append(center + radius * Vector((0.18 * math.cos(angle), 0.155 * math.sin(angle))))
            outlines.append(tuple(range(start, len(points))))
        def project(p):
            return a + (b - a) * p.x + (c - a) * p.y
        patch, regions = triangulate_inlays(vertices, triangle, points, outlines, project)
        faces.extend(patch)
        slots.extend(5 if 2 in ids and i == 0 else 4 if 1 in ids else base_slot for ids in regions)
    return faces, slots


def sawn_board(name, center, size, across, along, slot, rng, roll=0, bow=0, damp=False):
    """Cut a narrow irregular grain channel into a chamfered board's visible face."""
    width, length, depth = size
    edge = min(0.0026, depth * 0.18, width * 0.085)
    basis = Matrix((across, along, Vector(across).cross(Vector(along)))).transposed()
    basis @= Matrix.Rotation(roll, 3, "Y")
    fractions = [-0.5, -0.5 + edge / length,
                 rng.uniform(-0.40, -0.23), rng.uniform(0.12, 0.36),
                 0.5 - edge / length, 0.5]
    grain = rng.uniform(-0.22, 0.22)
    grain_half_width = rng.uniform(0.020, 0.055)
    relief = min(depth * 0.07, 0.00105)
    vertices, faces, slots = [], [], []
    for i, t in enumerate(fractions):
        end = i in (0, len(fractions) - 1)
        half_w = width / 2 + (rng.uniform(-0.0008, 0.0008) if not end else -edge / 2)
        half_d = depth / 2 - (edge * 0.65 if end else 0)
        seam = width * (grain + rng.uniform(-0.052, 0.052))
        seam_w = width * grain_half_width * rng.uniform(0.63, 1.3)
        top = half_d + bow * (1 - (2 * t)**2)
        cut = 0 if end else relief * rng.uniform(0.28, 1)
        profile = ((-half_w + edge, top), (seam - seam_w, top),
                   (seam, top - cut), (seam + seam_w, top), (half_w - edge, top),
                   (half_w, top - edge), (half_w, top - 2 * half_d + edge),
                   (half_w - edge, top - 2 * half_d), (-half_w + edge, top - 2 * half_d),
                   (-half_w, top - 2 * half_d + edge), (-half_w, top - edge))
        end_shape = (0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
        if end and not name.startswith("SquareTimberLeg"):
            end_shape = [rng.uniform(0, min(0.0034, edge)) * (-1 if t > 0 else 1) for _ in profile]
        for point, (x, z) in enumerate(profile):
            vertices.append(tuple(Vector(center) + basis @ Vector((x, t * length + end_shape[point], z))))

    n = len(profile)
    grain_slot = 4 if slot != 4 else 0
    start = rng.randrange(1, 4)
    stop = min(start + rng.randrange(1, 4), len(fractions) - 2)
    for i in range(len(fractions) - 1):
        for j in range(n):
            a = i * n + j
            b = i * n + (j + 1) % n
            quad = (a, a + n, b + n, b)
            if damp and (i, j) in ((1, 0), (1, 3), (3, 3), (3, 10)):
                stained_faces, stained_slots = damp_wood_face(vertices, quad, slot, rng)
                faces.extend(stained_faces)
                slots.extend(stained_slots)
                continue
            faces.append(quad)
            face_slot = slot
            if start <= i < stop and j == 2:
                face_slot = grain_slot
            slots.append(face_slot)
    end = (len(fractions) - 1) * n
    faces.extend((tuple(range(n - 1, -1, -1)), tuple(range(end, end + n))))
    slots.extend((slot, slot))
    return surface_materials(mesh(name, vertices, faces, WOOD_PALETTE[slot]), WOOD_PALETTE, slots)


def torn_cardboard():
    cx, cy = 0.435, -0.771
    outline = [(-0.193, -0.14), (-0.079, -0.151), (0.010, -0.132),
               (0.114, -0.125), (0.165, -0.096), (0.156, -0.054),
               (0.149, -0.045), (0.172, -0.031), (0.158, 0.045),
               (0.129, 0.088), (0.101, 0.083), (0.098, 0.115),
               (0.059, 0.102), (0.023, 0.123), (0.007, 0.112), (-0.058, 0.125),
               (-0.112, 0.110), (-0.186, 0.117), (-0.174, 0.086), (-0.191, 0.076)]
    vertices = [(cx + x, cy + y, 0.3825 + 0.005 * math.sin(i * 0.81) + 0.009 * max(x, 0))
                for i, (x, y) in enumerate(outline)]
    n = len(vertices)
    vertices.extend((x, y, z - 0.0030) for x, y, z in vertices[:])
    vertices.extend(((cx - 0.028, cy - 0.010, 0.385), (cx - 0.028, cy - 0.010, 0.382)))
    faces, slots = [], []
    for i in range(n):
        j = (i + 1) % n
        faces.extend(((i, j, 2 * n), (j + n, i + n, 2 * n + 1), (j, i, i + n, j + n)))
        slots.extend((0, 0, 1))
    names = ("M_Cardboard", BARK)
    surface_materials(mesh("TornCardboardShim", vertices, faces, names[0]), names, slots)
    for i in (8, 9, 11, 12, 13):
        x, y, z = vertices[i]
        beam("ExposedCorrugation", (x, y - 0.002, z - 0.001), (x + 0.004, y + 0.010, z + 0.0008),
             0.0016, 0.0014, "M_Cardboard")


# ---------------------------------------------------------------------------
# MODEL 2: Raw wood boarding-house bed
# ---------------------------------------------------------------------------
def build_wood_slat_bed():
    """Inexpensive rough-sawn bed with irregular longitudinal slats and three joists."""
    width, length = 1.30, 2.00
    surface, head, leg = 0.379, 0.608, 0.070
    hx, hy = (width - leg) / 2, (length - leg) / 2
    dark, brown = WOOD_PALETTE[:2]
    rng = random.Random(73212)

    for side in (-1, 1):
        x = side * hx
        for end in (-1, 1):
            y = end * hy
            h = head if end > 0 else 0.391
            sawn_board("SquareTimberLeg", (x, y, h / 2), (leg, h, leg),
                       (0, side, 0), (0, 0, 1), 0, rng, damp=True)
            for z in (0.284, 0.347):
                cylinder("ExposedWoodenPeg", (x + side * 0.033, y, z), (x + side * 0.0355, y, z),
                         0.009, brown, 8)
                cylinder("ApronNailHead", (x, y + end * 0.033, z), (x, y + end * 0.0356, z),
                         0.0044, RUST if z < 0.32 else STEEL, 6)

        sawn_board("LongApron", (x, 0, 0.314), (0.128, 2 * hy - 0.026, 0.050),
                   (0, 0, -side), (0, 1, 0), 0, rng, damp=True)
        box("InternalCleat", (side * (hx - 0.034), 0, 0.310),
            (0.036, 2 * hy - 0.11, 0.032), dark, edge=0.0015)

    for end in (-1, 1):
        sawn_board("EndApron", (0, end * hy, 0.314), (0.128, 2 * hx - 0.029, 0.048),
                   (0, 0, end), (1, 0, 0), 0, rng, damp=True)

    sawn_board("LowHeadboardPlank", (0, hy, 0.543), (0.119, 2 * hx - 0.020, 0.043),
               (0, 0, -1), (1, 0, 0), 0, rng, damp=True)
    for x in (-hx, hx):
        cylinder("HeadboardTenonPeg", (x, hy - 0.033, 0.546), (x, hy - 0.0355, 0.546), 0.008, brown, 8)

    for y in (-0.605, 0.008, 0.599):
        sawn_board("LoadBearingCrossJoist", (0, y, 0.324), (0.067, 1.184, 0.070),
                   (0, 1, 0), (-1, 0, 0), 4, rng)

    count, pitch = 24, 0.049
    offsets, lengths, rolls = [], [], []
    stocks = (0, 4, 1, 0, 3, 1, 4, 4, 0, 2, 1, 4, 3, 0, 1, 2, 4, 0, 3, 4, 1, 0, 2, 4)
    for i, slot in enumerate(stocks):
        offset = rng.uniform(0.003, 0.0048) * rng.choice((-1, 1))
        length_offset = rng.uniform(0.010, 0.020) * rng.choice((-1, 1))
        slat_x = (i - (count - 1) / 2) * pitch + offset
        slat_y = rng.uniform(-0.0045, 0.0045)
        slat_width = rng.uniform(0.0345, 0.0375)
        roll = (rng.uniform(0.50, 0.98) * rng.choice((-1, 1))) if i % 4 in (0, 3) else 0
        sawn_board(f"LongitudinalSlat_{i + 1:02}", (slat_x, slat_y, 0.369),
                   (slat_width, 1.864 + length_offset, rng.uniform(0.018, 0.022)),
                   (1, 0, 0), (0, 1, 0), slot, rng, math.radians(roll), rng.uniform(-0.0018, 0.0026))
        offsets.append(offset)
        lengths.append(length_offset)
        if roll:
            rolls.append(abs(roll))

    torn_cardboard()
    return {
        "nominal_m": [width, head, length],
        "style": "Boarding-house bed with rough-sawn wood slats",
        "leg_section_m": [leg, leg],
        "slats": count,
        "load_bearing_cross_joists": 3,
        "slat_stock_materials": [WOOD_PALETTE[slot] for slot in sorted(set(stocks))],
        "nominal_headboard_rise_m": round(head - surface, 3),
        "slat_abs_offset_x_m": [round(op(map(abs, offsets)), 4) for op in (min, max)],
        "slat_abs_length_variation_m": [round(op(map(abs, lengths)), 4) for op in (min, max)],
        "tilted_slats": len(rolls),
        "slat_roll_degrees": [round(min(rolls), 2), round(max(rolls), 2)],
    }

# ---------------------------------------------------------------------------
# MODEL 3: Nệm đặt sàn phủ chiếu trúc hạt nâu
# ---------------------------------------------------------------------------
def mattress_floral_panel(vertices, quad, rng):
    a, b, _, d = (Vector(vertices[i]) for i in quad)
    horizontal, vertical = b - a, d - a
    width, height = horizontal.length, vertical.length
    u_axis, v_axis = horizontal / width, vertical / height
    points = [Vector(p) for p in ((0, 0), (width, 0), (width, height), (0, height))]
    outlines = [(0, 1, 2, 3)]
    motifs = max(1, math.floor(width / 0.165))
    for i in range(motifs):
        x = width * (i + 0.5) / motifs + rng.uniform(-0.017, 0.017)
        y = rng.uniform(0.048, 0.066)
        phase = rng.uniform(0, TAU)
        radius = rng.uniform(0.023, 0.027)
        for center_x, center_y, r, steps, lobes in (
            (x, y, radius, 25, 5),
            (x - 0.047, y - 0.024, 0.008, 5, 0),
            (x + 0.044, y + 0.013, 0.007, 5, 0),
        ):
            first = len(points)
            for j in range(steps):
                angle = TAU * j / steps
                shape = (1 + 0.20 * math.cos(lobes * angle)) if lobes else 1
                points.append(Vector((center_x + math.cos(angle + phase) * shape * r,
                                      center_y + math.sin(angle + phase) * shape * r)))
            outlines.append(tuple(range(first, len(points))))
    def project(p):
        return a + p.x * u_axis + p.y * v_axis
    faces, regions = triangulate_inlays(vertices, quad, points, outlines, project)
    slots = [int(any(region != 0 for region in ids)) for ids in regions]
    return faces, slots, motifs


def build_mattress_bamboo():
    """Nệm mút bọc ga hoa văn hồng trắng, bề mặt phủ chiếu trúc hạt nâu mùa hè, chồng gối đầu giường."""
    W = 1.20   # Width (X)
    L = 1.95   # Length (Y)
    H_mat = 0.18 # Mattress body thickness (Z)

    # 1. Main mattress body with rounded edges
    layers = [
        (W - 0.05, L - 0.05, 0.08, 0.0),
        (W - 0.015, L - 0.015, 0.09, 0.015),
        (W, L, 0.095, 0.04),
        (W, L, 0.095, H_mat - 0.04),
        (W - 0.015, L - 0.015, 0.09, H_mat - 0.015),
        (W - 0.05, L - 0.05, 0.08, H_mat)
    ]
    verts = [p for w, d, r, z in layers for p in rounded_rect(w, d, r, z)]
    count = 32
    faces = [tuple(reversed(range(count)))]
    slots = [0]
    floral_motifs = 0
    rng = random.Random(19312)
    for i in range(len(layers) - 1):
        for j in range(count):
            k = (j + 1) % count
            quad = (i * count + j, i * count + k, (i + 1) * count + k, (i + 1) * count + j)
            if i == 2 and (verts[quad[1]] - verts[quad[0]]).length > 0.9:
                panel_faces, panel_slots, motifs = mattress_floral_panel(verts, quad, rng)
                faces.extend(panel_faces)
                slots.extend(panel_slots)
                floral_motifs += motifs
            else:
                faces.append(quad)
                slots.append(0)
    faces.append(tuple((len(layers) - 1) * count + j for j in range(count)))
    slots.append(0)
    names = ("M_ClothPink", "M_CanvasRed")
    mat_obj = surface_materials(mesh("MattressCore", verts, faces, names[0], smooth=True), names, slots)
    mat_obj.data.polygons[0].use_smooth = False
    mat_obj.data.polygons[-1].use_smooth = False

    # Piping cords around top and bottom perimeter
    for pz in (0.02, H_mat - 0.02):
        tube("PipingCord", rounded_rect(W - 0.008, L - 0.008, 0.092, pz, 16), 0.0035, "M_CanvasRed", 6, closed=True)

    # 2. Bamboo tile mat (chiếu trúc hạt nâu mắt vuông phủ bề mặt nệm)
    mat_top = H_mat
    mat_w = W - 0.06
    mat_l = L - 0.06
    mat_th = 0.012

    # Bamboo mat base tile
    box("BambooMatBase", (0, 0, mat_top + mat_th / 2), (mat_w, mat_l, mat_th), BARK, edge=0.002)

    border_w = 0.04
    bamboo_surface = mat_top + mat_th
    rectangular_frame("BambooBorder", (0, 0, bamboo_surface + 0.003),
                      (mat_w, mat_l), border_w, 0.006, BARK)

    inner_w = mat_w - 2 * border_w
    inner_l = mat_l - 2 * border_w
    grid_nx = 14
    grid_ny = 24
    tile_gap = 0.004
    tile_w = inner_w / grid_nx - tile_gap
    tile_l = inner_l / grid_ny - tile_gap
    tile_rng = random.Random(19313)
    for j in range(grid_ny):
        gy = -inner_l / 2 + inner_l * (j + 0.5) / grid_ny
        for i in range(grid_nx):
            gx = -inner_w / 2 + inner_w * (i + 0.5) / grid_nx
            tile_mat = tile_rng.choice(("M_DoorWoodDark", "M_DoorWoodDark", "M_FoodBrown"))
            box(f"BambooTile_{i}_{j}", (gx, gy, bamboo_surface + 0.002),
                (tile_w, tile_l, 0.004), tile_mat)

    # 3. Stack of pillows at head (+Y)
    p1_center = (-0.20, 0.65, mat_top + mat_th + 0.05)
    box("HeadPillow_Lower", p1_center, (0.58, 0.38, 0.09), STAIN, edge=0.035)

    p2_center = (-0.20, 0.65, mat_top + mat_th + 0.13)
    box("HeadPillow_Upper", p2_center, (0.54, 0.35, 0.08), "M_ClothPink", edge=0.032)
    flower_z = p2_center[2] + 0.043
    for flower_x, flower_y in ((-0.38, 0.60), (-0.18, 0.72), (0.00, 0.61)):
        for angle in (0, TAU / 3, 2 * TAU / 3):
            dx, dy = math.cos(angle) * 0.025, math.sin(angle) * 0.025
            cylinder("PillowFlowerPetal", (flower_x + dx, flower_y + dy, flower_z),
                     (flower_x + dx, flower_y + dy, flower_z + 0.004), 0.022, "M_CanvasRed", 8)
        cylinder("PillowFlowerCenter", (flower_x, flower_y, flower_z),
                 (flower_x, flower_y, flower_z + 0.005), 0.014, "M_ClothPink", 8)

    green_pillow_center = (-0.04, 0.67, mat_top + mat_th + 0.215)
    box("SmallGreenPillow", green_pillow_center, (0.38, 0.28, 0.075), STAIN, edge=0.03)

    return {
        "nominal_m": [W, green_pillow_center[2] + 0.0375, L],
        "style": "Nệm đặt sàn phủ chiếu trúc hạt tre",
        "bamboo_tiles": grid_nx * grid_ny,
        "bamboo_tile_gap_m": tile_gap,
        "inlaid_flowers": floral_motifs,
        "usable_backpack_patch_m": [[-0.45, -0.75], [0.45, 0.05]]
    }


BUILDERS = {
    "prop_bed_bunk_iron": build_bunk_bed,
    "prop_bed_wood_slats": build_wood_slat_bed,
    "prop_bed_mattress_bamboo": build_mattress_bamboo,
}


def normalize():
    """Clean meshes, center on ground XZ with floor Y=0 in Unity, apply Smart UV."""
    for obj in objects():
        activate(obj)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.mesh.normals_make_consistent(inside=False)
        bpy.ops.object.mode_set(mode="OBJECT")
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-6)
        bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=1e-8)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(obj.data)
        bm.free()

    coords = [v.co for o in objects() for v in o.data.vertices]
    offset = Vector(((min(v.x for v in coords) + max(v.x for v in coords)) / 2,
                     (min(v.y for v in coords) + max(v.y for v in coords)) / 2,
                     min(v.z for v in coords)))

    for obj in objects():
        for v in obj.data.vertices:
            v.co -= offset
        activate(obj)
        tri = obj.modifiers.new("Export_Triangulate", "TRIANGULATE")
        bpy.ops.object.modifier_apply(modifier=tri.name)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.006)
        bpy.ops.object.mode_set(mode="OBJECT")
        obj.data.update()

    return list(offset)


def stats():
    asset_objects = objects()
    coords = [o.matrix_world @ v.co for o in asset_objects for v in o.data.vertices]
    assert coords and all(math.isfinite(c) for v in coords for c in v)
    lo = [min(v[k] for v in coords) for k in range(3)]
    hi = [max(v[k] for v in coords) for k in range(3)]
    count = sum(len(o.data.vertices) for o in asset_objects)
    triangles = sum(len(o.data.polygons) for o in asset_objects)
    uv_min, uv_max = 1.0, 0.0
    used_materials = set()

    for obj in asset_objects:
        assert obj.location.length < 1e-5, (obj.name, "location", list(obj.location))
        assert max(abs(v) for v in obj.rotation_euler) < 1e-5, (obj.name, "rotation", list(obj.rotation_euler))
        assert all(abs(s - 1) < 1e-6 for s in obj.scale), (obj.name, "scale")
        assert obj.data.uv_layers.active and len(obj.data.uv_layers.active.data) == len(obj.data.loops), obj.name
        uv = [p.uv for p in obj.data.uv_layers.active.data]
        assert all(0 <= c <= 1 for vec in uv for c in vec), obj.name
        uv_min = min(uv_min, min(c for vec in uv for c in vec))
        uv_max = max(uv_max, max(c for vec in uv for c in vec))
        for polygon in obj.data.polygons:
            assert polygon.loop_total == 3 and polygon.area > 1e-12, (obj.name, polygon.index, polygon.area)
            a, b, c = (uv[i] for i in polygon.loop_indices)
            assert abs((b - a).cross(c - a)) > 1e-12, (obj.name, "degenerate UV", polygon.index)
            used_materials.add(obj.data.materials[polygon.material_index].name)

    assert lo[2] == 0, ("floor datum", lo)
    assert abs(lo[0] + hi[0]) < 1e-5 and abs(lo[1] + hi[1]) < 1e-5, ("centering", lo, hi)

    return {
        "vertices": count,
        "triangles": triangles,
        "dimensions_unity_m": [round(hi[0] - lo[0], 4), round(hi[2] - lo[2], 4), round(hi[1] - lo[1], 4)],
        "bounds_min_unity_m": [round(lo[0], 4), round(lo[2], 4), round(-hi[1], 4)],
        "materials": sorted(used_materials),
        "mesh_objects": len(asset_objects),
        "uv_range": [round(uv_min, 7), round(uv_max, 7)],
        "identity_transforms": True
    }


def validate_fbx_payload(path):
    from io_scene_fbx import parse_fbx
    root, _ = parse_fbx.parse(str(path))

    def child(node, key):
        return next((c for c in node.elems if c.id == key), None)

    global_props = child(child(root, b"GlobalSettings"), b"Properties70")
    settings = {p.props[0]: p.props[-1] for p in global_props.elems}
    assert settings[b"UpAxis"] == 1 and settings[b"UnitScaleFactor"] == 100.0, settings
    data = child(root, b"Objects")
    coords = []
    for node in data.elems:
        if node.id == b"Geometry":
            xyz = child(node, b"Vertices").props[0]
            coords.extend(zip(xyz[0::3], xyz[1::3], xyz[2::3]))
        if node.id == b"Model":
            props = child(node, b"Properties70")
            for p in props.elems:
                if p.props[0] in (b"Lcl Rotation", b"Lcl Translation", b"PreRotation", b"PostRotation"):
                    assert max(abs(x) for x in p.props[-3:]) < 1e-5, (path, p.props)
                if p.props[0] == b"Lcl Scaling":
                    assert all(abs(x - 1) < 1e-6 for x in p.props[-3:]), (path, p.props)
    lo = [min(v[k] for v in coords) for k in range(3)]
    hi = [max(v[k] for v in coords) for k in range(3)]
    assert abs(lo[1]) < 1e-5, (path, "FBX floor", lo)
    assert abs(lo[0] + hi[0]) < 1e-5 and abs(lo[2] + hi[2]) < 1e-5
    return [round(hi[k] - lo[k], 4) for k in range(3)]


def apply_preview_finish(stem, asset_objects):
    """Approximate surface response for Blender QA, after the FBX has already been exported."""
    variants = {}
    for obj in asset_objects:
        kind, axis = "fabric", None
        if stem == "prop_bed_wood_slats" and not obj.name.startswith(("TornCardboard", "ExposedCorrugation")):
            kind = "wood"
            axis = 1
            if obj.name.startswith(("SquareTimberLeg", "ApronNailHead", "ExposedWoodenPeg", "HeadboardTenonPeg")):
                axis = 2
            elif obj.name.startswith(("EndApron", "LowHeadboardPlank", "LoadBearingCrossJoist")):
                axis = 0
        elif obj.name.startswith("WovenSedgeMat"):
            kind = "straw"
        elif obj.name.startswith(("BambooTile", "BambooMatBase")):
            kind, axis = "bamboo", 1
        elif obj.name.startswith(("Upright", "Lower", "Upper", "Ladder", "Guard", "RoundedSafety", "WornEnamel",
                                  "RustExposed")) and not obj.name.startswith((
                                      "UpperPad", "UpperWindbreaker", "UpperFolded")):
            kind = "paint"
        elif obj.name.startswith("BlueDustTarp"):
            kind = "tarp"

        for slot in obj.material_slots:
            mat = slot.material
            if mat.name in {STEEL, "M_StainlessSteel", "M_Rubber"}:
                continue
            if mat.name == RUST:
                finish = ("rust", None)
            else:
                finish = (kind, axis)
            key = (mat.name, *finish)
            if key in variants:
                slot.material = variants[key]
                continue

            variant = mat.copy()
            variants[key] = variant
            slot.material = variant
            tree = variant.node_tree
            bsdf = tree.nodes.get("Principled BSDF")
            kind_, axis_ = finish
            if kind_ == "wood":
                scale = [320, 320, 320]
                scale[axis_] = 7
                brightness, roughness, strength, distance = (0.27, 1.08), 0.88, 0.21, 0.0017
            elif kind_ == "bamboo":
                scale = (225, 12, 250)
                brightness, roughness, strength, distance = (0.37, 1.02), 0.79, 0.13, 0.0009
            elif kind_ == "straw":
                scale = (305, 95, 230)
                brightness, roughness, strength, distance = (0.58, 1.13), 0.94, 0.24, 0.0015
            elif kind_ == "paint":
                scale = (95, 95, 95)
                brightness, roughness, strength, distance = (0.51, 0.96), 0.80, 0.12, 0.00095
            elif kind_ == "rust":
                scale = (130, 130, 130)
                brightness, roughness, strength, distance = (0.26, 0.90), 0.94, 0.23, 0.0014
            elif kind_ == "tarp":
                scale = (320, 350, 160)
                brightness, roughness, strength, distance = (0.55, 1.07), 0.86, 0.19, 0.0010
            else:
                scale = (245, 245, 235)
                brightness, roughness, strength, distance = (0.62, 1.05), 0.92, 0.16, 0.0012
            bsdf.inputs["Metallic"].default_value = 0
            if kind_ == "straw" and mat.name == "M_FoodBrown":
                bsdf.inputs["Base Color"].default_value = (0.35, 0.24, 0.126, 1)
            if kind_ == "straw" and mat.name == "M_FoodGreen":
                bsdf.inputs["Base Color"].default_value = (0.091, 0.17, 0.050, 1)
            if kind_ == "tarp" and mat.name == "M_CanvasBlue":
                bsdf.inputs["Base Color"].default_value = (0.082, 0.155, 0.254, 1)
            if kind_ == "wood" and mat.name == MOLD:
                bsdf.inputs["Base Color"].default_value = (0.102, 0.117, 0.075, 1)
            bsdf.inputs["Roughness"].default_value = roughness
            bsdf.inputs["Specular IOR Level"].default_value = 0.28

            geometry = tree.nodes.new("ShaderNodeNewGeometry")
            stretch = tree.nodes.new("ShaderNodeVectorMath")
            stretch.operation = "MULTIPLY"
            stretch.inputs[1].default_value = scale
            noise = tree.nodes.new("ShaderNodeTexNoise")
            noise.inputs["Scale"].default_value = 1
            noise.inputs["Detail"].default_value = 2.3
            mottling = tree.nodes.new("ShaderNodeTexNoise")
            mottling.inputs["Scale"].default_value = 8 if kind_ in {"wood", "tarp", "fabric"} else 19
            mottling.inputs["Detail"].default_value = 5
            small_scale = tree.nodes.new("ShaderNodeMixRGB")
            small_scale.inputs[0].default_value = 0.58
            ramp = tree.nodes.new("ShaderNodeValToRGB")
            for element, value, position in zip(ramp.color_ramp.elements, brightness, (0.27, 0.75)):
                element.position = position
                element.color = (value, value, value, 1)
            tint = tree.nodes.new("ShaderNodeMixRGB")
            tint.blend_type = "MULTIPLY"
            tint.inputs[0].default_value = 1
            tint.inputs[1].default_value = bsdf.inputs["Base Color"].default_value
            bump = tree.nodes.new("ShaderNodeBump")
            bump.inputs["Strength"].default_value = strength
            bump.inputs["Distance"].default_value = distance
            tree.links.new(geometry.outputs["Position"], stretch.inputs[0])
            tree.links.new(geometry.outputs["Position"], mottling.inputs["Vector"])
            tree.links.new(stretch.outputs["Vector"], noise.inputs["Vector"])
            tree.links.new(noise.outputs["Fac"], small_scale.inputs[1])
            tree.links.new(mottling.outputs["Fac"], small_scale.inputs[2])
            tree.links.new(small_scale.outputs[0], ramp.inputs["Fac"])
            tree.links.new(ramp.outputs["Color"], tint.inputs[2])
            tree.links.new(tint.outputs[0], bsdf.inputs["Base Color"])
            tree.links.new(noise.outputs["Fac"], bump.inputs["Height"])
            tree.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])


def render(stem):
    QA.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 32
    scene.cycles.use_denoising = True
    scene.cycles.device = "CPU"
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    bg = scene.world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.20, 0.23, 0.28, 1)
    bg.inputs["Strength"].default_value = 0.4
    scene.view_settings.view_transform = "AgX"
    scene.render.image_settings.file_format = "PNG"

    asset_objects = objects()
    for mat in {m for o in asset_objects for m in o.data.materials}:
        shader = mat.node_tree.nodes.get("Principled BSDF")
        rgba = shader.inputs["Base Color"].default_value
        shader.inputs["Base Color"].default_value = tuple(
            c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in rgba[:3]
        ) + (1,)
    apply_preview_finish(stem, asset_objects)

    co = [v.co for o in asset_objects for v in o.data.vertices]
    lo = Vector([min(v[k] for v in co) for k in range(3)])
    hi = Vector([max(v[k] for v in co) for k in range(3)])
    target = (lo + hi) / 2
    size = hi - lo
    extent = max(size)

    camera_direction = {
        "prop_bed_bunk_iron": (4.4, -2.6, 1.35),
        "prop_bed_wood_slats": (2.8, -4.2, 3.6),
    }.get(stem, (3.4, -4.3, 2.4))
    direction = Vector(camera_direction).normalized()
    cam_data = bpy.data.cameras.new("QA_camera")
    cam = bpy.data.objects.new("QA_camera", cam_data)
    scene.collection.objects.link(cam)
    cam.location = target + direction * extent * 2.2
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = extent * (1.49 if stem == "prop_bed_bunk_iron" else 1.41)
    scene.camera = cam

    def softbox(pos, power, scale):
        light_data = bpy.data.lights.new("QA_softbox", "AREA")
        light_data.energy = power
        light_data.shape = "DISK"
        light_data.size = scale
        light = bpy.data.objects.new("QA_softbox", light_data)
        scene.collection.objects.link(light)
        light.location = target + Vector(pos)
        light.rotation_euler = (target - light.location).to_track_quat("-Z", "Y").to_euler()

    for pos, power, scale in [((-3, -4, 6), 180, 5), ((4, -2, 5), 100, 4), ((0, 4, 6), 160, 3)]:
        softbox(pos, power, scale)

    floor_span = extent * (36 if stem == "prop_bed_bunk_iron" else 4)
    floor = box("QA_floor", (0, 0, -0.01), (floor_span, floor_span, 0.02), "M_ConcreteFloor")
    floor.data.materials.clear()
    mat = bpy.data.materials.new("QA_floor_only")
    mat.diffuse_color = (0.12, 0.14, 0.18, 1)
    floor.data.materials.append(mat)

    def write_png(suffix=""):
        out_file = QA / f"{stem}{suffix}.png"
        scene.render.filepath = str(out_file)
        bpy.ops.render.render(write_still=True)
        print(f"RENDERED_OK {out_file}", flush=True)

    write_png()
    if stem == "prop_bed_bunk_iron":
        floor.hide_render = True
        softbox((3.1, -2.0, -1.7), 55, 2.6)
        focus = target + Vector((0, 0, 0.39))
        cam.location = focus + Vector((4.0, -2.05, -0.78))
        cam.rotation_euler = (focus - cam.location).to_track_quat("-Z", "Y").to_euler()
        cam_data.ortho_scale = 2.33
        write_png("_tarp_detail")
    elif stem == "prop_bed_wood_slats":
        floor.hide_render = True
        softbox((2.0, -3.8, -2.7), 68, 2.9)
        cam.location = target + Vector((2.5, -3.9, -2.3))
        cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
        cam_data.ortho_scale = 2.55
        write_png("_joists_detail")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--render", action="store_true", help="Render preview PNGs")
    parser.add_argument("--save-blend", action="store_true", help="Save .blend file")
    parser.add_argument("--only", nargs="+", help="Only build specific models")
    args = parser.parse_known_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])[0]

    DEST.mkdir(parents=True, exist_ok=True)
    QA.mkdir(parents=True, exist_ok=True)

    targets = args.only or list(BUILDERS.keys())
    report_path = QA / "bed_props_report.json"
    previous = json.loads(report_path.read_text(encoding="utf-8")) if args.only and report_path.is_file() else []
    by_file = {entry["file"]: entry for entry in previous}
    for stem in targets:
        assert stem in BUILDERS, f"Unknown prop: {stem}"
        build = BUILDERS[stem]
        print(f"\n=== Building {stem} ===", flush=True)
        clear_scene()
        scene = bpy.context.scene
        scene.unit_settings.system = "METRIC"
        scene.unit_settings.scale_length = 1

        details = build()
        offset = normalize()
        before = stats()

        limit = 10000
        assert before["triangles"] < limit, f"{stem} triangles {before['triangles']} >= {limit}"

        path = DEST / f"{stem}.fbx"
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.export_scene.fbx(
            filepath=str(path), use_selection=True, object_types={"MESH"},
            global_scale=1, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
            axis_forward="-Z", axis_up="Y", bake_space_transform=True,
            use_mesh_modifiers=True, mesh_smooth_type="FACE", use_tspace=True,
            add_leaf_bones=False, bake_anim=False, path_mode="STRIP", embed_textures=False
        )

        serialized_dims = validate_fbx_payload(path)
        assert serialized_dims == before["dimensions_unity_m"], (stem, serialized_dims, before)

        if args.save_blend:
            SOURCE.mkdir(parents=True, exist_ok=True)
            bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / f"{stem}.blend"), check_existing=False)

        # FBX round-trip test
        clear_scene()
        bpy.ops.import_scene.fbx(filepath=str(path), use_custom_normals=True, bake_space_transform=True)
        after = stats()
        assert before == after, (stem, "FBX round-trip mismatch", before, after)

        REPORT.append({
            "file": path.name,
            **after,
            "triangle_limit_exclusive": limit,
            "details": details,
            "authoring_center_offset_blender_m": offset,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            "bytes": path.stat().st_size
        })
        print(f"BED_PROP_OK {json.dumps(REPORT[-1], ensure_ascii=False)}", flush=True)
        by_file[path.name] = REPORT[-1]

        if args.render:
            render(stem)

    entries = [by_file[f"{stem}.fbx"] for stem in BUILDERS if f"{stem}.fbx" in by_file]
    report_path.write_text(json.dumps(entries, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"\nAll bed props built successfully! Report saved to {report_path}")


if __name__ == "__main__":
    main()
