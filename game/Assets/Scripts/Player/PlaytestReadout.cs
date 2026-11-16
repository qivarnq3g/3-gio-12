#if UNITY_EDITOR || DEVELOPMENT_BUILD || PLAYTEST_BUILD
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BaGio12.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlaytestReadout : MonoBehaviour
    {
        FirstPersonPlayer player;
        bool visible;
        GUIStyle text;

        public bool IsVisible => visible;

        public void Toggle() { visible = !visible; }

        void Awake() { player = GetComponent<FirstPersonPlayer>(); }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame) Toggle();
        }

        public string GetReadoutText()
        {
            if (player == null) player = GetComponent<FirstPersonPlayer>();
            if (player == null) return string.Empty;

            Vector3 position = transform.position;
            return string.Format(CultureInfo.InvariantCulture,
                "PLAYTEST | F3: hide\nScene: {0}\nX: {1:F2}   Y: {2:F2}   Z: {3:F2}\nYaw: {4:F2} deg   Pitch: {5:F2} deg",
                SceneManager.GetActiveScene().name, position.x, position.y, position.z,
                transform.eulerAngles.y, player.ViewPitch);
        }

        void OnGUI()
        {
            if (!visible || Event.current.type != EventType.Repaint) return;
            string readout = GetReadoutText();
            if (string.IsNullOrEmpty(readout)) return;
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
                    fontSize = 16,
                    richText = false,
                    normal = { textColor = Color.white }
                };
            }

            var originalColor = GUI.color;
            var originalContentColor = GUI.contentColor;
            var originalMatrix = GUI.matrix;
            int originalDepth = GUI.depth;
            try
            {
                // Draw over other HUDs without changing their GUI state or handling input.
                GUI.matrix = Matrix4x4.identity;
                GUI.depth = -1000;
                float width = Mathf.Min(440, Screen.width - 24);
                var panel = new Rect(Screen.width - width - 12, 12, width, 104);
                GUI.color = new Color(0, 0, 0, .8f);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(panel.x + 10, panel.y + 8, panel.width - 20, panel.height - 16), readout, text);
            }
            finally
            {
                GUI.color = originalColor;
                GUI.contentColor = originalContentColor;
                GUI.matrix = originalMatrix;
                GUI.depth = originalDepth;
            }
        }
    }
}
#endif
