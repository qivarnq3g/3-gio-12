using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FloorGapRegression
{
    const int Layer = 31;
    const float CorridorZ = -40.95f;
    const float MaxGap = .004f;
    const float Scan = .1f;

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneRealistic.unity");
        var area = new Bounds(new Vector3(-7.5f, 7f, -41f), new Vector3(14f, 9f, 4f));
        var probes = new List<GameObject>();
        foreach (var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (f.sharedMesh == null || !f.gameObject.activeInHierarchy || !f.TryGetComponent<MeshRenderer>(out var r) || !r.enabled || !r.bounds.Intersects(area)) continue;
            var g = new GameObject("FloorGapProbe") { layer = Layer, hideFlags = HideFlags.DontSave };
            g.transform.SetParent(f.transform, false);
            g.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
            probes.Add(g);
        }
        Physics.SyncTransforms();
        var backfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;
        var gaps = new List<string>();
        int samples = 0;
        foreach (float level in new[] { 3.4f, 6.8f, 10.2f })
            foreach (var dir in new[] { Vector3.back, Vector3.forward })
                for (float x = -13f; x <= -1.5f; x += .02f)
                {
                    if (!Physics.Raycast(new Vector3(x, level + 1.2f, CorridorZ), Vector3.down, out var floor, 2, 1 << Layer)) continue;
                    samples++;
                    for (float h = -.02f; h <= Scan; h += .0025f)
                    {
                        if (!Physics.Raycast(new Vector3(x, floor.point.y + h, CorridorZ), dir, out var wall, 1f, 1 << Layer)) continue;
                        if (h > MaxGap) gaps.Add($"floor {level} x {x:F2} dir {dir.z:F0} gap {h * 100:F1}cm {wall.collider.transform.parent.name}");
                        break;
                    }
                }
        Physics.queriesHitBackfaces = backfaces;
        foreach (var g in probes) Object.DestroyImmediate(g);
        foreach (var g in gaps) Debug.LogError(g);
        if (samples == 0 || gaps.Count > 0)
        {
            Debug.LogError($"[FloorGapRegression] FAIL samples={samples} gaps={gaps.Count}");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log($"[FloorGapRegression] PASS samples={samples}");
        EditorApplication.Exit(0);
    }
}
