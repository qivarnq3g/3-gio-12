using System.Collections.Generic;
using System.Linq;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VisibleZFight
{
    const string NightScene = "Assets/Scenes/Night01/NightOneRealistic.unity";
    const float Cell = .25f, MaxDrop = 2.5f, Probe = .6f, Coplanar = 1e-4f, MinArea = .002f, Depth = .005f, Range = 250f;
    const int Layer = 31;

    public struct Hit { public MeshFilter filter; public FacadeZFight.Overlap overlap; public Vector3 at, eye; }

    public static void Patch()
    {
        int code = 1;
        try
        {
            EditorSceneManager.OpenScene(NightScene);
            var before = Collect(out var eyes);
            var lines = new List<string> { $"eyes={eyes} visible_before={before.Count}" };
            foreach (var group in before.GroupBy(h => h.filter))
            {
                var f = group.Key; var path = AssetDatabase.GetAssetPath(f.sharedMesh);
                if (!path.EndsWith(".asset")) { lines.Add($"  skip {f.name} ({path}) count={group.Count()}"); continue; }
                var keys = new HashSet<(int, int, int, int)>(group.Select(h => Key(h.overlap)));
                int moved = FacadeZFight.RecedeLosers(f, BoardingHouseZFight.Rank, Depth, 4, o => keys.Contains(Key(o)));
                EditorUtility.SetDirty(f.sharedMesh);
                lines.Add($"  {f.transform.root.name}/{f.name} pairs={group.Count()} receded_vertices={moved}");
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(NightScene);
            var after = Collect(out _);
            lines.Add($"visible_after={after.Count}");
            lines.AddRange(after.Take(20).Select(Describe));
            Debug.Log("VISIBLE_ZFIGHT_PATCH\n" + string.Join("\n", lines));
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
            var hits = Collect(out int eyes);
            bool ok = eyes > 50 && hits.Count == 0;
            Debug.Log((ok ? "[VisibleZFightRegression] PASS" : "[VisibleZFightRegression] FAIL") + $" eyes={eyes} visible={hits.Count}\n" + string.Join("\n", hits.Take(30).Select(Describe)));
            code = ok ? 0 : 1;
        }
        catch (System.Exception e) { Debug.LogException(e); }
        EditorApplication.Exit(code);
    }

    static (int, int, int, int) Key(FacadeZFight.Overlap o) => (o.sa, o.a, o.sb, o.b);

    static string Describe(Hit h) => $"  {h.filter.transform.root.name}/{h.filter.name} {h.overlap.matA}|{h.overlap.matB} n={h.overlap.normal} area={h.overlap.area:F3} at={h.at:F2} seen_from={h.eye:F1}";

    public static List<Hit> Collect(out int eyeCount)
    {
        Physics.SyncTransforms();
        var start = Object.FindFirstObjectByType<FirstPersonPlayer>().transform.position;
        var eyes = Reach(start).GroupBy(p => (Mathf.RoundToInt(p.x / 1.5f), Mathf.RoundToInt(p.z / 1.5f), Mathf.RoundToInt(p.y)))
            .Select(g => g.First() + Vector3.up * PlayerGeometry.StandingEye).ToList();
        eyeCount = eyes.Count;
        var filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .Where(f => f.sharedMesh != null && f.gameObject.activeInHierarchy && f.TryGetComponent<MeshRenderer>(out var r) && r.enabled).ToList();
        var probes = new List<GameObject>();
        foreach (var f in filters)
        {
            var g = new GameObject("VisibleZFight probe") { layer = Layer, hideFlags = HideFlags.DontSave };
            g.transform.SetParent(f.transform, false);
            g.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
            probes.Add(g);
        }
        Physics.SyncTransforms();
        bool backfaces = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
        var hits = new List<Hit>();
        try
        {
            foreach (var f in filters)
            {
                var r = f.GetComponent<MeshRenderer>();
                var overlaps = FacadeZFight.Find(f).Where(o => o.area > MinArea).ToList();
                if (overlaps.Count == 0) continue;
                var mesh = f.sharedMesh; var world = mesh.vertices.Select(f.transform.TransformPoint).ToArray();
                var subs = Enumerable.Range(0, mesh.subMeshCount).Select(mesh.GetTriangles).ToArray();
                foreach (var o in overlaps)
                {
                    if (o.sa == o.sb && Uniform(r, o.sa)) continue;
                    var ta = Tri(world, subs[o.sa], o.a); var tb = Tri(world, subs[o.sb], o.b);
                    if (Mathf.Abs(Vector3.Dot(o.normal, tb[0] - ta[0])) > Coplanar) continue;
                    var at = Centroid(ta, tb, o.normal); var p = at + o.normal * .01f;
                    foreach (var eye in eyes)
                    {
                        var d = eye - p;
                        if (Vector3.Dot(d, o.normal) < .05f || d.sqrMagnitude > Range * Range) continue;
                        if (!Physics.Linecast(p, eye, 1 << Layer, QueryTriggerInteraction.Ignore)) { hits.Add(new Hit { filter = f, overlap = o, at = at, eye = eye }); break; }
                    }
                }
            }
        }
        finally
        {
            Physics.queriesHitBackfaces = backfaces;
            foreach (var g in probes) Object.DestroyImmediate(g);
        }
        return hits;
    }

    static bool Uniform(MeshRenderer r, int submesh)
    {
        var m = submesh < r.sharedMaterials.Length ? r.sharedMaterials[submesh] : null;
        if (m == null) return true;
        foreach (var name in m.GetTexturePropertyNames()) if (m.GetTexture(name) != null) return false;
        return r.lightmapIndex < 0 || r.lightmapIndex >= 65534;
    }

    static Vector3[] Tri(Vector3[] world, int[] tris, int t) => new[] { world[tris[t * 3]], world[tris[t * 3 + 1]], world[tris[t * 3 + 2]] };

    static Vector3 Centroid(Vector3[] a, Vector3[] b, Vector3 n)
    {
        var u = Vector3.Cross(n, Mathf.Abs(n.y) > .9f ? Vector3.right : Vector3.up).normalized; var w = Vector3.Cross(n, u);
        float d = Vector3.Dot(n, a[0]);
        System.Func<Vector3, Vector2> flat = v => new Vector2(Vector3.Dot(v, u), Vector3.Dot(v, w));
        var poly = a.Select(flat).ToList(); var clip = b.Select(flat).ToArray();
        if (Signed(clip) < 0) clip = clip.Reverse().ToArray();
        for (int e = 0; e < 3 && poly.Count > 0; e++)
        {
            Vector2 p0 = clip[e], p1 = clip[(e + 1) % 3]; var input = poly; poly = new List<Vector2>();
            for (int i = 0; i < input.Count; i++)
            {
                Vector2 cur = input[i], prev = input[(i + input.Count - 1) % input.Count];
                bool cin = Side(p0, p1, cur) >= -1e-7f, pin = Side(p0, p1, prev) >= -1e-7f;
                if (cin) { if (!pin) poly.Add(Cut(prev, cur, p0, p1)); poly.Add(cur); }
                else if (pin) poly.Add(Cut(prev, cur, p0, p1));
            }
        }
        var c = poly.Count > 0 ? poly.Aggregate(Vector2.zero, (s, v) => s + v) / poly.Count : (flat(a[0]) + flat(a[1]) + flat(a[2])) / 3;
        return u * c.x + w * c.y + n * d;
    }

    static float Side(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    static Vector2 Cut(Vector2 a, Vector2 b, Vector2 p, Vector2 q) { float s1 = Side(p, q, a), s2 = Side(p, q, b); return a + (b - a) * (s1 / (s1 - s2)); }
    static float Signed(Vector2[] p) { float s = 0; for (int i = 0; i < p.Length; i++) { var a = p[i]; var b = p[(i + 1) % p.Length]; s += a.x * b.y - b.x * a.y; } return s / 2; }

    static List<Vector3> Reach(Vector3 start)
    {
        var seen = new HashSet<(int, int, int)>(); var result = new List<Vector3>();
        var open = new Queue<(int x, int z, float y)>();
        int sx = Mathf.RoundToInt(start.x / Cell), sz = Mathf.RoundToInt(start.z / Cell);
        Ground(new Vector3(sx * Cell, start.y + 1, sz * Cell), 3, out float sy);
        open.Enqueue((sx, sz, sy)); seen.Add((sx, sz, Mathf.RoundToInt(sy * 4)));
        var steps = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        while (open.Count > 0)
        {
            var c = open.Dequeue(); result.Add(new Vector3(c.x * Cell, c.y, c.z * Cell));
            foreach (var (dx, dz) in steps)
            {
                int nx = c.x + dx, nz = c.z + dz;
                var top = new Vector3(nx * Cell, c.y + Probe, nz * Cell);
                if (Physics.CheckSphere(top, .05f, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (!Ground(top, MaxDrop + Probe, out float y) || y - c.y > PlayerGeometry.StepOffset + .05f) continue;
                if (!Fits(new Vector3(nx * Cell, y, nz * Cell))) continue;
                if (seen.Add((nx, nz, Mathf.RoundToInt(y * 4)))) open.Enqueue((nx, nz, y));
            }
        }
        return result;
    }

    static bool Ground(Vector3 top, float below, out float height)
    {
        height = 0;
        if (!Physics.Raycast(top, Vector3.down, out var hit, below, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)) return false;
        height = hit.point.y; return true;
    }

    static bool Fits(Vector3 feet)
    {
        float r = PlayerGeometry.Radius;
        return !Physics.CheckCapsule(feet + Vector3.up * (r + PlayerGeometry.StepOffset), feet + Vector3.up * (PlayerGeometry.StandingHeight - r), r - .02f, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore);
    }
}
