using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RealisticStreetFixes
{
    const float RoadCenterZ = 8;
    const float SignEmission = .75f;
    const float SignSmoothness = .2f;
    const float SignLightGap = .85f;

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
        Physics.SyncTransforms();
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
        var groups = new List<List<Part>>();
        foreach (var side in new[] { false, true })
            foreach (var p in parts.Where(p => p.bounds.center.z > RoadCenterZ == side).OrderBy(p => p.bounds.center.x))
            {
                var last = groups.LastOrDefault(g => g[0].bounds.center.z > RoadCenterZ == side);
                if (last != null && p.bounds.min.x - last.Max(q => q.bounds.max.x) < 2.5f) last.Add(p); else groups.Add(new List<Part> { p });
            }
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
