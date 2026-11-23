using UnityEngine;
using UnityEngine.Rendering;

namespace BaGio12.Environment
{
    public sealed class ProbeLitOverride : MonoBehaviour
    {
        void Awake()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                r.lightmapIndex = -1;
                r.lightProbeUsage = LightProbeUsage.BlendProbes;
            }
        }
    }
}
