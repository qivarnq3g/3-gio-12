using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class LocalPlayBuild
{
    public static void Run()
    {
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Night01/NightOneGraybox.unity" },
            "D:/Projects/3-gio-12-play/Build/3gio12.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("LOCAL_PLAY_BUILD " + report.summary.result + " " + report.summary.totalErrors);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
