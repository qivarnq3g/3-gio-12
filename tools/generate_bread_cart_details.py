import bpy, bmesh, sys, math, random
from mathutils import Vector, Matrix

src, dst, tex = sys.argv[sys.argv.index("--") + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src, bake_space_transform=True)
cart = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
print("XFORM", tuple(round(v, 3) for v in cart.rotation_euler), tuple(cart.scale))
me = cart.data
rng = random.Random(12)


def mat(name):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    if name not in [s.material.name for s in cart.material_slots if s.material]:
        me.materials.append(m)
    return [s.material.name for s in cart.material_slots].index(name)


bm = bmesh.new()
bm.from_mesh(me)
bm.transform(cart.matrix_world)
bm.faces.ensure_lookup_table()
drop = {mat(n) for n in ("M_FoodBrown", "M_FoodGreen", "M_FoodYellow", "M_PlasticRed")}
seen, kill = set(), []
for f in bm.faces:
    if f.index in seen:
        continue
    st, grp = [f], []
    seen.add(f.index)
    while st:
        g = st.pop()
        grp.append(g)
        for e in g.edges:
            for h in e.link_faces:
                if h.index not in seen:
                    seen.add(h.index)
                    st.append(h)
    vs = {v for g in grp for v in g.verts}
    lo = Vector([min(v.co[i] for v in vs) for i in range(3)])
    hi = Vector([max(v.co[i] for v in vs) for i in range(3)])
    small_jar = grp[0].material_index == mat("M_Night2CartGlass") and (hi - lo).length < 0.2
    if grp[0].material_index in drop or small_jar:
        kill += grp
bmesh.ops.delete(bm, geom=kill, context="FACES")
print("REMOVED", len(kill))

uv = bm.loops.layers.uv.verify()
STRIPS = set()


def box(c, s, m, rot=0.0):
    r = Matrix.Rotation(rot, 3, "Z")
    vs = [bm.verts.new(Vector(c) + r @ Vector((sx * s[0] / 2, sy * s[1] / 2, sz * s[2] / 2)))
          for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
    for q in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
        f = bm.faces.new([vs[i] for i in q])
        f.material_index = m
    return vs


def cyl(c, r, h, m, seg=16, top=None, r2=None, rot=None):
    r2 = r if r2 is None else r2
    M = rot or Matrix.Identity(3)
    b = [bm.verts.new(Vector(c) + M @ Vector((r * math.cos(a), r * math.sin(a), 0))) for a in [i * 2 * math.pi / seg for i in range(seg)]]
    t = [bm.verts.new(Vector(c) + M @ Vector((r2 * math.cos(a), r2 * math.sin(a), h))) for a in [i * 2 * math.pi / seg for i in range(seg)]]
    for i in range(seg):
        j = (i + 1) % seg
        bm.faces.new((b[i], b[j], t[j], t[i])).material_index = m
    bm.faces.new(list(reversed(b))).material_index = m
    bm.faces.new(t).material_index = m if top is None else top


def loaf(c, length, w, h, yaw, crust, crumb):
    nl, nr = 18, 14
    R = Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(rng.uniform(-0.08, 0.08), 3, "X")
    bend = rng.uniform(-0.01, 0.01)
    rings = []
    for i in range(nl + 1):
        t = i / nl
        s = max(math.sin(math.pi * t) ** 0.55, 0.06)
        ring = []
        for k in range(nr):
            a = 2 * math.pi * k / nr
            y = math.cos(a) * w / 2 * s
            z = math.sin(a) * h / 2 * s
            if z < 0:
                z *= 0.45
            p = Vector(((t - 0.5) * length, y + bend * math.sin(math.pi * t), z + h * 0.22))
            ring.append(bm.verts.new(Vector(c) + R @ p))
        rings.append(ring)
    tipA = bm.verts.new(Vector(c) + R @ Vector((-0.5 * length - 0.004, 0, h * 0.2)))
    tipB = bm.verts.new(Vector(c) + R @ Vector((0.5 * length + 0.004, 0, h * 0.2)))
    for i in range(nl):
        for k in range(nr):
            kk = (k + 1) % nr
            f = bm.faces.new((rings[i][k], rings[i][kk], rings[i + 1][kk], rings[i + 1][k]))
            t = (i + 0.5) / nl
            a = 2 * math.pi * (k + 0.5) / nr
            off = math.pi / 2 - a
            f.material_index = crust
            f.smooth = True

    def surf(t, a, lift):
        s = max(math.sin(math.pi * t) ** 0.55, 0.06)
        y = math.cos(a) * w / 2 * s * lift
        z = math.sin(a) * h / 2 * s * lift
        return Vector(c) + R @ Vector(((t - 0.5) * length, y + bend * math.sin(math.pi * t), z + h * 0.22))

    for tc in (0.29, 0.5, 0.71):
        n = 10
        row = []
        for j in range(n + 1):
            u = -1 + 2 * j / n
            a = math.pi / 2 - 0.62 * u
            tt = tc + 0.075 * u
            half = 0.011 * (1 - u * u) ** 0.5 / length + 0.0005
            row.append((bm.verts.new(surf(tt - half, a, 1.012)), bm.verts.new(surf(tt + half, a, 1.012))))
        for j in range(n):
            f = bm.faces.new((row[j][0], row[j + 1][0], row[j + 1][1], row[j][1]))
            f.material_index = crumb
            f.smooth = True
            f.normal_update()
            if f.normal.dot(R @ Vector((0, math.cos(math.pi / 2 - 0.62 * (-1 + 2 * (j + 0.5) / n)), math.sin(math.pi / 2 - 0.62 * (-1 + 2 * (j + 0.5) / n))))) < 0:
                f.normal_flip()
            STRIPS.add(f)
    for k in range(nr):
        kk = (k + 1) % nr
        bm.faces.new((rings[0][kk], rings[0][k], tipA)).material_index = crust
        bm.faces.new((rings[nl][k], rings[nl][kk], tipB)).material_index = crust


crust, crumb = mat("M_FoodBrown"), mat("M_FoodYellow")
green, red = mat("M_FoodGreen"), mat("M_PlasticRed")
steel, glass = mat("M_StainlessSteel"), mat("M_Night2CartGlass")
paper, white = mat("M_PaperWhite"), mat("M_PlasticWhite")
wood, sign = mat("M_DoorWoodBrown"), mat("M_SignBanhMi")

FLOOR = 0.912
box((-0.31, -0.09, FLOOR + 0.006), (0.56, 0.34, 0.012), steel)
for x in (-0.59, -0.03):
    box((x, -0.09, FLOOR + 0.03), (0.01, 0.34, 0.06), steel)
for y in (-0.26, 0.08):
    box((-0.31, y, FLOOR + 0.03), (0.56, 0.01, 0.06), steel)
xs = [-0.535 + i * 0.083 for i in range(6)]
for x in xs:
    loaf((x + rng.uniform(-0.006, 0.006), -0.09 + rng.uniform(-0.012, 0.012), FLOOR + 0.012), rng.uniform(0.26, 0.29), 0.074, 0.06, math.pi / 2 + rng.uniform(-0.07, 0.07), crust, crumb)
for x in xs[:-1]:
    loaf((x + 0.042 + rng.uniform(-0.006, 0.006), -0.09 + rng.uniform(-0.015, 0.015), FLOOR + 0.058), rng.uniform(0.25, 0.29), 0.072, 0.058, math.pi / 2 + rng.uniform(-0.12, 0.12), crust, crumb)

for i, (x, fill) in enumerate(((0.07, "pickle"), (0.25, "cucumber"), (0.43, "herb"))):
    box((x, -0.09, FLOOR + 0.004), (0.165, 0.30, 0.008), steel)
    for sx in (-1, 1):
        box((x + sx * 0.0825, -0.09, FLOOR + 0.03), (0.006, 0.30, 0.055), steel)
    for sy in (-1, 1):
        box((x, -0.09 + sy * 0.15, FLOOR + 0.03), (0.165, 0.006, 0.055), steel)
    for n in range(70 if fill != "herb" else 45):
        px = x + rng.uniform(-0.07, 0.07)
        py = -0.09 + rng.uniform(-0.135, 0.135)
        pz = FLOOR + 0.012 + rng.uniform(0, 0.03)
        if fill == "pickle":
            box((px, py, pz), (0.055, 0.008, 0.008), crumb if n % 3 else red, rng.uniform(0, math.pi))
        elif fill == "cucumber":
            box((px, py, pz), (0.07, 0.013, 0.013), green, rng.uniform(-0.3, 0.3))
        else:
            box((px, py, pz + 0.01), (0.03, 0.018, 0.004), green, rng.uniform(0, math.pi))

SHELF = 1.186
cyl((-0.52, -0.09, SHELF + 0.048), 0.048, 0.17, green, 18, crumb, None, Matrix.Rotation(math.pi / 2, 3, "Y"))
for k in range(4):
    cyl((-0.33 + k * 0.016, -0.12 + k * 0.01, SHELF + 0.004 + k * 0.006), 0.044, 0.005, crumb, 18, None, None, Matrix.Rotation(0.12, 3, "X"))
cyl((-0.14, -0.06, SHELF), 0.05, 0.075, glass, 18)
cyl((-0.14, -0.06, SHELF + 0.002), 0.046, 0.055, crust, 18)
cyl((-0.14, -0.06, SHELF + 0.075), 0.052, 0.015, red, 18)
for k, x in enumerate((0.03, 0.09)):
    cyl((x, -0.10, SHELF), 0.026, 0.16, red if k == 0 else crust, 14)
    cyl((x, -0.10, SHELF + 0.16), 0.02, 0.025, white, 12, None, 0.004)
cyl((0.18, -0.08, SHELF), 0.045, 0.12, glass, 18)
cyl((0.18, -0.08, SHELF + 0.004), 0.041, 0.09, crumb, 18)
for k in range(8):
    box((0.40, -0.08, SHELF + 0.002 + k * 0.004), (0.22, 0.16, 0.003), paper, rng.uniform(-0.05, 0.05))

box((0.05, 0.21, 0.92), (0.38, 0.17, 0.02), wood)
box((0.33, 0.21, 0.92), (0.13, 0.025, 0.006), steel, 0.2)
box((0.43, 0.23, 0.925), (0.08, 0.02, 0.016), red, 0.2)


def quad(p0, p1, p2, p3, m, uvs):
    vs = [bm.verts.new(p) for p in (p0, p1, p2, p3)]
    f = bm.faces.new(vs)
    f.material_index = m
    for l, t in zip(f.loops, uvs):
        l[uv].uv = t
    return f


def plate(x0, x1, z0, z1, y, th, uvr):
    u0, v0, u1, v1 = uvr
    quad(Vector((x0, y, z0)), Vector((x1, y, z0)), Vector((x1, y, z1)), Vector((x0, y, z1)), sign, [(u0, v0), (u1, v0), (u1, v1), (u0, v1)])
    back = [Vector((x0, y + th, z0)), Vector((x0, y + th, z1)), Vector((x1, y + th, z1)), Vector((x1, y + th, z0))]
    quad(*back, steel, [(0, 0), (0, 1), (1, 1), (1, 0)])


plate(-0.61, 0.61, 1.565, 1.765, -0.312, 0.008, (0.0, 0.68, 1.0, 1.0))
box((0.0, -0.31, 1.56), (1.24, 0.012, 0.012), steel)
plate(-0.55, -0.22, 0.70, 0.78, -0.326, 0.004, (0.039, 0.1875, 0.986, 0.6484))

bm.normal_update()
bmesh.ops.recalc_face_normals(bm, faces=[f for f in bm.faces if f not in STRIPS and f.material_index in (crust, crumb, green, red, steel, glass, paper, white, wood)])
cart.matrix_world = Matrix.Identity(4)
bm.to_mesh(me)
bm.free()

bpy.context.view_layer.objects.active = cart
cart.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="DESELECT")
bpy.ops.object.mode_set(mode="OBJECT")
signs = {sign}
for p in me.polygons:
    p.select = p.material_index not in signs and all(abs(me.uv_layers.active.data[li].uv.x) < 1e-9 and abs(me.uv_layers.active.data[li].uv.y) < 1e-9 for li in p.loop_indices)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.uv.cube_project(cube_size=0.5)
bpy.ops.object.mode_set(mode="OBJECT")

img = bpy.data.images.load(tex)
sm = bpy.data.materials["M_SignBanhMi"]
sm.use_nodes = True
ti = sm.node_tree.nodes.new("ShaderNodeTexImage")
ti.image = img
sm.node_tree.links.new(ti.outputs[0], sm.node_tree.nodes["Principled BSDF"].inputs["Base Color"])

tris = sum(len(p.vertices) - 2 for p in me.polygons)
print("TRIS", tris, "SLOTS", [s.material.name for s in cart.material_slots])
bpy.ops.export_scene.fbx(filepath=dst, use_selection=False, apply_scale_options="FBX_SCALE_UNITS", apply_unit_scale=True,
                         bake_space_transform=True, axis_forward="-Z", axis_up="Y", path_mode="STRIP", embed_textures=False, mesh_smooth_type="FACE")
print("EXPORTED", dst)
