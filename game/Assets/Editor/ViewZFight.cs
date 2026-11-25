using System.Collections.Generic;
using System.Linq;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ViewZFight
{
    const string NightScene = "Assets/Scenes/Night01/NightOneRealistic.unity";
    const int Layer = 31, Columns = 20, Rows = 12;
    const float Gap = 1e-4f, Fov = 70, Range = 30, Depth = .005f;

    struct Pair { public MeshFilter a, b; public int ta, tb; public string matA, matB; public float gap; public Vector3 at, eye, normal; }

    public static void Report()
    {
        int code = 1;
        try
        {
            EditorSceneManager.OpenScene(NightScene);
            var pairs = Collect(Eyes(), out int eyes, out int rays);
            Debug.Log($"VIEW_ZFIGHT eyes={eyes} rays={rays} pairs={pairs.Count}\n" + string.Join("\n", Group(pairs).Take(40)));
            code = 0;
        }
        catch (System.Exception e) { Debug.LogException(e); }
        EditorApplication.Exit(code);
    }

    public static void Patch()
    {
        int code = 1;
        try
        {
            var scene = EditorSceneManager.OpenScene(NightOneRealisticSetup.SourcePath);
            var lines = new List<string> { $"signs={FixSigns()}" };
            var pairs = Collect(Eyes(), out int eyes, out _);
            lines.Add($"eyes={eyes} pairs_before={pairs.Count}");
            var moved = new HashSet<Transform>();
            var offsets = new Dictionary<MeshFilter, Dictionary<int, Vector3>>();
            foreach (var p in pairs)
            {
                if (Shrine(p.a) || Shrine(p.b))
                {
                    var t = (Shrine(p.a) ? p.a : p.b).transform;
                    if (moved.Add(t)) t.position += p.normal * Depth;
                }
                else if (Stand(p.a) || Stand(p.b))
                {
                    var f = Stand(p.a) ? p.a : p.b; var other = Stand(p.a) ? p.b : p.a;
                    if (!offsets.TryGetValue(f, out var o)) offsets[f] = o = new Dictionary<int, Vector3>();
                    foreach (var v in Region(f, Stand(p.a) ? p.ta : p.tb, p.normal, other.GetComponent<MeshRenderer>().bounds)) o[v] = -p.normal * Depth;
                }
                else lines.Add($"  unhandled {Name(p.a)} [{p.matA}] | {Name(p.b)} [{p.matB}] at={p.at:F2}");
            }
            lines.AddRange(moved.Select(t => $"  moved {t.name} by {Depth * 1000} mm"));
            foreach (var kv in offsets)
            {
                var f = kv.Key; var mesh = f.sharedMesh; var verts = mesh.vertices; var tr = f.transform;
                foreach (var o in kv.Value) verts[o.Key] = tr.InverseTransformPoint(tr.TransformPoint(verts[o.Key]) + o.Value);
                mesh.vertices = verts; mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
                lines.Add($"  receded {Name(f)} vertices={kv.Value.Count}");
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            NightOneRealisticSetup.Create();
            EditorSceneManager.OpenScene(NightScene);
            var after = Collect(Eyes(), out _, out _);
            lines.Add($"pairs_after={after.Count}");
            lines.AddRange(Group(after).Take(20));
            Debug.Log("VIEW_ZFIGHT_PATCH\n" + string.Join("\n", lines));
            code = 0;
        }
        catch (System.Exception e) { Debug.LogException(e); }
        EditorApplication.Exit(code);
    }

    public static void Regression()
    {
        int code = 1;
        try
        {
            EditorSceneManager.OpenScene(NightScene);
            var pairs = Collect(Eyes(), out int eyes, out int rays);
            bool ok = eyes > 50 && pairs.Count == 0;
            Debug.Log((ok ? "[ViewZFightRegression] PASS" : "[ViewZFightRegression] FAIL") + $" eyes={eyes} rays={rays} pairs={pairs.Count}\n" + string.Join("\n", Group(pairs).Take(30)));
            code = ok ? 0 : 1;
        }
        catch (System.Exception e) { Debug.LogException(e); }
        EditorApplication.Exit(code);
    }

    static bool Shrine(MeshFilter f) => f.name.StartsWith("Altar_Shrine");

    static bool Stand(MeshFilter f) => f.name == "Alley_Facades" && AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith(".asset");

    static int FixSigns()
    {
        int count = 0;
        foreach (var board in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Sign board ")))
        {
            Transform top = board.Find("Trim top"), back = board.Find("Back"), left = board.Find("Trim left"), right = board.Find("Trim right");
            if (top == null || back == null || left == null || right == null) continue;
            float trim = top.localScale.y, sx = top.localScale.x, sy = 2 * top.localPosition.y + trim;
            back.localScale = new Vector3(sx - trim, sy - trim, back.localScale.z);
            foreach (var t in new[] { left, right }) t.localScale = new Vector3(t.localScale.x, sy - 2 * trim, t.localScale.z);
            count++;
        }
        return count;
    }

    static IEnumerable<int> Region(MeshFilter f, int seed, Vector3 n, Bounds keep)
    {
        keep.Expand(.04f);
        var tris = f.sharedMesh.triangles; var world = f.sharedMesh.vertices.Select(f.transform.TransformPoint).ToArray();
        float d = Vector3.Dot(n, world[tris[seed * 3]]);
        var byPos = new Dictionary<Vector3Int, List<int>>();
        for (int t = 0; t < tris.Length / 3; t++)
        {
            Vector3 a = world[tris[t * 3]], b = world[tris[t * 3 + 1]], c = world[tris[t * 3 + 2]];
            var cr = Vector3.Cross(b - a, c - a); if (cr.sqrMagnitude < 1e-12f) continue;
            if (Vector3.Dot(cr.normalized, n) < .999f || Mathf.Abs(Vector3.Dot(n, a) - d) > .001f || !keep.Contains((a + b + c) / 3)) continue;
            for (int q = 0; q < 3; q++)
            {
                var k = Vector3Int.RoundToInt(world[tris[t * 3 + q]] * 10000);
                if (!byPos.TryGetValue(k, out var l)) byPos[k] = l = new List<int>();
                l.Add(t);
            }
        }
        var seen = new HashSet<int> { seed }; var queue = new Queue<int>(); queue.Enqueue(seed);
        while (queue.Count > 0)
        {
            int t = queue.Dequeue();
            for (int k = 0; k < 3; k++)
                if (byPos.TryGetValue(Vector3Int.RoundToInt(world[tris[t * 3 + k]] * 10000), out var l))
                    foreach (var u in l) if (seen.Add(u)) queue.Enqueue(u);
        }
        return seen.SelectMany(t => new[] { tris[t * 3], tris[t * 3 + 1], tris[t * 3 + 2] }).Distinct();
    }

    static IEnumerable<string> Group(List<Pair> pairs) =>
        pairs.GroupBy(p => (Name(p.a), Name(p.b), p.matA, p.matB, Mathf.RoundToInt(p.at.x), Mathf.RoundToInt(p.at.y), Mathf.RoundToInt(p.at.z)))
            .OrderByDescending(g => g.Count())
            .Select(g => { var s = g.OrderBy(p => p.gap).First(); return $"  rays={g.Count()} {Name(s.a)} [{s.matA}] | {Name(s.b)} [{s.matB}] gap_mm={s.gap * 1000:F3} n={s.normal:F2} at={s.at:F2} eye={s.eye:F1}"; });

    static List<Vector3> Eyes()
    {
        var start = Object.FindFirstObjectByType<FirstPersonPlayer>().transform.position;
        var eyes = new List<Vector3>();
        for (float x = -4; x <= 4.01f; x += 1.5f)
            for (float z = -.5f; z >= -57.5f; z -= 1.5f)
                if (Floor(new Vector3(x, start.y + 1.5f, z), out var feet)) eyes.Add(feet + Vector3.up * PlayerGeometry.StandingEye);
        for (float x = -6.5f; x <= -2f; x += .75f)
            for (float z = -38.5f; z >= -46.5f; z -= .75f)
                if (Floor(new Vector3(x, start.y + 1.5f, z), out var feet) && feet.y < .5f) eyes.Add(feet + Vector3.up * PlayerGeometry.StandingEye);
        return eyes.Distinct().ToList();
    }

    static bool Floor(Vector3 top, out Vector3 feet)
    {
        feet = default;
        if (!Physics.Raycast(top, Vector3.down, out var hit, 3, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)) return false;
        feet = hit.point; float r = PlayerGeometry.Radius;
        return !Physics.CheckCapsule(feet + Vector3.up * (r + .35f), feet + Vector3.up * (PlayerGeometry.StandingHeight - r), r - .05f, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore);
    }

    static List<Pair> Collect(List<Vector3> eyes, out int eyeCount, out int rays)
    {
        Physics.SyncTransforms();
        eyeCount = eyes.Count; rays = 0;
        var filters = new Dictionary<Collider, MeshFilter>();
        var probes = new List<GameObject>();
        foreach (var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (f.sharedMesh == null || !f.gameObject.activeInHierarchy || !f.TryGetComponent<MeshRenderer>(out var r) || !r.enabled) continue;
            var g = new GameObject("ViewZFight probe") { layer = Layer, hideFlags = HideFlags.DontSave };
            g.transform.SetParent(f.transform, false);
            var c = g.AddComponent<MeshCollider>(); c.sharedMesh = f.sharedMesh;
            filters[c] = f; probes.Add(g);
        }
        Physics.SyncTransforms();
        bool backfaces = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
        var pairs = new List<Pair>();
        try
        {
            float tanV = Mathf.Tan(Fov * .5f * Mathf.Deg2Rad), tanH = tanV * 16 / 9;
            foreach (var eye in eyes)
                for (int heading = 0; heading < 360; heading += 30)
                    foreach (int pitch in new[] { -20, 0, 25, 50 })
                    {
                        var rot = Quaternion.Euler(-pitch, heading, 0);
                        for (int i = 0; i < Columns; i++)
                            for (int j = 0; j < Rows; j++)
                            {
                                var dir = rot * new Vector3(((i + .5f) / Columns * 2 - 1) * tanH, ((j + .5f) / Rows * 2 - 1) * tanV, 1).normalized; rays++;
                                var hits = Physics.RaycastAll(eye, dir, Range, 1 << Layer, QueryTriggerInteraction.Ignore);
                                if (hits.Length < 2) continue;
                                System.Array.Sort(hits, (p, q) => p.distance.CompareTo(q.distance));
                                var h0 = hits[0];
                                if (Vector3.Dot(h0.normal, dir) >= 0) continue;
                                for (int k = 1; k < hits.Length && hits[k].distance - h0.distance < Gap; k++)
                                {
                                    var h1 = hits[k];
                                    if (Vector3.Dot(h1.normal, dir) >= 0 || Vector3.Dot(h0.normal, h1.normal) < .995f) continue;
                                    if (h0.collider == h1.collider && h0.triangleIndex == h1.triangleIndex) continue;
                                    MeshFilter fa = filters[h0.collider], fb = filters[h1.collider];
                                    string ma = Mat(fa, h0.triangleIndex), mb = Mat(fb, h1.triangleIndex);
                                    if (ma == mb && Plain(fa, ma)) continue;
                                    pairs.Add(new Pair { a = fa, b = fb, ta = h0.triangleIndex, tb = h1.triangleIndex, matA = ma, matB = mb, gap = h1.distance - h0.distance, at = h0.point, eye = eye, normal = h0.normal });
                                    break;
                                }
                            }
                    }
        }
        finally
        {
            Physics.queriesHitBackfaces = backfaces;
            foreach (var g in probes) Object.DestroyImmediate(g);
        }
        return pairs;
    }

    static bool Plain(MeshFilter f, string material)
    {
        var m = f.GetComponent<MeshRenderer>().sharedMaterials.FirstOrDefault(x => x != null && x.name == material);
        return m == null || m.GetTexturePropertyNames().All(n => m.GetTexture(n) == null);
    }

    static string Name(Component c) => c.transform.root == c.transform ? c.name : c.transform.root.name + "/" + c.name;

    static string Mat(MeshFilter f, int triangle)
    {
        var mesh = f.sharedMesh; var mats = f.GetComponent<MeshRenderer>().sharedMaterials; int t = triangle * 3;
        for (int s = 0; s < mesh.subMeshCount; s++)
            if (t >= mesh.GetIndexStart(s) && t < mesh.GetIndexStart(s) + mesh.GetIndexCount(s)) return s < mats.Length && mats[s] != null ? mats[s].name : "?";
        return "?";
    }
}
