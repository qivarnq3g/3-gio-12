using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PropContactRegression
{
    const string GameScene = "Assets/Scenes/Night01/NightOneRealistic.unity";
    public const float MaxPenetration = .01f;
    public const float MaxHover = .02f;
    const float AlleyHalf = 1.75f;
    static readonly string[] Outdoor = { "trashbag", "Stool_Stack", "old_tyre", "plastic_monobloc_chair", "plastic_crate", "MeterPost" };
    static readonly string[] Scenes = { "Assets/Scenes/RealisticPreview.unity", GameScene };
    static readonly string[] FloorProps = { "trashbag", "Stool_Stack", "old_tyre", "plastic_monobloc_chair", "plastic_crate", "MeterPost", "cardboard_box", "plastic_bottle_gallon" };
    static readonly string[] Solids = { "Alley_Facades", "Alley_Ground", "Alley_Poles", "Alley_Altars", "ShopFront_Counter", "ShopFront_Cooler", "Garage_Shell", "Garage_Steel", "Garage_Dress" };

    public static void Run()
    {
        EditorSceneManager.OpenScene(GameScene);
        EditorApplication.Exit(Measure(true) ? 0 : 1);
    }

    static List<MeshCollider> BuildSolids(List<Object> temp)
    {
        var solids = new List<MeshCollider>();
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || !Solids.Contains(r.name)) continue;
            var g = new GameObject("ContactSolid") { hideFlags = HideFlags.DontSave };
            g.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation); g.transform.localScale = r.transform.lossyScale;
            var c = g.AddComponent<MeshCollider>(); c.sharedMesh = r.GetComponent<MeshFilter>().sharedMesh; solids.Add(c); temp.Add(g);
            names[c] = r.name;
        }
        return solids;
    }

    static List<(MeshRenderer r, MeshCollider c)> BuildProps(List<Object> temp)
    {
        var list = new List<(MeshRenderer, MeshCollider)>();
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || !FloorProps.Any(p => r.name.StartsWith(p)) || r.transform.parent == null) continue;
            if (r.transform.parent.name == "env_alley" && Mathf.Abs(r.bounds.center.x) > AlleyHalf && !Outdoor.Any(p => r.name.StartsWith(p))) continue;
            var g = new GameObject("ContactProp") { hideFlags = HideFlags.DontSave };
            g.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation); g.transform.localScale = r.transform.lossyScale;
            var pc = g.AddComponent<MeshCollider>(); pc.sharedMesh = r.GetComponent<MeshFilter>().sharedMesh; pc.convex = true; temp.Add(g);
            list.Add((r, pc));
        }
        Physics.SyncTransforms();
        return list;
    }

    static void Sync(MeshRenderer r, MeshCollider c)
    {
        c.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
        Physics.SyncTransforms();
    }

    static float Wall(MeshCollider pc, List<MeshCollider> solids, Vector3 lift, out Vector3 push)
    {
        float worst = 0; push = Vector3.zero;
        foreach (var s in solids)
            if (Physics.ComputePenetration(pc, pc.transform.position + lift, pc.transform.rotation, s, s.transform.position, s.transform.rotation, out var dir, out var dist) && dir.y < .9f && dist > worst) { worst = dist; push = dir; }
        return worst;
    }

    static bool Overlap(Bounds a, Bounds b) => a.min.x < b.max.x - .02f && b.min.x < a.max.x - .02f && a.min.z < b.max.z - .02f && b.min.z < a.max.z - .02f;

    static float Pen(MeshCollider pc, List<MeshCollider> solids, Vector3 lift, out Vector3 push)
    {
        float worst = 0; push = Vector3.zero;
        foreach (var s in solids)
            if (Physics.ComputePenetration(pc, pc.transform.position + lift, pc.transform.rotation, s, s.transform.position, s.transform.rotation, out var dir, out var dist) && dist > worst) { worst = dist; push = dir; }
        return worst;
    }

    static bool Flat(List<(MeshRenderer r, MeshCollider c)> group, List<MeshCollider> solids)
    {
        float lo = float.MaxValue, hi = float.MinValue;
        foreach (var (r, _) in group)
        {
            var b = r.bounds;
            for (float u = .1f; u <= .91f; u += .2f)
                for (float v = .1f; v <= .91f; v += .2f)
                {
                    var ray = new Ray(new Vector3(Mathf.Lerp(b.min.x, b.max.x, u), b.max.y + .2f, Mathf.Lerp(b.min.z, b.max.z, v)), Vector3.down);
                    float y = float.MinValue;
                    foreach (var s in solids) if (s.Raycast(ray, out var h, 6f)) y = Mathf.Max(y, h.point.y);
                    if (y < -.02f) continue;
                    lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
                }
        }
        return hi - lo < .03f;
    }

    static bool Nested(MeshRenderer r, List<(MeshRenderer r, MeshCollider c)> props) =>
        props.Any(p => p.r != r && p.r.transform.parent == r.transform.parent && Overlap(p.r.bounds, r.bounds) && p.r.bounds.min.y < r.bounds.min.y - .005f && p.r.bounds.max.y > r.bounds.min.y + .05f);

    static float Support(MeshRenderer r, List<MeshCollider> solids, List<(MeshRenderer r, MeshCollider c)> props, float from)
    {
        var b = r.bounds; float support = float.MinValue;
        for (float u = .25f; u <= .76f; u += .25f)
            for (float v = .25f; v <= .76f; v += .25f)
            {
                var ray = new Ray(new Vector3(Mathf.Lerp(b.min.x, b.max.x, u), from, Mathf.Lerp(b.min.z, b.max.z, v)), Vector3.down);
                foreach (var s in solids) if (s.Raycast(ray, out var h, 6f)) support = Mathf.Max(support, h.point.y);
                foreach (var (o, oc) in props) if (o != r && o.bounds.min.y < b.min.y - .05f && oc.Raycast(ray, out var h, 6f)) support = Mathf.Max(support, h.point.y);
            }
        return support;
    }

    public static void Settle()
    {
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var temp = new List<Object>();
            var solids = BuildSolids(temp);
            var props = BuildProps(temp).OrderBy(p => p.r.bounds.min.y).ToList();
            var log = new List<string>();
            var groups = new List<List<(MeshRenderer r, MeshCollider c)>>();
            foreach (var p in props)
            {
                var hits = groups.Where(g => g.Any(q => q.r.transform.parent == p.r.transform.parent && Overlap(q.r.bounds, p.r.bounds))).ToList();
                var merged = new List<(MeshRenderer r, MeshCollider c)> { p };
                foreach (var g in hits) { merged.AddRange(g); groups.Remove(g); }
                groups.Add(merged);
            }
            foreach (var group in groups.OrderBy(g => g.Min(p => p.r.bounds.min.y)))
            {
                var starts = group.Select(p => p.r.transform.position).ToList();
                bool alley = group[0].r.transform.parent.name == "env_alley";
                var obstacles = alley ? solids.Where(s => names[s] != "Alley_Ground").ToList() : solids;
                var lift = new Vector3(0, alley ? .005f : .03f, 0);
                for (int i = 0; i < 80; i++)
                {
                    float worst = 0; var dir = Vector3.zero;
                    foreach (var (r, pc) in group)
                    {
                        float d = alley ? Pen(pc, obstacles, lift, out var pd) : Wall(pc, obstacles, lift, out pd);
                        if (d > worst) { worst = d; dir = pd; }
                    }
                    if (worst <= .004f && (!alley || Flat(group, solids))) break;
                    var step = alley ? new Vector3(-Mathf.Sign(group[0].r.bounds.center.x), 0, 0) : new Vector3(dir.x, 0, dir.z).normalized;
                    foreach (var (r, pc) in group) { r.transform.position += step * .01f; Sync(r, pc); }
                }
                var low = group.OrderBy(p => p.r.bounds.min.y).First();
                float support = Support(low.r, solids, props.Where(p => !group.Contains(p)).ToList(), low.r.bounds.min.y + .2f);
                if (support > float.MinValue && Mathf.Abs(support - low.r.bounds.min.y) > .003f)
                {
                    float dy = support - low.r.bounds.min.y;
                    foreach (var (r, pc) in group) { r.transform.position += new Vector3(0, dy, 0); Sync(r, pc); }
                }
                foreach (var (r, pc) in group.OrderBy(p => p.r.bounds.min.y))
                {
                    var below = group.Where(p => p.r != r && p.r.bounds.max.y < r.bounds.min.y + .03f && Overlap(p.r.bounds, r.bounds)).ToList();
                    if (below.Count == 0 || Nested(r, group)) continue;
                    float top = Support(r, new List<MeshCollider>(), below, r.bounds.min.y + .2f);
                    if (top > float.MinValue && Mathf.Abs(top - r.bounds.min.y) > .003f) { r.transform.position += new Vector3(0, top - r.bounds.min.y, 0); Sync(r, pc); }
                }
                for (int k = 0; k < group.Count; k++)
                {
                    var t = group[k].r.transform;
                    if ((t.position - starts[k]).sqrMagnitude > 1e-6f) { EditorUtility.SetDirty(t); log.Add($"{t.parent.name}/{t.name} moved {(t.position - starts[k]) * 100:F1}cm to {t.position:F3} group={group.Count}"); }
                }
            }
            foreach (var o in temp) Object.DestroyImmediate(o);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"PROP_SETTLE {path} moved={log.Count}\n" + string.Join("\n", log));
        }
        EditorApplication.Exit(0);
    }

    public static bool Measure(bool log)
    {
        var temp = new List<Object>();
        var solids = BuildSolids(temp);
        var all = BuildProps(temp);
        var lines = new List<string>(); var bad = new List<string>();
        foreach (var (r, pc) in all)
        {
            float worst = 0; string into = "";
            foreach (var s in solids)
                if (Physics.ComputePenetration(pc, pc.transform.position, pc.transform.rotation, s, s.transform.position, s.transform.rotation, out var dir, out var dist) && dist > worst)
                { worst = dist; into = $"{names[s]} dir={dir:F2}"; }
            float support = Support(r, solids, all, r.bounds.min.y + .05f);
            float hover = support > float.MinValue && !Nested(r, all) ? Mathf.Max(0, r.bounds.min.y - support) : 0;
            string where = $"{r.transform.parent.name}/{r.name} at {r.bounds.center:F2}";
            lines.Add($"{where} pen={worst * 100:F1}cm {into} hover={hover * 100:F1}cm");
            if (worst > MaxPenetration) bad.Add($"{where} penetrates {worst * 100:F1}cm {into}");
            if (hover > MaxHover) bad.Add($"{where} hovers {hover * 100:F1}cm above support");
        }
        foreach (var pole in Poles())
        {
            var g = new GameObject("ContactPole") { hideFlags = HideFlags.DontSave };
            g.transform.position = pole;
            var cap = g.AddComponent<CapsuleCollider>(); cap.radius = .14f; cap.height = .6f; cap.center = new Vector3(0, .32f, 0); temp.Add(g);
            Physics.SyncTransforms();
            float worst = 0;
            foreach (var s in solids.Where(s => names[s] == "Alley_Facades"))
                if (Physics.ComputePenetration(cap, g.transform.position, Quaternion.identity, s, s.transform.position, s.transform.rotation, out _, out var dist)) worst = Mathf.Max(worst, dist);
            lines.Add($"pole base {pole:F2} pen={worst * 100:F1}cm");
            if (worst > MaxPenetration) bad.Add($"pole at {pole:F2} cuts the facade plinth {worst * 100:F1}cm");
        }
        foreach (var o in temp) Object.DestroyImmediate(o);
        if (log) Debug.Log("[PropContact] report\n" + string.Join("\n", lines));
        foreach (var b in bad) Debug.LogError(b);
        bool ok = all.Count > 0 && bad.Count == 0;
        if (ok) Debug.Log($"[PropContact] PASS props={all.Count}"); else Debug.LogError($"[PropContact] FAIL props={all.Count} problems={bad.Count}");
        return ok;
    }

    static readonly Dictionary<MeshCollider, string> names = new Dictionary<MeshCollider, string>();

    static IEnumerable<Vector3> Poles()
    {
        var r = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(m => m.name == "Alley_Poles");
        if (r == null) yield break;
        var m = r.GetComponent<MeshFilter>().sharedMesh; var mats = r.sharedMaterials; var v = m.vertices;
        for (int s = 0; s < m.subMeshCount && s < mats.Length; s++)
        {
            if (mats[s] == null || !mats[s].name.Contains("ConcretePole")) continue;
            var bases = m.GetTriangles(s).Distinct().Select(i => r.transform.TransformPoint(v[i])).Where(p => p.y < .05f).ToList();
            var groups = new List<List<Vector3>>();
            foreach (var p in bases)
            {
                var gp = groups.FirstOrDefault(gr => (gr[0] - p).sqrMagnitude < .25f);
                if (gp == null) groups.Add(new List<Vector3> { p }); else gp.Add(p);
            }
            foreach (var gr in groups) { var c = gr.Aggregate(Vector3.zero, (a, b) => a + b) / gr.Count; yield return new Vector3(c.x, 0, c.z); }
        }
    }
    public static void SpreadStacks()
    {
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var stack = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name.StartsWith("Stool_Stack")).OrderBy(t => t.localPosition.y).ToList();
            if (stack.Count < 2) { Debug.LogError($"STOOL_SPREAD {path} count={stack.Count}"); EditorApplication.Exit(1); return; }
            var z = stack[0].localPosition.z;
            for (int k = 0; k < stack.Count; k++)
            {
                var p = stack[k].localPosition;
                stack[k].localPosition = new Vector3(p.x, p.y, z - .003f * k);
                EditorUtility.SetDirty(stack[k]);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"STOOL_SPREAD {path} count={stack.Count}");
        }
        EditorApplication.Exit(0);
    }
}
