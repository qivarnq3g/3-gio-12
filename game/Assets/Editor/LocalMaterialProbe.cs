using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LocalMaterialProbe
{
    public static void Run()
    {
        foreach (var path in new[] { "env_garage", "env_room7", "env_main_road", "env_homeware_shop", "env_rice_shop", "env_gas_station" })
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/Environment/{path}.fbx");
            var stats = go.GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials.Select((m, i) => (m, r.GetComponent<MeshFilter>().sharedMesh, i)))
                .GroupBy(x => x.m ? x.m.name : "null")
                .Select(g => $"{g.Key}:{g.Sum(x => x.sharedMesh.GetSubMesh(x.i).indexCount / 3)}");
            Debug.Log($"MATPROBE {path} {string.Join(" ", stats)}");
        }
        EditorApplication.Exit(0);
    }
}
