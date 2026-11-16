using UnityEngine;

namespace BaGio12.Gameplay
{
    public static class MenuSounds
    {
        const int Rate = 44100;
        static AudioClip relay, tick, hum;

        public static AudioClip Relay => relay != null ? relay : relay = Click("Sensor relay", .16f, 38, 1900, 115, .95f);
        public static AudioClip Tick => tick != null ? tick : tick = Click("Menu tick", .045f, 110, 3200, 0, .6f);

        public static AudioClip Hum
        {
            get
            {
                if (hum != null) return hum;
                int length = Rate * 2;
                var data = new float[length];
                var random = new System.Random(12);
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)Rate;
                    float phase = Mathf.PI * 2 * 100 * t;
                    float tone = Mathf.Sin(phase) * .5f + Mathf.Sin(phase * 2) * .22f + Mathf.Sin(phase * 3) * .1f;
                    float buzz = Mathf.Sign(Mathf.Sin(phase)) * .04f + (float)(random.NextDouble() * 2 - 1) * .015f;
                    data[i] = (tone + buzz) * .7f;
                }
                hum = AudioClip.Create("Tube hum", length, 1, Rate, false);
                hum.SetData(data, 0);
                return hum;
            }
        }

        static AudioClip Click(string name, float seconds, float decay, float ping, float thump, float gain)
        {
            int length = Mathf.CeilToInt(Rate * seconds);
            var data = new float[length];
            var random = new System.Random(name.Length * 31);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                float envelope = Mathf.Exp(-t * decay);
                float noise = (float)(random.NextDouble() * 2 - 1) * Mathf.Exp(-t * decay * 3);
                float body = Mathf.Sin(Mathf.PI * 2 * ping * t) * .35f + (thump > 0 ? Mathf.Sin(Mathf.PI * 2 * thump * t) * .6f : 0);
                data[i] = (noise * .7f + body) * envelope;
            }
            float peak = 0;
            foreach (var sample in data) peak = Mathf.Max(peak, Mathf.Abs(sample));
            if (peak > 0) for (int i = 0; i < length; i++) data[i] *= gain / peak;
            var clip = AudioClip.Create(name, length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
