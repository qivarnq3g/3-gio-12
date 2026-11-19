using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FacadeZFight
{
    const string PreviewScene = "Assets/Scenes/RealisticPreview.unity";
    const string NightScene = "Assets/Scenes/Night01/NightOneRealistic.unity";
    const float BalconyLedgeDepth = .08f;
    const float InteriorBackWall = .2f;

    public struct Overlap { public int a, b, sa, sb; public string matA, matB; public Vector3 normal; public float area; public Bounds where; }

    public static void Patch()
    {
        int code = 1;
        try
        {
            EditorSceneManager.OpenScene(PreviewScene);
            var f = GameObject.Find("Alley_Facades").GetComponent<MeshFilter>();
            var path = AssetDatabase.GetAssetPath(f.sharedMesh);
            int before = Find(f).Count(Visible);
            int walls = RecedeCoreBehindInteriorWalls(f);
            int ledges = PullBalconySlabsOffLedges(f);
            var collider = f.GetComponent<MeshCollider>();
            if (collider != null) { collider.sharedMesh = null; collider.sharedMesh = f.sharedMesh; }
            EditorUtility.SetDirty(f.sharedMesh);
            AssetDatabase.SaveAssets();
            int after = Find(f).Count(Visible);
            EditorSceneManager.OpenScene(NightScene);
            var night = GameObject.Find("Alley_Facades").GetComponent<MeshFilter>();
            bool shared = AssetDatabase.GetAssetPath(night.sharedMesh) == path;
            Debug.Log($"FACADE_ZFIGHT_PATCH mesh={path} visible_before={before} core_verts_moved={walls} slab_verts_moved={ledges} visible_after={after} night_scene_shares_mesh={shared}");
            code = after == 0 && shared ? 0 : 1;
        }
        catch (System.Exception e) { Debug.LogException(e); }
        EditorApplication.Exit(code);
    }

    public static void Regression()
    {
        int code = 1;
        try
        {
            var lines = new List<string>(); bool ok = true;
            foreach (var scene in new[] { PreviewScene, NightScene })
            {
                EditorSceneManager.OpenScene(scene);
                var f = GameObject.Find("Alley_Facades").GetComponent<MeshFilter>();
                var all = Find(f);
                var bad = all.Where(Visible).ToList();
                lines.Add($"{scene}: overlaps={all.Count} visible={bad.Count}");
                foreach (var o in bad.Take(10)) lines.Add($"  VISIBLE {o.matA} | {o.matB} n={o.normal} area={o.area:F3} at={o.where.center}");
                ok &= bad.Count == 0;
                foreach (var name in new[] { "Garage_Shell", "Garage_Steel", "Garage_Roof", "env_boarding_house" })
                {
                    var g = GameObject.Find(name).GetComponent<MeshFilter>();
                    var hits = Find(g).Where(o => o.area > .002f && !(o.normal.y < -.5f && o.where.max.y < .02f)).ToList();
                    lines.Add($"  {name}: overlaps={hits.Count}");
                    foreach (var o in hits.Take(5)) lines.Add($"    OVERLAP {o.matA} | {o.matB} n={o.normal} area={o.area:F3} at={o.where.center}");
                    ok &= hits.Count == 0;
                }
            }
            Debug.Log((ok ? "[FacadeZFightRegression] PASS\n" : "[FacadeZFightRegression] FAIL\n") + string.Join("\n", lines));
            code = ok ? 0 : 1;
        }
        catch (System.Exception e) { Debug.LogException(e); }
        EditorApplication.Exit(code);
    }

    static bool Visible(Overlap o)
    {
        bool walls = (o.matA == "M_FacadeGray" && o.matB == "M_WallPaintOffWhite") || (o.matB == "M_FacadeGray" && o.matA == "M_WallPaintOffWhite");
        bool balcony = o.matA == "M_WallPlinthGray" && o.matB == "M_WallPlinthGray" && Mathf.Abs(o.normal.y) > .99f && o.where.center.y > 1f;
        bool underground = o.normal.y < -.5f && o.where.max.y < .02f;
        return (walls || balcony) && !underground && o.area > .005f;
    }

    static int RecedeCoreBehindInteriorWalls(MeshFilter f)
    {
        var tr = f.transform; var mesh = f.sharedMesh; var r = f.GetComponent<MeshRenderer>();
        var verts = mesh.vertices; var world = verts.Select(tr.TransformPoint).ToArray();
        int gray = SubmeshOf(r, "M_FacadeGray");
        var grayVerts = new HashSet<int>(mesh.GetTriangles(gray));
        var otherVerts = new HashSet<int>(Enumerable.Range(0, mesh.subMeshCount).Where(s => s != gray).SelectMany(mesh.GetTriangles));
        var tris = mesh.GetTriangles(gray);
        var moved = new HashSet<int>();
        foreach (var o in Find(f).Where(o => Visible(o) && o.matA != "M_WallPlinthGray"))
        {
            int t = o.matA == "M_FacadeGray" ? o.a : o.b;
            var n = o.normal; float d = Vector3.Dot(n, world[tris[t * 3]]);
            var onPlane = new List<Bounds>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                var c = (world[tris[i]] + world[tris[i + 1]] + world[tris[i + 2]]) / 3;
                if (Mathf.Abs(Vector3.Dot(n, c) - d) < .002f && Vector3.Dot(Vector3.Cross(world[tris[i + 1]] - world[tris[i]], world[tris[i + 2]] - world[tris[i]]).normalized, n) > .99f)
                {
                    var b = new Bounds(world[tris[i]], Vector3.zero); b.Encapsulate(world[tris[i + 1]]); b.Encapsulate(world[tris[i + 2]]); onPlane.Add(b);
                }
            }
            var cap = TriBounds(tr, verts, tris, t);
            for (bool grew = true; grew;)
            {
                grew = false;
                foreach (var b in onPlane)
                    if (b.Intersects(cap) && !(cap.Contains(b.min) && cap.Contains(b.max))) { cap.Encapsulate(b); grew = true; }
            }
            cap.Expand(2 * .0125f);
            foreach (var v in grayVerts)
            {
                if (moved.Contains(v) || !cap.Contains(world[v]) || Mathf.Abs(Vector3.Dot(n, world[v]) - d) > .0125f) continue;
                if (otherVerts.Contains(v)) throw new System.Exception("FacadeZFight: core vertex shared with another material at " + world[v]);
                world[v] -= n * InteriorBackWall; moved.Add(v);
            }
        }
        foreach (var v in moved) verts[v] = tr.InverseTransformPoint(world[v]);
        mesh.vertices = verts; mesh.RecalculateBounds();
        return moved.Count;
    }

    static int PullBalconySlabsOffLedges(MeshFilter f)
    {
        var tr = f.transform; var mesh = f.sharedMesh; var r = f.GetComponent<MeshRenderer>();
        var verts = mesh.vertices; int plinth = SubmeshOf(r, "M_WallPlinthGray");
        var plinthVerts = new HashSet<int>(mesh.GetTriangles(plinth));
        var otherVerts = new HashSet<int>(Enumerable.Range(0, mesh.subMeshCount).Where(s => s != plinth).SelectMany(mesh.GetTriangles));
        var overlaps = Find(f).Where(o => Visible(o) && o.matA == "M_WallPlinthGray").ToList();
        var slabs = new List<Bounds>();
        var tris = mesh.GetTriangles(plinth);
        foreach (var o in overlaps)
        {
            var a = TriBounds(tr, verts, tris, o.a); var b = TriBounds(tr, verts, tris, o.b);
            var slab = Mathf.Min(Mathf.Abs(a.min.x), Mathf.Abs(a.max.x)) < Mathf.Min(Mathf.Abs(b.min.x), Mathf.Abs(b.max.x)) ? a : b;
            slabs.Add(slab);
        }
        int moved = 0;
        foreach (var v in plinthVerts)
        {
            var w = tr.TransformPoint(verts[v]);
            var hit = slabs.Where(s => w.y > s.min.y - .25f && w.y < s.max.y + .25f && w.z > s.min.z - .02f && w.z < s.max.z + .02f).ToList();
            if (hit.Count == 0) continue;
            float wallX = hit.Max(s => Mathf.Max(Mathf.Abs(s.min.x), Mathf.Abs(s.max.x)));
            if (Mathf.Abs(w.x) < wallX - .005f || Mathf.Abs(w.x) > wallX + .013f) continue;
            if (otherVerts.Contains(v)) throw new System.Exception("FacadeZFight: slab vertex shared with another material at " + w);
            w.x -= Mathf.Sign(w.x) * BalconyLedgeDepth; verts[v] = tr.InverseTransformPoint(w); moved++;
        }
        mesh.vertices = verts; mesh.RecalculateBounds();
        return moved;
    }

    static Bounds TriBounds(Transform tr, Vector3[] verts, int[] tris, int t)
    {
        var b = new Bounds(tr.TransformPoint(verts[tris[t * 3]]), Vector3.zero);
        b.Encapsulate(tr.TransformPoint(verts[tris[t * 3 + 1]])); b.Encapsulate(tr.TransformPoint(verts[tris[t * 3 + 2]]));
        return b;
    }

    static int SubmeshOf(MeshRenderer r, string material)
    {
        for (int s = 0; s < r.sharedMaterials.Length; s++) if (r.sharedMaterials[s] != null && r.sharedMaterials[s].name == material) return s;
        throw new System.Exception("FacadeZFight: no submesh " + material);
    }

    public static int RecedeLosers(MeshFilter f, System.Func<string, int> rank, float depth, int passes, System.Func<Overlap, bool> only = null)
    {
        var tr = f.transform; var mesh = f.sharedMesh; int total = 0;
        var verts = mesh.vertices;
        var subTris = Enumerable.Range(0, mesh.subMeshCount).Select(mesh.GetTriangles).ToArray();
        for (int pass = 0; pass < passes; pass++)
        {
            var overlaps = Find(f).Where(o => o.area > .002f && (only == null || only(o))).ToList();
            if (overlaps.Count == 0) break;
            var world = verts.Select(tr.TransformPoint).ToArray();
            var moved = new HashSet<int>();
            var offsets = new Dictionary<int, Vector3>();
            foreach (var o in overlaps.OrderByDescending(o => o.area))
            {
                var ca = Component(subTris[o.sa], world, o.a, o.normal, o.sa == o.sb ? o.b : -1);
                var cb = Component(subTris[o.sb], world, o.b, o.normal, o.sa == o.sb ? o.a : -1);
                int ra = rank(o.matA), rb = rank(o.matB);
                bool loseA = ra != rb ? ra < rb : Area(subTris[o.sa], world, ca) <= Area(subTris[o.sb], world, cb);
                var lose = loseA ? ca : cb; var tris = loseA ? subTris[o.sa] : subTris[o.sb];
                var idx = lose.SelectMany(t => new[] { tris[t * 3], tris[t * 3 + 1], tris[t * 3 + 2] }).Distinct().ToList();
                if (idx.Any(moved.Contains)) continue;
                foreach (var v in idx) { moved.Add(v); offsets[v] = -o.normal * depth; }
            }
            foreach (var kv in offsets) verts[kv.Key] = tr.InverseTransformPoint(world[kv.Key] + kv.Value);
            mesh.vertices = verts; mesh.RecalculateBounds();
            total += offsets.Count;
        }
        return total;
    }

    static List<int> Component(int[] tris, Vector3[] world, int seed, Vector3 n, int avoid)
    {
        foreach (var byIndex in new[] { false, true })
        {
            var c = Component(tris, world, seed, n, byIndex);
            if (!c.Contains(avoid)) return c;
        }
        return new List<int> { seed };
    }

    static List<int> Component(int[] tris, Vector3[] world, int seed, Vector3 n, bool byIndex)
    {
        float d = Vector3.Dot(n, world[tris[seed * 3]]);
        var byPos = new Dictionary<Vector3Int, List<int>>();
        System.Func<int, Vector3Int> key = v => byIndex ? new Vector3Int(v, 0, int.MinValue) : Vector3Int.RoundToInt(world[v] * 10000);
        for (int t = 0; t < tris.Length / 3; t++)
        {
            Vector3 a = world[tris[t * 3]], b = world[tris[t * 3 + 1]], c = world[tris[t * 3 + 2]];
            var cr = Vector3.Cross(b - a, c - a); if (cr.sqrMagnitude < 1e-12f) continue;
            if (Vector3.Dot(cr.normalized, n) < .999f || Mathf.Abs(Vector3.Dot(n, a) - d) > .001f) continue;
            for (int q = 0; q < 3; q++)
            {
                var k = key(tris[t * 3 + q]);
                if (!byPos.TryGetValue(k, out var l)) byPos[k] = l = new List<int>();
                l.Add(t);
            }
        }
        var seen = new HashSet<int> { seed }; var queue = new Queue<int>(); queue.Enqueue(seed);
        while (queue.Count > 0)
        {
            int t = queue.Dequeue();
            for (int k = 0; k < 3; k++)
                if (byPos.TryGetValue(key(tris[t * 3 + k]), out var l))
                    foreach (var u in l) if (seen.Add(u)) queue.Enqueue(u);
        }
        return seen.ToList();
    }

    static float Area(int[] tris, Vector3[] world, List<int> comp) =>
        comp.Sum(t => Vector3.Cross(world[tris[t * 3 + 1]] - world[tris[t * 3]], world[tris[t * 3 + 2]] - world[tris[t * 3]]).magnitude / 2);

    public static List<Overlap> Find(MeshFilter f)
    {
        var tr = f.transform; var mesh = f.sharedMesh; var r = f.GetComponent<MeshRenderer>();
        var world = mesh.vertices.Select(tr.TransformPoint).ToArray();
        var groups = new Dictionary<(int, int, int, int), List<(int s, int t, Vector3 a, Vector3 b, Vector3 c)>>();
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            var tris = mesh.GetTriangles(s);
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = world[tris[i]], b = world[tris[i + 1]], c = world[tris[i + 2]];
                var cross = Vector3.Cross(b - a, c - a); if (cross.magnitude < 2e-4f) continue;
                var n = cross.normalized; float d = Vector3.Dot(n, a);
                var key = (Mathf.RoundToInt(n.x * 200), Mathf.RoundToInt(n.y * 200), Mathf.RoundToInt(n.z * 200), Mathf.RoundToInt(d * 500));
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<(int, int, Vector3, Vector3, Vector3)>();
                list.Add((s, i / 3, a, b, c));
            }
        }
        var result = new List<Overlap>();
        foreach (var g in groups)
        {
            if (g.Value.Count < 2) continue;
            var n = new Vector3(g.Key.Item1, g.Key.Item2, g.Key.Item3).normalized;
            var u = Vector3.Cross(n, Mathf.Abs(n.y) > .9f ? Vector3.right : Vector3.up).normalized; var w = Vector3.Cross(n, u);
            var flat = g.Value.Select(t => (t.s, t.t, p: new[] { new Vector2(Vector3.Dot(t.a, u), Vector3.Dot(t.a, w)), new Vector2(Vector3.Dot(t.b, u), Vector3.Dot(t.b, w)), new Vector2(Vector3.Dot(t.c, u), Vector3.Dot(t.c, w)) }, src: t))
                .Select(t => (t.s, t.t, t.p, t.src, lo: Vector2.Min(t.p[0], Vector2.Min(t.p[1], t.p[2])), hi: Vector2.Max(t.p[0], Vector2.Max(t.p[1], t.p[2])))).OrderBy(t => t.lo.x).ToList();
            for (int i = 0; i < flat.Count; i++)
                for (int j = i + 1; j < flat.Count && flat[j].lo.x < flat[i].hi.x; j++)
                {
                    if (flat[j].lo.y >= flat[i].hi.y || flat[j].hi.y <= flat[i].lo.y) continue;
                    float area = ClipArea(flat[i].p, flat[j].p); if (area < 1e-3f) continue;
                    var where = new Bounds(flat[i].src.a, Vector3.zero); where.Encapsulate(flat[i].src.b); where.Encapsulate(flat[i].src.c);
                    result.Add(new Overlap { a = flat[i].t, b = flat[j].t, sa = flat[i].s, sb = flat[j].s, matA = Name(r, flat[i].s), matB = Name(r, flat[j].s), normal = n, area = area, where = where });
                }
        }
        return result.Select(o => string.CompareOrdinal(o.matA, o.matB) <= 0 ? o : new Overlap { a = o.b, b = o.a, sa = o.sb, sb = o.sa, matA = o.matB, matB = o.matA, normal = o.normal, area = o.area, where = o.where }).ToList();
    }

    static string Name(MeshRenderer r, int s) => s < r.sharedMaterials.Length && r.sharedMaterials[s] != null ? r.sharedMaterials[s].name : "null";

    static float ClipArea(Vector2[] subject, Vector2[] clip)
    {
        var poly = new List<Vector2>(subject);
        if (Signed(clip) < 0) clip = clip.Reverse().ToArray();
        for (int e = 0; e < 3 && poly.Count > 0; e++)
        {
            Vector2 p0 = clip[e], p1 = clip[(e + 1) % 3]; var input = poly; poly = new List<Vector2>();
            for (int i = 0; i < input.Count; i++)
            {
                Vector2 cur = input[i], prev = input[(i + input.Count - 1) % input.Count];
                bool cin = Side(p0, p1, cur) >= -1e-7f, pin = Side(p0, p1, prev) >= -1e-7f;
                if (cin) { if (!pin) poly.Add(Cross(prev, cur, p0, p1)); poly.Add(cur); }
                else if (pin) poly.Add(Cross(prev, cur, p0, p1));
            }
        }
        return poly.Count < 3 ? 0 : Mathf.Abs(Signed(poly.ToArray()));
    }

    static float Side(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    static Vector2 Cross(Vector2 a, Vector2 b, Vector2 p, Vector2 q)
    {
        float s1 = Side(p, q, a), s2 = Side(p, q, b); return a + (b - a) * (s1 / (s1 - s2));
    }
    static float Signed(Vector2[] p)
    {
        float s = 0; for (int i = 0; i < p.Length; i++) { var a = p[i]; var b = p[(i + 1) % p.Length]; s += a.x * b.y - b.x * a.y; } return s / 2;
    }
}
