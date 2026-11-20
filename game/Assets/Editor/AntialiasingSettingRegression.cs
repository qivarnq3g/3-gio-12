using System;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class AntialiasingSettingRegression
{
    public static void Run()
    {
        int code = 0;
        int original = QualitySettings.GetQualityLevel();
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var body = new GameObject("Player");
            var player = body.AddComponent<FirstPersonPlayer>();
            var eye = new GameObject("Eyes"); eye.transform.SetParent(body.transform, false);
            player.eyes = eye.AddComponent<Camera>();
            var data = player.eyes.GetUniversalAdditionalCameraData();
            Check(new GameSettings().antialiasing == (int)AntialiasingMode.SubpixelMorphologicalAntiAliasing, "default is not SMAA");
            Check(GameSettings.AntialiasingNames.Length == 4 && GameSettings.AntialiasingNames[0] == "Tắt", "choice labels changed");
            for (int i = 0; i < GameSettings.AntialiasingNames.Length; i++)
            {
                var settings = new GameSettings { antialiasing = i };
                settings.Apply(player);
                Check((int)data.antialiasing == i && data.antialiasingQuality == AntialiasingQuality.High, "choice " + i + " not applied to camera");
                var copy = new GameSettings { antialiasing = -1 };
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(settings), copy);
                Check(copy.antialiasing == i, "choice " + i + " lost in saved settings");
            }
            new GameSettings { antialiasing = 9 }.Apply(player);
            Check((int)data.antialiasing == GameSettings.AntialiasingNames.Length - 1, "out-of-range choice not clamped");
            var windows = GameSettings.QualityOrder(QualitySettings.GetActiveQualityLevelsForPlatform("Standalone"));
            Check(windows.Length == 2 && GameSettings.QualityLabel(windows[0]) == "Thấp" && GameSettings.QualityLabel(windows[1]) == "Cao", "Windows quality levels are not Thấp then Cao");
            var low = QualitySettings.GetRenderPipelineAssetAt(windows[0]) as UniversalRenderPipelineAsset;
            var high = QualitySettings.GetRenderPipelineAssetAt(windows[1]) as UniversalRenderPipelineAsset;
            Check(low != null && high != null && low.renderScale < high.renderScale && low.upscalingFilter == UpscalingFilterSelection.FSR
                && low.mainLightShadowmapResolution < high.mainLightShadowmapResolution && low.additionalLightsShadowmapResolution < high.additionalLightsShadowmapResolution, "low quality is not cheaper than high");
            foreach (var level in windows)
                Check(new SerializedObject(QualitySettings.GetRenderPipelineAssetAt(level)).FindProperty("m_SoftShadowQuality").intValue == (int)SoftShadowQuality.Medium,
                    "soft shadows of " + QualitySettings.names[level] + " are not Medium");
            Check(QualitySettings.names[GameSettings.DefaultQuality()] == "PC", "default quality is not PC");
            QualitySettings.SetQualityLevel(windows[0], true);
            new BaGio12.Persistence.GamePreferences().Apply(player);
            Check(QualitySettings.GetQualityLevel() == windows[1], "fresh preferences did not select Cao");
            Debug.Log("ANTIALIASING_SETTING_REGRESSION_PASSED quality_windows=" + string.Join(",", Array.ConvertAll(windows, i => QualitySettings.names[i])));
        }
        catch (Exception error) { Debug.LogException(error); code = 1; }
        finally { QualitySettings.SetQualityLevel(original, true); }
        EditorApplication.Exit(code);
    }

    static void Check(bool pass, string message)
    {
        if (!pass) throw new Exception("ANTIALIASING SETTING REGRESSION: " + message);
    }
}
