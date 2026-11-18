using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CrudeModelPatch
{
    const string PreviewScene = "Assets/Scenes/RealisticPreview.unity";
    const string AlleyModel = "Assets/Art/Models/Environment/env_alley.fbx";
    const string TexRoot = "Assets/Art/TexturesPBR";
    const string MatRoot = "Assets/Art/MaterialsPBR";

    public static void Apply()
    {
        AssetDatabase.Refresh();
        foreach (var id in new[] { "alley_concrete", "garage_floor" }) ImportSet(id);
        var scene = EditorSceneManager.OpenScene(PreviewScene);
        var replace = typeof(RealisticSceneBuilder).GetMethod("ReplaceModelMesh", BindingFlags.NonPublic | BindingFlags.Static);
        var logs = new[] { "Alley_Wires", "Alley_Poles", "Alley_Glass" }.Select(n => (n, log: (string)replace.Invoke(null, new object[] { AlleyModel, n }))).ToList();
        if (logs.Any(l => l.log == null)) { Debug.LogError("CRUDE_APPLY mesh replace failed " + string.Join(",", logs.Where(l => l.log == null).Select(l => l.n))); EditorApplication.Exit(1); return; }
        int clamped = ClampBoardingGlass();
        Floor("M_ConcreteFloor", "alley_concrete", 3.5f, 1f);
        Floor("M_ConcreteWet", "alley_concrete", 3.5f, .82f);
        Floor("M_GarageFloor", "garage_floor", 3f, 1f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, PreviewScene);
        AssetDatabase.SaveAssets();
        Debug.Log($"CRUDE_APPLY {string.Join(" | ", logs.Select(l => l.n + " " + l.log))} glass_clamped={clamped}");
        EditorApplication.Exit(0);
    }

    static void ImportSet(string id)
    {
        foreach (var (kind, ext) in new[] { ("albedo", "jpg"), ("normal", "jpg"), ("mask", "png") })
        {
            var path = $"{TexRoot}/{id}/{id}_{kind}.{ext}";
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogError("CRUDE_APPLY missing " + path); continue; }
            ti.textureType = kind == "normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = kind == "albedo";
            ti.maxTextureSize = 1024;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.SaveAndReimport();
        }
    }

    static void Floor(string name, string id, float size, float tint)
    {
        var path = $"{MatRoot}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexRoot}/{id}/{id}_albedo.jpg"));
        m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexRoot}/{id}/{id}_normal.jpg")); m.EnableKeyword("_NORMALMAP");
        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexRoot}/{id}/{id}_mask.png");
        m.SetTexture("_MetallicGlossMap", mask); m.EnableKeyword("_METALLICSPECGLOSSMAP");
        m.SetTexture("_OcclusionMap", mask); m.EnableKeyword("_OCCLUSIONMAP");
        m.SetFloat("_Smoothness", 1f); m.SetFloat("_BumpScale", 1f); m.SetFloat("_OcclusionStrength", .8f);
        m.SetColor("_BaseColor", Color.white * tint);
        m.SetTextureScale("_BaseMap", new Vector2(1 / size, 1 / size));
        EditorUtility.SetDirty(m);
    }

    static int ClampBoardingGlass()
    {
        var go = GameObject.Find("Alley_Glass"); if (go == null) return -1;
        var mf = go.GetComponent<MeshFilter>(); var tr = go.transform;
        var mesh = Object.Instantiate(mf.sharedMesh); mesh.name = "Alley_Glass";
        var v = mesh.vertices; int n = 0;
        for (int i = 0; i < v.Length; i++)
        {
            var w = tr.TransformPoint(v[i]);
            if (w.x < -1.80f && w.x > -1.98f && w.y > 3f && w.z > -48.1f && w.z < -37.9f) { w.x = -1.80f; v[i] = tr.InverseTransformPoint(w); n++; }
        }
        if (n == 0) { Object.DestroyImmediate(mesh); return 0; }
        mesh.vertices = v; mesh.RecalculateBounds();
        var path = "Assets/Art/MeshesPBR/Alley_Glass_clamped.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        mf.sharedMesh = mesh;
        return n;
    }
}
