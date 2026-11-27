using UnityEditor;
using UnityEngine;

public class AlleyPropsZFight : AssetPostprocessor
{
    public const string Model = "Assets/Art/Models/Environment/env_alley.fbx";
    static readonly string[] Meshes = { "ShopFront_Cooler" };

    public static void Reimport()
    {
        AssetDatabase.ImportAsset(Model, ImportAssetOptions.ForceUpdate);
        EditorApplication.Exit(0);
    }

    void OnPostprocessModel(GameObject root)
    {
        if (assetPath != Model) return;
        int moved = 0;
        foreach (var f in root.GetComponentsInChildren<MeshFilter>())
            if (f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null && System.Array.IndexOf(Meshes, f.name) >= 0)
                moved += FacadeZFight.RecedeLosers(f, BoardingHouseZFight.Rank, BoardingHouseZFight.Depth, 4);
        Debug.Log($"ALLEY_PROPS_ZFIGHT receded_vertices={moved}");
    }
}
