using UnityEngine;

namespace BaGio12.Story
{
    public static class NightSfx
    {
        const int Rate = 22050;
        static AudioClip thump, rip, grab, ring, scuffle, heartbeat;
        public static AudioClip Thump => thump != null ? thump : thump = Make("Thump", .28f, Thumped);
        public static AudioClip Rip => rip != null ? rip : rip = Make("Raincoat rip", .5f, Ripped);
        public static AudioClip Grab => grab != null ? grab : grab = Make("Grab", .35f, Grabbed);
        public static AudioClip Ring => ring != null ? ring : ring = Make("Ear ring", 4.5f, Ringing);
        public static AudioClip Scuffle => scuffle != null ? scuffle : scuffle = Make("Scuffle", 1.2f, Scuffled);
        public static AudioClip Heartbeat => heartbeat != null ? heartbeat : heartbeat = Make("Heartbeat", .9f, Beat);

        static AudioClip Make(string name, float seconds, System.Func<int, int, System.Random, float> sample)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            var random = new System.Random(name.GetHashCode());
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(sample(i, count, random), -1, 1);
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
        static float Noise(System.Random random) => (float)random.NextDouble() * 2 - 1;
        static float Thumped(int i, int n, System.Random random)
        {
            float t = i / (float)Rate;
            float body = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(95, 48, t / .28f) * t) * Mathf.Exp(-t * 16);
            return body * .9f + Noise(random) * Mathf.Exp(-t * 60) * .35f;
        }
        static float lowPass;
        static float Ripped(int i, int n, System.Random random)
        {
            float t = i / (float)Rate;
            lowPass = Mathf.Lerp(lowPass, Noise(random), .35f + .4f * t);
            float crackle = random.NextDouble() < .02 ? Noise(random) : 0;
            return (lowPass * .55f + crackle * .6f) * Mathf.Sin(Mathf.PI * t / .5f);
        }
        static float Grabbed(int i, int n, System.Random random)
        {
            float t = i / (float)Rate;
            lowPass = Mathf.Lerp(lowPass, Noise(random), .2f);
            return lowPass * Mathf.Exp(-t * 9) * .8f + Mathf.Sin(2 * Mathf.PI * 70 * t) * Mathf.Exp(-t * 22) * .6f;
        }
        static float Ringing(int i, int n, System.Random random)
        {
            float t = i / (float)Rate;
            float envelope = Mathf.Min(1, t * 8) * Mathf.Exp(-t * .75f);
            return (Mathf.Sin(2 * Mathf.PI * 3150 * t) + .35f * Mathf.Sin(2 * Mathf.PI * 3190 * t)) * envelope * .16f;
        }
        static float Scuffled(int i, int n, System.Random random)
        {
            float t = i / (float)Rate;
            lowPass = Mathf.Lerp(lowPass, Noise(random), .08f);
            float steps = Mathf.Pow(Mathf.Abs(Mathf.Sin(Mathf.PI * t * 5.5f)), 6);
            return lowPass * (.25f + steps) * .7f;
        }
        static float Beat(int i, int n, System.Random random)
        {
            float t = i / (float)Rate;
            float first = Mathf.Sin(2 * Mathf.PI * 52 * t) * Mathf.Exp(-t * 28);
            float s = t - .22f;
            float second = s > 0 ? Mathf.Sin(2 * Mathf.PI * 46 * s) * Mathf.Exp(-s * 30) * .7f : 0;
            return (first + second) * .9f;
        }
    }
}
