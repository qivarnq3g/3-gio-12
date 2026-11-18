#if PLAYTEST_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BaGio12.Story;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace BaGio12.Gameplay
{
    public sealed class PerfRoute : MonoBehaviour
    {
        const int Capacity = 30000;
        static readonly string[] Markers =
        {
            "Main Thread", "GC Allocated In Frame", "GC.Collect", "Draw Calls Count", "SetPass Calls Count", "Shadow Casters Count",
            "CreateGpuProgram", "CreateGraphicsPipelineImpl", "Texture.WaitForAsyncUpload", "Gfx.UploadTexture", "Texture.AwakeFromLoad",
            "Gfx.WaitForPresentOnGfxThread", "Gfx.WaitForGfxCommandsFromMainThread", "Gfx.PresentFrame",
            "BehaviourUpdate", "LateBehaviourUpdate", "FixedBehaviourUpdate", "Physics.Simulate", "Physics.SyncTransforms", "NavMeshManager",
            "Camera.Render", "Culling", "Shadows.PrepareShadowmap", "Shadows.ExecuteDrawShadows", "Shadows.Draw", "Shadows.ExtractCasters",
            "MainLightShadow", "AdditionalLightsShadow", "DrawOpaqueObjects", "UGUI.Rendering.UpdateBatches", "GUI.Repaint",
            "System Used Memory", "Total Used Memory", "Gfx Used Memory", "Texture Memory",
            "PlayerLoop", "Semaphore.WaitForSignal", "Gfx.WaitForGfxCommandWriteSpace", "RenderLoop", "SubmitAndPresentJob", "Gfx.SetRenderTarget",
            "EarlyUpdate.PresentBeforeUpdate", "PostLateUpdate.FinishFrameRendering", "PostLateUpdate.PresentAfterDraw", "Update.ScriptRunBehaviourUpdate",
            "PreLateUpdate.ScriptRunBehaviourLateUpdate", "PostLateUpdate.UpdateAllRenderers", "PostLateUpdate.PlayerUpdateCanvases", "Gfx.ProcessCommands",
            "RenderPipelineManager.DoRenderLoop_Internal()", "Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera", "Gfx.CreateRenderTexture", "RenderTexture.Create",
        };
        readonly List<int> unresolved = new List<int>();

        struct Leg { public string name; public Vector3 from, to; public float yawFrom, yawTo, pitch, seconds; }

        string outputPath, variant;
        FirstPersonPlayer player;
        readonly List<ProfilerRecorder> recorders = new List<ProfilerRecorder>();
        readonly List<string> recorderNames = new List<string>();
        float[] dt, gpu, cpu, px, pz, yaw;
        float[,] values;
        int[] shadowed;
        Light[] shadowLights;
        readonly Plane[] planes = new Plane[6];
        int[] leg;
        readonly List<string> legNames = new List<string>();
        int frames, currentLeg = -1;
        readonly FrameTiming[] timing = new FrameTiming[1];
        readonly StringBuilder flicker = new StringBuilder(), lightReport = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-perfRoute");
            if (i < 0 || i + 1 >= args.Length) return;
            var host = new GameObject("PerfRoute").AddComponent<PerfRoute>();
            host.outputPath = args[i + 1];
            int v = Array.IndexOf(args, "-perfVariant");
            host.variant = v >= 0 && v + 1 < args.Length ? args[v + 1] : "";
            DontDestroyOnLoad(host.gameObject);
        }

        void Start()
        {
            Application.runInBackground = true;
            dt = new float[Capacity]; gpu = new float[Capacity]; cpu = new float[Capacity];
            px = new float[Capacity]; pz = new float[Capacity]; yaw = new float[Capacity]; leg = new int[Capacity]; shadowed = new int[Capacity];
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var available = new Dictionary<string, ProfilerRecorderHandle>();
            var all = new StringBuilder();
            foreach (var h in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(h);
                all.Append(d.Category.Name).Append('\t').Append(d.Name).Append('\t').Append(d.UnitType).Append('\n');
                if (!available.ContainsKey(d.Name)) available[d.Name] = h;
            }
            File.WriteAllText(outputPath + ".markers.txt", all.ToString());
            foreach (var name in Markers)
            {
                if (!available.ContainsKey(name)) unresolved.Add(recorders.Count);
                var category = available.TryGetValue(name, out var h) ? ProfilerRecorderHandle.GetDescription(h).Category : ProfilerCategory.Render;
                recorders.Add(ProfilerRecorder.StartNew(category, name, 1));
                recorderNames.Add(name);
            }
            shadowLights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (variant.Contains("shadowmask")) QualitySettings.shadowmaskMode = ShadowmaskMode.Shadowmask;
            if (variant.Contains("vsync")) QualitySettings.vSyncCount = 1;
            if (variant.Contains("cap60")) Application.targetFrameRate = 60;
            if (variant.Contains("half")) Screen.SetResolution(960, 540, FullScreenMode.Windowed);
            foreach (var l in shadowLights)
            {
                bool lamp = l.type == LightType.Spot && l.name.StartsWith("StreetLamp");
                if ((variant == "nolampshadow" || variant == "noshadow") && lamp) l.shadows = LightShadows.None;
                if ((variant == "nomoonshadow" || variant == "noshadow") && l.type == LightType.Directional) l.shadows = LightShadows.None;
                if (variant == "noallshadow") l.shadows = LightShadows.None;
                if (variant == "noaltarshadow" && l.name.StartsWith("Altar")) l.shadows = LightShadows.None;
                if (variant == "nolampaltar" && (lamp || l.name.StartsWith("Altar"))) l.shadows = LightShadows.None;
                if (variant.Contains("farlamps") && lamp && Mathf.Abs(l.transform.position.x) > 45) l.shadows = LightShadows.None;
                if (variant.Contains("noaltar") && l.name.StartsWith("Altar")) l.shadows = LightShadows.None;
                if (variant.Contains("nomixed") && l.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed) l.shadows = LightShadows.None;
                if (variant.Contains("nearlamps") && lamp && Mathf.Abs(l.transform.position.x) > 12) l.shadows = LightShadows.None;
                if (variant.Contains("nomoon") && l.type == LightType.Directional) l.shadows = LightShadows.None;
                if (variant.Contains("hard") && l.shadows == LightShadows.Soft) l.shadows = LightShadows.Hard;
                var extra = l.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
                if (extra != null && variant.Contains("softmed")) extra.softShadowQuality = UnityEngine.Rendering.Universal.SoftShadowQuality.Medium;
                if (extra != null && variant.Contains("softlow")) extra.softShadowQuality = UnityEngine.Rendering.Universal.SoftShadowQuality.Low;
                if (l.shadows != LightShadows.None)
                    lightReport.AppendFormat(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}\tbakeType={3}\tisBaked={4}\toutputType={5}\trange={6}\n", l.name, l.type, l.shadows,
                        "-", l.bakingOutput.isBaked, l.bakingOutput.lightmapBakeType, l.transform.position);
            }
            float smallLimit = variant.Contains("small10") ? 1f : variant.Contains("small05") ? .5f : variant.Contains("small03") ? .3f : 0f;
            var census = new Dictionary<string, (int count, float size, int subs, int clip)>();
            int casters = 0, culled = 0, totalSubs = 0;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || r.shadowCastingMode == ShadowCastingMode.Off || r is ParticleSystemRenderer) continue;
                casters++;
                float size = r.bounds.size.magnitude;
                string key = System.Text.RegularExpressions.Regex.Replace(r.name, @"[\s_(]*\d+\)?$", "");
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) key += " | " + mf.sharedMesh.name;
                census.TryGetValue(key, out var e);
                int subs = r.sharedMaterials.Length, clip = r.sharedMaterials.Count(m => m != null && m.IsKeywordEnabled("_ALPHATEST_ON"));
                totalSubs += subs;
                census[key] = (e.count + 1, e.size + size, e.subs + subs, e.clip + clip);
                if (size < smallLimit && r.shadowCastingMode != ShadowCastingMode.ShadowsOnly) { r.shadowCastingMode = ShadowCastingMode.Off; culled++; }
            }
            var cs = new StringBuilder();
            cs.AppendFormat(CultureInfo.InvariantCulture, "casters\t{0}\tculled\t{1}\tlimit\t{2}\tsubmeshes\t{3}\n", casters, culled, smallLimit, totalSubs);
            foreach (var kv in census.OrderByDescending(k => k.Value.subs).Take(60))
                cs.AppendFormat(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}\t{3}\t{4:F2}\n", kv.Value.subs, kv.Value.clip, kv.Value.count, kv.Key, kv.Value.size / kv.Value.count);
            File.WriteAllText(outputPath + ".casters.tsv", cs.ToString());
            values = new float[Capacity, recorders.Count];
            StartCoroutine(Run());
        }

        static Vector3 Ground(Vector3 p)
        {
            return Physics.Raycast(new Vector3(p.x, 2.4f, p.z), Vector3.down, out var hit, 6, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore)
                ? hit.point + Vector3.up * .05f : new Vector3(p.x, .05f, p.z);
        }

        static Leg Hold(string n, Vector3 p, float y, float s) => new Leg { name = n, from = p, to = p, yawFrom = y, yawTo = y, seconds = s };
        static Leg Turn(string n, Vector3 p, float y, float s) => new Leg { name = n, from = p, to = p, yawFrom = y, yawTo = y + 360, seconds = s };
        static Leg Walk(string n, Vector3 a, Vector3 b, float y, float speed) => new Leg { name = n, from = a, to = b, yawFrom = y, yawTo = y, seconds = Vector3.Distance(a, b) / speed };

        IEnumerator Run()
        {
            for (int i = 0; i < 30 && player == null; i++) { player = FindFirstObjectByType<FirstPersonPlayer>(); yield return null; }
            var director = FindFirstObjectByType<NightOneDirector>();
            if (director != null) foreach (var o in director.observers) if (o != null) o.enabled = false;
            var budget = FindFirstObjectByType<BaGio12.Environment.ShadowBudget>();
            if (budget != null && variant.Contains("nobudget")) Destroy(budget);
            if (budget != null && variant.Contains("b12")) { budget.fullDistance = 12; budget.fadeDistance = 18; }
            if (budget != null && variant.Contains("b6")) { budget.fullDistance = 6; budget.fadeDistance = 11; }
            File.WriteAllText(outputPath + ".budget.txt", budget == null ? "none" : budget.fullDistance + " " + budget.fadeDistance + (variant.Contains("nobudget") ? " destroyed" : ""));
            Vector3 mouth = new Vector3(0, 0, -1), shop = new Vector3(.4f, 0, -3.5f), mid = new Vector3(.4f, 0, -30), gate = new Vector3(-.6f, 0, -38.5f), garage = new Vector3(.3f, 0, -47.5f), street = new Vector3(0, 0, 2);
            var legs = new List<Leg>
            {
                Hold("warmup", mouth, 180, 4),
                Turn("turn-mouth", mouth, 180, 2),
                Walk("walk-to-shop", mouth, shop, 180, 2.5f),
                Turn("turn-shop", shop, 180, 2),
                Hold("look-shop", shop, 90, 2),
                Walk("walk-to-mid", shop, mid, 180, 2.5f),
                Turn("turn-mid", mid, 180, 2),
                Walk("walk-to-gate", mid, gate, 180, 2.5f),
                Turn("turn-gate", gate, 150, 2),
                Walk("walk-to-garage", gate, garage, 180, 2.5f),
                Turn("turn-garage", garage, 180, 2),
                Hold("still-garage", garage, 180, 4.5f),
                Walk("walk-back", garage, mouth, 0, 4f),
                Walk("walk-street", mouth, street, 0, 2.5f),
                Turn("turn-street", street, 0, 2),
                Hold("still-mouth", mouth, 180, 4.5f),
                Turn("turn-mouth-again", mouth, 180, 2),
            };
            foreach (var l in legs)
            {
                legNames.Add(l.name); currentLeg = legNames.Count - 1;
                if (l.name.StartsWith("still")) StartCoroutine(Flicker(l.name, 24));
                for (float t = 0; t < l.seconds; t += Time.unscaledDeltaTime)
                {
                    float k = Mathf.Clamp01(t / l.seconds);
                    player.Teleport(Ground(Vector3.Lerp(l.from, l.to, k)), Mathf.Lerp(l.yawFrom, l.yawTo, k));
                    yield return null;
                }
                if (l.from == l.to && l.yawFrom == l.yawTo) ScreenCapture.CaptureScreenshot(outputPath + "." + l.name + ".view.png");
            }
            currentLeg = -1;
            Write();
            Application.Quit(0);
        }

        IEnumerator Flicker(string name, int count)
        {
            yield return new WaitForEndOfFrame();
            float[] previous = null;
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForEndOfFrame();
                var shot = ScreenCapture.CaptureScreenshotAsTexture();
                var pixels = shot.GetPixels32();
                int w = shot.width, h = shot.height, bw = w / 32, bh = h / 32;
                var blocks = new float[bw * bh];
                for (int y = 0; y < bh * 32; y++)
                    for (int x = 0; x < bw * 32; x++)
                    {
                        var c = pixels[y * w + x];
                        blocks[(y / 32) * bw + x / 32] += (c.r + c.g + c.b) / 3f / 1024f;
                    }
                if (previous != null)
                {
                    float worst = 0; int changed = 0;
                    for (int k = 0; k < blocks.Length; k++)
                    {
                        float d = Math.Abs(blocks[k] - previous[k]);
                        worst = Math.Max(worst, d); if (d > 4) changed++;
                    }
                    flicker.AppendFormat(CultureInfo.InvariantCulture, "{0}\t{1}\t{2:F2}\t{3}\n", name, i, worst, changed);
                    if (changed > 0) File.WriteAllBytes(outputPath + "." + name + "." + i + ".png", shot.EncodeToPNG());
                }
                previous = blocks;
                Destroy(shot);
            }
        }

        void LateUpdate()
        {
            if (currentLeg < 0 || frames >= Capacity || player == null) return;
            FrameTimingManager.CaptureFrameTimings();
            bool timed = FrameTimingManager.GetLatestTimings(1, timing) > 0;
            int f = frames++;
            dt[f] = Time.unscaledDeltaTime * 1000;
            cpu[f] = timed ? (float)timing[0].cpuFrameTime : -1;
            gpu[f] = timed ? (float)timing[0].gpuFrameTime : -1;
            px[f] = player.transform.position.x; pz[f] = player.transform.position.z; yaw[f] = player.transform.eulerAngles.y;
            leg[f] = currentLeg;
            shadowed[f] = VisibleShadowedSpots();
            for (int r = 0; r < recorders.Count; r++)
            {
                var rec = recorders[r];
                float v = rec.Valid && rec.Count > 0 ? rec.LastValue : -1;
                values[f, r] = rec.UnitType == ProfilerMarkerDataUnit.TimeNanoseconds && v >= 0 ? v / 1e6f : v;
            }
        }

        int VisibleShadowedSpots()
        {
            var cam = player.eyes;
            if (cam == null) return -1;
            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            int n = 0;
            foreach (var l in shadowLights)
            {
                if (l == null || !l.isActiveAndEnabled || l.shadows == LightShadows.None || l.type == LightType.Directional) continue;
                if (l.bakingOutput.isBaked && l.bakingOutput.lightmapBakeType == LightmapBakeType.Baked) continue;
                if (GeometryUtility.TestPlanesAABB(planes, new Bounds(l.transform.position, Vector3.one * l.range * 2))) n++;
            }
            return n;
        }

        void Write()
        {
            var sb = new StringBuilder();
            sb.Append("frame,leg,dt_ms,cpu_ms,gpu_ms,x,z,yaw,shadow_spots");
            foreach (var n in recorderNames) sb.Append(',').Append(n.Replace(',', ' '));
            sb.Append('\n');
            for (int f = 0; f < frames; f++)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2:F3},{3:F3},{4:F3},{5:F2},{6:F2},{7:F0},{8}", f, legNames[leg[f]], dt[f], cpu[f], gpu[f], px[f], pz[f], yaw[f], shadowed[f]);
                for (int r = 0; r < recorders.Count; r++) sb.Append(',').Append(values[f, r].ToString("0.###", CultureInfo.InvariantCulture));
                sb.Append('\n');
            }
            File.WriteAllText(outputPath, sb.ToString());
            File.WriteAllText(outputPath + ".flicker.tsv", flicker.ToString());
            File.WriteAllText(outputPath + ".lights.tsv", lightReport.ToString());
            File.WriteAllText(outputPath + ".info.txt", string.Format(CultureInfo.InvariantCulture,
                "{0}x{1} vSync={2} quality={3} gfx={4} gpu={5} startup={6:F1}s\n", Screen.width, Screen.height, QualitySettings.vSyncCount,
                QualitySettings.names[QualitySettings.GetQualityLevel()], SystemInfo.graphicsDeviceType, SystemInfo.graphicsDeviceName, Time.realtimeSinceStartup));
            foreach (var r in recorders) r.Dispose();
        }
    }
}
#endif
