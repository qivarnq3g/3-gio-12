using System.Collections.Generic;
using System.Linq;
using BaGio12.Stealth;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CharacterSetup
{
    public const string Root = "Assets/Art/Characters/";
    const string AnimModel = Root + "Animations/UAL1_Standard.fbx";
    const string Controller = Root + "Person.controller";
    static readonly string[] People = { "Hung", "Lien", "Woman", "Boy", "Girl", "Neighbor" };

    public static void Import()
    {
        AssetDatabase.Refresh();
        foreach (var key in People)
        {
            var path = ModelPath(key);
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            if (mi == null) throw new System.Exception("CHAR_SETUP missing " + path);
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = false;
            mi.importCameras = false; mi.importLights = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.materialLocation = ModelImporterMaterialLocation.External;
            foreach (var part in new[] { "Skin", "Eyes", "Brows", "Hair", "Clothes", "Shoes" })
            {
                var m = PersonMaterial(key, part);
                if (m != null) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), $"M_Person{key}{part}"), m);
            }
            mi.SaveAndReimport();
        }
        var ai = (ModelImporter)AssetImporter.GetAtPath(AnimModel);
        ai.animationType = ModelImporterAnimationType.Human;
        ai.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        ai.importAnimation = true;
        ai.materialImportMode = ModelImporterMaterialImportMode.None;
        var clips = ai.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.loopTime = c.name.EndsWith("_Loop");
            c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
            c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
        }
        ai.clipAnimations = clips;
        ai.SaveAndReimport();
        BuildController();
        AssetDatabase.SaveAssets();
        Debug.Log("CHAR_SETUP imported " + string.Join(",", People) + " clips=" + clips.Length);
    }

    static string ModelPath(string key) => Root + $"char_person_{key.ToLowerInvariant()}.fbx";

    static Material PersonMaterial(string key, string part)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Textures/T_Person{key}{part}_Albedo.png");
        if (tex == null) return null;
        var path = Root + $"Materials/M_Person{key}{part}.mat";
        System.IO.Directory.CreateDirectory(Root + "Materials");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white);
        bool cut = part == "Hair" || part == "Brows";
        m.SetFloat("_AlphaClip", cut ? 1 : 0); m.SetFloat("_Cutoff", .45f);
        if (cut) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", cut ? 0 : 2);
        m.renderQueue = cut ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
        m.SetFloat("_Smoothness", part == "Eyes" ? .8f : part == "Skin" ? .32f : part == "Shoes" ? .35f : .15f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static AnimationClip Clip(string name) =>
        AssetDatabase.LoadAllAssetsAtPath(AnimModel).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name))
        ?? throw new System.Exception("CHAR_SETUP missing clip " + name);

    static void BuildController()
    {
        AssetDatabase.DeleteAsset(Controller);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(Controller);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var sm = ctrl.layers[0].stateMachine;
        var state = ctrl.CreateBlendTreeInController("Locomotion", out var tree, 0);
        tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
        tree.AddChild(Clip("Idle_Loop"), 0f);
        tree.AddChild(Clip("Walk_Loop"), 1.3f);
        tree.AddChild(Clip("Jog_Fwd_Loop"), 3.0f);
        tree.AddChild(Clip("Sprint_Loop"), 5.5f);
        sm.defaultState = state;
    }

    static string KeyFor(NightObserver o, ref int neighbour)
    {
        var n = o.displayName ?? o.name;
        if (n.StartsWith("Chú Hùng")) return "Hung";
        if (n.StartsWith("Cô Liên")) return "Lien";
        if (n.StartsWith("Người phụ nữ")) return "Woman";
        if (n.StartsWith("Đứa nhỏ 1")) return "Boy";
        if (n.StartsWith("Đứa nhỏ 2")) return "Girl";
        return neighbour++ % 2 == 0 ? "Neighbor" : "Woman";
    }

    public static int DressObservers(IEnumerable<NightObserver> observers)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);
        if (ctrl == null) throw new System.Exception("CHAR_SETUP run CharacterSetup.Import first, missing " + Controller);
        int neighbour = 0, done = 0;
        foreach (var o in observers)
        {
            var key = KeyFor(o, ref neighbour);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(key));
            if (asset == null) { Debug.LogError("CHAR_SETUP missing model " + key); continue; }
            var placeholder = o.transform.Find("Body placeholder");
            if (placeholder != null) Object.DestroyImmediate(placeholder.gameObject);
            var old = o.transform.Find("Person"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, o.transform);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = "Person";
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            var anim = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath(key)).OfType<Avatar>().FirstOrDefault();
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = false;
            var drive = o.GetComponent<ObserverAnimator>() ?? o.gameObject.AddComponent<ObserverAnimator>();
            drive.animator = anim;
            done++;
        }
        Debug.Log("CHAR_SETUP dressed " + done);
        return done;
    }
}
