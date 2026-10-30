using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ArtPipelineCheck
{
    const string ShotPath = "Logs/ArtPipelineCheck.png";

    [MenuItem("3 Gio 12/Check Art Models")]
    public static void CheckFromMenu()
    {
        var failures = CheckAll();
        if (failures.Count == 0) Debug.Log("[ArtCheck] All models passed");
        foreach (var f in failures) Debug.LogError("[ArtCheck] " + f);
    }

    public static void Run()
    {
        int code = 0;
        try
        {
            var failures = CheckAll();
            foreach (var f in failures) Debug.LogError("[ArtCheck] " + f);
            RenderPreview();
            code = failures.Count == 0 ? 0 : 1;
            Debug.Log("[ArtCheck] " + (code == 0 ? "PASS" : "FAIL") + ", preview " + Path.GetFullPath(ShotPath));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            code = 2;
        }
        EditorApplication.Exit(code);
    }

    static IEnumerable<string> ModelPaths()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelImportRules.ModelsRoot.TrimEnd('/') }))
            yield return AssetDatabase.GUIDToAssetPath(guid);
    }

    static List<string> CheckAll()
    {
        var failures = new List<string>();
        foreach (var path in ModelPaths())
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var t = root.transform;
            if (t.localScale != Vector3.one) failures.Add(path + ": root scale " + t.localScale);
            if (Quaternion.Angle(t.localRotation, Quaternion.identity) > 0.01f) failures.Add(path + ": root rotation " + t.localRotation.eulerAngles);
            foreach (Transform child in t)
                if (Quaternion.Angle(child.localRotation, Quaternion.identity) > 0.01f || child.localScale != Vector3.one)
                    failures.Add(path + ": child " + child.name + " rotation " + child.localRotation.eulerAngles + " scale " + child.localScale);
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { failures.Add(path + ": no renderer"); continue; }
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            if (Mathf.Abs(bounds.min.y) > 0.01f) failures.Add(path + ": pivot not at floor, min y " + bounds.min.y.ToString("F3"));
            if (bounds.size.y < 0.01f || bounds.size.y > 30f) failures.Add(path + ": height " + bounds.size.y.ToString("F3") + " m");
            foreach (var r in renderers)
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { failures.Add(path + ": missing material on " + r.name); continue; }
                    var matPath = AssetDatabase.GetAssetPath(m);
                    if (!matPath.StartsWith("Assets/Art/Materials/")) failures.Add(path + ": material " + m.name + " not from Assets/Art/Materials (" + matPath + ")");
                    if (m.shader.name != "Toon/Toon") failures.Add(path + ": material " + m.name + " uses shader " + m.shader.name);
                }
            Debug.Log("[ArtCheck] " + name + " size " + bounds.size.ToString("F3") + " min " + bounds.min.ToString("F3") + " renderers " + renderers.Length);
        }
        return failures;
    }

    static void RenderPreview()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var light = new GameObject("Sun").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = Vector3.one * 0.5f;
        var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.55f, 0.55f, 0.55f) };
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;
        var reference = GameObject.CreatePrimitive(PrimitiveType.Cube);
        reference.name = "Reference 1m";
        reference.transform.localScale = new Vector3(0.05f, 1f, 0.05f);
        reference.transform.position = new Vector3(-0.6f, 0.5f, 0f);
        float x = 0f;
        foreach (var path in ModelPaths())
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            go.transform.position = new Vector3(x, 0f, 0f);
            x += 0.8f;
        }
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.transform.position = new Vector3(x * 0.4f, 1.0f, -2.4f);
        cam.transform.LookAt(new Vector3(x * 0.4f - 0.2f, 0.4f, 0f));
        cam.backgroundColor = new Color(0.18f, 0.2f, 0.24f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        var rt = new RenderTexture(1024, 768, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(ShotPath, tex.EncodeToPNG());
    }
}
