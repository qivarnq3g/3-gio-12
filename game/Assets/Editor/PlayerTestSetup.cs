using System;
using System.IO;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PlayerTestSetup
{
    static Material Mat(string name, Color color)
    {
        string path = "Assets/Scenes/PlayerTestMaterials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color; return mat;
    }
    static GameObject Box(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
        obj.transform.position = pos; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat; return obj;
    }
    [MenuItem("3 Gio 12/Create Player Test Scene")]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/Scenes/PlayerTestMaterials"); AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var gray = Mat("Concrete", new Color(.4f,.43f,.46f));
        var floor = Mat("Floor", new Color(.23f,.27f,.3f));
        var doorMat = Mat("Door", new Color(.15f,.5f,.45f));
        var gold = Mat("Evidence", new Color(1,.65f,.1f));
        Box("Floor", new Vector3(0,-.15f,0), new Vector3(10,.3f,14), floor);
        Box("West wall", new Vector3(-5,1.5f,0), new Vector3(.3f,3,14), gray);
        Box("East wall", new Vector3(5,1.5f,0), new Vector3(.3f,3,14), gray);
        Box("Back wall", new Vector3(0,1.5f,-7), new Vector3(10,3,.3f), gray);
        Box("Far wall", new Vector3(0,1.5f,7), new Vector3(10,3,.3f), gray);
        Box("Partition left", new Vector3(-3,1.5f,1), new Vector3(4,3,.3f), gray);
        Box("Partition right", new Vector3(3,1.5f,1), new Vector3(4,3,.3f), gray);
        Box("Door frame", new Vector3(0,2.8f,1), new Vector3(2,.4f,.3f), gray);
        Box("Door", new Vector3(0,1.25f,1), new Vector3(1.9f,2.5f,.25f), doorMat).AddComponent<TestDoor>();
        Box("Evidence", new Vector3(0,.85f,4), Vector3.one * .35f, gold).AddComponent<TestPickup>();
        Box("Evidence stand", new Vector3(0,.3f,4), new Vector3(1,.6f,1), gray);
        Box("Crouch tunnel", new Vector3(-3,1.6f,-2), new Vector3(2.5f,.6f,3), gray);
        Box("World running indicator", new Vector3(3,1.8f,4), Vector3.one * .6f, gold).AddComponent<TestWorldClock>();
        var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional;
        light.intensity = 1.5f; light.transform.rotation = Quaternion.Euler(50,-30,0);
        RenderSettings.ambientLight = new Color(.5f,.5f,.5f);
        var player = new GameObject("Player"); player.layer = 2;
        player.transform.position = new Vector3(0,.1f,-3);
        var cc = player.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = .3f;
        cc.center = Vector3.up * .9f; cc.skinWidth = .02f; cc.stepOffset = .25f;
        var camObject = new GameObject("Main Camera"); camObject.tag = "MainCamera"; camObject.layer = 2;
        camObject.transform.SetParent(player.transform, false); camObject.transform.localPosition = Vector3.up * 1.6f;
        var cam = camObject.AddComponent<Camera>(); cam.nearClipPlane = .03f; cam.fieldOfView = 70;
        camObject.AddComponent<AudioListener>();
        var controller = player.AddComponent<FirstPersonPlayer>(); controller.eyes = cam;
        controller.inputTemplate = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (controller.inputTemplate == null) throw new Exception("Missing Input System asset");
        // Geometry placeholders for forearms only, no body/head or reflections.
        for (int i=0;i<2;i++)
        {
            var arm = Box(i == 0 ? "Left forearm placeholder" : "Right forearm placeholder", Vector3.zero, new Vector3(.085f,.085f,.32f), gray);
            arm.layer = 2; UnityEngine.Object.DestroyImmediate(arm.GetComponent<Collider>());
            arm.transform.SetParent(camObject.transform, false);
            arm.transform.localPosition = new Vector3(i == 0 ? -.22f : .22f,-.22f,.4f);
        }
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/PlayerTest.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("PLAYER_TEST_SCENE_CREATED");
    }
    public static void OpenForReview()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PlayerTest.unity");
        Selection.activeGameObject = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>().gameObject;
    }
    public static void Validate()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PlayerTest.unity");
        var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
        if (player == null || player.eyes == null || player.inputTemplate == null) throw new Exception("Player setup incomplete");
        player.SendMessage("Awake");
        if (player.eyes.nearClipPlane > .05f) throw new Exception("Unsafe near clip");
        var door = UnityEngine.Object.FindFirstObjectByType<TestDoor>();
        door.Interact(player); if (!door.IsOpen) throw new Exception("Door failed to open");
        door.Interact(player); if (door.IsOpen) throw new Exception("Door failed to close");
        var pickup = UnityEngine.Object.FindFirstObjectByType<TestPickup>();
        pickup.Interact(player); if (player.ItemsCollected != 1 || pickup.gameObject.activeSelf) throw new Exception("Pickup failed");
        player.SetCrouch(true); Physics.SyncTransforms();
        if (!player.SetCrouch(false)) throw new Exception("Standing in open space failed");
        player.GetComponent<CharacterController>().enabled = false;
        player.transform.position = new Vector3(-3,0,-2);
        player.GetComponent<CharacterController>().enabled = true;
        player.SetCrouch(true); Physics.SyncTransforms();
        if (player.SetCrouch(false)) throw new Exception("Standing through ceiling allowed");
        player.SetPhone(1); if (player.ActivePhone != 1 || Time.timeScale != 1) throw new Exception("Phone state failed");
        player.SetPhone(2); if (player.ActivePhone != 2) throw new Exception("Phone switch failed");
        player.SetPhone(0);
        Debug.Log("PLAYER_TEST_VALIDATION_PASSED");
    }
}
