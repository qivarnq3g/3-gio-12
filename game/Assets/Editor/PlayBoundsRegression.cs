using System;
using System.Linq;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayBoundsRegression
{
    static PlayBoundsRegression() { if (SessionState.GetBool("PlayBoundsRegression", false)) EditorApplication.playModeStateChanged += Changed; }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RealisticPreview.unity");
        SessionState.SetBool("PlayBoundsRegression", true);
        EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredPlayMode) return;
        Application.runInBackground = true;
        EditorApplication.update += Tick;
    }
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        if (UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>() == null) return;
        EditorApplication.update -= Tick;
        Execute();
    }
    static void Execute()
    {
        int code = 1;
        try
        {
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
            player.enabled = false;
            var body = player.GetComponent<CharacterController>();
            var root = GameObject.Find("PlayBounds");
            var walls = root.GetComponentsInChildren<BoxCollider>();
            var (attempts, falls) = Walk(body, walls);
            root.SetActive(false); Physics.SyncTransforms();
            var (_, controlFalls) = Walk(body, walls);
            Debug.Log("PLAY_BOUNDS_REGRESSION walls=" + walls.Length + " attempts=" + attempts + " falls=" + falls + " falls_without_walls=" + controlFalls);
            if (attempts > 0 && falls == 0 && controlFalls > 0) { Debug.Log("PLAY_BOUNDS_REGRESSION_PASSED"); code = 0; }
        }
        catch (Exception e) { Debug.LogException(e); }
        SessionState.SetBool("PlayBoundsRegression", false);
        EditorApplication.Exit(code);
    }
    static (int attempts, int falls) Walk(CharacterController body, BoxCollider[] walls)
    {
        int attempts = 0, falls = 0;
        var sides = new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (var wall in walls)
        {
            var c = wall.transform.TransformPoint(wall.center);
            float half = wall.size.x * .5f;
            for (float along = -half; along <= half; along += 1f)
                foreach (var side in sides)
                {
                    var probe = c + Vector3.right * along + side * .8f + Vector3.down * (wall.size.y * .5f - 2);
                    if (!Physics.Raycast(probe, Vector3.down, out var hit, 3f, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    var feet = hit.point;
                    float r = PlayerGeometry.Radius;
                    if (Physics.CheckCapsule(feet + Vector3.up * (r + .3f), feet + Vector3.up * (body.height - r), r, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    attempts++;
                    body.enabled = false; body.transform.position = feet + Vector3.up * .05f; body.enabled = true;
                    Physics.SyncTransforms();
                    for (int i = 0; i < 90; i++) body.Move((-side * 5f + Vector3.down * 9f) * .02f);
                    if (body.transform.position.y < feet.y - 2) falls++;
                }
        }
        return (attempts, falls);
    }
}
