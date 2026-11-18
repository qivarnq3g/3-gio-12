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
            Debug.Log("ANTIALIASING_SETTING_REGRESSION_PASSED");
        }
        catch (Exception error) { Debug.LogException(error); code = 1; }
        EditorApplication.Exit(code);
    }

    static void Check(bool pass, string message)
    {
        if (!pass) throw new Exception("ANTIALIASING SETTING REGRESSION: " + message);
    }
}
