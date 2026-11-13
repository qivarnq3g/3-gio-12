using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
        int decals = Object.FindObjectsByType<DecalProjector>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(d => d.material == null);
        if (decals > 0) Debug.LogError("SCENE_MATERIALS decal projectors without material: " + decals);
        Debug.Log("SCENE_MATERIALS_REGRESSION renderers=" + broken.Count + " empty_slots=" + slots + " empty_decals=" + decals);
        return slots + decals;
    }

    public static void Run() => EditorApplication.Exit(CountEmptySlots() == 0 ? 0 : 1);
}
