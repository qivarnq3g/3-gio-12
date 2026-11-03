using UnityEngine;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(FirstPersonPlayer), typeof(CharacterController))]
    public sealed class PlayerCameraMotion : MonoBehaviour
    {
        public float walkStride = 2f, runStride = 2.5f, crouchStride = 1.2f;
        public float walkBob = .045f, runBob = .075f, crouchBob = .025f;
        public float sway = .55f, strafeRoll = 2.2f, stepRoll = .45f;
        public float runFovBoost = 7f, eyeSpeed = 9f;
        public float intensity = 1f;

        public event System.Action<float> Step;
        public float Speed { get; private set; }
        public float BaseFov
        {
            get => baseFov > 0 ? baseFov : player.eyes.fieldOfView;
            set { baseFov = fov = value; player.eyes.fieldOfView = value; }
        }

        FirstPersonPlayer player;
        CharacterController body;
        float phase, weight, roll, eye, dip, dipVelocity, baseFov, fov, airSpeed;
        bool wasGrounded = true;

        void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            body = GetComponent<CharacterController>();
            eye = PlayerGeometry.StandingEye;
        }

        void Start()
        {
            if (baseFov <= 0) baseFov = fov = player.eyes.fieldOfView;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            var velocity = body.velocity;
            var local = transform.InverseTransformDirection(new Vector3(velocity.x, 0, velocity.z));
            Speed = local.magnitude;
            bool grounded = body.isGrounded;

            if (grounded && !wasGrounded) dipVelocity -= Mathf.Clamp(airSpeed * .05f, 0, .6f);
            airSpeed = grounded ? 0 : Mathf.Max(airSpeed, -velocity.y);
            wasGrounded = grounded;
            dipVelocity += (-dip * 160 - dipVelocity * 18) * dt;
            dip += dipVelocity * dt;

            bool moving = grounded && Speed > .25f;
            weight = Mathf.MoveTowards(weight, moving ? 1 : 0, dt * (moving ? 4 : 2.5f));
            float stride = player.IsCrouched ? crouchStride : player.IsRunning ? runStride : walkStride;
            float amplitude = player.IsCrouched ? crouchBob : player.IsRunning ? runBob : walkBob;
            if (moving)
            {
                float before = phase;
                phase += Mathf.PI * 2 * Speed / stride * dt;
                if (Mathf.Floor(before / Mathf.PI - .5f) != Mathf.Floor(phase / Mathf.PI - .5f)) Step?.Invoke(player.IsCrouched ? .4f : player.IsRunning ? 1f : .7f);
                if (phase > Mathf.PI * 200) phase -= Mathf.PI * 200;
            }
            else phase = Mathf.Lerp(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, dt * 3);

            float vertical = -Mathf.Abs(Mathf.Sin(phase)) * amplitude * weight * intensity;
            float lateral = Mathf.Cos(phase) * amplitude * sway * weight * intensity;
            float targetRoll = (-local.x / Mathf.Max(player.runSpeed, .1f) * strafeRoll + Mathf.Cos(phase) * stepRoll * weight) * intensity;
            roll = Mathf.Lerp(roll, targetRoll, dt * 8);

            float targetEye = player.IsCrouched ? PlayerGeometry.CrouchedEye : PlayerGeometry.StandingEye;
            eye = Mathf.Lerp(eye, targetEye, dt * eyeSpeed);

            fov = Mathf.Lerp(fov, baseFov + (player.IsRunning ? runFovBoost * Mathf.Max(intensity, .3f) : 0), dt * 5);
            player.eyes.fieldOfView = fov;

            var cam = player.eyes.transform;
            cam.localPosition = new Vector3(lateral, eye + vertical + dip * intensity, 0);
            cam.localRotation = Quaternion.Euler(player.ViewPitch + vertical * 12, 0, roll);
        }
    }
}
