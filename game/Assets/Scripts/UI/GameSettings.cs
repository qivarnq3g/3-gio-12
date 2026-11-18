using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BaGio12.Gameplay
{
    [Serializable]
    public sealed class GameSettings
    {
        const string Key = "player-settings-v1";
        static readonly List<(ColorAdjustments adjustments, float baseline)> exposures = new List<(ColorAdjustments, float)>();

        public float sensitivity = .09f;
        public bool invertY;
        public float fieldOfView = 70;
        public float headBob = 1;
        public float volume = 1;
        public float brightness;

        public GameSettings Copy() => (GameSettings)MemberwiseClone();

        public static GameSettings Load(GameSettings fallback)
        {
            var json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return fallback.Copy();
            var loaded = fallback.Copy();
            try { JsonUtility.FromJsonOverwrite(json, loaded); }
            catch (ArgumentException) { return fallback.Copy(); }
            loaded.sensitivity = Mathf.Clamp(loaded.sensitivity, .02f, .3f);
            loaded.fieldOfView = Mathf.Clamp(loaded.fieldOfView, 55, 95);
            loaded.headBob = Mathf.Clamp01(loaded.headBob);
            loaded.volume = Mathf.Clamp01(loaded.volume);
            loaded.brightness = Mathf.Clamp(loaded.brightness, -1.5f, 1.5f);
            return loaded;
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public void Apply(FirstPersonPlayer player)
        {
            player.sensitivity = sensitivity;
            player.InvertY = invertY;
            var motion = player.GetComponent<PlayerCameraMotion>();
            if (motion != null) { motion.BaseFov = fieldOfView; motion.intensity = headBob; }
            AudioListener.volume = volume;
            ApplyBrightness(brightness);
        }

        static void ApplyBrightness(float offset)
        {
            exposures.RemoveAll(entry => entry.adjustments == null);
            if (exposures.Count == 0) CollectExposures();
            foreach (var (adjustments, baseline) in exposures) adjustments.postExposure.Override(baseline + offset);
        }

        static void CollectExposures()
        {
            foreach (var volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                if (!volume.isGlobal || !volume.HasInstantiatedProfile() && volume.sharedProfile == null) continue;
                if (volume.profile.TryGet(out ColorAdjustments adjustments) && adjustments.postExposure.overrideState)
                    exposures.Add((adjustments, adjustments.postExposure.value));
            }
            if (exposures.Count > 0) return;
            var go = new GameObject("Brightness volume");
            var added = go.AddComponent<Volume>();
            added.isGlobal = true; added.priority = 1000;
            added.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            exposures.Add((added.profile.Add<ColorAdjustments>(false), 0));
        }
    }
}
