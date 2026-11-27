using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CeilingLightLeak
{
    public const float OuterAngle = 170f;
    public const float InnerAngle = 140f;
    public const float CorridorIntensity = 4.5f;
    public const string GlowName = "Ceiling glow";
    public const float GlowRange = .6f;
    public const float GlowShare = .8f;
    public const float MaxLeak = 2f;
    const int Layer = 31;
    const float SlabDepth = .45f;
    static readonly string[] Scenes = { "Assets/Scenes/RealisticPreview.unity", "Assets/Scenes/Night01/NightOneRealistic.unity" };
    static readonly string[] KeepPoint = { "Boarding landing lamp ", "Boarding ground bulb", "Boarding entry bulb", "Boarding lien bulb" };
    static readonly Bounds Area = new Bounds(new Vector3(-7.5f, 8f, -41f), new Vector3(14f, 13f, 4f));
    static readonly float[] FloorTops = { 3.4f, 6.8f, 10.2f };

    public static Light AimDown(Light l)
    {
        l.type = LightType.Spot;
        l.spotAngle = OuterAngle;
        l.innerSpotAngle = InnerAngle;
        l.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        var t = l.transform.Find(GlowName);
        var glow = t != null ? t.GetComponent<Light>() : new GameObject(GlowName).AddComponent<Light>();
        glow.transform.SetParent(l.transform, false);
        glow.transform.localPosition = Vector3.zero;
        glow.type = LightType.Point;
        glow.range = GlowRange;
        glow.intensity = l.intensity * GlowShare;
        glow.color = l.color;
        glow.shadows = LightShadows.None;
        glow.lightmapBakeType = LightmapBakeType.Realtime;
        return l;
    }

    static bool IsCorridorLamp(Light l) => l.name.StartsWith("BH_F") && l.name.Contains("MotionLight") && l.name != GlowName;

    static bool IsKeptPoint(Light l)
    {
        foreach (var n in KeepPoint) if (l.name.StartsWith(n)) return true;
        return false;
    }

    public static void Patch()
    {
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            int aimed = 0, kept = 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l == null) continue;
                if (IsCorridorLamp(l))
                {
                    l.intensity = CorridorIntensity;
                    AimDown(l);
                    aimed++;
                }
                else if (IsKeptPoint(l))
                {
                    l.type = LightType.Point;
                    l.spotAngle = 30f;
                    l.innerSpotAngle = 21.80208f;
                    l.transform.rotation = Quaternion.identity;
                    var glow = l.transform.Find(GlowName);
                    if (glow != null) Object.DestroyImmediate(glow.gameObject);
                    kept++;
                }
                else continue;
                EditorUtility.SetDirty(l);
                EditorUtility.SetDirty(l.transform);
            }
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"CEILING_LIGHT_PATCH {path} aimed={aimed} kept_point={kept}");
        }
        EditorApplication.Exit(0);
    }

    static bool ThroughSlab(float light, float block, float sample)
    {
        foreach (float top in FloorTops)
            if (block >= top - SlabDepth && block <= top + .05f && (light - top) * (sample - top) < 0) return true;
        return false;
    }

    static float Illuminance(Light l, Vector3 p, Vector3 n)
    {
        var d = p - l.transform.position;
        float dist = d.magnitude;
        if (dist > l.range) return 0;
        float ndotl = Vector3.Dot(n, -d / dist);
        if (ndotl <= 0) return 0;
        float window = Mathf.Clamp01(1 - Mathf.Pow(dist / l.range, 4));
        float e = l.intensity * window * window / (dist * dist) * ndotl;
        if (l.type == LightType.Spot)
        {
            float cosOuter = Mathf.Cos(l.spotAngle * .5f * Mathf.Deg2Rad);
            float cosInner = Mathf.Cos(l.innerSpotAngle * .5f * Mathf.Deg2Rad);
            float s = Mathf.Clamp01((Vector3.Dot(l.transform.forward, d / dist) - cosOuter) / Mathf.Max(cosInner - cosOuter, 1e-4f));
            e *= s * s;
        }
        return e;
    }

    public static void Regression()
    {
        EditorSceneManager.OpenScene(Scenes[1]);
        EditorApplication.Exit(Measure() ? 0 : 1);
    }

    public static bool Measure()
    {
        var probes = new List<GameObject>();
        foreach (var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (f.sharedMesh == null || !f.gameObject.activeInHierarchy || !f.TryGetComponent<MeshRenderer>(out var r) || !r.enabled || r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off || !r.bounds.Intersects(Area)) continue;
            var g = new GameObject("LeakProbe") { layer = Layer, hideFlags = HideFlags.DontSave };
            g.transform.SetParent(f.transform, false);
            g.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
            probes.Add(g);
        }
        Physics.SyncTransforms();
        var backfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;
        var samples = new List<(Vector3 p, Vector3 n)>();
        foreach (float top in FloorTops)
            foreach (var dir in new[] { Vector3.back, Vector3.forward })
                for (float x = -13f; x <= -1.5f; x += .25f)
                    foreach (float h in new[] { .02f, .08f, .2f, .5f })
                        if (Physics.Raycast(new Vector3(x, top + h, -40.95f), dir, out var w, 1f, 1 << Layer))
                            samples.Add((w.point + w.normal * .005f, w.normal));
        var leaks = new List<string>();
        var report = new List<string>();
        int lamps = 0;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (!l.isActiveAndEnabled || l.shadows != LightShadows.None || (l.type != LightType.Point && l.type != LightType.Spot) || !Area.Contains(l.transform.position)) continue;
            lamps++;
            float worst = 0; string where = "";
            foreach (var (p, n) in samples)
            {
                float e = Illuminance(l, p, n);
                if (e <= worst) continue;
                var d = p - l.transform.position;
                if (!Physics.Raycast(l.transform.position + d.normalized * .15f, d.normalized, out var block, d.magnitude - .16f, 1 << Layer) || !ThroughSlab(l.transform.position.y, block.point.y, p.y)) continue;
                worst = e; where = $"{p:F2} through {block.collider.transform.parent.name}@{block.point.y:F2}";
            }
            if (worst > 0) report.Add($"{l.name} type={l.type} leak={worst:F2} at {where}");
            if (worst > MaxLeak) leaks.Add($"{l.name} type={l.type} leak={worst:F2} > {MaxLeak} at {where}");
        }
        Physics.queriesHitBackfaces = backfaces;
        foreach (var g in probes) Object.DestroyImmediate(g);
        Debug.Log("[CeilingLightLeak] report\n" + string.Join("\n", report));
        foreach (var s in leaks) Debug.LogError(s);
        if (lamps == 0 || samples.Count == 0 || leaks.Count > 0)
        {
            Debug.LogError($"[CeilingLightLeak] FAIL lamps={lamps} samples={samples.Count} leaks={leaks.Count}");
            return false;
        }
        Debug.Log($"[CeilingLightLeak] PASS lamps={lamps} samples={samples.Count}");
        return true;
    }
}
