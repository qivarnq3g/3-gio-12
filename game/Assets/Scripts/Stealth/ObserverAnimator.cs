using UnityEngine;
namespace BaGio12.Stealth
{
    public sealed class ObserverAnimator : MonoBehaviour
    {
        public Animator animator;
        static readonly int Speed = Animator.StringToHash("Speed");
        Vector3 last;
        float smoothed;

        void OnEnable() { last = transform.position; smoothed = 0; }

        void Update()
        {
            if (animator == null || Time.deltaTime <= 0) return;
            var p = transform.position;
            float speed = new Vector2(p.x - last.x, p.z - last.z).magnitude / Time.deltaTime;
            last = p;
            if (speed > 12) speed = 0;
            smoothed = Mathf.Lerp(smoothed, speed, 1 - Mathf.Exp(-Time.deltaTime * 8));
            animator.SetFloat(Speed, smoothed);
        }
    }
}
