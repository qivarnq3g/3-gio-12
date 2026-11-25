using System.Collections.Generic;
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
        AssetDatabase.ImportAsset("Assets/Art/Models/Environment/env_room7.fbx", ImportAssetOptions.ForceUpdate);
        EditorApplication.Exit(0);
    }

    void OnPostprocessModel(GameObject root)
    {
        if (assetPath.EndsWith("/env_room7.fbx"))
        {
            int sunk = 0;
            foreach (var f in root.GetComponentsInChildren<MeshFilter>())
                if (f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null) sunk += CloseFloorGaps(f, Room7Floor);
            Debug.Log($"ROOM7_FLOOR_GAP vertices={sunk}");
            return;
        }
        if (!assetPath.EndsWith("/env_boarding_house.fbx")) return;
        int moved = 0, closed = 0;
        foreach (var f in root.GetComponentsInChildren<MeshFilter>())
            if (f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null)
            {
                moved += FacadeZFight.RecedeLosers(f, Rank, Depth, 4);
                closed += CloseFloorGaps(f, Rect.zero);
            }
        Debug.Log($"BOARDING_HOUSE_ZFIGHT receded_vertices={moved} floor_gap_vertices={closed}");
    }

    public static readonly float[] RoomFloorTops = { 3.37f, 6.8f, 10.17f };
    public const float GapBand = .045f;
    public const float Sink = .01f;
    public const float SinkStep = .003f;
    public static readonly Rect LoweredSlab = Rect.MinMaxRect(-13.06f, -47.81f, -1.44f, -40.21f);
    public static readonly Rect Room7Floor = Rect.MinMaxRect(-5.19f, -47.79f, -1.46f, -41.71f);

    static bool IsFloor(Material m) => m != null && (m.name.Contains("Floor") || m.name.Contains("Grout"));

    public static int CloseFloorGaps(MeshFilter f, Rect ownFloor) => CloseFloorGaps(f.sharedMesh, f.GetComponent<MeshRenderer>().sharedMaterials, ownFloor);

    public static int CloseFloorGaps(Mesh mesh, Material[] mats, Rect ownFloor)
    {
        var floorVerts = new HashSet<int>();
        var rank = new int[mesh.vertexCount];
        for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
        {
            if (mats[s] == null) continue;
            int r = Rank(mats[s].name);
            foreach (var i in mesh.GetTriangles(s))
            {
                if (IsFloor(mats[s])) floorVerts.Add(i);
                rank[i] = Mathf.Max(rank[i], r);
            }
        }
        var v = mesh.vertices;
        int moved = 0;
        for (int i = 0; i < v.Length; i++)
        {
            if (floorVerts.Contains(i)) continue;
            var p = new Vector2(v[i].x, v[i].z);
            if (!LoweredSlab.Contains(p) || ownFloor.Contains(p)) continue;
            foreach (var top in RoomFloorTops)
                if (v[i].y > top + .0005f && v[i].y <= top + GapBand)
                {
                    v[i].y = top - Sink - rank[i] * SinkStep;
                    moved++;
                    break;
                }
        }
        if (moved == 0) return 0;
        mesh.vertices = v;
        mesh.RecalculateBounds();
        return moved;
    }

    public static void PatchRoom7Copy()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/RealisticPreview.unity");
        int moved = 0;
        foreach (var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (f.name != "env_room7" || f.sharedMesh == null || !AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/Art/MeshesPBR/")) continue;
            moved += CloseFloorGaps(f, Room7Floor);
            EditorUtility.SetDirty(f.sharedMesh);
            var mc = f.GetComponent<MeshCollider>(); if (mc) { mc.sharedMesh = null; mc.sharedMesh = f.sharedMesh; }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"ROOM7_COPY_FLOOR_GAP vertices={moved}");
        EditorApplication.Exit(0);
    }
}
