using System;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEngine;

public static class CameraMotionRegression
{
    public static void Run()
    {
        int code = 0;
        try
        {
            foreach (var dt in new[] { 1f / 144, 1f / 60, .1f, .125f, .25f })
            {
                float dip = 0, velocity = -.6f, worst = 0;
                for (int i = 0; i < 40; i++)
                {
                    PlayerCameraMotion.SettleDip(ref dip, ref velocity, dt);
                    worst = Mathf.Max(worst, Mathf.Abs(dip));
                }
                if (worst > .05f || Mathf.Abs(dip) > .01f) throw new Exception($"landing dip unstable at dt={dt}: worst={worst} last={dip}");
            }
            Debug.Log("CAMERA_MOTION_REGRESSION_PASSED");
        }
        catch (Exception error) { Debug.LogException(error); code = 1; }
        EditorApplication.Exit(code);
    }
}
