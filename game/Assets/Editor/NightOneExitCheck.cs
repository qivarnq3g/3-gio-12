using System.Collections.Generic;
using System.Linq;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NightOneExitCheck
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneRealistic.unity");
        Physics.SyncTransforms();
        const float Cell = .25f;
        var start = Object.FindFirstObjectByType<FirstPersonPlayer>().transform.position;
        var seen = new Dictionary<(int, int), (int px, int pz)>();
        var open = new Queue<(int x, int z, float y)>();
        int sx = Mathf.RoundToInt(start.x / Cell), sz = Mathf.RoundToInt(start.z / Cell);
        Physics.Raycast(new Vector3(sx * Cell, start.y + 1, sz * Cell), Vector3.down, out var h0, 3, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore);
        open.Enqueue((sx, sz, h0.point.y)); seen[(sx, sz)] = (sx, sz);
        (int, int)? exit = null;
        while (open.Count > 0 && exit == null)
        {
            var c = open.Dequeue();
            foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = c.x + dx, nz = c.z + dz; if (seen.ContainsKey((nx, nz))) continue;
                var top = new Vector3(nx * Cell, c.y + .6f, nz * Cell);
                if (Physics.CheckSphere(top, .05f, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (!Physics.Raycast(top, Vector3.down, out var hit, 3.1f, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                float y = hit.point.y;
                if (y - c.y > PlayerGeometry.StepOffset + .05f) continue;
                float r = PlayerGeometry.Radius; var f = new Vector3(nx * Cell, y, nz * Cell);
                if (Physics.CheckCapsule(f + Vector3.up * (r + PlayerGeometry.StepOffset), f + Vector3.up * (PlayerGeometry.StandingHeight - r), r - .02f, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                seen[(nx, nz)] = (c.x, c.z); open.Enqueue((nx, nz, y));
                if (nz * Cell > 3.2f) { exit = (nx, nz); break; }
            }
        }
        Debug.Log($"NIGHT_ONE_EXIT start={start} reachable={seen.Count} exit={(exit.HasValue ? exit.Value.ToString() : "none")}");
        if (exit.HasValue)
        {
            var path = new List<string>(); var k = exit.Value;
            while (true) { path.Add($"({k.Item1 * Cell:0.##},{k.Item2 * Cell:0.##})"); var p = seen[k]; if (p == k) break; k = p; }
            path.Reverse();
            Debug.Log("NIGHT_ONE_EXIT_PATH " + string.Join(" ", path.Where((_, i) => i % 4 == 0 || i == path.Count - 1)));
        }
        Debug.Log(exit.HasValue ? "NIGHT_ONE_EXIT_CHECK_FAILED" : "NIGHT_ONE_EXIT_CHECK_PASSED");
        EditorApplication.Exit(exit.HasValue ? 1 : 0);
    }
}
