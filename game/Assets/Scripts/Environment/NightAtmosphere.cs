using UnityEngine;
using UnityEngine.Rendering;

namespace BaGio12.Environment
{
    [ExecuteAlways]
    public sealed class NightAtmosphere : MonoBehaviour
    {
        public Color fogColor = new Color(0.06f, 0.08f, 0.14f);
        public float fogDensity = 0.011f;
        public Color skyAmbient = new Color(0.10f, 0.13f, 0.22f);
        public Color equatorAmbient = new Color(0.07f, 0.07f, 0.10f);
        public Color groundAmbient = new Color(0.03f, 0.03f, 0.04f);

        void OnEnable()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = skyAmbient;
            RenderSettings.ambientEquatorColor = equatorAmbient;
            RenderSettings.ambientGroundColor = groundAmbient;
        }
    }
}
