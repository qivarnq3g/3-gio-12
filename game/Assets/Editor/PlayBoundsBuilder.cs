using System.Collections.Generic;
using System.Linq;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayBoundsBuilder
{
    const string ScenePath = "Assets/Scenes/RealisticPreview.unity";
    const string RootName = "PlayBounds";
    const float Cell = .25f, WallHeight = 6f, MaxDrop = 2.5f, Probe = .6f;

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var result = Generate();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("PLAY_BOUNDS leaks_before=" + result.before + " walls=" + result.walls + " leaks_after=" + result.after + " reachable=" + result.reachable);
        EditorApplication.Exit(result.after == 0 ? 0 : 1);
    }

    public static (int before, int walls, int after, int reachable) Generate()
    {
        var old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);
        Physics.SyncTransforms();
        var start = Object.FindFirstObjectByType<FirstPersonPlayer>().transform.position;
        var first = Flood(start, out _);
        var root = new GameObject(RootName);
        var rows = first.GroupBy(c => (c.z, floor: Mathf.RoundToInt(c.y))).ToList();
        int walls = 0;
        foreach (var row in rows)
        {
            var xs = row.Select(c => c.x).Distinct().OrderBy(x => x).ToList();
            for (int i = 0; i < xs.Count;)
            {
                int j = i;
                while (j + 1 < xs.Count && xs[j + 1] == xs[j] + 1) j++;
                var wall = new GameObject("Wall_" + walls++);
                wall.transform.SetParent(root.transform);
                var box = wall.AddComponent<BoxCollider>();
                box.center = new Vector3((xs[i] + xs[j]) * .5f * Cell, row.Key.floor + WallHeight * .5f - 1, row.Key.z * Cell);
                box.size = new Vector3((j - i + 1) * Cell, WallHeight, Cell);
                i = j + 1;
            }
        }
        Physics.SyncTransforms();
        var second = Flood(start, out int reachable);
        return (first.Count, walls, second.Count, reachable);
    }

    static bool Ground(Vector3 top, float below, out float height)
    {
        height = 0;
        if (!Physics.Raycast(top, Vector3.down, out var hit, below, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)) return false;
        height = hit.point.y;
        return true;
    }

    static bool Fits(Vector3 feet)
    {
        float r = PlayerGeometry.Radius;
        var low = feet + Vector3.up * (r + PlayerGeometry.StepOffset);
        var high = feet + Vector3.up * (PlayerGeometry.StandingHeight - r);
        return !Physics.CheckCapsule(low, high, r - .02f, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore);
    }

    static List<Vector3Int> Flood(Vector3 start, out int reachable)
    {
        var seen = new HashSet<(int, int, int)>();
        var leaks = new HashSet<Vector3Int>();
        var open = new Queue<(int x, int z, float y)>();
        int sx = Mathf.RoundToInt(start.x / Cell), sz = Mathf.RoundToInt(start.z / Cell);
        Ground(new Vector3(sx * Cell, start.y + 1, sz * Cell), 3, out float sy);
        open.Enqueue((sx, sz, sy));
        seen.Add((sx, sz, Mathf.RoundToInt(sy * 4)));
        var steps = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        while (open.Count > 0)
        {
            var c = open.Dequeue();
            foreach (var (dx, dz) in steps)
            {
                int nx = c.x + dx, nz = c.z + dz;
                var top = new Vector3(nx * Cell, c.y + Probe, nz * Cell);
                if (Physics.CheckSphere(top, .05f, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (!Ground(top, MaxDrop + Probe, out float y))
                {
                    leaks.Add(new Vector3Int(nx, Mathf.RoundToInt(c.y), nz));
                    continue;
                }
                if (y - c.y > PlayerGeometry.StepOffset + .05f) continue;
                if (!Fits(new Vector3(nx * Cell, y, nz * Cell))) continue;
                if (seen.Add((nx, nz, Mathf.RoundToInt(y * 4)))) open.Enqueue((nx, nz, y));
            }
        }
        reachable = seen.Count;
        return leaks.ToList();
    }
}
