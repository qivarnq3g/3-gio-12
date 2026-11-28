import bpy, bmesh, sys, math, random
from mathutils import Vector, Matrix

src, dst = sys.argv[sys.argv.index("--") + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
for o in bpy.context.scene.objects:
    o.select_set(o in meshes)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.join()
cart = bpy.context.view_layer.objects.active
for o in list(bpy.context.scene.objects):
    if o is not cart:
        bpy.data.objects.remove(o)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
cart.name = cart.data.name = "Night2_BreadCart"
me = cart.data
for p in me.polygons:
    p.material_index = 0
body = bpy.data.materials.new("M_BanhMiCart")
me.materials.clear()
me.materials.append(body)

COUNTER = 2.13
S = 0.90 / COUNTER
for v in me.vertices:
    v.co *= S
zmin = min(v.co.z for v in me.vertices)
for v in me.vertices:
    v.co.z -= zmin
rng = random.Random(12)


def mat(name):
    if name not in [m.name for m in me.materials]:
        me.materials.append(bpy.data.materials.get(name) or bpy.data.materials.new(name))
    return [m.name for m in me.materials].index(name)


bm = bmesh.new()
bm.from_mesh(me)
uv = bm.loops.layers.uv.verify()
NEW = set()


def face(vs, m):
    f = bm.faces.new(vs)
    f.material_index = m
    NEW.add(f)
    return f


def box(c, s, m, rot=0.0):
    r = Matrix.Rotation(rot, 3, "Z")
    vs = [bm.verts.new(Vector(c) + r @ Vector((sx * s[0] / 2, sy * s[1] / 2, sz * s[2] / 2)))
          for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
    for q in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
        face([vs[i] for i in q], m)


def loaf(c, length, w, h, yaw, crust, crumb):
    nl, nr = 18, 14
    R = Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(rng.uniform(-0.08, 0.08), 3, "X")
    bend = rng.uniform(-0.01, 0.01)

    def surf(t, a, lift):
        s = max(math.sin(math.pi * t) ** 0.55, 0.06)
        y = math.cos(a) * w / 2 * s * lift
        z = math.sin(a) * h / 2 * s * lift
        if z < 0:
            z *= 0.45
        return Vector(c) + R @ Vector(((t - 0.5) * length, y + bend * math.sin(math.pi * t), z + h * 0.22))

    rings = [[bm.verts.new(surf(i / nl, 2 * math.pi * k / nr, 1)) for k in range(nr)] for i in range(nl + 1)]
    tipA = bm.verts.new(Vector(c) + R @ Vector((-0.5 * length - 0.004, 0, h * 0.2)))
    tipB = bm.verts.new(Vector(c) + R @ Vector((0.5 * length + 0.004, 0, h * 0.2)))
    for i in range(nl):
        for k in range(nr):
            kk = (k + 1) % nr
            face((rings[i][k], rings[i][kk], rings[i + 1][kk], rings[i + 1][k]), crust).smooth = True
    for k in range(nr):
        kk = (k + 1) % nr
        face((rings[0][kk], rings[0][k], tipA), crust).smooth = True
        face((rings[nl][k], rings[nl][kk], tipB), crust).smooth = True
    for tc in (0.29, 0.5, 0.71):
        n = 10
        row = []
        for j in range(n + 1):
            u = -1 + 2 * j / n
            a = math.pi / 2 - 0.62 * u
            half = 0.011 * (1 - u * u) ** 0.5 / length + 0.0005
            row.append((bm.verts.new(surf(tc + 0.075 * u - half, a, 1.012)), bm.verts.new(surf(tc + 0.075 * u + half, a, 1.012))))
        for j in range(n):
            f = face((row[j][0], row[j + 1][0], row[j + 1][1], row[j][1]), crumb)
            f.smooth = True
            f.normal_update()
            a = math.pi / 2 - 0.62 * (-1 + 2 * (j + 0.5) / n)
            if f.normal.dot(R @ Vector((0, math.cos(a), math.sin(a)))) < 0:
                f.normal_flip()


crust, crumb = mat("M_FoodBrown"), mat("M_FoodYellow")
steel, frame = mat("M_StainlessSteel"), mat("M_AluminumFrame")
glass, sign = mat("M_Night2CartGlass"), mat("M_SignBanhMi")

TOP = 0.90
X0, X1, Y0, Y1, H = -0.58, 0.12, -0.23, 0.20, 0.36
cx, cy = (X0 + X1) / 2, (Y0 + Y1) / 2
box((cx, cy, TOP + 0.006), (X1 - X0, Y1 - Y0, 0.012), steel)
for x in (X0, X1):
    for y in (Y0, Y1):
        box((x, y, TOP + H / 2), (0.018, 0.018, H), frame)
for y in (Y0, Y1):
    box((cx, y, TOP + H), (X1 - X0, 0.018, 0.018), frame)
    box((cx, y, TOP + 0.015), (X1 - X0, 0.018, 0.018), frame)
for x in (X0, X1):
    box((x, cy, TOP + H), (0.018, Y1 - Y0, 0.018), frame)


def pane(*ps):
    face([bm.verts.new(p) for p in ps], glass)
    face([bm.verts.new(p) for p in reversed(ps)], glass)


pane(Vector((X0, Y0, TOP + 0.02)), Vector((X1, Y0, TOP + 0.02)), Vector((X1, Y0, TOP + H)), Vector((X0, Y0, TOP + H)))
pane(Vector((X0, Y0, TOP + 0.02)), Vector((X0, Y1, TOP + 0.02)), Vector((X0, Y1, TOP + H)), Vector((X0, Y0, TOP + H)))
pane(Vector((X1, Y0, TOP + 0.02)), Vector((X1, Y1, TOP + 0.02)), Vector((X1, Y1, TOP + H)), Vector((X1, Y0, TOP + H)))
pane(Vector((X0, Y0, TOP + H)), Vector((X1, Y0, TOP + H)), Vector((X1, Y1, TOP + H)), Vector((X0, Y1, TOP + H)))

xs = [X0 + 0.055 + i * 0.098 for i in range(7)]
for x in xs:
    loaf((x + rng.uniform(-0.006, 0.006), cy + rng.uniform(-0.012, 0.012), TOP + 0.012), rng.uniform(0.27, 0.30), 0.078, 0.062, math.pi / 2 + rng.uniform(-0.07, 0.07), crust, crumb)
for x in xs[:-1]:
    loaf((x + 0.049 + rng.uniform(-0.006, 0.006), cy + rng.uniform(-0.015, 0.015), TOP + 0.06), rng.uniform(0.26, 0.30), 0.076, 0.06, math.pi / 2 + rng.uniform(-0.12, 0.12), crust, crumb)

y = Y0 - 0.004
f = face([bm.verts.new(p) for p in (Vector((X1 - 0.27, y, TOP + 0.24)), Vector((X1 - 0.03, y, TOP + 0.24)), Vector((X1 - 0.03, y, TOP + 0.30)), Vector((X1 - 0.27, y, TOP + 0.30)))], sign)
for l, t in zip(f.loops, [(0.039, 0.1875), (0.986, 0.1875), (0.986, 0.6484), (0.039, 0.6484)]):
    l[uv].uv = t

bm.normal_update()
bmesh.ops.recalc_face_normals(bm, faces=[f for f in NEW if f.material_index in (steel, frame)])
bm.to_mesh(me)
bm.free()

bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="DESELECT")
bpy.ops.object.mode_set(mode="OBJECT")
names = [m.name for m in me.materials]
for p in me.polygons:
    p.select = names[p.material_index] not in (body.name, "M_SignBanhMi")
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.uv.cube_project(cube_size=0.5)
bpy.ops.object.mode_set(mode="OBJECT")

pts = [v.co for v in me.vertices]
print("SIZE", tuple(round(max(p[i] for p in pts) - min(p[i] for p in pts), 3) for i in range(3)), "TRIS", sum(len(p.vertices) - 2 for p in me.polygons), "SLOTS", names)
bpy.ops.export_scene.fbx(filepath=dst, use_selection=False, apply_scale_options="FBX_SCALE_UNITS", apply_unit_scale=True,
                         bake_space_transform=True, axis_forward="-Z", axis_up="Y", path_mode="STRIP", embed_textures=False, mesh_smooth_type="FACE")
print("EXPORTED", dst)
