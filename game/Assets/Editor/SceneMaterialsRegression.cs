using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneMaterialsRegression
{
    const string ScenePath = "Assets/Scenes/RealisticPreview.unity";

    public static int CountEmptySlots()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var broken = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r.sharedMaterials.Any(m => m == null)).ToList();
        int slots = broken.Sum(r => r.sharedMaterials.Count(m => m == null));
        foreach (var name in broken.Select(r => r.transform.root.name).Distinct().Take(20))
            Debug.LogError("SCENE_MATERIALS empty slot under " + name);
        Debug.Log("SCENE_MATERIALS_REGRESSION renderers=" + broken.Count + " empty_slots=" + slots);
        return slots;
    }

    public static void Run() => EditorApplication.Exit(CountEmptySlots() == 0 ? 0 : 1);
}
