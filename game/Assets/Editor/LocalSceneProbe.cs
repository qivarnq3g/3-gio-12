using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LocalSceneProbe
{
    public static void Run()
    {
        foreach (var path in new[] { "Assets/Art/Blockout/night1_3_blockout.fbx", "Assets/Art/Models/Environment/env_garage.fbx", "Assets/Art/Models/Environment/env_room7.fbx" })
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var b = new Bounds(); bool first = true;
            foreach (var r in root.GetComponentsInChildren<Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
            Debug.Log($"PROBE {path} bounds {b.min} {b.max} children {root.transform.childCount}");
            foreach (var t in root.GetComponentsInChildren<Transform>())
                if (t.name.StartsWith("CHECKPOINT") || t.name.StartsWith("PLACE") || t.name.StartsWith("DOOR") || t.name.StartsWith("CLUE") || t.name.StartsWith("SPAWN") || t.parent == root.transform)
                {
                    var rr = t.GetComponent<Renderer>();
                    Debug.Log($"PROBE   {t.name} pos {t.position} {(rr ? rr.bounds.min + " " + rr.bounds.max : "")}");
                }
            Object.DestroyImmediate(root);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/Night01/NightOneGraybox.unity");
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Checkpoint") || t.name.Contains("Room") || t.name.Contains("Parking") || t.name.Contains("Stair") && t.parent == null || t.name.Contains("bike") || t.name.Contains("Corridor") || t.name.Contains("Door")))
            Debug.Log($"PROBE SCENE {t.name} pos {t.position} scale {t.lossyScale}");
        EditorApplication.Exit(0);
    }
}
