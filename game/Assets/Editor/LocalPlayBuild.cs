using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class LocalPlayBuild
{
    public static void Run()
    {
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Night01/NightOneGraybox.unity" },
            "D:/Projects/3-gio-12-play/Build/3gio12.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("LOCAL_PLAY_BUILD " + report.summary.result + " " + report.summary.totalErrors);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    public static void Preview()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLCore });
        AssetDatabase.SaveAssets();
        bool windows = PreviewPlayer("D:/Projects/3-gio-12-play/BuildReal/3gio12-tathuc.exe", BuildTarget.StandaloneWindows64);
        bool linux = windows && PreviewPlayer("D:/Projects/3-gio-12-play/BuildLinux/3gio12.x86_64", BuildTarget.StandaloneLinux64);
        EditorApplication.Exit(linux ? 0 : 1);
    }

    static bool PreviewPlayer(string path, BuildTarget target)
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/RealisticPreview.unity" },
            locationPathName = path,
            target = target,
            options = BuildOptions.None,
            extraScriptingDefines = new[] { "PLAYTEST_BUILD" }
        });
        Debug.Log("LOCAL_PREVIEW_BUILD " + target + " " + report.summary.result + " " + report.summary.totalErrors);
        return report.summary.result == BuildResult.Succeeded;
    }
}
