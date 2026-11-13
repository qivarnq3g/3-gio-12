using UnityEngine;
using UnityEngine.UI;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(FirstPersonPlayer), typeof(PlayerCameraMotion))]
    public sealed class ControlsHint : MonoBehaviour
    {
        public float delay = 1.2f, lingerAfterLearned = 3f, maxShown = 75f;

        FirstPersonPlayer player;
        PlayerCameraMotion motion;
        CanvasGroup group;
        CanvasGroup[] rows;
        float moved, ran, looked, learnedAt = -1, lastYaw, lastPitch, shownFor;
        bool crouched, finished;

        void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            motion = GetComponent<PlayerCameraMotion>();
        }

        void Start()
        {
            var canvas = GameUi.Canvas(transform, "Controls hint", 20);
            group = canvas.GetComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
            var panel = GameUi.Place(GameUi.Box(canvas.transform, "Panel", new Color(0, 0, 0, .42f)), Vector2.zero, Vector2.zero, new Vector2(56, 56), new Vector2(560, 330));
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(26, 34, 22, 24); layout.spacing = 12; layout.childAlignment = TextAnchor.LowerLeft;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            layout.childControlWidth = layout.childControlHeight = false; layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var heading = GameUi.Text(panel, "Heading", "ĐIỀU KHIỂN", 17, GameUi.Muted);
            heading.characterSpacing = 18; ((RectTransform)heading.transform).sizeDelta = new Vector2(heading.GetPreferredValues().x + 20, 30);
            rows = new[]
            {
                Row(panel, "Đi lại", "W", "A", "S", "D"),
                Row(panel, "Nhìn quanh", "Chuột"),
                Row(panel, "Chạy", "Shift"),
                Row(panel, "Cúi người", "C"),
                Row(panel, "Tương tác", "E"),
                Row(panel, "Tạm dừng", "Esc"),
            };
            lastYaw = transform.eulerAngles.y; lastPitch = player.ViewPitch;
        }

        static CanvasGroup Row(RectTransform panel, string action, params string[] keys)
        {
            var row = GameUi.ControlRow(panel, action, 34, keys);
            return row.gameObject.AddComponent<CanvasGroup>();
        }

        void Update()
        {
            if (group == null || finished) return;
            bool active = !player.IsPaused && player.ActivePhone == 0 && !player.ExternalUI && !player.showTestHud;
            float dt = Time.unscaledDeltaTime;
            if (active)
            {
                shownFor += dt;
                if (motion.Speed > 1) moved += dt;
                if (player.IsRunning) ran += dt;
                crouched |= player.IsCrouched;
                float yaw = transform.eulerAngles.y;
                looked += Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) + Mathf.Abs(player.ViewPitch - lastPitch);
                lastYaw = yaw; lastPitch = player.ViewPitch;
            }
            Fade(rows[0], moved > 1.2f); Fade(rows[1], looked > 120); Fade(rows[2], ran > 1f); Fade(rows[3], crouched);
            bool learned = moved > 1.2f && looked > 120 && ran > 1f && crouched;
            if (learned && learnedAt < 0) learnedAt = shownFor;
            bool expired = (learnedAt >= 0 && shownFor - learnedAt > lingerAfterLearned) || shownFor > maxShown;
            float target = active && shownFor > delay && !expired ? 1 : 0;
            group.alpha = Mathf.MoveTowards(group.alpha, target, dt * (target > 0 ? 2.5f : 1.2f));
            if (expired && group.alpha <= 0) { finished = true; Destroy(group.gameObject); }
        }

        static void Fade(CanvasGroup row, bool done)
        {
            row.alpha = Mathf.MoveTowards(row.alpha, done ? .28f : 1, Time.unscaledDeltaTime * 2);
        }
    }
}
