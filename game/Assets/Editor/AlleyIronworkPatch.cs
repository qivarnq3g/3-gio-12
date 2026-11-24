using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AlleyIronworkPatch
{
    const string PreviewScene = "Assets/Scenes/RealisticPreview.unity";
    const string GameScene = "Assets/Scenes/Night01/NightOneRealistic.unity";
    const string AlleyModel = "Assets/Art/Models/Environment/env_alley.fbx";
    static readonly string[] Meshes = { "Alley_Poles", "Alley_Wires", "Alley_Ironwork" };

    public static void Apply()
    {
        (AssetImporter.GetAtPath(AlleyModel) as ModelImporter)?.SaveAndReimport();
        var replace = typeof(RealisticSceneBuilder).GetMethod("ReplaceModelMesh", BindingFlags.NonPublic | BindingFlags.Static);
        var preview = EditorSceneManager.OpenScene(PreviewScene);
        var done = new Dictionary<string, (Mesh mesh, Material[] mats)>();
        foreach (var n in Meshes)
        {
            var f = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(m => m.name == n);
            var oldGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(f.sharedMesh));
            var log = (string)replace.Invoke(null, new object[] { AlleyModel, n });
            if (log == null) { Debug.LogError("ALLEY_IRON replace failed " + n); EditorApplication.Exit(1); return; }
            done[n] = (f.sharedMesh, f.GetComponent<MeshRenderer>().sharedMaterials);
            Debug.Log($"ALLEY_IRON preview {n} old_guid={oldGuid} new={AssetDatabase.GetAssetPath(f.sharedMesh)} {log}");
        }
        EditorSceneManager.MarkSceneDirty(preview);
        EditorSceneManager.SaveScene(preview);
        var game = EditorSceneManager.OpenScene(GameScene);
        foreach (var n in Meshes)
        {
            var fs = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(m => m.name == n).ToArray();
            if (fs.Length != 1) { Debug.LogError($"ALLEY_IRON game {n} count={fs.Length}"); EditorApplication.Exit(1); return; }
            var f = fs[0];
            f.sharedMesh = done[n].mesh;
            f.GetComponent<MeshRenderer>().sharedMaterials = done[n].mats;
            var mc = f.GetComponent<MeshCollider>(); if (mc) mc.sharedMesh = done[n].mesh;
            EditorUtility.SetDirty(f); EditorUtility.SetDirty(f.GetComponent<MeshRenderer>()); if (mc) EditorUtility.SetDirty(mc);
            Debug.Log($"ALLEY_IRON game {n} collider={(mc != null)}");
        }
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);
        AssetDatabase.SaveAssets();
        EditorApplication.Exit(0);
    }
}
