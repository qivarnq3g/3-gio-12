using UnityEngine;

namespace BaGio12.Gameplay
{
    public static class MenuSounds
    {
        const int Rate = 44100;
        static AudioClip relay, tick, hum, open, close, confirm, deny, message, note;

        public static AudioClip Relay => relay != null ? relay : relay = Click("Sensor relay", .16f, 38, 1900, 115, .95f);
        public static AudioClip Tick => tick != null ? tick : tick = Click("Menu tick", .045f, 110, 3200, 0, .6f);
        public static AudioClip Open => open != null ? open : open = Click("Panel open", .24f, 20, 620, 88, .55f);
        public static AudioClip Close => close != null ? close : close = Click("Panel close", .13f, 42, 470, 70, .42f);
        public static AudioClip Confirm => confirm != null ? confirm : confirm = Click("Choice confirm", .2f, 28, 980, 120, .6f);
        public static AudioClip Deny => deny != null ? deny : deny = Click("Choice deny", .11f, 58, 210, 135, .5f);
        public static AudioClip Message => message != null ? message : message = Click("Message in", .2f, 26, 1260, 0, .42f);
        public static AudioClip Note => note != null ? note : note = Click("Objective note", .32f, 13, 740, 0, .3f);

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
