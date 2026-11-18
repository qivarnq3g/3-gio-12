"""Rebuild issue #96 props from approved references; run with Blender 5.2.

blender --background --python tools/generate_issue96_props.py
Optional arguments after --: --render --save-blend --only <stem> [<stem> ...]
All geometry is authored in metres, Blender Z-up/front -Y. The FBX exporter
bakes Y-up into geometry, leaving every exported node at identity transform.
Reports are generated from FBX round trips, not estimated primitive counts.
"""

import argparse
import hashlib
import json
import math
import re
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "game/Assets/Art/Models/Props"
MATERIALS = ROOT / "game/Assets/Art/Materials"
QA = ROOT / "tools/out/issue96"
SOURCE = ROOT / "art-source/issue96"
TAU = math.tau
REPORT = []


def material(name):
    """Read existing Unity colour; never create/rename a Unity material."""
    old = bpy.data.materials.get(name)
    if old:
        return old
    path = MATERIALS / (name + ".mat")
    assert path.is_file(), path
    match = re.search(r"_BaseColor: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)",
                      path.read_text(encoding="utf-8"))
    assert match, path
    color = tuple(float(v) for v in match.groups())
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    metal = name in {"M_SteelDark", "M_MetalHandle", "M_ShutterBlue"}
    shader.inputs["Metallic"].default_value = .68 if metal else 0
    shader.inputs["Roughness"].default_value = .34 if metal else .78
    if name == "M_GlassFrosted":
        shader.inputs["Roughness"].default_value = .23
    if name == "M_BulbWarmGlow":
        shader.inputs["Emission Color"].default_value = (1, .73, .35, 1)
        shader.inputs["Emission Strength"].default_value = .6
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
    return bevel(obj, min(edge, min(size)*.45)) if edge else obj


def beam(name, a, b, width, depth, mat):
    """Rectangular stock along a segment; its narrow axis faces front."""
    a, b = Vector(a), Vector(b)
    d = (b-a).normalized()
    u = Vector((0, 1, 0))
    if abs(d.dot(u)) > .99:
        u = Vector((1, 0, 0))
    v = d.cross(u).normalized() * (width/2)
    u = v.normalized().cross(d).normalized() * (depth/2)
    verts = [tuple(p+i*u+j*v) for p in (a, b) for i, j in
             [(-1, -1), (1, -1), (1, 1), (-1, 1)]]
    return mesh(name, verts, [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4),
                             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], mat)


def tube(name, points, radius, mat, sides=8, closed=False):
    points = [Vector(p) for p in points]
    vertices, faces = [], []
    for i, p in enumerate(points):
        prev = points[(i-1) % len(points)] if closed or i else points[0]
        nxt = points[(i+1) % len(points)] if closed or i < len(points)-1 else p
        tangent = (nxt-prev).normalized()
        normal = tangent.cross(Vector((0, 0, 1)))
        if normal.length < .01:
            normal = tangent.cross(Vector((0, 1, 0)))
        normal.normalize()
        other = tangent.cross(normal).normalized()
        for j in range(sides):
            vertices.append(tuple(p+radius*(normal*math.cos(TAU*j/sides)
                                           + other*math.sin(TAU*j/sides))))
    for i in range(len(points) if closed else len(points)-1):
        k = (i+1) % len(points)
        for j in range(sides):
            n = (j+1) % sides
            faces.append((i*sides+j, i*sides+n, k*sides+n, k*sides+j))
    if not closed:
        faces += [tuple(reversed(range(sides))),
                  tuple((len(points)-1)*sides+j for j in range(sides))]
    return mesh(name, vertices, faces, mat, smooth=True)


def lathe(name, profile, mat, sides=16, transform=None, caps=True):
    verts = [(r*math.cos(TAU*j/sides), r*math.sin(TAU*j/sides), z)
             for z, r in profile for j in range(sides)]
    if transform:
        verts = [tuple(transform @ Vector(v)) for v in verts]
    faces = []
    for i in range(len(profile)-1):
        for j in range(sides):
            n = (j+1) % sides
            faces.append((i*sides+j, i*sides+n, (i+1)*sides+n, (i+1)*sides+j))
    if caps:
        faces += [tuple(reversed(range(sides))),
                  tuple((len(profile)-1)*sides+j for j in range(sides))]
    return mesh(name, verts, faces, mat, True)


def cylinder(name, a, b, radius, mat, sides=12):
    direction = Vector(b)-Vector(a)
    transform = Matrix.Translation(Vector(a)) @ direction.to_track_quat("Z", "Y").to_matrix().to_4x4()
    return lathe(name, [(0, radius), (direction.length, radius)], mat, sides, transform)


def channel(name, a, b, width, depth, thickness, mat):
    """Closed, genuinely U-shaped section, open on the +Y side."""
    a, b = Vector(a), Vector(b)
    d = (b-a).normalized()
    u = Vector((1, 0, 0)) if abs(d.z) > .9 else Vector((0, 0, 1))
    v = d.cross(u).normalized()
    w, h, t = width/2, depth/2, thickness
    profile = [(-w,-h),(w,-h),(w,h),(w-t,h),(w-t,-h+t),
               (-w+t,-h+t),(-w+t,h),(-w,h)]
    verts = [tuple(p+u*x+v*y) for p in (a,b) for x,y in profile]
    faces = [tuple(reversed(range(8))), tuple(range(8,16))]
    faces += [(j, (j+1)%8, (j+1)%8+8, j+8) for j in range(8)]
    return mesh(name, verts, faces, mat)


def rounded_rect(w, d, r, z, steps=8):
    return [(cx+r*math.cos(TAU*(q/4+j/(4*steps))),
             cy+r*math.sin(TAU*(q/4+j/(4*steps))), z)
            for q,(cx,cy) in enumerate([(w/2-r,d/2-r),(-w/2+r,d/2-r),
                                        (-w/2+r,-d/2+r),(w/2-r,-d/2+r)])
            for j in range(steps)]


def ellipsoid(name, center, size, mat, longitude=24, latitude=12, exponent=1, rotation=0):
    """Pole-safe sphere topology, also used as a soft rectangular pillow."""
    def signed(x, power):
        return math.copysign(abs(x)**power, x)
    verts = [(0, 0, -size[2]/2)]
    for i in range(1, latitude):
        a = -math.pi/2+math.pi*i/latitude
        for j in range(longitude):
            b = TAU*j/longitude
            verts.append((size[0]/2*signed(math.cos(a), exponent)*signed(math.cos(b), exponent),
                          size[1]/2*signed(math.cos(a), exponent)*signed(math.sin(b), exponent),
                          size[2]/2*signed(math.sin(a), .85 if exponent < 1 else 1)))
    top = len(verts)
    verts.append((0,0,size[2]/2))
    rot = Matrix.Rotation(rotation, 4, "X")
    verts = [tuple(rot @ Vector(v) + Vector(center)) for v in verts]
    faces = [(0,1+(j+1)%longitude,1+j) for j in range(longitude)]
    for i in range(latitude-2):
        for j in range(longitude):
            k = 1+i*longitude+j
            n = 1+i*longitude+(j+1)%longitude
            faces.append((k,n,n+longitude,k+longitude))
    faces += [(top, 1+(latitude-2)*longitude+j,
               1+(latitude-2)*longitude+(j+1)%longitude) for j in range(longitude)]
    return mesh(name, verts, faces, mat, True)


def pole(height, bottom_d, top_d, compact):
    # Hollow spun-concrete shaft, including 50 mm wall at the visible top.
    lathe("Spun_concrete_shaft", [(0,bottom_d/2), (height,top_d/2),
          (height,top_d/2-.05), (height-.12,top_d/2-.05)], "M_ConcretePole", 32)
    def radius(z):
        return bottom_d/2+(top_d-bottom_d)*z/(2*height)
    def band(z, thick=.035):
        r = radius(z)+.004
        lathe("Galvanized_clamp_band", [(z-thick/2,r), (z+thick/2,r),
              (z+thick/2,r-.003), (z-thick/2,r-.003), (z-thick/2,r)],
              "M_SteelDark", 20, caps=False)
        box("Clamp_tensioner", (r+.012,-.02,z), (.04,.055,thick), "M_SteelDark")
    cross_w = 1.2 if compact else 1.7
    for row in range(2 if compact else 3):
        z = height-.70-row*.84
        band(z-.05, .055)
        channel("Steel_crossarm", (-cross_w/2,.035,z), (cross_w/2,.035,z),
                .08,.065,.007,"M_SteelDark")
        box("Crossarm_saddle", (0,-.03,z), (.18,.17,.14), "M_SteelDark",.004)
        for x in (-.065,.065):
            cylinder("Crossarm_hex_bolt", (x,-.126,z), (x,-.139,z), .013,"M_MetalHandle",6)
        for side in (-1,1):
            beam("Crossarm_diagonal_brace", (side*.07,.035,z-.32),
                 (side*cross_w*.40,.035,z-.025), .022,.016,"M_SteelDark")
            x = side*(cross_w/2-.095)
            cylinder("Insulator_pin", (x,.035,z), (x,.035,z+.32),.012,"M_SteelDark",8)
            # Six flared ceramic/glass sheds on every pin; real radial silhouette.
            profile = [(0,.025)]
            for k in range(6):
                s = .022+k*.040
                profile += [(s,.026),(s+.008,.058),(s+.016,.049),(s+.025,.025)]
            profile += [(.277,.025)]
            lathe("Ribbed_pin_insulator",profile,"M_GlassFrosted",12,
                  Matrix.Translation((x,.035,z+.035)))
            cylinder("Wire_cap", (x,.035,z+.31),(x,.035,z+.34),.025,"M_SteelDark",8)
        # Short slack jumper only; no scene-spanning overhead wires.
        tube("Crossarm_slack_jumper", [(-cross_w*.44,.08,z+.34),
             (-cross_w*.30,.11,z+.15),(0,.13,z+.11),
             (cross_w*.30,.11,z+.15),(cross_w*.44,.08,z+.34)], .007,"M_WireBlack",6)
    lamp_z = height-1.8
    reach = 1.35 if compact else 1.8
    band(lamp_z,.06)
    band(lamp_z-.40,.045)
    tube("Swept_street_light_arm",[(0,-.08,lamp_z-.25), (0,-.20,lamp_z),
         (0,-reach*.48,lamp_z+.52),(0,-reach*.82,lamp_z+.69),
         (0,-reach,lamp_z+.69)], .026,"M_SteelDark",12)
    tube("Lamp_diagonal_stay",[(0,-.12,lamp_z-.4),
         (0,-reach*.62,lamp_z+.52)],.012,"M_SteelDark",8)
    tilt = math.radians(-13)
    ellipsoid("Tilted_luminaire_housing",(0,-reach-.19,lamp_z+.68),
              (.26,.62,.12),"M_SteelDark",24,8,rotation=tilt)
    ellipsoid("Warm_diffuser",(0,-reach-.19,lamp_z+.631),
              (.215,.52,.047),"M_BulbWarmGlow",24,6,rotation=tilt)
    # Two individual meter housings and their steel mounting straps, 2-3 m.
    for z in (2.22,2.78):
        band(z,.042)
        box("Meter_mounting_rail",(0,-radius(z)-.019,z),(.58,.036,.04),"M_SteelDark")
    for i,x in enumerate((-.155,.155)):
        z = 2.46+i*.13
        box("Composite_meter_housing",(x,-.235,z),(.255,.175,.42),"M_MeterBoxGray",.012)
        box("Meter_door_seam",(x,-.326,z),(.229,.012,.389),"M_SteelDark",.009)
        box("Composite_meter_door",(x,-.335,z),(.218,.010,.378),"M_MeterBoxGray",.008)
        box("Window_gasket",(x,-.343,z+.06),(.165,.008,.177),"M_WireBlack",.005)
        box("Frosted_meter_window",(x,-.349,z+.06),(.145,.005,.157),"M_GlassFrosted",.004)
        box("Meter_number_plate",(x,-.343,z-.105),(.115,.006,.035),"M_PaperWhite")
        cylinder("Meter_door_latch",(x+.077,-.348,z-.08),(x+.077,-.358,z-.08),
                 .012,"M_SteelDark",8)
        for dx in (-.096,.096):
            for dz in (-.165,.165):
                cylinder("Meter_door_screw",(x+dx,-.34,z+dz),(x+dx,-.346,z+dz),
                         .004,"M_MetalHandle",6)
        tube("Meter_service_conduit",[(x,-.25,z-.20),(x,-.20,z-.36),
             (x*.5,-.16,3.25),(x*.5,-.17,3.75)],.012,"M_WireBlack",8)
    coil_z = 3.95 if compact else 4.55
    for k in range(3):
        tube("Spare_cable_coil",[(.04+.285*math.cos(TAU*j/40),
             -.21-k*.027,coil_z+.285*math.sin(TAU*j/40)) for j in range(40)],
             .012,"M_WireBlack",6,True)
    for dx in (-.16,.22):
        box("Cable_coil_tie",(dx,-.28,coil_z+.20),(.018,.09,.09),"M_SteelDark")
    tube("Coil_drop_tail",[(.31,-.25,coil_z),(.34,-.24,coil_z-.38),
         (.20,-.20,3.27),(.02,-.17,3.10)],.010,"M_WireBlack",6)
    # Curved paper sheets follow the shaft with a 3 mm offset, torn lower edge.
    def curved_patch(name,z0,z1,left,right,mat,r_offset=.003,torn=False):
        verts = []
        for row,z in enumerate((z0,z1)):
            for j in range(9):
                a = left+(right-left)*j/8
                zz = z + (.008*math.sin(j*7) if torn and row == 0 else 0)
                r = radius(zz)+r_offset
                verts.append((r*math.sin(a),-r*math.cos(a),zz))
        return mesh(name,verts,[(j,j+1,j+10,j+9) for j in range(8)],mat)
    for z,left,right in [(1.00,-.80,.48),(1.48,-.45,.65)]:
        curved_patch("Torn_advertisement_paper",z,z+.27,left,right,"M_PaperWhite",torn=True)
        for line in range(4):
            zz = z+.055+line*.043
            curved_patch("Advertisement_print_lines",zz,zz+.006,left+.12,right-.15,
                          "M_WireBlack",.0036)
    # White painted inventory code 07; thin conformal faces, not floating text.
    segments = {"0":"abcdef", "7":"abc"}
    bars = {"a":(-.025,.11,.025,.11), "b":(.028,.105,.028,.058),
            "c":(.028,.05,.028,.004), "d":(-.025,0,.025,0),
            "e":(-.028,.004,-.028,.05), "f":(-.028,.058,-.028,.105)}
    for digit,xc in zip("07",(-.04,.04)):
        for s in segments[digit]:
            x0,z0,x1,z1 = bars[s]
            a0,a1=(xc+x0)/radius(1.91),(xc+x1)/radius(1.91)
            if z0 == z1:
                curved_patch("Painted_pole_code_07",1.91+z0-.004,1.91+z0+.004,
                              a0,a1,"M_PaperWhite",.0006)
            else:
                curved_patch("Painted_pole_code_07",1.91+min(z0,z1),1.91+max(z0,z1),
                              a0-.025,a0+.025,"M_PaperWhite",.0006)
    return {"shaft_m": [bottom_d, height, top_d], "crossarm_rows":2 if compact else 3,
            "meter_count":2, "reference":"ref_utility_pole.jpg"}


def gate(width, height):
    steel, blue = "M_SteelDark", "M_ShutterBlue"
    # Entire assembly fits nominal opening; rail underside is the floor datum.
    box("Fixed_bottom_track_web",(0,0,.007),(width,.09,.014),steel)
    for y in (-.038,.038):
        box("Fixed_bottom_track_lip",(0,y,.022),(width,.014,.03),steel)
    box("Fixed_top_track_web",(0,0,height-.007),(width,.10,.014),steel)
    for y in (-.042,.042):
        box("Fixed_top_track_lip",(0,y,height-.032),(width,.016,.05),steel)
    for side in (-1,1):
        box("Fixed_jamb",(side*(width/2-.015),.012,height/2),(.03,.065,height),steel)
    positions = [-width/2+.1+i*.1 for i in range(round(width/.1)-1)]
    for i,x in enumerate(positions):
        leaf = "LeftLeaf_" if x < 0 else "RightLeaf_"
        # Double centre stile replaces the central U, creating a proper joint.
        if abs(x) < .0001:
            continue
        channel(leaf+"U_upright",(x,0,.037),(x,0,height-.057),.029,.032,.003,blue)
        if i % 3 == 0:
            cylinder(leaf+"track_roller",(x,-.022,height-.047),(x,.022,height-.047),.019,steel,10)
    for side in (-1,1):
        leaf = "LeftLeaf_" if side < 0 else "RightLeaf_"
        channel(leaf+"meeting_stile",(side*.021,-.007,.028),
                (side*.021,-.007,height-.045),.035,.044,.004,blue)
        xs = [x for x in positions if x*side > .001]
        xs += [side*.021]
        xs.sort()
        for z in (.38,height/2,height-.38):
            for a,b in zip(xs,xs[1:]):
                # 180 mm-tall, short scissor strips, with physical front/back weave.
                beam(leaf+"scissor_front",(a,-.024,z-.09),(b,-.024,z+.09),.014,.005,blue)
                beam(leaf+"scissor_back",(a,-.017,z+.09),(b,-.017,z-.09),.014,.005,blue)
                cylinder(leaf+"scissor_pivot",((a+b)/2,-.030,z),
                         ((a+b)/2,-.036,z),.008,"M_MetalHandle",6)
            for x in xs:
                for dz in (-.09,.09):
                    cylinder(leaf+"end_rivet",(x,-.027,z+dz),(x,-.032,z+dz),.005,steel,6)
        x = side*.047
        tube(leaf+"pull_handle",[(x,-.038,height*.47-.085),
             (x,-.092,height*.47-.06),(x,-.099,height*.47+.06),
             (x,-.038,height*.47+.085)],.008,"M_MetalHandle",8)
        box(leaf+"lock_escutcheon",(side*.025,-.041,height*.47-.15),
            (.042,.012,.06),"M_MetalHandle",.003)
    box("RightLeaf_lock_hasps",(0,-.054,height*.47-.15),(.072,.012,.018),"M_MetalHandle")
    box("RightLeaf_padlock_body",(.014,-.069,height*.47-.19),(.035,.018,.040),"M_MetalHandle",.005)
    tube("RightLeaf_padlock_shackle",[(.003,-.069,height*.47-.17),
         (.003,-.069,height*.47-.151),(.025,-.069,height*.47-.151),
         (.025,-.069,height*.47-.17)],.0035,"M_MetalHandle",6)
    return {"opening_m":[width,height], "upright_pitch_m":.1,
            "diamond_band_height_m":.18, "diamond_bands":3,
            "reference":"ref_collapsible_gate_nguyenmode.jpg"}


def clip_polygon(poly, axis, value, keep_less):
    result = []
    for a,b in zip(poly, poly[1:]+poly[:1]):
        inside_a = a[axis] <= value+1e-9 if keep_less else a[axis] >= value-1e-9
        inside_b = b[axis] <= value+1e-9 if keep_less else b[axis] >= value-1e-9
        if inside_a:
            result.append(a)
        if inside_a != inside_b:
            t = (value-a[axis])/(b[axis]-a[axis])
            result.append(tuple(a[k]+t*(b[k]-a[k]) for k in range(2)))
    # Remove duplicate clipping vertices, especially at exact diamond corners.
    clean = []
    for p in result:
        if not clean or math.dist(p,clean[-1]) > 1e-7:
            clean.append(p)
    if len(clean)>1 and math.dist(clean[0],clean[-1]) < 1e-7:
        clean.pop()
    return clean


def boolean_difference(obj, cutter):
    activate(obj)
    mod = obj.modifiers.new("Physical drainage / molded recess", "BOOLEAN")
    mod.operation, mod.solver, mod.object = "DIFFERENCE", "EXACT", cutter
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter, do_unlink=True)


def wedge_body(name, width, depth, height, mat, front=.012):
    z = lambda y: front+(height-front)*(y/depth+.5)
    verts = [(-width/2,-depth/2,0),(width/2,-depth/2,0),(width/2,depth/2,0),
             (-width/2,depth/2,0),(-width/2,-depth/2,front),
             (width/2,-depth/2,front),(width/2,depth/2,height),(-width/2,depth/2,height)]
    return mesh(name,verts,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),
                            (2,3,7,6),(3,0,4,7)],mat),z


def concrete(width, depth):
    # Explicit shell: no Boolean on the densely segmented tread boundary.
    slope=lambda y:.012+.138*(y/depth+.5)
    verts,faces=[],[]
    boundaries={"front":set(),"back":set(),"left":set(),"right":set()}
    def surface(x,y,drop=0):
        # Small asymmetric corner spalls are part of the silhouette.
        k=max(0,1-(depth/2-y)/.035)*max(0,1-(width/2-abs(x))/.035)
        return (x-math.copysign(.008*k,x),y,slope(y)-drop-(.009 if x<0 else .006)*k)
    sx,sy=.30,.20
    n=math.ceil(width/sx+depth/sy)+2
    for u in range(-n,n):
        for v in range(-n,n):
            p=[((a+b)*sx/2,(a-b)*sy/2) for a,b in
               [(u,v),(u+1,v),(u+1,v+1),(u,v+1)]]
            p.reverse()
            for axis,value,less in [(0,-width/2,False),(0,width/2,True),
                                    (1,-depth/2,False),(1,depth/2,True)]:
                if p:
                    p=clip_polygon(p,axis,value,less)
            if len(p)<3:
                continue
            area=abs(sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(p,p[1:]+p[:1])))/2
            if area<1e-8:
                continue
            cx,cy=[sum(q[k] for q in p)/len(p) for k in (0,1)]
            start,count=len(verts),len(p)
            for x,y in p:
                boundary=False
                for key,condition in [("front",abs(y+depth/2)<1e-7),
                     ("back",abs(y-depth/2)<1e-7),("left",abs(x+width/2)<1e-7),
                     ("right",abs(x-width/2)<1e-7)]:
                    if condition:
                        boundaries[key].add((round(x,7),round(y,7)))
                        boundary=True
                verts.append(surface(x,y,0 if boundary else .003))
            for x,y in p:
                distance=math.hypot(cx-x,cy-y)
                k=min(.18,.004/max(distance,1e-6))
                verts.append(surface(x+k*(cx-x),y+k*(cy-y)))
            faces.append(tuple(start+count+j for j in range(count)))
            for j in range(count):
                k=(j+1)%count
                faces.append((start+j,start+k,start+count+k,start+count+j))
    # Front and back walls follow all tread boundary vertices.
    for key in ("front","back"):
        points=sorted(boundaries[key],reverse=key=="back")
        for (ax,ay),(bx,by) in zip(points,points[1:]):
            start=len(verts)
            verts += [surface(ax,ay),(ax,ay,0),(bx,by,0),surface(bx,by)]
            faces.append(tuple(start+j for j in range(4)))
    g0,g1=depth*.15-.0275,depth*.15+.0275
    bottom=[(-depth/2,0),(g0,0),(g0,.026),(g1,.026),(g1,0),(depth/2,0)]
    for key,side in [("right",1),("left",-1)]:
        points=sorted(boundaries[key],key=lambda p:p[1])
        start=len(verts)
        ring=[surface(x,y) for x,y in points]+[(side*width/2,y,z) for y,z in reversed(bottom)]
        verts += ring
        face=tuple(start+j for j in range(len(ring)))
        faces.append(face if side<0 else tuple(reversed(face)))
    for (a,za),(b,zb) in zip(bottom,bottom[1:]):
        start=len(verts)
        verts += [(-width/2,a,za),(-width/2,b,zb),(width/2,b,zb),(width/2,a,za)]
        faces.append(tuple(start+j for j in range(4)))
    mesh("Cast_concrete_diamond_tread_and_open_bottom_drain",verts,faces,"M_ConcreteFloor")
    return {"nominal_m":[width,.15,depth],"groove_depth_m":.003,
            "bottom_drain_width_m":.055,"bottom_drain_height_m":.026,
            "reference":"Issue #96 dimensions (no photo supplied)"}


def rubber():
    obj,slope=wedge_body("Molded_rubber_body",.5,.25,.15,"M_Rubber",.014)
    bevel(obj,.002)
    for x in (-.19,.19):
        cut=cylinder("Anchor_recess_cutter",(x,.077,-.02),(x,.077,.20),.009,"M_Rubber",8)
        boolean_difference(obj,cut)
    # Yellow inserts are separate thin faces set on the sloping rubber surface.
    for x in (-.12,0,.12):
        coords=[(x-.025,-.108),(x+.025,-.108),(x+.034,.105),(x-.034,.105)]
        mesh("Yellow_reflective_insert",[(a,b,slope(b)+.0008) for a,b in coords],
             [(0,1,2,3)],"M_CurbYellow")
    for j in range(8):
        y=-.092+j*.025
        mesh("Molded_anti_slip_ridge",[(-.235,y-.0025,slope(y-.0025)),
             (.235,y-.0025,slope(y-.0025)),(.235,y,slope(y)+.002),
             (-.235,y,slope(y)+.002),(-.235,y+.0025,slope(y+.0025)),
             (.235,y+.0025,slope(y+.0025))],[(0,1,2,3),(3,2,5,4)],"M_Rubber")
    return {"nominal_m":[.5,.15,.25],"anchor_holes":2,
            "reference":"Issue #96 dimensions (no photo supplied)"}


def steel_ramp():
    width,depth=.8,.6
    slope=lambda y:.030+.12*(y/depth+.5)
    def sloping_stock(name,x0,x1,y0,y1,thickness):
        verts=[(x,y,slope(y)-t) for t in (thickness,0)
               for x,y in [(x0,y0),(x1,y0),(x1,y1),(x0,y1)]]
        return mesh(name,verts,[(3,2,1,0),(4,5,6,7),(0,1,5,4),
             (1,2,6,5),(2,3,7,6),(3,0,4,7)],"M_SteelDark")
    for x in (-.384,.384):
        sloping_stock("Welded_side_box_section",x-.016,x+.016,-.3,.3,.03)
    for y in (-.284,.284):
        sloping_stock("Welded_cross_box_section",-.368,.368,y-.016,y+.016,.03)
    for i in range(17):
        x=-.34+i*.0425
        sloping_stock("Parallel_longitudinal_flat_bar",x-.008,x+.008,-.268,.268,.014)
        for y in (-.265,.265):
            z=slope(y)
            mesh("Weld_fillet",[(x-.011,y-.004,z-.002),(x+.011,y-.004,z-.002),
                 (x+.011,y+.004,z-.002),(x-.011,y+.004,z-.002),
                 (x-.009,y,z+.001),(x+.009,y,z+.001)],
                 [(0,3,2,1),(0,1,5,4),(3,4,5,2),(0,4,3),(1,2,5)],"M_SteelDark")
    # Rear legs and an underside tie carry load without filling the open grid.
    for x in (-.35,.35):
        box("Rear_support_foot",(x,.277,.058),(.032,.032,.116),"M_SteelDark")
    box("Underside_cross_tie",(0,.04,slope(.04)-.023),(.736,.025,.018),"M_SteelDark")
    return {"nominal_m":[.8,.15,.6],"parallel_bars":17,
            "reference":"ref_curb_ramp_steel.jpg"}


def mattress():
    layers=[(1.15,1.85,.10,0),(1.188,1.888,.105,.007),
            (1.2,1.9,.11,.023),(1.2,1.9,.11,.073),
            (1.186,1.886,.10,.094),(1.14,1.84,.09,.10)]
    verts=[p for w,d,r,z in layers for p in rounded_rect(w,d,r,z)]
    count=32
    faces=[tuple(reversed(range(count)))]
    for i in range(len(layers)-1):
        for j in range(count):
            k=(j+1)%count
            faces.append((i*count+j,i*count+k,(i+1)*count+k,(i+1)*count+j))
    faces.append(tuple((len(layers)-1)*count+j for j in range(count)))
    obj=mesh("Foam_mattress_plain_fitted_sheet",verts,faces,"M_ClothBlue",True)
    # Keep the broad top absolutely planar; only side rings are softly shaded.
    obj.data.polygons[0].use_smooth=False
    obj.data.polygons[-1].use_smooth=False
    for z in (.023,.076):
        tube("Same_colour_sewn_piping",rounded_rect(1.193,1.893,.108,z,12),
             .0025,"M_ClothBlue",6,True)
    ellipsoid("Single_white_pillow",(0,.63,.1525),(.62,.40,.105),
              "M_PaperWhite",40,20,exponent=.42)
    return {"mattress_body_m":[1.2,.1,1.9],"pillow_count":1,
            "flat_clear_patch_blender_xy_m":[[-.4,-.65],[.4,.20]],
            "flat_patch_height_m":.1,"reference":"Approved sample A in Issue #96 (text brief)"}


BUILDERS = {
    "prop_utility_pole_10m":lambda:pole(10.5,.330,.190,False),
    "prop_utility_pole_8m":lambda:pole(8.5,.255,.160,True),
    "prop_collapsible_gate_shop":lambda:gate(3.2,2.6),
    "prop_collapsible_gate_small":lambda:gate(1.2,2.2),
    "prop_curb_ramp_concrete_a":lambda:concrete(1.2,.50),
    "prop_curb_ramp_concrete_b":lambda:concrete(2.0,.55),
    "prop_curb_ramp_concrete_c":lambda:concrete(3.0,.60),
    "prop_curb_ramp_rubber":rubber,
    "prop_curb_ramp_steel":steel_ramp,
    "prop_mattress_room7":mattress,
}


def objects():
    return [o for o in bpy.context.scene.objects if o.type == "MESH"]


def normalize(stem):
    # Group gates into fixed frame and two independent sliding leaves.
    groups={}
    for obj in objects():
        group="Fixed_frame" if "gate" in stem else stem
        if obj.name.startswith("LeftLeaf_"):
            group="Left_sliding_leaf"
        elif obj.name.startswith("RightLeaf_"):
            group="Right_sliding_leaf"
        groups.setdefault(group,[]).append(obj)
    for name,objs in groups.items():
        activate(objs[0])
        for o in objs:
            o.select_set(True)
        if len(objs)>1:
            bpy.ops.object.join()
        obj=bpy.context.object
        obj.name=name
        obj.data.name=name+"_mesh"
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        bm=bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7)
        bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-8)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(obj.data)
        bm.free()
    if stem=="prop_curb_ramp_rubber":
        # Mold edge rounding shortens its apex; restore exact 150 mm envelope.
        top=max(v.co.z for o in objects() for v in o.data.vertices)
        for obj in objects():
            for v in obj.data.vertices:
                v.co.z *= .15/top
    coords=[v.co for o in objects() for v in o.data.vertices]
    offset=Vector(((min(v.x for v in coords)+max(v.x for v in coords))/2,
                   (min(v.y for v in coords)+max(v.y for v in coords))/2,
                   min(v.z for v in coords)))
    for obj in objects():
        # Geometry moves; every object pivot remains the shared floor datum.
        for v in obj.data.vertices:
            v.co-=offset
        activate(obj)
        tri=obj.modifiers.new("Exported_triangle_topology","TRIANGULATE")
        bpy.ops.object.modifier_apply(modifier=tri.name)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.006)
        bpy.ops.object.mode_set(mode="OBJECT")
        obj.data.update()
    return list(offset)


def stats():
    coords=[o.matrix_world@v.co for o in objects() for v in o.data.vertices]
    lo=[min(v[k] for v in coords) for k in range(3)]
    hi=[max(v[k] for v in coords) for k in range(3)]
    count=sum(len(o.data.vertices) for o in objects())
    triangles=sum(len(o.data.polygons) for o in objects())
    for obj in objects():
        assert obj.location.length < 1e-5, (obj.name,"location",list(obj.location))
        assert max(abs(v) for v in obj.rotation_euler)<1e-5, (obj.name,"rotation",list(obj.rotation_euler))
        assert all(abs(s-1)<1e-6 for s in obj.scale), (obj.name,"scale")
        assert len(obj.data.uv_layers)>0
        bad=[(p.index,p.area,[tuple(obj.data.vertices[i].co) for i in p.vertices])
             for p in obj.data.polygons if len(p.vertices)!=3 or p.area<=1e-12]
        assert not bad, (obj.name,"degenerate triangle",bad[:5])
        uv=obj.data.uv_layers.active.data
        for p in obj.data.polygons:
            a,b,c=[uv[i].uv for i in p.loop_indices]
            assert abs((b-a).x*(c-a).y-(b-a).y*(c-a).x)>1e-14, (obj.name,"degenerate UV")
        assert all(-1e-6<=c<=1.000001 for lp in uv for c in lp.uv)
    assert abs(lo[2])<1e-5, ("floor",lo)
    assert abs(lo[0]+hi[0])<1e-5 and abs(lo[1]+hi[1])<1e-5
    return {"vertices":count,"triangles":triangles,
            "dimensions_unity_m":[round(hi[0]-lo[0],6),round(hi[2]-lo[2],6),round(hi[1]-lo[1],6)],
            "bounds_min_unity_m":[round(lo[0],6),round(lo[2],6),round(-hi[1],6)],
            "materials":sorted({m.name for o in objects() for m in o.data.materials}),
            "mesh_objects":len(objects())}


def validate_fbx_payload(path):
    """Check the actual serialized Y-up mesh and transforms, independently of reimport."""
    from io_scene_fbx import parse_fbx
    root,_=parse_fbx.parse(str(path))
    def child(node,key):
        return next((c for c in node.elems if c.id==key),None)
    global_props=child(child(root,b"GlobalSettings"),b"Properties70")
    settings={p.props[0]:p.props[-1] for p in global_props.elems}
    assert settings[b"UpAxis"]==1 and settings[b"UnitScaleFactor"]==100.0, settings
    data=child(root,b"Objects")
    coords=[]
    for node in data.elems:
        if node.id==b"Geometry":
            xyz=child(node,b"Vertices").props[0]
            coords.extend(zip(xyz[0::3],xyz[1::3],xyz[2::3]))
        if node.id==b"Model":
            props=child(node,b"Properties70")
            for p in props.elems:
                if p.props[0] in (b"Lcl Rotation",b"Lcl Translation",b"PreRotation",b"PostRotation"):
                    assert max(abs(x) for x in p.props[-3:])<1e-5,(path,p.props)
                if p.props[0]==b"Lcl Scaling":
                    assert max(abs(x-1) for x in p.props[-3:])<1e-6,(path,p.props)
    lo=[min(v[k] for v in coords) for k in range(3)]
    hi=[max(v[k] for v in coords) for k in range(3)]
    assert abs(lo[1])<1e-5,(path,"FBX floor",lo)
    assert abs(lo[0]+hi[0])<1e-5 and abs(lo[2]+hi[2])<1e-5
    return [round(hi[k]-lo[k],6) for k in range(3)]


def render(stem):
    QA.mkdir(parents=True,exist_ok=True)
    scene=bpy.context.scene
    scene.render.engine="CYCLES"
    scene.cycles.samples=24
    scene.cycles.use_denoising=True
    scene.cycles.device="CPU"
    scene.render.resolution_x=1120
    scene.render.resolution_y=960
    scene.render.resolution_percentage=100
    scene.world.use_nodes=True
    background=scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value=(.22,.25,.30,1)
    background.inputs["Strength"].default_value=.35
    scene.view_settings.view_transform="AgX"
    scene.render.image_settings.file_format="PNG"
    asset_objects=objects()
    # Unity source colours are displayed as sRGB; preview BSDF expects linear RGB.
    for mat in {m for o in asset_objects for m in o.data.materials}:
        shader=mat.node_tree.nodes.get("Principled BSDF")
        rgba=shader.inputs["Base Color"].default_value
        shader.inputs["Base Color"].default_value=tuple(
            c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in rgba[:3])+(1,)
    co=[v.co for o in asset_objects for v in o.data.vertices]
    lo=Vector([min(v[k] for v in co) for k in range(3)])
    hi=Vector([max(v[k] for v in co) for k in range(3)])
    target=(lo+hi)/2
    size=hi-lo
    is_pole="pole" in stem
    is_gate="gate" in stem
    extent=max(size)
    direction=Vector((3.5,-4.7,1.5)) if is_pole else Vector((1.8,-4.8,2.0 if is_gate else 4.3))
    direction.normalize()
    cam_data=bpy.data.cameras.new("QA_camera")
    cam=bpy.data.objects.new("QA_camera",cam_data)
    scene.collection.objects.link(cam)
    cam.location=target+direction*extent*2.4
    cam.rotation_euler=(target-cam.location).to_track_quat("-Z","Y").to_euler()
    cam_data.type="ORTHO"
    view=cam.rotation_euler.to_matrix().transposed()
    projected=[view@(v-target) for v in co]
    px=max(v.x for v in projected)-min(v.x for v in projected)
    py=max(v.y for v in projected)-min(v.y for v in projected)
    aspect=scene.render.resolution_x/scene.render.resolution_y
    cam_data.ortho_scale=max(px,py*aspect)*1.18
    center_view=Vector(((max(v.x for v in projected)+min(v.x for v in projected))/2,
                        (max(v.y for v in projected)+min(v.y for v in projected))/2,0))
    cam.location += view.transposed()@center_view
    scene.camera=cam
    for pos,power,scale in [((-3,-4,7),150,5),((4,-1,5),80,4),((0,4,6),180,3)]:
        light_data=bpy.data.lights.new("QA_softbox","AREA")
        light_data.energy=power*max(1,extent**2/5)
        light_data.shape="DISK"
        light_data.size=scale*max(.4,extent/3)
        light=bpy.data.objects.new("QA_softbox",light_data)
        scene.collection.objects.link(light)
        light.location=target+Vector(pos)*max(.4,extent/3)
        light.rotation_euler=(target-light.location).to_track_quat("-Z","Y").to_euler()
    floor=box("QA_floor",(0,0,-.012),(extent*200,extent*200,.02),"M_ConcreteFloor")
    # QA-only neutral floor material, never included in FBX or reported materials.
    floor.data.materials.clear()
    mat=bpy.data.materials.new("QA_floor_only")
    mat.diffuse_color=(.11,.13,.16,1)
    mat.use_nodes=True
    mat.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value=(.11,.13,.16,1)
    mat.node_tree.nodes.get("Principled BSDF").inputs["Roughness"].default_value=.85
    floor.data.materials.append(mat)
    scene.render.filepath=str(QA/(stem+".png"))
    bpy.ops.render.render(write_still=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    # Avoid .001 material suffixes on subsequent FBX round trips.
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    for data in list(bpy.data.meshes):
        if data.users==0:
            bpy.data.meshes.remove(data)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--render",action="store_true")
    parser.add_argument("--save-blend",action="store_true")
    parser.add_argument("--only",nargs="+",choices=list(BUILDERS))
    args=parser.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [])
    assert bpy.app.version[:2]==(5,2), bpy.app.version_string
    DEST.mkdir(parents=True,exist_ok=True)
    for stem,build in BUILDERS.items():
        if args.only and stem not in args.only:
            continue
        clear_scene()
        scene=bpy.context.scene
        scene.unit_settings.system="METRIC"
        scene.unit_settings.scale_length=1
        details=build()
        offset=normalize(stem)
        before=stats()
        if "nominal_m" in details:
            assert max(abs(a-b) for a,b in zip(before["dimensions_unity_m"],details["nominal_m"]))<1e-5
        if "opening_m" in details:
            assert max(abs(a-b) for a,b in zip(before["dimensions_unity_m"][:2],details["opening_m"]))<1e-5
        if "shaft_m" in details:
            assert abs(before["dimensions_unity_m"][1]-details["shaft_m"][1])<1e-5
        limit=1000 if "curb_ramp" in stem else 10000
        assert before["triangles"]<limit,(stem,before["triangles"],limit)
        path=DEST/(stem+".fbx")
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={"MESH"},
            global_scale=1,apply_unit_scale=True,apply_scale_options="FBX_SCALE_UNITS",
            axis_forward="-Z",axis_up="Y",bake_space_transform=True,
            use_mesh_modifiers=True,mesh_smooth_type="FACE",use_tspace=True,
            add_leaf_bones=False,bake_anim=False,path_mode="STRIP",embed_textures=False)
        serialized_dims=validate_fbx_payload(path)
        assert serialized_dims==before["dimensions_unity_m"],(stem,serialized_dims,before)
        if args.save_blend:
            SOURCE.mkdir(parents=True,exist_ok=True)
            bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(stem+".blend")),check_existing=False)
        clear_scene()
        bpy.ops.import_scene.fbx(filepath=str(path),use_custom_normals=True,bake_space_transform=True)
        after=stats()
        assert before==after,(stem,"FBX round-trip mismatch",before,after)
        if stem=="prop_mattress_room7":
            bpy.context.view_layer.update()
            obj=objects()[0]
            # Verify the usable backpack area against the actual imported surface.
            for i in range(5):
                for j in range(5):
                    origin=Vector((-.4+.8*i/4,-.65+.85*j/4,.35))
                    hit,point,normal,_=obj.ray_cast(origin,Vector((0,0,-1)))
                    assert hit and abs(point.z-.1)<1e-5 and normal.z>.999
        REPORT.append({"file":path.name,**after,"triangle_limit_exclusive":limit,
            "checks":{"uv_non_degenerate":True,"uv_in_0_1":True,"all_nodes_identity":True,
                      "floor_y_zero":True,"centered_xz":True,"fbx_round_trip":True,
                      "serialized_y_up_metres":True},
            "details":details,"authoring_center_offset_blender_m":offset,
            "sha256":hashlib.sha256(path.read_bytes()).hexdigest(),"bytes":path.stat().st_size})
        print("ISSUE96_OK",json.dumps(REPORT[-1],ensure_ascii=False),flush=True)
        if args.render:
            render(stem)
    report_path=ROOT/"tools/issue96_props_report.json"
    if args.only and report_path.exists():
        existing=json.loads(report_path.read_text(encoding="utf-8"))["models"]
        fresh={r["file"]:r for r in REPORT}
        models=[fresh.get(r["file"],r) for r in existing]
        models += [r for r in REPORT if r["file"] not in {x["file"] for x in existing}]
    else:
        models=REPORT
    report_path.write_text(json.dumps({"blender":bpy.app.version_string,
        "axis":"Unity X width, Y height, Z depth; metres",
        "vertices":"Blender mesh vertices after FBX reimport; Unity may split at UV/normal seams",
        "unity_art_pipeline_check":"Pending Anti's Unity execution", "models":models},
        ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    lines=["# Bàn giao model cho Anti: issue #96", "",
           "Đã dựng bằng Blender 5.2.3 LTS và kiểm tra bằng cách xuất rồi nhập lại từng FBX.",
           "Kích thước là bao toàn bộ model theo trục Unity (X rộng, Y cao, Z sâu), đơn vị mét.",
           "Số đỉnh là số đỉnh lưới Blender sau nhập lại; Unity có thể tách thêm đỉnh tại biên UV hoặc pháp tuyến.", "",
           "| Tệp FBX | Kích thước (X, Y, Z), m | Số đỉnh | Số tam giác | Vật liệu |",
           "|---|---|---:|---:|---|"]
    for row in models:
        dim=" × ".join(f"{d:.6f}".rstrip("0").rstrip(".") for d in row["dimensions_unity_m"])
        mats=", ".join("`"+m+"`" for m in row["materials"])
        lines.append(f"| `{row['file']}` | {dim} | {row['vertices']} | {row['triangles']} | {mats} |")
    lines += ["", "Tất cả 10 model: đáy Y = 0, giữa X/Z, các nút không có dịch chuyển hoặc xoay, tỉ lệ 1; có UV trong 0–1, không có tam giác hình học hoặc UV suy biến. Đã đọc trực tiếp các nút và tọa độ trong FBX để kiểm tra Y hướng lên và đơn vị mét. Các ngưỡng tam giác đều đạt.",
              "", "Thân nệm đúng 1,20 × 1,90 × 0,10 m; chiều cao 0,205 m trong bảng tính cả gối. Mặt nệm trống phẳng 0,80 × 0,85 m. Cột 10m/8m có thân dài 10,50/8,50 m; chiều rộng và chiều sâu trong bảng bao gồm xà, cần đèn và hộp điện. Hai cửa tách khung cố định và hai cánh trượt; chưa có hoạt ảnh xếp cửa.",
              "", "Nguồn kích thước, phạm vi sử dụng ảnh và tình trạng tra cứu catalogue đã ghi trong [NGUON.md](../game/Assets/Art/_Source/NGUON.md). Chỉ dùng tên vật liệu đã tồn tại, không sửa vật liệu Unity.",
              "", "Script: [generate_issue96_props.py](generate_issue96_props.py). Dữ liệu kiểm tra và SHA-256 của từng FBX: [issue96_props_report.json](issue96_props_report.json). Ảnh render từ FBX nhập lại nằm trong `tools/out/issue96/`; nguồn Blender tại `art-source/issue96/` khi dùng tùy chọn dưới đây, cần chuyển lên Drive theo quy định dự án.",
              "", "```powershell",
              '& "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --python-exit-code 1 --python tools/generate_issue96_props.py -- --render --save-blend',
              "```", "",
              "Chưa chạy Unity ArtPipelineCheck. Anti nhập 10 asset, kiểm tra ánh xạ vật liệu, chạy `ArtPipelineCheck.Run` và bổ sung các tệp `.meta` do Unity tạo. Không sửa `env_alley.fbx`, `env_garage.fbx`, `RealisticSceneBuilder.cs` hoặc cảnh `.unity`.", ""]
    (ROOT/"tools/issue96_props_report.md").write_text("\n".join(lines),encoding="utf-8")
    print("ISSUE96_COMPLETE",len(REPORT),str(report_path),flush=True)


if __name__=="__main__":
    main()
