using System.IO;
using System.Linq;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class LocalArtPreview
{
    const string ScenePath = "Assets/Scenes/ArtPreview.unity";
    static readonly string[] Models = {
        "Assets/Art/Models/Environment/env_garage.fbx", "Assets/Art/Models/Environment/env_room7.fbx",
        "Assets/Art/Models/Environment/env_main_road.fbx", "Assets/Art/Models/Environment/env_homeware_shop.fbx",
        "Assets/Art/Models/Environment/env_rice_shop.fbx", "Assets/Art/Models/Environment/env_gas_station.fbx" };

    static GameObject Place(string path)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        go.transform.position = Vector3.zero; return go;
    }
    static Bounds RendererBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
    static void Collide(GameObject root)
    {
        foreach (var f in root.GetComponentsInChildren<MeshFilter>())
        {
            var r = f.GetComponent<Renderer>();
            if (r != null && r.enabled && f.GetComponent<Collider>() == null) f.gameObject.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
        }
    }
    static Light Point(string name, Vector3 at, Color c, float range, float intensity)
    {
        var l = new GameObject(name).AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity;
        l.shadows = LightShadows.Soft; l.transform.position = at; return l;
    }

    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var envs = Models.Where(File.Exists).Select(Place).ToList();
        var blockout = Place("Assets/Art/Blockout/night1_3_blockout.fbx");
        var cover = envs.Select(RendererBounds).ToList();
        int hidden = 0;
        foreach (var r in blockout.GetComponentsInChildren<Renderer>())
        {
            var c = r.bounds.center;
            if (cover.Any(b => { var e = b; e.Expand(.1f); return e.Contains(c); })) { r.enabled = false; hidden++; }
            else if (cover.Any(b => b.Intersects(r.bounds))) r.transform.position += Vector3.down * .03f;
        }
        foreach (var t in blockout.GetComponentsInChildren<Transform>())
            if (t.name.StartsWith("CHECKPOINT") || t.name.StartsWith("PLACE") || t.name.StartsWith("DOOR") || t.name.StartsWith("SPAWN") || t.name.StartsWith("CLUE"))
            { var r = t.GetComponent<Renderer>(); if (r) r.enabled = false; }
        envs.ForEach(Collide); Collide(blockout);

        var cubAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Props/prop_motorbike_cub.fbx");
        if (cubAsset != null)
        {
            var cub = (GameObject)PrefabUtility.InstantiatePrefab(cubAsset);
            cub.transform.SetPositionAndRotation(new Vector3(-3.15f, 0, -52.6f), Quaternion.Euler(0, 90, 0));
            var b = RendererBounds(cub); var box = cub.AddComponent<BoxCollider>();
            box.center = cub.transform.InverseTransformPoint(b.center); box.size = new Vector3(.7f, b.size.y, 1.8f);
        }

        var street = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/NightStreet.prefab");
        if (street != null) PrefabUtility.InstantiatePrefab(street);
        else
        {
            var moon = new GameObject("Moon").AddComponent<Light>(); moon.type = LightType.Directional; moon.intensity = .15f;
            moon.color = new Color(.55f, .65f, 1f); moon.transform.rotation = Quaternion.Euler(40, -30, 0);
        }
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.07f, .08f, .12f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .015f; RenderSettings.fogColor = new Color(.05f, .06f, .09f);

        Point("Garage bulb", new Vector3(0, 2.5f, -53), new Color(1f, .78f, .5f), 9, 2.5f);
        Point("Room7 bulb", new Vector3(-3.3f, 12.9f, -44.5f), new Color(.85f, .92f, 1f), 7, 1.8f);
        foreach (var t in blockout.GetComponentsInChildren<Transform>().Where(t => t.name.Contains("MotionLight")).ToList())
        {
            var r = t.GetComponent<Renderer>();
            if (r) Point(t.name + " light", r.bounds.center + Vector3.down * .1f, new Color(1f, .85f, .65f), 5, 1.2f).shadows = LightShadows.None;
        }
        for (int i = 0; i < 4; i++) Point("Alley lamp " + i, new Vector3(i % 2 == 0 ? -1.1f : 1.1f, 3.2f, -6 - i * 11), new Color(1f, .75f, .45f), 8, 1.6f);

        var player = new GameObject("Player"); player.layer = PlayerGeometry.PlayerLayer;
        player.transform.SetPositionAndRotation(new Vector3(0, .1f, -0.8f), Quaternion.Euler(0, 180, 0));
        var cc = player.AddComponent<CharacterController>(); cc.height = PlayerGeometry.StandingHeight; cc.radius = PlayerGeometry.Radius;
        cc.center = Vector3.up * (PlayerGeometry.StandingHeight / 2); cc.skinWidth = PlayerGeometry.SkinWidth; cc.stepOffset = PlayerGeometry.StepOffset;
        var camObject = new GameObject("Main Camera"); camObject.tag = "MainCamera"; camObject.layer = PlayerGeometry.PlayerLayer;
        camObject.transform.SetParent(player.transform, false); camObject.transform.localPosition = Vector3.up * PlayerGeometry.StandingEye;
        var cam = camObject.AddComponent<Camera>(); cam.nearClipPlane = .03f; cam.fieldOfView = 70;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.02f, .025f, .05f); RenderSettings.skybox = null;
        camObject.AddComponent<AudioListener>();
        var controller = player.AddComponent<FirstPersonPlayer>(); controller.eyes = cam; controller.showTestHud = false;
        controller.inputTemplate = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"ART_PREVIEW_SCENE envs={envs.Count} hidden={hidden}");
        Shots(cam);
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "D:/Projects/3-gio-12-play/BuildArt/3gio12-canh.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("ART_PREVIEW_BUILD " + report.summary.result);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static void Shots(Camera cam)
    {
        var views = new (string name, Vector3 pos, Vector3 rot)[] {
            ("alley", new Vector3(0, 1.6f, -1), new Vector3(5, 180, 0)),
            ("garage", new Vector3(0, 1.6f, -48.5f), new Vector3(10, 180, 0)),
            ("room7", new Vector3(-2.3f, 11.8f, -42.3f), new Vector3(15, 200, 0)),
            ("road", new Vector3(0, 1.6f, 3), new Vector3(5, 60, 0)) };
        var rt = new RenderTexture(1280, 720, 24); var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        Directory.CreateDirectory("Logs");
        foreach (var v in views)
        {
            cam.transform.SetPositionAndRotation(v.pos, Quaternion.Euler(v.rot));
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes($"Logs/ArtPreview_{v.name}.png", tex.EncodeToPNG());
        }
        cam.targetTexture = null; RenderTexture.active = null;
        cam.transform.localPosition = Vector3.up * PlayerGeometry.StandingEye; cam.transform.localRotation = Quaternion.identity;
    }
}
