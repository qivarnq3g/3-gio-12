using UnityEngine;
namespace BaGio12.Stealth
{
    public sealed class ObserverAnimator : MonoBehaviour
    {
        public Animator animator;
        [SerializeField] Transform washStation;
        [SerializeField] AnimationClip washPropsClip;
        [SerializeField] GameObject[] heldWashProps;
        static readonly int Speed = Animator.StringToHash("Speed");
        static readonly int Washing = Animator.StringToHash("Washing");
        static readonly int WashState = Animator.StringToHash("Base Layer.Dishwashing");
        Vector3 last;
        float smoothed;
        NightObserver observer;
        Vector3 modelPosition;
        Quaternion modelRotation;
        bool? washing;

        void Awake()
        {
            if (!washStation || !animator) return;
            observer = GetComponent<NightObserver>();
            modelPosition = animator.transform.localPosition;
            modelRotation = animator.transform.localRotation;
        }

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

        void LateUpdate()
        {
            if (!observer || !washStation || !animator || !washPropsClip) return;
            Vector3 offset = transform.position - washStation.position;
            offset.y = 0;
            bool atStation = observer.Mode == ObserverMode.Normal && observer.Attention < 1
                && offset.sqrMagnitude < .25f;
            if (washing != atStation)
            {
                washing = atStation;
                animator.SetBool(Washing, atStation);
                foreach (var prop in heldWashProps)
                    if (prop) prop.SetActive(atStation);
                if (!atStation) RestoreModelTransform();
            }

            if (!atStation) return;
            animator.transform.SetPositionAndRotation(washStation.position, washStation.rotation);
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != WashState && animator.IsInTransition(0))
                state = animator.GetNextAnimatorStateInfo(0);
            if (state.fullPathHash == WashState)
                washPropsClip.SampleAnimation(washStation.gameObject, Mathf.Repeat(state.normalizedTime, 1f) * washPropsClip.length);
        }

        void OnDisable()
        {
            if (washing == true && animator) RestoreModelTransform();
            washing = null;
        }

        void RestoreModelTransform()
        {
            animator.transform.localPosition = modelPosition;
            animator.transform.localRotation = modelRotation;
        }
    }
}
