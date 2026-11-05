using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class PlaytestBuild
{
    public static void Run()
    {
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Night01/NightOneGraybox.unity" },
            "../Build/3gio12.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("PLAYTEST_BUILD_RESULT " + report.summary.result + " " + report.summary.totalErrors);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    public static void Preview()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLCore });
        AssetDatabase.SaveAssets();
        bool windows = PreviewPlayer("../BuildReal/3gio12-tathuc.exe", BuildTarget.StandaloneWindows64);
        bool linux = windows && PreviewPlayer("../BuildLinux/3gio12.x86_64", BuildTarget.StandaloneLinux64);
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
        Debug.Log("PLAYTEST_PREVIEW_RESULT " + target + " " + report.summary.result + " " + report.summary.totalErrors);
        return report.summary.result == BuildResult.Succeeded;
    }
}
