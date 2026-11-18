using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RealisticStreetFixes
{
    const float RoadCenterZ = 8;
    const float SignEmission = .75f;
    const float SignSmoothness = .2f;
    const float SignLightGap = .85f;
    const float FloorLift = .005f;
    const float GateHeight = 2.4f;
    const string StreetTreeRoot = "Street trees";
    const string StreetTreeModel = "Assets/Art/Models/Environment/env_street_tree.fbx";
    const string StreetTreeMaterials = "Assets/Art/Models/Environment/Materials";
    const string TreeTextures = "Assets/Art/Textures";
    const float StreetTreeHeight = 8.56f;
    const float TreeCutoff = .4f;
    const string ShallowAlleyRoot = "Shallow alleys";
    const string ShallowAlleyPrefix = "ShallowAlley_";
    const float FenceThickness = .1f;
    static readonly (float x0, float x1, float z, float height, string material)[] OpenLots =
    {
        (13.25f, 16.5f, -.1f, 3.4f, "M_BrickExposed"), (62f, 65.5f, -.1f, 3.4f, "M_BrickExposed"),
        (166f, 183f, 16f, 3.4f, "M_TinRust"), (-118.2f, -91f, 27.9f, 3.4f, "M_TinRust"), (-148f, -145f, 16f, 3.4f, "M_BrickExposed"), (-92f, -91f, 16f, 3.4f, "M_TinRust")
    };
    const string GroundPatchMaterial = "M_ConcreteFloor";
    static readonly (Vector3 min, Vector3 max)[] GroundPatches = { (new Vector3(-13.25f, -.12f, -48f), new Vector3(16.5f, -.02f, .05f)), (new Vector3(-92.1f, -.12f, 15.9f), new Vector3(-90.9f, -.02f, 28.1f)) };
    static readonly HashSet<string> ShallowAlleyModeled = new HashSet<string> { "Bike", "Crate", "CrateTarp", "TeaCooler", "TeaStool", "TeaStoolStack", "TeaTable", "TeaTarp", "TrashBin" };
    static readonly Dictionary<string, string> ShallowAlleyMaterials = new Dictionary<string, string>
    {
        { "BackWall", "M_FacadeGray" }, { "NarrowHouse", "M_FacadeCream" }, { "Floor", "M_ConcreteFloor" },
        { "GateBar", "M_SteelDark" }, { "GateRailLow", "M_SteelDark" }, { "GateRailTop", "M_SteelDark" },
        { "Crate", "M_PlasticRed" }, { "CrateTarp", "M_CanvasBlue" }, { "TeaTarp", "M_CanvasBlue" },
        { "TeaCooler", "M_PlasticWhite" }, { "TeaStool", "M_PlasticBlue" }, { "TeaStoolStack", "M_PlasticBlue" },
        { "TeaTable", "M_PlasticWhite" }, { "TrashBin", "M_PlasticGreen" }
    };

    sealed class Part { public int[] verts; public Bounds bounds; public string material; }

    public static void Apply(string matRoot)
    {
        Physics.SyncTransforms();
        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        var signs = renderers.SelectMany(r => Parts(r, n => n.StartsWith("M_SignStreet"))).Select(p => p.bounds).ToList();
        TuneSigns(matRoot);
        MoveSignLights(signs);
        BackShutters(renderers, matRoot);
        var trees = renderers.FirstOrDefault(r => r.name == "MainRoad_Trees");
        var poles = renderers.Where(r => r.name == "MainRoad_UtilityPoles").SelectMany(r => Parts(r, n => n == "M_ConcretePole")).Select(p => p.bounds).ToList();
        if (trees != null) SpreadTrees(trees, signs, poles);
        ShallowAlleys(matRoot);
        StreetTrees();
        Physics.SyncTransforms();
    }

    static List<List<Part>> TreeGroups(List<Part> parts)
    {
        var groups = new List<List<Part>>();
        foreach (var side in new[] { false, true })
            foreach (var p in parts.Where(p => p.bounds.center.z > RoadCenterZ == side).OrderBy(p => p.bounds.center.x))
            {
                var last = groups.LastOrDefault(g => g[0].bounds.center.z > RoadCenterZ == side);
                if (last != null && p.bounds.min.x - last.Max(q => q.bounds.max.x) < 2.5f) last.Add(p); else groups.Add(new List<Part> { p });
            }
        return groups;
    }

    static Material TreeMaterial(string name, string baseMap, string normalMap, bool leaves)
    {
        var path = $"{StreetTreeMaterials}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TreeTextures}/{baseMap}.png"));
        if (normalMap != null) { m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TreeTextures}/{normalMap}.png")); m.EnableKeyword("_NORMALMAP"); }
        m.SetFloat("_Smoothness", leaves ? .22f : .12f);
        m.enableInstancing = true;
        if (leaves)
        {
            m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", TreeCutoff); m.SetFloat("_Cull", (float)CullMode.Off);
            m.EnableKeyword("_ALPHATEST_ON"); m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = (int)RenderQueue.AlphaTest;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    public static int StreetTrees()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(StreetTreeModel);
        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        var old = renderers.FirstOrDefault(r => r.name == "MainRoad_Trees");
        if (model == null || old == null) return 0;
        var previous = GameObject.Find(StreetTreeRoot);
        if (previous != null) Object.DestroyImmediate(previous);
        var materials = new[] { TreeMaterial("M_StreetTreeBark", "T_StreetTreeBark", "T_StreetTreeBark_Normal", false), TreeMaterial("M_StreetTreeBranch", "T_StreetTreeBranch", "T_StreetTreeBranch_Normal", false), TreeMaterial("M_StreetTreeLeaves", "T_StreetTreeLeaves", null, true) };
        var signs = renderers.SelectMany(r => Parts(r, n => n.StartsWith("M_SignStreet"))).Select(p => p.bounds).ToList();
        var poles = renderers.Where(r => r.name == "MainRoad_UtilityPoles").SelectMany(r => Parts(r, n => n == "M_ConcretePole")).Select(p => p.bounds).ToList();
        old.enabled = false;
        var oldCollider = old.GetComponent<MeshCollider>(); if (oldCollider != null) oldCollider.enabled = false;
        Physics.SyncTransforms();
        var crown = model.GetComponentInChildren<MeshFilter>().sharedMesh;
        var crownVertices = crown.vertices;
        var crownVerts = crown.GetTriangles(crown.subMeshCount - 1).Distinct().Where((_, i) => i % 4 == 0).Select(i => crownVertices[i]).ToArray();
        var world = old.GetComponent<MeshFilter>().sharedMesh.vertices.Select(old.transform.TransformPoint).ToArray();
        var root = new GameObject(StreetTreeRoot);
        var rng = new System.Random(1203);
        int placed = 0, tight = 0; float tallest = 0;
        foreach (var g in TreeGroups(Parts(old, _ => true)))
        {
            var leaves = g.Where(p => p.material.StartsWith("M_Leaf")).Select(p => p.bounds).ToList();
            if (leaves.Count == 0 || leaves.Max(b => b.max.y) < 1) continue;
            var trunk = g.Where(p => p.material == "M_TreeBark").OrderByDescending(p => p.bounds.size.y).First();
            float low = trunk.verts.Min(i => world[i].y);
            var feet = trunk.verts.Where(i => world[i].y < low + .1f).Select(i => world[i]).ToList();
            var foot = feet.Aggregate(Vector3.zero, (a, b) => a + b) / feet.Count;
            float wanted = Mathf.Clamp(leaves.Max(b => b.max.y) * 1.25f / StreetTreeHeight, .7f, 1.05f) * (float)(.92 + rng.NextDouble() * .14);
            float startYaw = (float)rng.NextDouble() * 360;
            bool far = foot.z > RoadCenterZ;
            (float scale, float yaw, int hits)? best = null;
            foreach (var shrink in new[] { 1f, .9f, .8f, .7f, .6f })
            {
                for (int k = 0; k < 16 && best?.hits != 0; k++)
                {
                    float yaw = startYaw + k * 22.5f, scale = wanted * shrink;
                    var rot = Quaternion.Euler(0, yaw, 0);
                    var box = new Bounds(foot + rot * crownVerts[0] * scale, Vector3.zero);
                    foreach (var v in crownVerts) box.Encapsulate(foot + rot * v * scale);
                    int hits = signs.Count(s => s.center.z > RoadCenterZ == far && box.max.x + .2f > s.min.x && box.min.x - .2f < s.max.x && box.max.z + .2f > s.min.z && box.min.z - .2f < s.max.z && box.max.y > s.min.y)
                        + poles.Count(q => Mathf.Abs(q.center.x - box.center.x) < box.extents.x && Mathf.Abs(q.center.z - box.center.z) < box.extents.z)
                        + Physics.OverlapBox(box.center, box.extents * .95f).Count(c => c.name.StartsWith("MainRoad_Houses") || c.name.StartsWith("env_"));
                    if (best == null || hits < best.Value.hits) best = (scale, yaw, hits);
                }
                if (best?.hits == 0) break;
            }
            var tree = (GameObject)PrefabUtility.InstantiatePrefab(model);
            tree.name = "Street tree " + placed;
            tree.transform.SetParent(root.transform);
            tree.transform.SetPositionAndRotation(foot, Quaternion.Euler(0, best.Value.yaw, 0));
            tree.transform.localScale = Vector3.one * best.Value.scale;
            foreach (var r in tree.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials = materials;
            var solid = tree.AddComponent<CapsuleCollider>();
            solid.center = new Vector3(0, 1.5f / best.Value.scale, 0); solid.height = 3 / best.Value.scale; solid.radius = .16f / best.Value.scale;
            tallest = Mathf.Max(tallest, StreetTreeHeight * best.Value.scale);
            placed++; if (best.Value.hits > 0) tight++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"REAL_STREET_TREES placed={placed} tight={tight} tallest={tallest:F1}");
        return placed;
    }

    public static int ShallowAlleys(string matRoot)
    {
        var old = GameObject.Find(ShallowAlleyRoot);
        if (old != null) Object.DestroyImmediate(old);
        var parts = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith(ShallowAlleyPrefix)).ToList();
        var root = new GameObject(ShallowAlleyRoot);
        var groups = new Dictionary<string, List<Bounds>>();
        foreach (var part in parts)
        {
            var kind = Regex.Replace(part.name, @"^ShallowAlley_[NF]\d+_|(_?\d+)+$", "");
            if (!ShallowAlleyMaterials.TryGetValue(kind, out var material)) continue;
            var box = part.bounds;
            if (kind == "Floor") box.SetMinMax(box.min, box.max + Vector3.up * FloorLift);
            if (!ShallowAlleyModeled.Contains(kind))
            {
                if (!groups.TryGetValue(material, out var boxes)) groups[material] = boxes = new List<Bounds>();
                boxes.Add(box);
            }
            if (kind == "GateBar") continue;
            var solid = new GameObject(part.name);
            solid.transform.SetParent(root.transform);
            var collider = solid.AddComponent<BoxCollider>();
            collider.center = kind == "GateRailLow" ? new Vector3(box.center.x, GateHeight * .5f, box.center.z) : box.center;
            collider.size = kind == "GateRailLow" ? new Vector3(box.size.x, GateHeight, box.size.z) : box.size;
        }
        foreach (var lot in OpenLots)
        {
            var box = new Bounds(); box.SetMinMax(new Vector3(lot.x0, 0, lot.z), new Vector3(lot.x1, lot.height, lot.z + FenceThickness));
            if (!groups.TryGetValue(lot.material, out var boxes)) groups[lot.material] = boxes = new List<Bounds>();
            boxes.Add(box);
            var solid = new GameObject("OpenLot_" + lot.x0);
            solid.transform.SetParent(root.transform);
            var collider = solid.AddComponent<BoxCollider>();
            collider.center = box.center; collider.size = box.size;
        }
        if (!groups.TryGetValue(GroundPatchMaterial, out var ground)) groups[GroundPatchMaterial] = ground = new List<Bounds>();
        foreach (var area in GroundPatches) { var patch = new Bounds(); patch.SetMinMax(area.min, area.max); ground.Add(patch); }
        var mesh = new Mesh { name = ShallowAlleyRoot, indexFormat = IndexFormat.UInt32, subMeshCount = groups.Count };
        var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>();
        var triangles = groups.Keys.Select(_ => new List<int>()).ToList();
        int slot = 0;
        foreach (var boxes in groups.Values)
        {
            foreach (var box in boxes) AddBox(box, vertices, normals, uvs, triangles[slot]);
            slot++;
        }
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs);
        for (int i = 0; i < groups.Count; i++) mesh.SetTriangles(triangles[i], i);
        mesh.RecalculateBounds(); mesh.RecalculateTangents();
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = root.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = groups.Keys.Select(n => AssetDatabase.LoadAssetAtPath<Material>($"{matRoot}/{n}.mat")).ToArray();
        return groups.Values.Sum(g => g.Count);
    }

    static void AddBox(Bounds box, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
    {
        foreach (var normal in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
        {
            var u = normal.y != 0 ? Vector3.right : Vector3.Cross(Vector3.up, normal);
            var v = Vector3.Cross(normal, u);
            var center = box.center + Vector3.Scale(normal, box.extents);
            var du = Vector3.Scale(u, box.extents); var dv = Vector3.Scale(v, box.extents);
            int start = vertices.Count;
            foreach (var corner in new[] { center - du - dv, center - du + dv, center + du + dv, center + du - dv })
            {
                vertices.Add(corner); normals.Add(normal);
                uvs.Add(new Vector2(Vector3.Dot(corner, u), Vector3.Dot(corner, v)));
            }
            triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
        }
    }

    static void TuneSigns(string matRoot)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>($"{matRoot}/M_SignStreetLit.mat"); if (m == null) return;
        m.SetColor("_EmissionColor", Color.white * SignEmission); m.SetFloat("_Smoothness", SignSmoothness);
        EditorUtility.SetDirty(m);
    }

    static void MoveSignLights(List<Bounds> signs)
    {
        int moved = 0;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Point && l.name.StartsWith("Point_")))
        {
            var p = l.transform.position;
            foreach (var s in signs.Where(s => p.x > s.min.x - .5f && p.x < s.max.x + .5f && Mathf.Abs(p.y - s.center.y) < 1f && Mathf.Abs(p.z - s.center.z) < SignLightGap - .01f))
            {
                p.z = s.center.z + Mathf.Sign(RoadCenterZ - s.center.z) * SignLightGap; l.transform.position = p; moved++; break;
            }
        }
        Debug.Log("REAL_FIX sign_lights_moved=" + moved);
    }

    static void BackShutters(MeshRenderer[] renderers, string matRoot)
    {
        var old = GameObject.Find("Shutter backing"); if (old != null) Object.DestroyImmediate(old);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); var unit = cube.GetComponent<MeshFilter>().sharedMesh;
        var boxes = new List<CombineInstance>();
        foreach (var r in renderers.Where(r => r.name.StartsWith("MainRoad_Houses") || r.name.StartsWith("Shutter_")))
            foreach (var p in Parts(r, n => n.Contains("Shutter") || r.name.StartsWith("Shutter_")))
            {
                var b = p.bounds; if (b.size.y < 1.5f || Mathf.Max(b.size.x, b.size.z) < .6f) continue;
                bool alongZ = b.size.z > b.size.x; float thin = alongZ ? b.size.x : b.size.z;
                var inward = alongZ ? new Vector3(Mathf.Sign(b.center.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(b.center.z - RoadCenterZ));
                var size = alongZ ? new Vector3(.08f, b.size.y + .35f, b.size.z + .12f) : new Vector3(b.size.x + .12f, b.size.y + .35f, .08f);
                var center = b.center + inward * (thin / 2 + .045f) + Vector3.down * .125f;
                boxes.Add(new CombineInstance { mesh = unit, transform = Matrix4x4.TRS(center, Quaternion.identity, size) });
            }
        Object.DestroyImmediate(cube);
        var mesh = new Mesh { name = "ShutterBacking", indexFormat = IndexFormat.UInt32 }; mesh.CombineMeshes(boxes.ToArray(), true, true);
        var meshPath = $"{matRoot}/ShutterBacking.asset"; AssetDatabase.DeleteAsset(meshPath); AssetDatabase.CreateAsset(mesh, meshPath);
        var matPath = $"{matRoot}/ShutterBacking.mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.SetColor("_BaseColor", Color.black); AssetDatabase.CreateAsset(mat, matPath); }
        var go = new GameObject("Shutter backing"); go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
        Debug.Log("REAL_FIX shutter_backs=" + boxes.Count);
    }

    static void SpreadTrees(MeshRenderer renderer, List<Bounds> signs, List<Bounds> poles)
    {
        var filter = renderer.GetComponent<MeshFilter>(); var mesh = filter.sharedMesh; var t = renderer.transform;
        var parts = Parts(renderer, _ => true);
        var world = mesh.vertices.Select(t.TransformPoint).ToArray();
        var groups = TreeGroups(parts);
        var placed = new List<float>(); int shifted = 0, removed = 0, leaned = 0;
        foreach (var g in groups)
        {
            var leaves = g.Where(p => p.material.StartsWith("M_Leaf")).Select(p => p.bounds).ToList(); if (leaves.Count == 0) continue;
            var canopy = leaves[0]; foreach (var leaf in leaves) canopy.Encapsulate(leaf);
            if (canopy.max.y < 1) continue;
            var trunk = g.First(p => p.material == "M_TreeBark").bounds; var indices = g.SelectMany(p => p.verts).Distinct().ToList();
            bool far = canopy.center.z > RoadCenterZ; var sameSide = signs.Where(s => s.center.z > RoadCenterZ == far).ToList();
            bool Free(float dx) => !sameSide.Any(s => canopy.max.x + dx + .2f > s.min.x && canopy.min.x + dx - .2f < s.max.x)
                && !poles.Any(q => Mathf.Abs(q.center.x - trunk.center.x - dx) < 1f && Mathf.Abs(q.center.z - trunk.center.z) < 2f)
                && !placed.Any(x => Mathf.Abs(x - (trunk.center.x + dx) * (far ? 1 : -1)) < 5f);
            float? shift = null;
            for (float d = 0; d <= 10 && shift == null; d += .25f) { if (Free(d)) shift = d; else if (Free(-d)) shift = -d; }
            if (shift == null) { foreach (var i in indices) world[i] = trunk.center + Vector3.down * 5; removed++; continue; }
            if (shift != 0) { foreach (var i in indices) world[i].x += shift.Value; canopy.center += Vector3.right * shift.Value; shifted++; }
            placed.Add((trunk.center.x + shift.Value) * (far ? 1 : -1));
            float toHouse = far ? 1 : -1, houseFace = float.MaxValue;
            for (float x = canopy.min.x; x <= canopy.max.x; x += .5f)
                for (float y = canopy.min.y; y <= canopy.max.y; y += .5f)
                    foreach (var hit in Physics.RaycastAll(new Vector3(x, y, RoadCenterZ), Vector3.forward * toHouse, 12).Where(h => h.collider.name.StartsWith("MainRoad_Houses") || h.collider.name.StartsWith("env_")))
                        houseFace = Mathf.Min(houseFace, Mathf.Abs(hit.point.z - RoadCenterZ));
            if (houseFace == float.MaxValue) continue;
            float canopyReach = Mathf.Abs((far ? canopy.max.z : canopy.min.z) - RoadCenterZ);
            float need = Mathf.Clamp(canopyReach + .25f - houseFace, 0, 3);
            if (need <= 0) continue;
            float span = Mathf.Max(canopy.min.y - 1, .5f);
            foreach (var i in indices) world[i].z -= toHouse * need * Mathf.Clamp01((world[i].y - 1) / span);
            leaned++;
        }
        var edited = Object.Instantiate(mesh); edited.name = mesh.name;
        edited.SetVertices(world.Select(t.InverseTransformPoint).ToArray()); edited.RecalculateBounds();
        var path = AssetDatabase.GetAssetPath(mesh); AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(edited, path);
        filter.sharedMesh = edited; var c = renderer.GetComponent<MeshCollider>(); if (c != null) { c.sharedMesh = null; c.sharedMesh = edited; }
        Debug.Log($"REAL_FIX trees={groups.Count} shifted={shifted} removed={removed} leaned={leaned}");
    }

    static List<Part> Parts(MeshRenderer r, System.Func<string, bool> wanted)
    {
        var result = new List<Part>(); var mesh = r.GetComponent<MeshFilter>().sharedMesh; if (mesh == null) return result;
        var mats = r.sharedMaterials; var verts = mesh.vertices; var t = r.transform;
        for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
        {
            if (mats[s] == null || !wanted(mats[s].name)) continue;
            var tri = mesh.GetTriangles(s); var parent = new Dictionary<int, int>(); var weld = new Dictionary<Vector3Int, int>();
            int Find(int a) { while (parent[a] != a) { parent[a] = parent[parent[a]]; a = parent[a]; } return a; }
            foreach (var i in tri.Distinct())
            {
                var key = Vector3Int.RoundToInt(verts[i] * 1000);
                if (weld.TryGetValue(key, out var first)) parent[i] = Find(first); else { weld[key] = i; parent[i] = i; }
            }
            for (int i = 0; i < tri.Length; i += 3) { int a = Find(tri[i]); parent[Find(tri[i + 1])] = a; parent[Find(tri[i + 2])] = a; }
            foreach (var g in tri.Distinct().GroupBy(Find))
            {
                var b = new Bounds(t.TransformPoint(verts[g.First()]), Vector3.zero); foreach (var i in g) b.Encapsulate(t.TransformPoint(verts[i]));
                result.Add(new Part { verts = g.ToArray(), bounds = b, material = mats[s].name });
            }
        }
        return result;
    }
}
