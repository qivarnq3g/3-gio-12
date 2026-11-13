#if UNITY_EDITOR || PLAYTEST_BUILD
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(FirstPersonPlayer))]
    public sealed class PlaytestReadout : MonoBehaviour
    {
        const float RefreshSeconds = .1f, ProbeDistance = 50;

        FirstPersonPlayer player;
        Canvas canvas;
        TextMeshProUGUI text;
        float nextRefresh, smoothedFrame = 1 / 60f;

        void Awake() { player = GetComponent<FirstPersonPlayer>(); }

        void Start()
        {
            canvas = GameUi.Canvas(transform, "Playtest readout", 90);
            canvas.GetComponent<CanvasGroup>().blocksRaycasts = false;
            var panel = GameUi.Place(GameUi.Box(canvas.transform, "Panel", new Color(0, 0, 0, .6f)), Vector2.up, Vector2.up, new Vector2(24, -24), new Vector2(760, 206));
            text = GameUi.Text(panel, "Readout", "", 24, GameUi.Ink);
            text.alignment = TextAlignmentOptions.TopLeft;
            GameUi.Place((RectTransform)text.transform, Vector2.up, Vector2.up, new Vector2(16, -12), new Vector2(728, 182));
            canvas.gameObject.SetActive(false);
        }

        void Update()
        {
            smoothedFrame = Mathf.Lerp(smoothedFrame, Time.unscaledDeltaTime, .05f);
            var keys = Keyboard.current;
            if (keys != null && keys.f3Key.wasPressedThisFrame) canvas.gameObject.SetActive(!canvas.gameObject.activeSelf);
            if (!canvas.gameObject.activeSelf || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            text.text = Describe();
        }

        string Describe()
        {
            var p = transform.position;
            float yaw = Mathf.Repeat(transform.eulerAngles.y, 360);
            float pitch = Mathf.DeltaAngle(0, player.ViewPitch);
            var eye = player.eyes != null ? player.eyes.transform : transform;
            return string.Format(CultureInfo.InvariantCulture,
                "X {0:F2}   Y {1:F2}   Z {2:F2}\nHướng {3:F0}°   Ngước {4:F0}°\nDưới chân: {5}\nĐang nhìn: {6}\n{7}   {8:F0} FPS   {9}\nV: bay   F5 hẻm, F6 giữa hẻm, F7 cổng, F8 nhà xe, F9 phòng 7",
                p.x, p.y, p.z, yaw, pitch,
                Hit(p + Vector3.up * .5f, Vector3.down),
                Hit(eye.position, eye.forward),
                SceneManager.GetActiveScene().name, 1 / Mathf.Max(smoothedFrame, .0001f), SystemInfo.graphicsDeviceType);
        }

        string Hit(Vector3 origin, Vector3 direction)
        {
            foreach (var hit in Sorted(Physics.RaycastAll(origin, direction, ProbeDistance, ~0, QueryTriggerInteraction.Ignore)))
                if (!hit.collider.transform.IsChildOf(transform)) return hit.collider.name;
            return "(trống)";
        }

        static RaycastHit[] Sorted(RaycastHit[] hits)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            return hits;
        }
    }
}
#endif
