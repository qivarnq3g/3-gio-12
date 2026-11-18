using UnityEngine;
namespace BaGio12.Story
{
    public sealed class HeldPhoneView : MonoBehaviour
    {
        public NightOneDirector director;
        public GameObject view;
        public Vector2 bob = new Vector2(.004f, .006f);
        Vector3 rest, last;
        float phase, weight;

        void Start()
        {
            if (view != null) rest = view.transform.localPosition;
            if (director != null) last = director.player.transform.position;
        }

        void LateUpdate()
        {
            if (director == null || view == null) return;
            bool show = director.State.stage == NightStage.Prologue && !director.State.phonePlaced;
            if (view.activeSelf != show) view.SetActive(show);
            var p = director.player.transform.position;
            float speed = Time.deltaTime > 0 ? new Vector2(p.x - last.x, p.z - last.z).magnitude / Time.deltaTime : 0;
            last = p;
            if (!show) return;
            weight = Mathf.MoveTowards(weight, Mathf.Clamp01(speed / 2.5f), Time.deltaTime * 4);
            phase += Time.deltaTime * (3 + speed * 2.2f);
            view.transform.localPosition = rest + new Vector3(Mathf.Sin(phase) * bob.x, -Mathf.Abs(Mathf.Cos(phase)) * bob.y, 0) * weight;
        }
    }
}
