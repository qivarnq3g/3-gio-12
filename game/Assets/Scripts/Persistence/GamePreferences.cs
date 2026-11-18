using System;
using BaGio12.Gameplay;
using UnityEngine;

namespace BaGio12.Persistence
{
    [Serializable] public sealed class GamePreferences
    {
        public int schemaVersion = 1;
        public float volume = 1, mouseSensitivity = .09f;
        public int quality = -1;
        public bool showContentWarning = true;
        public static bool Valid(GamePreferences p) => p.schemaVersion == 1 && Finite(p.volume) && p.volume >= 0 && p.volume <= 1
            && Finite(p.mouseSensitivity) && p.mouseSensitivity >= .02f && p.mouseSensitivity <= .3f
            && p.quality >= -1 && p.quality < QualitySettings.names.Length;
        public static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        public void Apply(FirstPersonPlayer player)
        {
            AudioListener.volume = volume;
            player.sensitivity = mouseSensitivity;
            if (quality >= 0 && quality != QualitySettings.GetQualityLevel()) QualitySettings.SetQualityLevel(quality, true);
        }
    }
}
