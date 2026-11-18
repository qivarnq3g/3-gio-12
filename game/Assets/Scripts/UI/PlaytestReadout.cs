#if UNITY_EDITOR || PLAYTEST_BUILD
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(FirstPersonPlayer))]
    public sealed class PlaytestReadout : MonoBehaviour
    {
        const float RefreshSeconds = .1f;

        FirstPersonPlayer player;
        Canvas canvas;
        TextMeshProUGUI text;
        float nextRefresh;
        bool visible;

        void Awake() { player = GetComponent<FirstPersonPlayer>(); }

        void Start()
        {
            canvas = GameUi.Canvas(transform, "Playtest readout", 90);
            canvas.GetComponent<CanvasGroup>().blocksRaycasts = false;
            var panel = GameUi.Place(GameUi.Box(canvas.transform, "Panel", new Color(0, 0, 0, .6f)), Vector2.one, Vector2.one, new Vector2(-24, -24), new Vector2(640, 52));
            text = GameUi.Text(panel, "Readout", "", 24, GameUi.Ink);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.enableAutoSizing = true;
            text.fontSizeMin = 17;
            text.fontSizeMax = 24;
            GameUi.Place(text.rectTransform, Vector2.one, Vector2.one, new Vector2(-14, 0), new Vector2(612, 52));
            canvas.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!player.IsPaused && Keyboard.current?.f3Key.wasPressedThisFrame == true)
                visible = !visible;

            bool show = visible && !player.IsPaused;
            if (canvas.gameObject.activeSelf != show)
            {
                canvas.gameObject.SetActive(show);
                nextRefresh = 0;
            }

            if (!show || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            text.text = Describe();
        }

        string Describe()
        {
            var p = transform.position;
            float yaw = Mathf.Repeat(transform.eulerAngles.y, 360);
            float pitch = Mathf.DeltaAngle(0, player.ViewPitch);
            return string.Format(CultureInfo.InvariantCulture,
                "X {0:F2}   Y {1:F2}   Z {2:F2}  |  Hướng {3:F0}° · Góc dọc {4:F0}°",
                p.x, p.y, p.z, yaw, pitch);
        }
    }
}
#endif
