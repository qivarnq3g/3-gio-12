"""Build the ward police station and reception counter with Blender 5.2."""

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "game/Assets/Art"
PROJECT_FONT = ART / "Resources/Fonts/BeVietnamPro-Medium.ttf"
PALETTE = {
    "M_WallPaintYellow": (0.74, 0.57, 0.32),
    "M_WallPaintOffWhite": (0.79, 0.75, 0.63),
    "M_WallPlinthGray": (0.22, 0.24, 0.22),
    "M_FloorTileCream": (0.61, 0.58, 0.48),
    "M_FloorTileGray": (0.28, 0.30, 0.29),
    "M_ShutterGreen": (0.065, 0.16, 0.11),
    "M_SteelDark": (0.07, 0.08, 0.08),
    "M_GlassDark": (0.12, 0.19, 0.19),
    "M_BikeBlack": (0.12, 0.12, 0.13),
    "M_PoliceGlass": (0.62, 0.74, 0.78),
    "M_AluminumFrame": (0.62, 0.64, 0.66),
    "M_MetalHandle": (0.55, 0.56, 0.58),
    "M_Rubber": (0.05, 0.05, 0.05),
    "M_ScreenBlack": (0.06, 0.07, 0.08),
    "M_PlasticRed": (0.60, 0.05, 0.04),
    "M_PoliceSignRed": (0.51, 0.031, 0.024),
    "M_PoliceSignGold": (0.97, 0.69, 0.16),
    "M_DoorWoodBrown": (0.34, 0.19, 0.10),
    "M_DoorWoodDark": (0.13, 0.071, 0.039),
    "M_PaperWhite": (0.83, 0.81, 0.70),
    "M_PlasticBlue": (0.038, 0.15, 0.34),
    "M_PlasticWhite": (0.71, 0.72, 0.65),
}


def use_material(obj, name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (*PALETTE[name], .28 if name == "M_PoliceGlass" else 1)
        if name == "M_PoliceGlass":
            mat.surface_render_method = "BLENDED"
    obj.data.materials.append(mat)


def box(name, center, size, material, bevel=0, solid=False):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = ("Solid_" if solid else "") + name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    use_material(obj, material)
    if bevel:
        mod = obj.modifiers.new("Rounded edges", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def tube(name, first, last, radius, material):
    start, end = Vector(first), Vector(last)
    direction = end - start
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=radius, depth=direction.length,
                                        location=(start + end) * 0.5)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    use_material(obj, material)


def lettering(name, text, center, height, max_width, material):
    curve = bpy.data.curves.new(name, "FONT")
    curve.font = bpy.data.fonts.load(str(PROJECT_FONT), check_existing=True)
    curve.body = text
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.size = height
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    if not obj.data.uv_layers:
        obj.data.uv_layers.new()
    bpy.context.view_layer.update()
    obj.scale.x = min(1, max_width / obj.dimensions.x)
    obj.rotation_euler = (math.pi / 2, 0, math.pi)
    obj.location = center
    use_material(obj, material)


def window(x, y, z, width, height, grille=True, facing=1):
    box("Window reveal", (x, y, z), (width + .19, .10, height + .19), "M_WallPlinthGray", .012)
    box("Window glazing", (x, y + facing * .07, z), (width, .024, height), "M_GlassDark")
    metal = "M_SteelDark"
    for side in (-1, 1):
        box("Window jamb", (x + side * width / 2, y + facing * .095, z), (.046, .065, height + .05), metal)
        box("Window rail", (x, y + facing * .095, z + side * height / 2), (width, .065, .05), metal)
    box("Window meeting rail", (x, y + facing * .1, z), (width, .063, .035), metal)
    box("Window mullion", (x, y + facing * .1, z), (.041, .061, height), metal)
    box("Window sill", (x, y + facing * .1, z - height / 2 - .1), (width + .24, .30, .09), "M_WallPaintOffWhite", .012)
    if grille:
        for i in range(1, 11):
            box("Window grille", (x - width / 2 + width * i / 11, y + facing * .17, z), (.018, .025, height), metal)


def build_station():
    cream = "M_WallPaintYellow"
    trim = "M_WallPaintOffWhite"
    stone = "M_WallPlinthGray"
    green = "M_ShutterGreen"
    dark = "M_SteelDark"
    red = "M_PoliceSignRed"
    gold = "M_PoliceSignGold"

    # The +Y facing facade exports toward the road (-Z in Unity).
    box("Reception floor", (0, -5.02, .045), (15.14, 9.15, .09), "M_FloorTileCream", solid=True)
    box("Entrance apron", (0, .21, .0125), (15.60, 1.52, .025), "M_FloorTileGray", solid=True)
    box("Porch step", (0, -.34, .025), (3.65, .52, .05), "M_FloorTileGray", .012, True)
    for side in (-1, 1):
        x = side * 4.44
        box("Facade wing", (x, -1.17, 1.62), (6.32, .24, 3.08), cream, .012, True)
        box("Facade skirting", (x, -1.025, .43), (6.32, .06, .69), stone, .012)
        box("Side wall", (side * 7.54, -5.34, 3.05), (.26, 8.55, 5.97), cream, .012, True)
        box("Side base render", (side * 7.68, -5.34, .40), (.035, 8.45, .64), stone)
        box("Porch pillar", (side * 1.58, -.61, 1.52), (.36, .35, 2.87), trim, .02, True)
        box("Pillar plinth", (side * 1.58, -.61, .25), (.43, .43, .32), stone, .016)
        box("Door jamb", (side * 1.25, -1.00, 1.21), (.105, .16, 2.26), dark, .009)
        window(side * 4.72, -1.00, 1.86, 2.73, 1.30)
        window(side * 4.72, -.97, 4.49, 2.75, 1.33, False)
        window(side * 4.72, -9.78, 4.42, 2.64, 1.17, grille=False, facing=-1)

    box("Rear wall", (0, -9.64, 3.05), (15.10, .24, 5.97), cream, .012, True)
    box("Upper facade", (0, -1.15, 4.54), (15.1, .26, 3.02), cream, .012)
    box("Entrance transom", (0, -1.15, 2.75), (2.58, .25, .81), trim, .012)
    box("Open doorway head", (0, -1.00, 2.34), (2.58, .16, .105), dark, .009)
    box("Porch canopy", (0, -.68, 3.06), (3.99, 1.32, .20), trim, .028)
    box("Canopy fascia", (0, -.010, 3.07), (3.94, .052, .14), stone, .01)
    box("Canopy lamp", (0, -.54, 2.94), (.92, .15, .054), "M_PlasticWhite", .016)
    box("Floor band", (0, -.96, 3.23), (15.40, .16, .17), trim, .019)
    box("Reception ceiling", (0, -5.28, 3.03), (14.91, 8.32, .11), trim)
    box("Roof slab", (0, -5.31, 6.03), (15.44, 8.99, .22), trim, .023)
    box("Front parapet", (0, -1.0, 6.37), (15.39, .24, .47), cream, .020)
    box("Rear parapet", (0, -9.61, 6.37), (15.39, .23, .47), cream, .020)
    for side in (-1, 1):
        box("Parapet return", (side * 7.55, -5.31, 6.37), (.24, 8.39, .47), cream, .020)
        box("Parapet coping", (side * 7.55, -5.31, 6.62), (.34, 8.32, .085), trim, .016)
        x = side * 7.40
        y = -.96 if side == 1 else -9.80
        tube("Rainwater pipe", (x, y, .28), (x, y, 5.82), .044, "M_PlasticWhite")
        for z in (1.25, 2.85, 4.43):
            tube("Pipe collar", (x, y, z), (x, y, z + .038), .053, dark)
    for y in (-1.0, -9.61):
        box("Parapet coping", (0, y, 6.62), (15.48, .34, .085), trim, .016)

    # Piers and folded leaves leave a 3.20 m clear passage aligned with the door.
    for side in (-1, 1):
        x = side * 4.76
        box("Fence wall", (x, .46, .37), (6.04, .28, .74), cream, .02, True)
        box("Fence coping", (x, .46, .77), (6.14, .37, .10), trim, .018)
        for pier in (1.82, 4.76, 7.65):
            box("Gate pier", (side * pier, .46, 1.03), (.43, .48, 2.06), cream, .023, True)
            box("Pier shoe", (side * pier, .46, .20), (.49, .54, .40), stone, .014)
            box("Pier cap", (side * pier, .46, 2.085), (.53, .58, .105), trim, .019)
        for a, b in ((2.035, 4.545), (4.975, 7.435)):
            box("Fence lower rail", (side * (a + b) / 2, .46, .86), (b - a, .05, .055), green)
            box("Fence upper rail", (side * (a + b) / 2, .46, 1.74), (b - a, .05, .055), green)
            for i in range(1, 13):
                box("Fence picket", (side * (a + (b - a) * i / 13), .46, 1.31), (.035, .037, 1.03), green, .003)
        for z in (.24, 1.78):
            box("Open gate rail", (side * 1.83, -.17, z), (.060, 1.06, .06), green)
        for y in (-.69, .35):
            box("Open gate stile", (side * 1.83, y, 1.01), (.06, .062, 1.61), green)
        for i in range(1, 8):
            box("Gate picket", (side * 1.83, -.69 + i * .13, 1.01), (.038, .036, 1.55), green)
        box("Gate latch", (side * 1.83, -.70, 1.10), (.13, .10, .06), dark, .01)

    box("Sign backing", (0, -.926, 3.99), (4.88, .12, 1.21), dark, .025)
    box("Red enamel sign", (0, -.849, 3.99), (4.76, .036, 1.09), red, .012)
    for side in (-1, 1):
        box("Sign horizontal border", (0, -.824, 3.99 + side * .475), (4.56, .008, .016), gold)
        box("Sign vertical border", (side * 2.28, -.824, 3.99), (.016, .008, .96), gold)
    lettering("Station title", "CÔNG AN PHƯỜNG", (0, -.824, 4.19), .37, 4.07, gold)
    lettering("Fictional ward", "HẺM LƯƠNG", (0, -.824, 3.79), .47, 3.84, gold)

    # The visitor can see the service desk and a waiting bench from the gate.
    box("Waiting bench seat", (3.28, -2.80, .55), (2.10, .50, .10), "M_DoorWoodBrown", .023, True)
    box("Waiting bench back", (3.28, -3.015, .92), (2.10, .075, .40), "M_DoorWoodBrown", .018)
    for x in (2.41, 4.15):
        for y in (-3.0, -2.60):
            box("Waiting bench frame", (x, y, .32), (.054, .054, .46), dark, .008)
    box("Notice board frame", (3.25, -5.80, 1.70), (1.25, .065, .82), "M_DoorWoodDark", .017)
    box("Notice board display", (3.25, -5.75, 1.70), (1.16, .031, .73), green)
    for i in range(3):
        x = 2.95 + i * .29
        box("Pinned notice", (x, -5.729, 1.73 - i * .021), (.22, .005, .31), "M_PaperWhite", .003)
        for line in range(4):
            box("Notice line", (x, -5.725, 1.80 - i * .021 - line * .034), (.15, .002, .008), stone)
    box("Reception partition", (3.38, -5.91, 1.54), (6.25, .18, 2.9), trim, .016, True)


def build_desk():
    # +Y faces the visitor, -Y faces the officer. Solid_ pieces keep the collision used by Night 3.
    wood = "M_DoorWoodBrown"
    dark = "M_DoorWoodDark"
    black = "M_BikeBlack"
    paper = "M_PaperWhite"
    alu = "M_AluminumFrame"
    box("Service counter top", (0, .015, 1.045), (1.91, .74, .088), wood, .029, True)
    box("Visitor panel", (0, .257, .53), (1.68, .12, .94), dark, .015, True)
    box("Kick plate", (0, .327, .105), (1.71, .027, .17), black, .008)
    for side in (-1, 1):
        box("Counter side panel", (side * .823, -.035, .525), (.11, .61, .95), wood, .016, True)
        box("Counter glides", (side * .81, -.029, .03), (.125, .55, .06), "M_Rubber", .008)

    # Visitor face: slats framed by a red band with brass pinstripes and a base rail.
    for i in range(20):
        box("Vertical timber slat", (-.75 + i * .079, .327, .55), (.043, .026, .64), wood, .009)
    box("Front fascia band", (0, .347, .935), (1.64, .018, .09), "M_PoliceSignRed", .006)
    for z in (.893, .977):
        box("Fascia pinstripe", (0, .357, z), (1.62, .004, .008), "M_PoliceSignGold")
    box("Slat base rail", (0, .344, .212), (1.64, .022, .036), dark, .006)

    # Glass screen with a document slot, aluminium frame and the reception sign on top.
    box("Screen bottom channel", (0, .30, 1.099), (1.84, .034, .02), alu, .004)
    box("Screen top cap", (0, .30, 1.51), (1.86, .034, .02), alu, .004)
    for side in (-1, 1):
        box("Screen post", (side * .915, .30, 1.30), (.03, .034, .42), alu, .005)
        box("Lower screen glass", (side * .565, .30, 1.1545), (.69, .008, .091), "M_PoliceGlass")
        box("Slot jamb", (side * .225, .30, 1.1545), (.014, .03, .091), alu)
    box("Upper screen glass", (0, .30, 1.355), (1.80, .008, .29), "M_PoliceGlass")
    box("Slot lintel", (0, .30, 1.205), (.464, .03, .014), alu)
    box("Sign frame", (0, .296, 1.62), (.90, .03, .22), "M_PoliceSignGold", .006)
    box("Desk plaque", (0, .30, 1.62), (.86, .04, .19), "M_PoliceSignRed", .006)
    lettering("Ward label", "CÔNG AN PHƯỜNG HẺM LƯƠNG", (0, .322, 1.672), .032, .76, "M_PoliceSignGold")
    lettering("Reception label", "TIẾP NHẬN", (0, .322, 1.595), .085, .60, "M_PoliceSignGold")

    # Officer side: three-drawer pedestal under the right of the top.
    box("Drawer pedestal", (.6035, -.0725, .5275), (.325, .525, .935), wood, .01, True)
    for z0, z1 in ((.80, .97), (.47, .78), (.09, .45)):
        box("Drawer front", (.6035, -.341, (z0 + z1) / 2), (.30, .012, z1 - z0 - .012), wood, .005)
        tube("Drawer pull", (.55, -.356, z1 - .045), (.66, -.356, z1 - .045), .006, "M_MetalHandle")

    # Monitor, keyboard and mouse.
    box("Monitor foot", (.35, -.055, 1.102), (.34, .23, .025), black, .016)
    box("Monitor stand", (.35, -.065, 1.231), (.062, .06, .24), black, .015)
    box("Monitor case", (.35, -.04, 1.407), (.55, .097, .35), black, .020)
    box("Monitor screen", (.35, -.092, 1.407), (.51, .012, .30), "M_GlassDark", .009)
    box("Keyboard", (.35, -.27, 1.10), (.44, .14, .022), black, .006)
    for row in range(4):
        for col in range(12):
            box("Key", (.163 + col * .034, -.317 + row * .029, 1.114), (.028, .024, .008), "M_ScreenBlack", .002)
    box("Mouse pad", (.66, -.26, 1.091), (.20, .18, .004), "M_Rubber")
    box("Mouse", (.66, -.26, 1.106), (.06, .10, .028), black, .012)

    # Desk phone.
    box("Phone base", (-.02, -.20, 1.114), (.20, .17, .05), black, .012)
    for row in range(4):
        for col in range(3):
            box("Phone key", (-.01 + col * .025, -.255 + row * .022, 1.141), (.018, .015, .005), "M_PlasticWhite", .002)
    box("Phone handset", (-.07, -.17, 1.152), (.05, .19, .03), black, .013)

    # Open visitor ledger, stamp and ink pad, pen cup, two-tier document tray.
    box("Ledger cover", (-.34, -.21, 1.093), (.40, .28, .008), "M_PoliceSignRed", .003)
    for x in (-.44, -.24):
        box("Ledger page", (x, -.21, 1.101), (.19, .26, .012), paper, .002)
        for i in range(7):
            box("Ledger rule", (x, -.12 - i * .028, 1.1075), (.16, .003, .001), black)
    box("Ink pad", (-.70, -.29, 1.098), (.11, .075, .018), black, .004)
    box("Ink", (-.70, -.29, 1.1075), (.09, .055, .002), "M_PlasticRed")
    box("Stamp base", (-.62, -.30, 1.104), (.05, .05, .03), black, .005)
    tube("Stamp handle", (-.62, -.30, 1.119), (-.62, -.30, 1.185), .014, dark)
    tube("Stamp knob", (-.62, -.30, 1.18), (-.62, -.30, 1.205), .021, dark)
    tube("Pen cup", (-.86, -.12, 1.089), (-.86, -.12, 1.19), .035, "M_PlasticBlue")
    for (dx, dy), color in zip(((-.012, .008), (.010, -.006), (.002, .014)), (black, "M_PlasticRed", "M_PlasticBlue")):
        tube("Pen", (-.86 + dx, -.12 + dy, 1.11), (-.86 + dx * 3, -.12 + dy * 3, 1.255), .005, color)
    for z in (1.095, 1.175):
        box("Tray shelf", (-.62, .05, z), (.34, .25, .008), black, .003)
        box("Tray lip", (-.62, -.073, z + .015), (.34, .006, .03), black)
        for i in range(3):
            box("Tray form", (-.62 + i * .006, .06 + i * .004, z + .006 + i * .004), (.29, .205, .004), paper)
    for x in (-.78, -.46):
        for y in (-.07, .17):
            tube("Tray post", (x, y, 1.095), (x, y, 1.175), .005, black)


def export_model(destination):
    # Preserve each collision piece and batch the decorative geometry into one mesh.
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.select_all(action="DESELECT")
    details = [obj for obj in bpy.context.scene.objects if not obj.name.startswith("Solid_")]
    for obj in details:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = details[0]
    bpy.ops.object.join()
    bpy.context.object.name = "Details"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(filepath=str(destination), use_selection=True, object_types={"MESH"},
                             apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", use_tspace=True,
                             add_leaf_bones=False, bake_anim=False, path_mode="STRIP")
    triangle_count = sum(len(obj.data.loop_triangles) for obj in bpy.context.scene.objects)
    print(f"{destination.name}: {triangle_count:,} triangles; {len(bpy.context.scene.objects)} meshes")


def render_views(output, stem, target, scale):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 1152
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "BOTH"
    scene.display.shading.background_type = "WORLD"
    scene.world = bpy.data.worlds.new("Solid background")
    scene.world.color = (.14, .17, .21)
    camera = bpy.data.objects.new("Evidence camera", bpy.data.cameras.new("Evidence camera"))
    bpy.context.collection.objects.link(camera)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = scale
    scene.camera = camera
    for name, vector in {
        "front_left": (.50, 1, .40), "front_right": (-.62, 1, .46),
        "rear_left": (.62, -1, .52), "rear_right": (-.64, -1, .49),
    }.items():
        camera.location = Vector(target) + Vector(vector) * scale
        camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(output / f"{stem}_{name}.png")
        bpy.ops.render.render(write_still=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--render", type=Path, help="Directory for Solid comparison views")
    options = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    output = options.render.resolve() if options.render else None
    if output:
        output.mkdir(parents=True, exist_ok=True)
    for folder, stem, build, target, scale in (
        ("Environment", "env_police_station", build_station, (0, -4.18, 3.06), 20.3),
        ("Props", "Night3_PoliceDesk", build_desk, (0, 0, .76), 3.15),
    ):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.context.scene.unit_settings.system = "METRIC"
        build()
        export_model(ART / "Models" / folder / f"{stem}.fbx")
        if output:
            render_views(output, stem, target, scale)


if __name__ == "__main__":
    main()
