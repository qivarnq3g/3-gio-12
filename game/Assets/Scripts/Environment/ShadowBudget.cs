using UnityEngine;
using UnityEngine.SceneManagement;

namespace BaGio12.Environment
{
    public sealed class ShadowBudget : MonoBehaviour
    {
        public const string SceneName = "NightOneRealistic";
        public float fullDistance = 9f;
        public float fadeDistance = 15f;

        Light[] lights;
        LightShadows[] modes;
        float[] strengths;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneName || FindFirstObjectByType<ShadowBudget>() != null) return;
            var host = new GameObject("Shadow budget");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<ShadowBudget>();
        }

        void Start()
        {
            var found = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int n = 0;
            foreach (var l in found) if (Managed(l)) n++;
            lights = new Light[n]; modes = new LightShadows[n]; strengths = new float[n];
            n = 0;
            foreach (var l in found)
            {
                if (!Managed(l)) continue;
                lights[n] = l; modes[n] = l.shadows; strengths[n] = l.shadowStrength; n++;
            }
            LateUpdate();
        }

        static bool Managed(Light l) => l.type != LightType.Directional && l.shadows != LightShadows.None;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || lights == null) return;
            Vector3 eye = cam.transform.position;
            float span = Mathf.Max(.01f, fadeDistance - fullDistance);
            for (int i = 0; i < lights.Length; i++)
            {
                var l = lights[i];
                if (l == null) continue;
                float k = Mathf.Clamp01((fadeDistance - Vector3.Distance(eye, l.transform.position)) / span);
                var want = k > 0 ? modes[i] : LightShadows.None;
                if (l.shadows != want) l.shadows = want;
                float strength = strengths[i] * k;
                if (k > 0 && !Mathf.Approximately(l.shadowStrength, strength)) l.shadowStrength = strength;
            }
        }

        void OnDestroy()
        {
            if (lights == null) return;
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) { lights[i].shadows = modes[i]; lights[i].shadowStrength = strengths[i]; }
        }
    }
}
