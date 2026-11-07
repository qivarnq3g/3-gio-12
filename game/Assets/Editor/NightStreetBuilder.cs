using System;
using System.IO;
using BaGio12.Environment;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class NightStreetBuilder
{
    const string LightsJson = "Assets/Art/Models/Environment/env_main_road_lights.json";
    const string ProfilePath = "Assets/Settings/VP_NightStreet.asset";
    const string PrefabPath = "Assets/Prefabs/Environment/NightStreet.prefab";
    static readonly string[] Models = { "env_main_road", "env_homeware_shop", "env_rice_shop", "env_gas_station" };

    [Serializable] class LightEntry { public string kind; public float[] pos; public float[] color; public float intensity; public float range; public string name; }
    [Serializable] class LightFile { public LightEntry[] lights; }

    [MenuItem("3 Gio 12/Build Night Street Prefab")]
    public static void Build()
    {
        var root = new GameObject("NightStreet");
        root.AddComponent<NightAtmosphere>();
        foreach (var name in Models)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/Environment/{name}.fbx");
            ((GameObject)PrefabUtility.InstantiatePrefab(model)).transform.SetParent(root.transform, false);
        }
        var lights = new GameObject("Lights").transform;
        lights.SetParent(root.transform, false);
        var moon = new GameObject("Moon").AddComponent<Light>();
        moon.transform.SetParent(lights, false);
        moon.type = LightType.Directional;
        moon.transform.rotation = Quaternion.Euler(35, -60, 0);
        moon.color = new Color(0.55f, 0.65f, 1f);
        moon.intensity = 0.25f;
        moon.shadows = LightShadows.Soft;
        var file = JsonUtility.FromJson<LightFile>(File.ReadAllText(LightsJson));
        int count = 0;
        foreach (var entry in file.lights)
        {
            var spot = entry.kind == "spot";
            var name = string.IsNullOrEmpty(entry.name) ? (spot ? "StreetLamp" : "Point") + "_" + count : entry.name;
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(lights, false);
            l.transform.localPosition = new Vector3(-entry.pos[0], entry.pos[2], -entry.pos[1]);
            l.color = new Color(entry.color[0], entry.color[1], entry.color[2]);
            l.intensity = entry.intensity;
            l.range = entry.range;
            if (spot)
            {
                l.type = LightType.Spot;
                l.transform.localRotation = Quaternion.Euler(90, 0, 0);
                l.spotAngle = 120;
                l.innerSpotAngle = 60;
                l.shadows = LightShadows.Soft;
                SetShadowTier(l, UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium);
            }
            else
            {
                l.type = LightType.Point;
                l.shadows = LightShadows.None;
            }
            count++;
        }
        var volume = new GameObject("PostProcess").AddComponent<Volume>();
        volume.transform.SetParent(root.transform, false);
        volume.isGlobal = true;
        volume.sharedProfile = Profile();
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("[NightStreet] prefab " + PrefabPath + " lights " + count);
    }

    public static void SetShadowTier(Light light, int tier)
    {
        var data = new SerializedObject(light.GetUniversalAdditionalLightData());
        data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = tier;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    static VolumeProfile Profile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile != null) return profile;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.8f);
        bloom.intensity.Override(1.4f);
        bloom.scatter.Override(0.75f);
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.6f);
        color.contrast.Override(12f);
        color.saturation.Override(12f);
        profile.Add<Vignette>(true).intensity.Override(0.28f);
        foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
        AssetDatabase.SaveAssets();
        return profile;
    }
}
