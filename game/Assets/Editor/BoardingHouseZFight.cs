using UnityEditor;
using UnityEngine;

public class BoardingHouseZFight : AssetPostprocessor
{
    public const float Depth = .005f;

    public static int Rank(string material)
    {
        if (material.Contains("Floor")) return 0;
        if (material.Contains("Ceiling")) return 1;
        if (material.Contains("WallPaintYellow")) return 2;
        if (material.Contains("Wall")) return 3;
        if (material.Contains("Skirting") || material.Contains("ConcretePole")) return 4;
        return 5;
    }

    public static void Reimport()
    {
        AssetDatabase.ImportAsset("Assets/Art/Models/Environment/env_boarding_house.fbx", ImportAssetOptions.ForceUpdate);
        EditorApplication.Exit(0);
    }

    void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.EndsWith("/env_boarding_house.fbx")) return;
        int moved = 0;
        foreach (var f in root.GetComponentsInChildren<MeshFilter>())
            if (f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null) moved += FacadeZFight.RecedeLosers(f, Rank, Depth, 4);
        Debug.Log($"BOARDING_HOUSE_ZFIGHT receded_vertices={moved}");
    }
}
