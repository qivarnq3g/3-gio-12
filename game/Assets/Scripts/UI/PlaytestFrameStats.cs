#if PLAYTEST_BUILD
using System;
using System.Globalization;
using UnityEngine;

namespace BaGio12.Gameplay
{
    // Opt-in measurement for manual playtests; does not move the player or disable NPCs.
    public sealed class PlaytestFrameStats : MonoBehaviour
    {
        FirstPersonPlayer player;
        int frames, quality = -1, width, height;
        double seconds;
        bool sampling;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-playtestStats") < 0) return;
            new GameObject("Playtest frame stats").AddComponent<PlaytestFrameStats>();
        }
        void Update()
        {
            if (player == null) player = FindFirstObjectByType<FirstPersonPlayer>();
            int current = QualitySettings.GetQualityLevel();
            bool active = player != null && !player.IsPaused && Application.isFocused;
            bool changed = current != quality || width != Screen.width || height != Screen.height;
            if (!active || !sampling || changed)
            {
                frames = 0; seconds = 0; sampling = active;
                quality = current; width = Screen.width; height = Screen.height;
                return;
            }
            frames++; seconds += Time.unscaledDeltaTime;
            if (seconds < 10) return;
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "PLAYTEST_FRAME_STATS fps={0:F1} seconds={1:F2} frames={2} quality={3} resolution={4}x{5}",
                frames / seconds, seconds, frames, QualitySettings.names[quality], width, height));
            frames = 0; seconds = 0;
        }
    }
}
#endif
