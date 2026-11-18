using UnityEngine;

namespace BaGio12.Gameplay
{
    [RequireComponent(typeof(PlayerCameraMotion))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        public string clipFolder = "Audio/Footsteps";
        public float volume = 1f;

        AudioClip[] clips;
        AudioSource source;
        PlayerCameraMotion motion;
        int last = -1;

        void Awake()
        {
            clips = Resources.LoadAll<AudioClip>(clipFolder);
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.priority = 64;
            motion = GetComponent<PlayerCameraMotion>();
        }

        void OnEnable() { motion.Step += Play; }
        void OnDisable() { motion.Step -= Play; }

        void Play(float intensity)
        {
            if (clips.Length == 0) return;
            int pick = Random.Range(0, clips.Length);
            if (clips.Length > 1 && pick == last) pick = (pick + 1) % clips.Length;
            last = pick;
            source.pitch = Random.Range(.92f, 1.06f) * (intensity > .9f ? 1.05f : 1f);
            source.PlayOneShot(clips[pick], volume * intensity * Random.Range(.85f, 1f));
        }
    }
}
