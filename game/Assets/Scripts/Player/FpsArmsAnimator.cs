using BaGio12.Story;
using UnityEngine;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(Animator))]
    public sealed class FpsArmsAnimator : MonoBehaviour
    {
        [SerializeField] Renderer raincoatLeftSleeve;
        [SerializeField] Renderer raincoatRightSleeve;
        [SerializeField] Renderer raincoatLeftGlove;
        [SerializeField] Renderer raincoatRightGlove;
        [SerializeField] Renderer bareLeftArm;
        [SerializeField] Renderer bareRightArm;
        [SerializeField] Renderer[] studentBandages;

        int outfit = -2;

        static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
        static readonly int Walk = Animator.StringToHash("Base Layer.Walk");
        static readonly int Run = Animator.StringToHash("Base Layer.Run");
        static readonly int HoldPhone = Animator.StringToHash("Base Layer.HoldPhone");
        static readonly int PickUp = Animator.StringToHash("Base Layer.PickUp");

        public FirstPersonPlayer player;
        public Camera armsCamera;
        public Transform animatedCamera;
        Animator animator;
        MeshRenderer phone;
        internal NightOneDirector Story { get; set; }
        float pickupDuration;
        float pickupUntil;
        int state;

        void Awake()
        {
            animator = GetComponent<Animator>();
            phone = System.Array.Find(GetComponentsInChildren<MeshRenderer>(true), r => r.name == "prop_ngan_phone");
            if (phone != null) phone.forceRenderingOff = true;
            if (player == null) player = GetComponentInParent<FirstPersonPlayer>();
            pickupDuration = System.Array.Find(animator.runtimeAnimatorController.animationClips, clip => clip.name == "PickUp").length;
        }

        void OnEnable() { if (player != null) player.PickedUp += StartPickup; }
        void OnDisable() { if (player != null) player.PickedUp -= StartPickup; }

        void StartPickup()
        {
            pickupUntil = Time.time + pickupDuration;
            animator.Play(PickUp, 0, 0);
            state = PickUp;
            if (phone != null) phone.forceRenderingOff = true;
        }

        void Update()
        {
            UpdateOutfit();
            if (player == null || Time.time < pickupUntil) return;
            bool holdingPhone = player.ActivePhone != 0 || (Story != null && Story.CarryingProloguePhone);
            int next = holdingPhone ? HoldPhone : player.IsRunning ? Run : player.IsMoving ? Walk : Idle;
            if (next == state) return;
            if ((state == Walk || state == Run) && (next == Walk || next == Run))
            {
                var gait = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
                animator.CrossFade(next, .10f, 0, Mathf.Repeat(gait.normalizedTime, 1f));
            }
            else
            {
                animator.CrossFadeInFixedTime(next, .10f);
            }
            state = next;
            if (phone != null) phone.forceRenderingOff = state != HoldPhone;
        }

        void LateUpdate()
        {
            if (armsCamera != null && animatedCamera != null)
                armsCamera.transform.SetPositionAndRotation(animatedCamera.position, animatedCamera.rotation);
        }

        void UpdateOutfit()
        {
            if (raincoatRightSleeve == null) return;

            // The first capture tears the left sleeve. The second leaves the left glove behind.
            int damage = Story != null && Story.State.stage == NightStage.Prologue
                ? Story.State.prologue_beaten : -1;
            if (damage == outfit) return;
            outfit = damage;

            bool raincoat = damage >= 0;
            raincoatLeftSleeve.forceRenderingOff = damage != 0;
            raincoatRightSleeve.forceRenderingOff = !raincoat;
            raincoatLeftGlove.forceRenderingOff = !raincoat || damage >= 2;
            raincoatRightGlove.forceRenderingOff = !raincoat;
            bareLeftArm.forceRenderingOff = raincoat && damage < 2;
            bareRightArm.forceRenderingOff = raincoat;
            foreach (var bandage in studentBandages)
                bandage.forceRenderingOff = raincoat;
        }
    }
}
