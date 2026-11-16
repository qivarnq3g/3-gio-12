using System;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlaytestReadoutRegression
{
    public static void Run()
    {
        Debug.Log("PLAYTEST_READOUT_REGRESSION_START");

        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PlayerTest.unity");
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
            if (player == null)
            {
                throw new Exception("FirstPersonPlayer not found in PlayerTest.unity scene!");
            }

            // Trigger Awake on the player
            player.SendMessage("Awake");

            var readout = player.GetComponent<PlaytestReadout>();
            if (readout == null)
            {
                throw new Exception("PlaytestReadout was not automatically attached to FirstPersonPlayer on Awake!");
            }

            // Test 1: Defaults to hidden
            if (readout.IsVisible)
            {
                throw new Exception("PlaytestReadout should be hidden by default!");
            }

            // Test 2: Toggle makes it visible
            readout.Toggle();
            if (!readout.IsVisible)
            {
                throw new Exception("Toggle() did not make PlaytestReadout visible!");
            }

            // Test 3: Readout text contains coordinates, scene name, and viewing angles
            string text = readout.GetReadoutText();
            Debug.Log("READOUT CONTENT:\n" + text);

            if (!text.Contains("PlayerTest"))
            {
                throw new Exception("Readout text does not contain active scene name!");
            }

            if ((!text.Contains("X:") && !text.Contains("X ")) || (!text.Contains("Y:") && !text.Contains("Y ")) || (!text.Contains("Z:") && !text.Contains("Z ")))
            {
                throw new Exception("Readout text missing X, Y, or Z coordinates!");
            }

            if (!text.Contains("Yaw") && !text.Contains("Hướng"))
            {
                throw new Exception("Readout text missing Yaw or Hướng angle!");
            }

            // Test 4: Toggle again hides it
            readout.Toggle();
            if (readout.IsVisible)
            {
                throw new Exception("Toggle() did not hide PlaytestReadout on second call!");
            }

            Debug.Log("PLAYTEST_READOUT_REGRESSION_PASSED");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }
}
