using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaGio12.Gameplay;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class LocalRealisticPreview
{
    const string ScenePath = "Assets/Scenes/RealisticPreview.unity";
    const string TexRoot = "Assets/Art/TexturesPBR";
    const string MatRoot = "Assets/Art/MaterialsPBR";
    const string DecalRoot = "Assets/Art/Decals";
    const string AlleyModel = "Assets/Art/Models/Environment/env_alley.fbx";
    static readonly string[] Models = {
        "Assets/Art/Models/Environment/env_garage.fbx", "Assets/Art/Models/Environment/env_room7.fbx",
        "Assets/Art/Models/Environment/env_homeware_shop.fbx",
        "Assets/Art/Models/Environment/env_rice_shop.fbx", "Assets/Art/Models/Environment/env_gas_station.fbx" };
    static readonly string[] AlleyBlockout = { "House_", "BH_Front", "Alley_Floor", "Pole_", "Lamp_", "Boarding_Lien_Balcony", "Cover_", "Dishwasher_", "Lien_PlantPot", "Lien_Gate" };
    static readonly (float y0, float y1)[] LeftHouses = { (0, 5), (5, 9), (9, 13), (13, 17), (17, 21), (21, 25), (25, 30), (30, 34), (34, 40), (40, 48) };
    static readonly (float y0, float y1)[] RightHouses = { (0, 4), (4, 8), (8, 12), (12, 16), (16, 21), (21, 25), (25, 29), (29, 33), (33, 38), (38, 48) };

    sealed class Surface { public string tex; public float size, smooth = 1, tintMix = .55f, metal, wet; }
    static readonly Dictionary<string, Surface> Surfaces = new Dictionary<string, Surface> {
        ["M_ConcreteFloor"] = new Surface { tex = "concrete_floor_worn_001", size = 2.5f, tintMix = 0, wet = .5f },
        ["M_ConcreteWet"] = new Surface { tex = "concrete_floor_worn_001", size = 2.5f, tintMix = 0, wet = .85f },
        ["M_WallPaintYellow"] = new Surface { tex = "painted_plaster_wall", size = 2.5f },
        ["M_WallStainYellow"] = new Surface { tex = "peeling_painted_wall", size = 2.5f },
        ["M_WallPaintOffWhite"] = new Surface { tex = "painted_plaster_wall", size = 2.5f },
        ["M_WallMoldStain"] = new Surface { tex = "peeling_painted_wall", size = 2.5f },
        ["M_CeilingWhite"] = new Surface { tex = "painted_plaster_wall", size = 3f },
        ["M_FacadePink"] = new Surface { tex = "painted_plaster_wall", size = 3f, tintMix = .75f },
        ["M_FacadeGreen"] = new Surface { tex = "painted_plaster_wall", size = 3f, tintMix = .75f },
        ["M_FacadeGray"] = new Surface { tex = "dirty_concrete", size = 3f, tintMix = .5f },
        ["M_FacadeWhite"] = new Surface { tex = "peeling_painted_wall", size = 3f, tintMix = .4f },
        ["M_FacadeYellow"] = new Surface { tex = "painted_plaster_wall", size = 3f, tintMix = .75f },
        ["M_FacadeCream"] = new Surface { tex = "peeling_painted_wall", size = 3f, tintMix = .6f },
        ["M_FacadeTeal"] = new Surface { tex = "painted_plaster_wall", size = 3f, tintMix = .75f },
        ["M_FacadeOrange"] = new Surface { tex = "painted_plaster_wall", size = 3f, tintMix = .75f },
        ["M_FacadeBlue"] = new Surface { tex = "painted_plaster_wall", size = 3f, tintMix = .75f },
        ["M_LimeWhite"] = new Surface { tex = "painted_plaster_wall", size = 2.5f },
        ["M_WallPlinthGray"] = new Surface { tex = "dirty_concrete", size = 2f, tintMix = .3f },
        ["M_ConcretePole"] = new Surface { tex = "dirty_concrete", size = 2f, tintMix = .2f },
        ["M_CurbYellow"] = new Surface { tex = "dirty_concrete", size = 1.5f, tintMix = .7f },
        ["M_BrickExposed"] = new Surface { tex = "brick_wall_001", size = 1.6f, tintMix = 0 },
        ["M_TinRoof"] = new Surface { tex = "rusty_corrugated_iron", size = 2f, tintMix = 0 },
        ["M_TinRust"] = new Surface { tex = "rusty_corrugated_iron", size = 2f, tintMix = 0 },
        ["M_ShutterGray"] = new Surface { tex = "painted_metal_shutter", size = 2f, tintMix = .5f },
        ["M_ShutterGreen"] = new Surface { tex = "painted_metal_shutter", size = 2f, tintMix = .7f },
        ["M_ShutterBlue"] = new Surface { tex = "painted_metal_shutter", size = 2f, tintMix = .7f },
        ["M_SteelDark"] = new Surface { tex = "rusty_painted_metal", size = 1f, tintMix = .5f },
        ["M_SteelPaintedBlue"] = new Surface { tex = "rusty_painted_metal", size = 1f, tintMix = .7f },
        ["M_Asphalt"] = new Surface { tex = "asphalt_02", size = 4f, tintMix = 0, wet = .6f },
        ["M_SidewalkTile"] = new Surface { tex = "square_pavers", size = 2f, tintMix = .3f },
        ["M_FloorTileCream"] = new Surface { tex = "floor_tiles_06", size = 1.2f, tintMix = .3f },
        ["M_FloorTileCreamDark"] = new Surface { tex = "floor_tiles_06", size = 1.2f, tintMix = .5f },
        ["M_FloorTileGray"] = new Surface { tex = "floor_tiles_06", size = 1.2f, tintMix = .4f },
        ["M_DoorWoodDark"] = new Surface { tex = "dark_wood", size = 1.5f, tintMix = .2f },
        ["M_DoorWoodBrown"] = new Surface { tex = "dark_wood", size = 1.5f, tintMix = .1f },
        ["M_WoodShutterGreen"] = new Surface { tex = "dark_wood", size = 1.5f, tintMix = .75f },
    };

    static Color ToonColor(Material m)
    {
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return Color.gray;
    }
    static Texture ToonTexture(Material m)
    {
        foreach (var p in new[] { "_MainTex", "_BaseMap" }) if (m.HasProperty(p) && m.GetTexture(p) != null) return m.GetTexture(p);
        return null;
    }
    static Texture2D Tex(string path, bool normal = false, bool linear = false, int max = 2048)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp == null) return null;
        bool dirty = false;
        if (normal && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; dirty = true; }
        if (linear && imp.sRGBTexture) { imp.sRGBTexture = false; dirty = true; }
        if (imp.maxTextureSize != max) { imp.maxTextureSize = max; dirty = true; }
        if (!normal && path.EndsWith(".png") && !imp.alphaIsTransparency) { imp.alphaIsTransparency = true; dirty = true; }
        if (dirty) imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Texture2D SurfaceTex(string id, string kind)
    {
        var ext = kind == "mask" ? "png" : "jpg";
        return Tex($"{TexRoot}/{id}/{id}_{kind}.{ext}", kind == "normal", kind == "mask");
    }
    static Texture2D WetMask(string id, float wet)
    {
        var path = $"{TexRoot}/{id}/{id}_wet{Mathf.RoundToInt(wet * 100)}.png";
        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        src.LoadImage(File.ReadAllBytes($"{TexRoot}/{id}/{id}_mask.png"));
        int w = src.width, h = src.height; var px = src.GetPixels32();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / w * Mathf.PI * 2, v = (float)y / h * Mathf.PI * 2;
                float n = .5f + .22f * Mathf.Sin(u + 2 * v + 1.3f) + .17f * Mathf.Sin(3 * u - v + .4f) + .11f * Mathf.Sin(5 * u + 4 * v + 2.1f);
                float t = Mathf.Clamp01(wet * Mathf.SmoothStep(0, 1, Mathf.Clamp01((n - .5f + wet) * 1.6f)));
                var p = px[y * w + x];
                p.a = (byte)Mathf.Lerp(p.a, 235, t);
                px[y * w + x] = p;
            }
        src.SetPixels32(px);
        File.WriteAllBytes(path, src.EncodeToPNG());
        Object.DestroyImmediate(src);
        AssetDatabase.ImportAsset(path);
        return Tex(path, linear: true, max: 1024);
    }

    static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
    static void Save(Material m) { Directory.CreateDirectory(MatRoot); AssetDatabase.CreateAsset(m, $"{MatRoot}/{m.name}.mat"); }

    static Material Scanned(string n)
    {
        var dir = $"{TexRoot}/{n}";
        if (!AssetDatabase.IsValidFolder(dir)) return null;
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = n };
        var png = $"{dir}/{n}_albedo.png";
        bool alpha = File.Exists(png);
        m.SetTexture("_BaseMap", Tex(alpha ? png : $"{dir}/{n}_albedo.jpg", max: 1024));
        if (File.Exists($"{dir}/{n}_normal.jpg")) { m.SetTexture("_BumpMap", Tex($"{dir}/{n}_normal.jpg", true, max: 1024)); m.EnableKeyword("_NORMALMAP"); }
        if (File.Exists($"{dir}/{n}_mask.png"))
        {
            var mask = Tex($"{dir}/{n}_mask.png", linear: true, max: 1024);
            m.SetTexture("_MetallicGlossMap", mask); m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetTexture("_OcclusionMap", mask); m.EnableKeyword("_OCCLUSIONMAP");
            m.SetFloat("_Smoothness", 1f);
        }
        if (alpha) { m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", .5f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cull", 0); }
        if (n.Contains("glass"))
        {
            m.SetColor("_EmissionColor", new Color(1f, .62f, .3f) * 6f); m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        return m;
    }

    static Material Convert(Material toon)
    {
        if (toon == null) return null;
        if (Cache.TryGetValue(toon.name, out var done)) return done;
        var n = toon.name;
        var scanned = Scanned(n);
        if (scanned != null) { Save(scanned); return Cache[n] = scanned; }
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = n };
        var c = ToonColor(toon);
        m.SetFloat("_Smoothness", .3f);
        if (Surfaces.TryGetValue(n, out var s))
        {
            m.SetTexture("_BaseMap", SurfaceTex(s.tex, "albedo")); m.SetTexture("_BumpMap", SurfaceTex(s.tex, "normal")); m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", s.wet > 0 ? WetMask(s.tex, s.wet) : SurfaceTex(s.tex, "mask")); m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Smoothness", s.wet > 0 ? 1f : Mathf.Clamp01(.85f * s.smooth)); m.SetFloat("_BumpScale", s.wet > 0 ? .7f : 1.2f);
            m.SetColor("_BaseColor", Color.Lerp(Color.white, c, s.tintMix) * (1 - .45f * s.wet));
            m.SetTextureScale("_BaseMap", new Vector2(1 / s.size, 1 / s.size));
        }
        else
        {
            m.SetColor("_BaseColor", c);
            var t = ToonTexture(toon); if (t != null) m.SetTexture("_BaseMap", t);
            if (n.Contains("Glass")) { m.SetFloat("_Smoothness", .92f); }
            else if (n.Contains("Plastic") || n.Contains("Sign") || n.Contains("Screen") || n.Contains("PVC")) m.SetFloat("_Smoothness", .55f);
            else if (n.Contains("Metal") || n.Contains("Steel") || n.Contains("Aluminum") || n.Contains("Stainless") || n.Contains("Handle") || n.Contains("Chrome"))
            { m.SetFloat("_Metallic", .85f); m.SetFloat("_Smoothness", .55f); }
            else if (n.Contains("Rubber") || n.Contains("Wire") || n.Contains("Soil") || n.Contains("Cloth") || n.Contains("Canvas") || n.Contains("Cardboard") || n.Contains("Interior")) m.SetFloat("_Smoothness", .15f);
            else if (n.Contains("Leaf")) m.SetFloat("_Smoothness", .4f);
        }
        if (n.Contains("Glow") || n.Contains("Lit") || n.Contains("Signal") && !n.Contains("Off") || n.Contains("LensAmber") || n.Contains("Gauge"))
        {
            Color e = toon.HasProperty("_Emissive_Color") ? toon.GetColor("_Emissive_Color") : c;
            if (e.maxColorComponent < .05f) e = c;
            var et = ToonTexture(toon);
            if (et != null) { m.SetTexture("_EmissionMap", et); m.SetColor("_EmissionColor", Color.white * 1.3f); }
            else m.SetColor("_EmissionColor", e * (n.Contains("Window") ? 1.5f : n.Contains("Altar") ? 1.2f : 4f));
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        Save(m);
        return Cache[n] = m;
    }

    static void BoxUV(MeshFilter f, Renderer r, bool keepTextured)
    {
        var src = f.sharedMesh; if (src == null) return;
        var mesh = Object.Instantiate(src); mesh.name = src.name + "_box";
        var v = mesh.vertices; var nrm = mesh.normals; var uv = mesh.uv;
        if (nrm == null || nrm.Length != v.Length) { mesh.RecalculateNormals(); nrm = mesh.normals; }
        var keep = new HashSet<int>();
        if (keepTextured)
            for (int sm = 0; sm < mesh.subMeshCount && sm < r.sharedMaterials.Length; sm++)
            {
                var mat = r.sharedMaterials[sm];
                if (mat != null && !Surfaces.ContainsKey(mat.name)) foreach (var i in mesh.GetTriangles(sm)) keep.Add(i);
            }
        var l2w = f.transform.localToWorldMatrix;
        var outUv = new Vector2[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            if (keep.Contains(i) && uv != null && uv.Length == v.Length) { outUv[i] = uv[i]; continue; }
            var p = l2w.MultiplyPoint3x4(v[i]); var nn = l2w.MultiplyVector(nrm[i]);
            float ax = Mathf.Abs(nn.x), ay = Mathf.Abs(nn.y), az = Mathf.Abs(nn.z);
            outUv[i] = ay >= ax && ay >= az ? new Vector2(p.x, p.z) : ax >= az ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
        }
        mesh.uv = outUv; mesh.RecalculateTangents();
        Directory.CreateDirectory("Assets/Art/MeshesPBR");
        AssetDatabase.CreateAsset(mesh, $"Assets/Art/MeshesPBR/{f.gameObject.name}_{f.gameObject.GetHashCode()}_{Mathf.Abs(src.name.GetHashCode())}.asset");
        f.sharedMesh = mesh;
        var mc = f.GetComponent<MeshCollider>(); if (mc) mc.sharedMesh = mesh;
    }

    static GameObject Place(string path)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.transform.position = Vector3.zero; return go;
    }
    static Bounds RendererBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
    static void Collide(GameObject root)
    {
        foreach (var f in root.GetComponentsInChildren<MeshFilter>())
        {
            var r = f.GetComponent<Renderer>();
            if (r != null && r.enabled && f.GetComponent<Collider>() == null && r.bounds.size.magnitude > .6f) f.gameObject.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
        }
    }
    static Light Point(string name, Vector3 at, Color c, float range, float intensity, bool shadows = true)
    {
        var l = new GameObject(name).AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity;
        l.shadows = shadows ? LightShadows.Soft : LightShadows.None; l.transform.position = at; l.lightmapBakeType = LightmapBakeType.Mixed;
        NightStreetBuilder.SetShadowTier(l, UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow); return l;
    }
    static void Probe(Vector3 at, Vector3 size)
    {
        var p = new GameObject("Reflection probe").AddComponent<ReflectionProbe>(); p.transform.position = at; p.size = size;
        p.mode = ReflectionProbeMode.Realtime; p.refreshMode = ReflectionProbeRefreshMode.OnAwake; p.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        p.resolution = 256; p.boxProjection = true; p.intensity = 1;
    }

    static void PrepareImports()
    {
        if (AssetDatabase.IsValidFolder(MatRoot)) AssetDatabase.DeleteAsset(MatRoot);
        if (AssetDatabase.IsValidFolder("Assets/Art/MeshesPBR")) AssetDatabase.DeleteAsset("Assets/Art/MeshesPBR");
        foreach (var path in Models.Append(AlleyModel))
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter; if (imp == null) continue;
            imp.generateSecondaryUV = true; imp.secondaryUVHardAngle = 70; imp.secondaryUVPackMargin = 8;
            imp.SaveAndReimport();
        }
        var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        if (!rd.rendererFeatures.Any(f => f is DecalRendererFeature))
        {
            var feat = ScriptableObject.CreateInstance<DecalRendererFeature>(); feat.name = "Decal";
            AssetDatabase.AddObjectToAsset(feat, rd);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out string _, out long id);
            var so = new SerializedObject(rd);
            var feats = so.FindProperty("m_RendererFeatures"); var map = so.FindProperty("m_RendererFeatureMap");
            feats.arraySize++; feats.GetArrayElementAtIndex(feats.arraySize - 1).objectReferenceValue = feat;
            map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
            so.ApplyModifiedProperties(); EditorUtility.SetDirty(rd); AssetDatabase.SaveAssets();
        }
    }

    static Material DecalMat(string tex, string normal = null)
    {
        var key = "Decal_" + tex; if (Cache.TryGetValue(key, out var done)) return done;
        var m = new Material(Shader.Find("Shader Graphs/Decal")) { name = key, enableInstancing = true };
        var t = Tex($"{DecalRoot}/{tex}.png", max: 1024);
        var imp = (TextureImporter)AssetImporter.GetAtPath($"{DecalRoot}/{tex}.png");
        if (imp.wrapMode != TextureWrapMode.Clamp) { imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport(); }
        m.SetTexture("Base_Map", t);
        if (normal != null) { m.SetTexture("Normal_Map", Tex($"{DecalRoot}/{normal}.png", true, max: 1024)); m.SetFloat("Normal_Blend", .8f); }
        else m.SetFloat("Normal_Blend", 0f);
        Save(m); return Cache[key] = m;
    }
    static void Decal(Transform parent, string tex, Vector3 at, Vector3 forward, Vector2 size, float depth = .3f, string normal = null, float up = 0)
    {
        var go = new GameObject("Decal " + tex); go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(forward, Mathf.Abs(forward.y) > .9f ? Vector3.forward : Vector3.up) * Quaternion.Euler(0, 0, up));
        var d = go.AddComponent<DecalProjector>(); d.material = DecalMat(tex, normal); d.size = new Vector3(size.x, size.y, depth); d.pivot = Vector3.zero; d.drawDistance = 60;
    }
    static Material Additive(string tex, Color c)
    {
        var key = "Add_" + tex + "_" + Mathf.Abs(c.GetHashCode()); if (Cache.TryGetValue(key, out var done)) return done;
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = key };
        m.SetTexture("_BaseMap", Tex($"{DecalRoot}/{tex}.png", max: 256)); m.SetColor("_BaseColor", c);
        m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 2); m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
        Save(m); return Cache[key] = m;
    }
    static void Halo(Vector3 at, Color c, float size)
    {
        var go = new GameObject("Halo"); go.transform.position = at;
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.startLifetime = 100000; main.startSpeed = 0; main.startSize = size; main.maxParticles = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.startColor = Color.white; main.playOnAwake = true;
        var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
        var sh = ps.shape; sh.enabled = false;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Additive("P_Halo", c);
        var dust = new GameObject("Dust"); dust.transform.position = at + Vector3.down * .8f;
        var dp = dust.AddComponent<ParticleSystem>(); dp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var dm = dp.main; dm.startLifetime = 9; dm.startSpeed = .02f; dm.startSize = new ParticleSystem.MinMaxCurve(.006f, .014f); dm.maxParticles = 60;
        dm.startColor = new Color(c.r, c.g, c.b, .5f); dm.prewarm = true; dm.simulationSpace = ParticleSystemSimulationSpace.World;
        var de = dp.emission; de.rateOverTime = 6;
        var ds = dp.shape; ds.shapeType = ParticleSystemShapeType.Sphere; ds.radius = 1.1f;
        var dn = dp.noise; dn.enabled = true; dn.strength = .05f; dn.frequency = .3f;
        dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = Additive("P_Halo", new Color(1, .9f, .75f, .6f));
    }
    static void Puddle(Transform parent, int k, Vector3 at, Vector2 size, float yaw)
    {
        var key = "Puddle" + k;
        if (!Cache.TryGetValue(key, out var m))
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = key };
            m.SetTexture("_BaseMap", Tex($"{DecalRoot}/P_Puddle{k}.png", max: 512)); m.SetColor("_BaseColor", new Color(.03f, .03f, .035f, .92f));
            m.SetFloat("_Smoothness", .97f); m.SetFloat("_Metallic", 0);
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_BlendModePreserveSpecular", 0); m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
            Save(m); Cache[key] = m;
        }
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(q.GetComponent<Collider>());
        q.name = key; q.transform.SetParent(parent, false);
        q.transform.SetPositionAndRotation(at, Quaternion.Euler(90, yaw, 0)); q.transform.localScale = new Vector3(size.x, size.y, 1);
        var r = q.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static void DressAlley()
    {
        var root = new GameObject("Alley dressing").transform;
        var rng = new System.Random(312);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        var posters = new[] { "D_PosterDrill", "D_PosterSeptic", "D_PosterLoan" };
        foreach (var side in new[] { -1, 1 })
        {
            var into = new Vector3(side, 0, 0); float x = side * 1.25f;
            for (float y = .6f; y < 47.5f; y += R(1.2f, 2.4f))
                foreach (var ledge in new[] { 3.25f, 6.65f })
                    if (rng.NextDouble() < .5) { float h = R(1.3f, 2.4f); Decal(root, "D_Leak" + rng.Next(6), new Vector3(x, ledge - h / 2, -y), into, new Vector2(R(.9f, 1.6f), h)); }
            for (float y = 0; y < 48; y += 3) Decal(root, "D_DampBase", new Vector3(x, .5f, -(y + 1.5f)), into, new Vector2(3.1f, 1f), .5f);
            var houses = side < 0 ? LeftHouses : RightHouses;
            for (int k = 0; k < houses.Length; k++)
            {
                var (y0, y1) = houses[k];
                Decal(root, "D_Plate" + ((k * 2 + (side > 0 ? 1 : 0)) % 13), new Vector3(x, 2.35f, -(y0 + .11f)), into, new Vector2(.22f, .14f), .4f);
                if (rng.NextDouble() < .7) Decal(root, posters[rng.Next(3)], new Vector3(x, R(1.25f, 1.9f), -(y1 - .11f)), into, new Vector2(.2f, .28f), .4f, null, R(-4, 4));
                if (rng.NextDouble() < .5) Decal(root, posters[rng.Next(3)], new Vector3(x, R(1.0f, 2.2f), -R(y0 + .6f, y1 - .6f)), into, new Vector2(.32f, .45f), .5f, null, R(-6, 6));
            }
        }
        Decal(root, "D_ShopSign", new Vector3(-1.25f, 3.06f, -2.5f), Vector3.left, new Vector2(4.1f, .44f), .5f);
        Decal(root, "D_RoomForRent", new Vector3(1.25f, 1.55f, -44.75f), Vector3.right, new Vector2(.75f, .25f), .4f);
        foreach (var y in new[] { 4.5f, 11f, 17.5f, 26f, 33f, 41f }) Decal(root, "D_Cracks", new Vector3(R(-.6f, .3f), 0, -y), Vector3.down, new Vector2(R(1.2f, 2f), R(1.2f, 2f)), .3f, "D_Cracks_normal", R(0, 360));
        foreach (var y in new[] { 18.4f, 37.2f }) Decal(root, "D_Manhole", new Vector3(-.2f, 0, -y), Vector3.down, new Vector2(.75f, .75f), .3f, "D_Manhole_normal", R(0, 360));
        foreach (var (y, k) in new[] { (3.2f, 0), (9.8f, 1), (14.5f, 2), (23.5f, 0), (28.9f, 1), (35.4f, 2), (45f, 1) })
            Puddle(root, k, new Vector3(R(-.7f, .5f), .009f, -y), new Vector2(R(.9f, 1.6f), R(.7f, 1.3f)), R(0, 180));
    }

    static void DressGarage()
    {
        var root = new GameObject("Garage dressing").transform;
        var rng = new System.Random(1253);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        foreach (var side in new[] { -1, 1 })
        {
            var into = new Vector3(side, 0, 0); float x = side * 4.5f;
            for (float y = 48.6f; y < 57.6f; y += R(1.1f, 2f))
                if (rng.NextDouble() < .65) { float h = R(1.2f, 2.2f); Decal(root, "D_Leak" + rng.Next(6), new Vector3(x, 2.8f - h / 2, -y), into, new Vector2(R(.8f, 1.5f), h)); }
            for (float y = 48.2f; y < 57.8f; y += 3.2f) Decal(root, "D_DampBase", new Vector3(x, .45f, -(y + 1.6f)), into, new Vector2(3.3f, .9f), .5f);
        }
        for (float x = -4f; x < 4.2f; x += R(1.2f, 2f))
            if (rng.NextDouble() < .7) { float h = R(1.2f, 2.2f); Decal(root, "D_Leak" + rng.Next(6), new Vector3(x, 2.8f - h / 2, -57.8f), Vector3.back, new Vector2(R(.8f, 1.5f), h)); }
        for (float x = -4.2f; x < 4.3f; x += 3f) Decal(root, "D_DampBase", new Vector3(x + 1.5f, .45f, -57.8f), Vector3.back, new Vector2(3.1f, .9f), .5f);
        Decal(root, "D_PosterDrill", new Vector3(-4.5f, 1.5f, -49.6f), Vector3.left, new Vector2(.32f, .45f), .4f, null, 3);
    }

    static void ParkBikes(GameObject blockout)
    {
        var bikes = blockout.GetComponentsInChildren<Renderer>()
            .Where(r => r.name.Contains("Bike")).OrderBy(r => r.name).ToArray();
        (float x, float z, float yaw)[] Spots(IEnumerable<Renderer> renderers) => renderers.Select((r, k) =>
        {
            var center = r.bounds.center;
            bool acrossAlley = r.bounds.size.x > r.bounds.size.z;
            float yaw = acrossAlley ? (center.x < 0 ? 90f : 270f) : (center.x < 0 ? 0f : 180f);
            return (center.x, center.z, yaw + (k % 4 == 2 ? 180f : 0f));
        }).ToArray();

        // Motorbikes ONLY belong in the garage room at the end of the alley (Issue #76)
        var garage = Spots(bikes.Where(r => r.name.StartsWith("Garage_Bike_")));

        // Disable ALL blockout bikes in blockout gym/gym fbx (garage, alley, shallow alley)
        foreach (var r in bikes)
        {
            r.enabled = false;
            foreach (var collider in r.GetComponents<Collider>()) collider.enabled = false;
        }

        // Spawn detailed Cub motorbikes in the garage at the end of the alley
        ParkBikes(garage, "Garage Cub ", 6);

        // Apply varied paint colors and BoxCollider to the 14 Street_Cub_00..Street_Cub_13 meshes on the main road sidewalk
        var paints = new[] { new Color(.12f, .22f, .5f), new Color(.75f, .75f, .73f), new Color(.08f, .08f, .09f), new Color(.6f, .08f, .08f), new Color(.2f, .35f, .25f), new Color(.5f, .45f, .35f) };
        int streetIdx = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Street_Cub_")).OrderBy(r => r.name))
        {
            int bike = (streetIdx++ + 3) % paints.Length;
            r.enabled = true;
            r.sharedMaterials = r.sharedMaterials.Select(Convert).Select(m =>
            {
                if (m == null || !m.name.Contains("CubPaint")) return m;
                var key = "M_CubPaint_" + bike; if (Cache.TryGetValue(key, out var v)) return v;
                v = new Material(m) { name = key }; v.SetColor("_BaseColor", paints[bike]); Save(v); return Cache[key] = v;
            }).ToArray();
            var mb = r.GetComponent<MeshFilter>().sharedMesh.bounds;
            var box = r.gameObject.GetComponent<BoxCollider>();
            if (box == null) box = r.gameObject.AddComponent<BoxCollider>();
            box.center = mb.center;
            box.size = mb.size;
        }
    }

    static void ParkBikes((float x, float z, float yaw)[] spots, string prefix, int paintOffset)
    {
        var cubAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Props/prop_motorbike_cub.fbx"); if (cubAsset == null) return;
        var paints = new[] { new Color(.12f, .22f, .5f), new Color(.75f, .75f, .73f), new Color(.08f, .08f, .09f), new Color(.6f, .08f, .08f), new Color(.2f, .35f, .25f), new Color(.5f, .45f, .35f) };
        for (int k = 0; k < spots.Length; k++)
        {
            var (x, z, yaw) = spots[k];
            int bike = (k + paintOffset) % paints.Length;
            var cub = (GameObject)PrefabUtility.InstantiatePrefab(cubAsset);
            PrefabUtility.UnpackPrefabInstance(cub, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            cub.name = prefix + k;
            cub.transform.SetPositionAndRotation(new Vector3(x, 0, z), Quaternion.Euler(0, yaw + (k % 3 - 1) * 6, 0));
            foreach (var r in cub.GetComponentsInChildren<MeshRenderer>())
            {
                r.enabled = true;
                r.sharedMaterials = r.sharedMaterials.Select(Convert).Select(m =>
                {
                    if (m == null || !m.name.Contains("CubPaint")) return m;
                    var key = "M_CubPaint_" + bike; if (Cache.TryGetValue(key, out var v)) return v;
                    v = new Material(m) { name = key }; v.SetColor("_BaseColor", paints[bike]); Save(v); return Cache[key] = v;
                }).ToArray();
            }
            var b = RendererBounds(cub); var box = cub.AddComponent<BoxCollider>();
            box.center = cub.transform.InverseTransformPoint(b.center); box.size = new Vector3(.7f, b.size.y, 1.8f);
        }
    }

    static void TintShutters(GameObject alley)
    {
        var tints = new[] { new Color(.62f, .66f, .7f), new Color(.5f, .62f, .55f), new Color(.66f, .55f, .45f), new Color(.85f, .85f, .83f), new Color(.45f, .52f, .64f), new Color(.7f, .68f, .6f) };
        int k = 0;
        foreach (var r in alley.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("Shutter_")).OrderBy(r => r.name))
        {
            int t = (k++ * 4) % tints.Length; var key = "PH_rollershutter_door_" + t;
            if (!Cache.TryGetValue(key, out var v)) { v = new Material(r.sharedMaterial) { name = key }; v.SetColor("_BaseColor", tints[t]); Save(v); Cache[key] = v; }
            r.sharedMaterial = v;
        }
    }

    static void AlleyLights(GameObject alley)
    {
        int k = 0;
        foreach (var t in alley.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("LampBulb")).ToList())
        {
            var at = t.GetComponent<Renderer>().bounds.center;
            bool cool = k % 3 == 1, weak = k % 5 == 3; k++;
            var c = cool ? new Color(.82f, .92f, 1f) : weak ? new Color(1f, .6f, .3f) : new Color(1f, .7f, .42f);
            Point("Alley bulb " + k, at + Vector3.down * .06f, c, weak ? 6 : 9, weak ? 1.4f : cool ? 3.5f : 5f);
            Halo(at, new Color(c.r, c.g, c.b, weak ? .3f : .55f), weak ? .45f : .7f);
        }
        foreach (var center in Clusters(alley, "M_TubeLightGlow", 3f))
        {
            Point("Tube light", center + Vector3.down * .25f, new Color(.85f, .95f, 1f), 7, 3f);
            Halo(center, new Color(.85f, .95f, 1f, .35f), .9f);
        }
        foreach (var center in Clusters(alley, "M_AltarRedGlow", .5f))
        {
            Point("Altar light", center, new Color(1f, .16f, .08f), 1.4f, .3f, false);
            Halo(center, new Color(1f, .2f, .1f, .25f), .18f);
        }
    }

    static IEnumerable<Vector3> Clusters(GameObject root, string material, float cell)
    {
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || mats[i].name != material) continue;
                var mesh = r.GetComponent<MeshFilter>().sharedMesh; var verts = mesh.vertices;
                foreach (var g in mesh.GetTriangles(i).Select(v => r.transform.TransformPoint(verts[v])).GroupBy(p => (Mathf.Round(p.x / cell), Mathf.Round(p.y / cell), Mathf.Round(p.z / cell))))
                    yield return new Vector3(g.Average(p => p.x), g.Average(p => p.y), g.Average(p => p.z));
            }
        }
    }

    static Material SkyGlow()
    {
        var path = $"{MatRoot}/SkyGlow.png";
        var tex = new Texture2D(4, 256, TextureFormat.RGBA32, false);
        var zenith = new Color(.035f, .045f, .075f); var horizon = new Color(.3f, .19f, .12f);
        for (int y = 0; y < 256; y++)
        {
            var c = Color.Lerp(horizon, zenith, Mathf.Pow(Mathf.Clamp01((y / 255f - .5f) * 2f), .5f));
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        Directory.CreateDirectory(MatRoot);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path); imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = false; imp.SaveAndReimport();
        var mat = new Material(Shader.Find("Skybox/Panoramic")) { name = "SkyGlow" };
        mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        Save(mat);
        return mat;
    }

    static void MarkStatic(IEnumerable<GameObject> roots)
    {
        foreach (var root in roots)
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                var mf = r.GetComponent<MeshFilter>();
                if (!r.enabled || mf == null || mf.sharedMesh == null) continue;
                var uv2 = new List<Vector2>(); mf.sharedMesh.GetUVs(1, uv2);
                if (uv2.Count == 0) continue;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                if (r.bounds.size.magnitude < 1.5f) r.receiveGI = ReceiveGI.LightProbes;
                else r.scaleInLightmap = r.name.StartsWith("Alley_Facades") ? .5f : 1f;
            }
    }

    static void Probes()
    {
        var g = new GameObject("Light probes").AddComponent<LightProbeGroup>();
        var pts = new List<Vector3>();
        for (float y = -1; y <= 58; y += 2)
            foreach (var x in y > 48 ? new[] { -3.5f, 0f, 3.5f } : new[] { -.9f, 0f, .9f })
                foreach (var h in new[] { .3f, 1.4f, 2.8f }) pts.Add(new Vector3(x, h, -y));
        for (float y = 38.5f; y < 48; y += 2) for (float x = 2; x < 13; x += 2.5f) foreach (var h in new[] { 1.4f, 4.8f, 8.2f, 11.6f }) pts.Add(new Vector3(x, h, -y));
        g.probePositions = pts.ToArray();
    }

    static void Bake()
    {
        var ls = new LightingSettings { name = "AlleyLighting" };
        ls.bakedGI = true; ls.realtimeGI = false; ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
        ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU; ls.lightmapResolution = 8; ls.lightmapPadding = 4; ls.lightmapMaxSize = 2048;
        ls.directSampleCount = 32; ls.indirectSampleCount = 256; ls.environmentSampleCount = 64; ls.maxBounces = 3;
        ls.ao = true; ls.aoMaxDistance = .6f; ls.aoExponentIndirect = 1.2f; ls.aoExponentDirect = 0;
        ls.filteringMode = LightingSettings.FilterMode.Auto; ls.lightmapCompression = LightmapCompression.HighQuality;
        Lightmapping.lightingSettings = ls;
        var t = System.Diagnostics.Stopwatch.StartNew();
        bool ok = Lightmapping.Bake();
        Debug.Log($"REAL_PREVIEW_BAKE ok={ok} seconds={t.Elapsed.TotalSeconds:F0} lightmaps={LightmapSettings.lightmaps.Length}");
    }

    public static void Build()
    {
        PrepareImports();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var envs = Models.Where(File.Exists).Select(Place).ToList();
        var street = Place("Assets/Prefabs/Environment/NightStreet.prefab"); envs.Add(street);
        var alley = Place(AlleyModel);
        var blockout = Place("Assets/Art/Blockout/night1_3_blockout.fbx");
        var cover = envs.Select(RendererBounds).ToList();
        foreach (var r in blockout.GetComponentsInChildren<Renderer>())
        {
            var c = r.bounds.center;
            if (AlleyBlockout.Any(p => r.name.StartsWith(p))) r.enabled = false;
            else if (cover.Any(b => { var e = b; e.Expand(.1f); return e.Contains(c); })) r.enabled = false;
            else if (cover.Any(b => b.Intersects(r.bounds))) r.transform.position += Vector3.down * .03f;
        }
        foreach (var t in blockout.GetComponentsInChildren<Transform>())
            if (t.name.StartsWith("CHECKPOINT") || t.name.StartsWith("PLACE") || t.name.StartsWith("DOOR") || t.name.StartsWith("SPAWN") || t.name.StartsWith("CLUE"))
            { var r = t.GetComponent<Renderer>(); if (r) r.enabled = false; }
        envs.Add(alley);

        foreach (var env in envs)
            foreach (var r in env.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.name.StartsWith("Street_Cub_") && !r.name.StartsWith("prop_motorbike_cub"))
                    BoxUV(r.GetComponent<MeshFilter>(), r, true);
                r.sharedMaterials = r.sharedMaterials.Select(Convert).ToArray();
            }

        ParkBikes(blockout);

        var pastel = new[] { new Color(.86f, .8f, .62f), new Color(.7f, .78f, .74f), new Color(.82f, .7f, .64f), new Color(.78f, .8f, .86f), new Color(.9f, .88f, .82f) };
        var blockFloor = Convert(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_ConcreteFloor.mat"));
        int k = 0;
        foreach (var r in blockout.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled))
        {
            BoxUV(r.GetComponent<MeshFilter>(), r, false);
            bool floor = r.name.Contains("Floor") || r.name.Contains("Stair") || r.bounds.size.y < .25f;
            if (floor) { r.sharedMaterial = blockFloor; continue; }
            var m = new Material(Cache.TryGetValue("M_WallStainYellow", out var w) ? w : blockFloor) { name = $"Block_{k++}" };
            m.SetColor("_BaseColor", Color.Lerp(Color.white, pastel[Mathf.Abs(r.name.GetHashCode()) % pastel.Length], .6f));
            Save(m); r.sharedMaterial = m;
        }
        envs.ForEach(Collide); Collide(blockout);

        DressAlley();
        DressGarage();
        TintShutters(alley);

        RenderSettings.skybox = SkyGlow();
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.018f, .022f, .032f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = .06f; RenderSettings.fogColor = new Color(.05f, .045f, .045f);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
        var bloom = profile.Add<Bloom>(true); bloom.threshold.Override(.85f); bloom.intensity.Override(.9f); bloom.scatter.Override(.75f);
        var adj = profile.Add<ColorAdjustments>(true); adj.postExposure.Override(1.1f); adj.contrast.Override(14); adj.saturation.Override(-14);
        var wb = profile.Add<WhiteBalance>(true); wb.temperature.Override(-8);
        var vig = profile.Add<Vignette>(true); vig.intensity.Override(.34f); vig.smoothness.Override(.45f);
        var grain = profile.Add<FilmGrain>(true); grain.type.Override(FilmGrainLookup.Thin1); grain.intensity.Override(.35f);
        var ca = profile.Add<ChromaticAberration>(true); ca.intensity.Override(.08f);
        AssetDatabase.CreateAsset(profile, $"{MatRoot}/RealisticNight.asset");
        var volume = new GameObject("Realistic post").AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 50; volume.sharedProfile = profile;

        Point("Garage bulb", new Vector3(0, 2.5f, -53), new Color(1f, .72f, .42f), 9, 5f); Halo(new Vector3(0, 2.62f, -53), new Color(1f, .72f, .42f, .5f), .6f);
        Point("Room7 tube", new Vector3(-3.3f, 12.9f, -44.5f), new Color(.86f, .94f, 1f), 8, 6f);
        foreach (var t in blockout.GetComponentsInChildren<Transform>().Where(t => t.name.Contains("MotionLight")).ToList())
        {
            var r = t.GetComponent<Renderer>();
            if (r) Point(t.name + " light", r.bounds.center + Vector3.down * .1f, new Color(1f, .85f, .65f), 6, 3f, false);
        }
        AlleyLights(alley);
        Probe(new Vector3(0, 1.6f, -53), new Vector3(9.5f, 3.6f, 10.5f));
        Probe(new Vector3(-3.3f, 11.6f, -44.7f), new Vector3(4.2f, 3.5f, 6.4f));
        for (int i = 0; i < 6; i++) Probe(new Vector3(0, 1.6f, -4 - i * 8), new Vector3(2.6f, 4f, 8.2f));
        Probe(new Vector3(0, 3, 10), new Vector3(60, 10, 25));
        Probes();

        var player = new GameObject("Player"); player.layer = PlayerGeometry.PlayerLayer;
        player.transform.SetPositionAndRotation(new Vector3(0, .1f, -0.8f), Quaternion.Euler(0, 180, 0));
        var cc = player.AddComponent<CharacterController>(); cc.height = PlayerGeometry.StandingHeight; cc.radius = PlayerGeometry.Radius;
        cc.center = Vector3.up * (PlayerGeometry.StandingHeight / 2); cc.skinWidth = PlayerGeometry.SkinWidth; cc.stepOffset = PlayerGeometry.StepOffset;
        var camObject = new GameObject("Main Camera"); camObject.tag = "MainCamera"; camObject.layer = PlayerGeometry.PlayerLayer;
        camObject.transform.SetParent(player.transform, false); camObject.transform.localPosition = Vector3.up * PlayerGeometry.StandingEye;
        var cam = camObject.AddComponent<Camera>(); cam.nearClipPlane = .03f; cam.fieldOfView = 70;
        cam.clearFlags = CameraClearFlags.Skybox; cam.backgroundColor = RenderSettings.fogColor;
        var data = cam.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.TemporalAntiAliasing;
        camObject.AddComponent<AudioListener>();
        var controller = player.AddComponent<FirstPersonPlayer>(); controller.eyes = cam; controller.showTestHud = false;
        controller.inputTemplate = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");

        var gi = envs.Where(e => e != street).Where(e => { var c = RendererBounds(e).center; return c.z > -70 && c.z < 5 && Mathf.Abs(c.x) < 20; }).ToList();
        MarkStatic(gi);
        Debug.Log($"REAL_PREVIEW_SCENE materials={Cache.Count} static_roots={string.Join(",", gi.Select(g => g.name))}");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        if (System.Environment.GetCommandLineArgs().Contains("-bake")) Bake();
        EditorSceneManager.SaveScene(scene, ScenePath);
        foreach (var p in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) p.RenderProbe();
        Shots(cam);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "D:/Projects/3-gio-12-play/BuildReal/3gio12-tathuc.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
            extraScriptingDefines = new[] { "PLAYTEST_BUILD" }
        });
        Debug.Log("REAL_PREVIEW_BUILD " + report.summary.result);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static void Shots(Camera cam, string prefix = "Real", string dir = "Logs")
    {
        var views = new (string name, Vector3 pos, Vector3 rot)[] {
            ("alley", new Vector3(0, 1.6f, -1), new Vector3(5, 180, 0)),
            ("alley_mid", new Vector3(.4f, 1.6f, -30), new Vector3(3, 180, 0)),
            ("alley_shop", new Vector3(.8f, 1.55f, -4.2f), new Vector3(4, 300, 0)),
            ("alley_up", new Vector3(.2f, 1.6f, -10), new Vector3(-28, 190, 0)),
            ("alley_back", new Vector3(-.3f, 1.6f, -40), new Vector3(4, 0, 0)),
            ("alley_gate", new Vector3(-.6f, 1.6f, -38.5f), new Vector3(6, 150, 0)),
            ("garage", new Vector3(0, 1.6f, -48.5f), new Vector3(10, 180, 0)),
            ("garage_entry", new Vector3(.3f, 1.6f, -45.5f), new Vector3(6, 185, 0)),
            ("garage_back", new Vector3(-.5f, 1.6f, -57.2f), new Vector3(8, 15, 0)),
            ("garage_wave", new Vector3(-.4f, 1.5f, -50.2f), new Vector3(22, 120, 0)),
            ("garage_left", new Vector3(1.2f, 1.6f, -55.5f), new Vector3(10, 315, 0)),
            ("room7", new Vector3(-2.3f, 11.8f, -42.3f), new Vector3(15, 200, 0)) };
        var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        Directory.CreateDirectory(dir);
        foreach (var v in views)
        {
            cam.transform.SetPositionAndRotation(v.pos, Quaternion.Euler(v.rot));
            cam.targetTexture = rt; for (int i = 0; i < 8; i++) cam.Render(); RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes($"{dir}/{prefix}_{v.name}.png", tex.EncodeToPNG());
        }
        cam.targetTexture = null; RenderTexture.active = null;
        cam.transform.localPosition = Vector3.up * PlayerGeometry.StandingEye; cam.transform.localRotation = Quaternion.identity;
    }

    public static void Capture()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-shots");
        var dir = i >= 0 && i + 1 < args.Length ? args[i + 1] : "Logs";
        EditorSceneManager.OpenScene(ScenePath);
        var cam = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c => c.CompareTag("MainCamera"));
        Shots(cam, "Real", dir);
        var clay = new Material(Shader.Find("Universal Render Pipeline/Lit")); clay.SetColor("_BaseColor", new Color(.55f, .55f, .55f)); clay.SetFloat("_Smoothness", .2f);
        foreach (var d in Object.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None)) d.enabled = false;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) r.sharedMaterials = r.sharedMaterials.Select(_ => clay).ToArray();
        Shots(cam, "Clay", dir);
        Debug.Log("REAL_PREVIEW_CAPTURE " + dir);
        EditorApplication.Exit(0);
    }
}
