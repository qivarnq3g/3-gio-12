using System.Collections.Generic;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Stealth;
using BaGio12.Story;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class NightOneRealisticSetup
{
    public const string SourcePath = "Assets/Scenes/RealisticPreview.unity";
    public const string ScenePath = "Assets/Scenes/Night01/NightOneRealistic.unity";
    const string NavPath = "Assets/Scenes/Night01/NightOneRealisticNavMesh.asset";
    const string MatRoot = "Assets/Scenes/Night01/Materials";
    static Transform blockout, gameplay;
    static readonly List<string> missing = new List<string>();
    static readonly List<NightObserver> watchers = new List<NightObserver>();
    static Material dark, skin, paper;

    static Material Mat(string name, Color color)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>($"{MatRoot}/{name}.mat");
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        AssetDatabase.CreateAsset(m, $"{MatRoot}/{name}.mat");
        return m;
    }

    static Transform Child(string name) =>
        blockout == null ? null : blockout.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

    static void FoldingGrille(Transform root, float x, float z0, float z1, float height)
    {
        var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/MaterialsPBR/M_SteelPaintedGreen.mat");
        float zMin = Mathf.Min(z0, z1), zMax = Mathf.Max(z0, z1), span = zMax - zMin;
        void Bar(string name, Vector3 at, Vector3 size, float tilt)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = name;
            Object.DestroyImmediate(b.GetComponent<Collider>());
            b.transform.SetParent(root, false);
            b.transform.position = at; b.transform.rotation = Quaternion.Euler(tilt, 0, 0); b.transform.localScale = size;
            var r = b.GetComponent<Renderer>(); r.sharedMaterial = steel; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(b, StaticEditorFlags.BatchingStatic);
        }
        Bar("Grille rail top", new Vector3(x, height - .03f, zMin + span / 2), new Vector3(.05f, .06f, span), 0);
        Bar("Grille rail low", new Vector3(x, .04f, zMin + span / 2), new Vector3(.05f, .05f, span), 0);
        int posts = Mathf.Max(2, Mathf.RoundToInt(span / .13f));
        float step = span / posts;
        for (int i = 0; i <= posts; i++)
            Bar("Grille post " + i, new Vector3(x, height / 2, zMin + i * step), new Vector3(.03f, height - .08f, .018f), 0);
        float pitch = step * 2;
        int cells = Mathf.Max(1, posts / 2);
        int rows = Mathf.Max(2, Mathf.RoundToInt((height - .2f) / (pitch * 1.6f)));
        float rowH = (height - .2f) / rows;
        float length = Mathf.Sqrt(pitch * pitch + rowH * rowH), angle = Mathf.Atan2(rowH, pitch) * Mathf.Rad2Deg;
        for (int row = 0; row < rows; row++)
            for (int i = 0; i < cells; i++)
            {
                var c = new Vector3(x - .012f, .1f + (row + .5f) * rowH, zMin + (i + .5f) * pitch);
                Bar($"Grille strap {row}_{i}a", c, new Vector3(.012f, .014f, length), angle);
                Bar($"Grille strap {row}_{i}b", c, new Vector3(.012f, .014f, length), -angle);
            }
    }

    static Bounds Box(string name, Vector3 min, Vector3 max)
    {
        var fallback = new Bounds((min + max) / 2, max - min);
        var t = Child(name);
        var filter = t == null ? null : t.GetComponent<MeshFilter>();
        var mesh = filter == null ? null : filter.sharedMesh;
        if (mesh == null) { missing.Add(name); return fallback; }
        var local = mesh.bounds;
        var world = new Bounds(t.TransformPoint(local.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
            world.Encapsulate(t.TransformPoint(local.center + Vector3.Scale(local.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2 - Vector3.one)));
        if (Vector3.Distance(world.center, fallback.center) > .5f)
            Debug.LogWarning($"NIGHT1_REAL_ANCHOR_MOVED {name} scene={world.center} blockout={fallback.center}");
        return world;
    }

    static Vector3 Point(string name, Vector3 fallback)
    {
        var t = Child(name);
        if (t == null) { missing.Add(name); return fallback; }
        return t.position;
    }

    static Transform Marker(string name, Vector3 position, float yaw = 0)
    {
        var o = new GameObject(name).transform;
        o.SetParent(gameplay, false); o.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        return o;
    }

    static GameObject Interact(string name, Bounds area, float pad, NightAction action, string caption, Material visible = null)
    {
        GameObject o;
        if (visible != null)
        {
            o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            o.GetComponent<Renderer>().sharedMaterial = visible;
            o.transform.localScale = area.size;
        }
        else
        {
            o = new GameObject();
            o.AddComponent<BoxCollider>().size = area.size + Vector3.one * pad * 2;
        }
        o.name = name; o.transform.SetParent(gameplay, false); o.transform.position = area.center;
        var interaction = o.AddComponent<NightInteraction>(); interaction.action = action; interaction.caption = caption;
        return o;
    }

    static Bounds At(Vector3 center, Vector3 size) => new Bounds(center, size);

    static Light Lamp(string name, Vector3 position, Color color, float intensity, float range = 7)
    {
        var l = new GameObject(name).AddComponent<Light>();
        l.transform.SetParent(gameplay, false); l.transform.position = position;
        l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = color; l.shadows = LightShadows.None;
        return l;
    }

    static NightObserver Watch(string name, Vector3 position, float yaw, float interval, float duration, float delay, NightOneDirector story, bool child = false)
    {
        var o = new GameObject(name); o.transform.SetParent(gameplay, false);
        o.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        float height = child ? 1.2f : 1.65f;
        var agent = o.AddComponent<NavMeshAgent>(); agent.radius = .25f; agent.height = height; agent.baseOffset = 0;
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body placeholder";
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(o.transform, false);
        body.transform.localScale = new Vector3(.45f, height / 2, .35f); body.transform.localPosition = Vector3.up * height / 2;
        body.GetComponent<Renderer>().sharedMaterial = skin;
        var w = o.AddComponent<NightObserver>();
        w.director = story; w.displayName = name; w.interval = interval; w.watchDuration = duration; w.startDelay = delay; w.child = child;
        watchers.Add(w);
        return w;
    }

    static GameObject MovableCopy(Transform source, string name, Transform parent)
    {
        var copy = Object.Instantiate(source.gameObject, parent, true); copy.name = name;
        foreach (var t in copy.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        foreach (var c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (var r in copy.GetComponentsInChildren<Renderer>(true)) { r.lightmapIndex = -1; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
        return copy;
    }
    static void PlacePushedBike(Transform bike, Transform player)
    {
        bike.localPosition = Vector3.zero; bike.localRotation = Quaternion.identity;
        var local = LocalBounds(bike, player);
        if (local.size.x > local.size.z) { bike.localRotation = Quaternion.Euler(0, 90, 0); local = LocalBounds(bike, player); }
        bike.localPosition = new Vector3(.42f - local.center.x, -local.min.y, .35f + local.extents.z - local.center.z);
        Debug.Log($"NIGHT1_REAL_PUSH_BIKE size={local.size} offset={bike.localPosition}");
    }
    static Bounds LocalBounds(Transform bike, Transform player)
    {
        var renderers = bike.GetComponentsInChildren<Renderer>(true);
        var bounds = new Bounds(player.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
        foreach (var r in renderers)
        {
            var b = r.bounds;
            for (int i = 0; i < 8; i++)
                bounds.Encapsulate(player.InverseTransformPoint(new Vector3(i % 2 == 0 ? b.min.x : b.max.x, i / 2 % 2 == 0 ? b.min.y : b.max.y, i / 4 == 0 ? b.min.z : b.max.z)));
        }
        return bounds;
    }

    const string PbrRoot = "Assets/Art/MaterialsPBR";
    static readonly Dictionary<string, Dictionary<string, string>> PbrMap = new Dictionary<string, Dictionary<string, string>>
    {
        ["HomewareShop_ShelfKit_Box"] = new Dictionary<string, string> { ["M_Cardboard"] = "PH_cardboard_box_01" },
        ["Night2_Shelf"] = new Dictionary<string, string> { ["M_PlasticBlue"] = "SF_rack_plastic", ["M_PlasticWhite"] = "M_SteelGalv" },
        ["Night2_BreadCart"] = new Dictionary<string, string>
        {
            ["M_TinRoof"] = "M_TinRust", ["M_StainlessSteel"] = "SF_counter_alu",
            ["M_FoodBrown"] = "SF_goods", ["M_FoodGreen"] = "SF_goods", ["M_FoodYellow"] = "SF_goods",
        },
        ["Night2_ClampLamp"] = new Dictionary<string, string> { ["M_StainlessSteel"] = "SF_rack_chrome", ["M_SteelDark"] = "M_SteelDark" },
        ["Night2_RiceShop"] = new Dictionary<string, string>
        {
            ["M_StainlessSteel"] = "SF_counter_alu",
        },
        ["Night2_LienTable"] = new Dictionary<string, string> { ["M_PlasticBlue"] = "SF_rack_plastic" },
    };

    // Swap imported FBX materials for the PBR set; cart glass keeps its transparent URP material.
    static void ApplyPbrMaterials(GameObject go)
    {
        PbrMap.TryGetValue(go.name, out var map);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || mats[i].name == "M_Night2CartGlass" || (go.name == "Night2_MilkTea" && mats[i].name == "M_PlasticRed")) continue;
                var target = map != null && map.TryGetValue(mats[i].name, out var t) ? t : mats[i].name;
                var pbr = AssetDatabase.LoadAssetAtPath<Material>($"{PbrRoot}/{target}.mat");
                if (pbr != null) mats[i] = pbr;
            }
            r.sharedMaterials = mats;
        }
    }

    static void SetupNightTwoProps(Transform parent)
    {
        var n2Root = new GameObject("Night2 props").transform;
        n2Root.SetParent(parent, false);

        // 1. Night2_BreadCart: replace blockout BanhMi_Cart
        var oldCart = Child("BanhMi_Cart");
        if (oldCart != null)
        {
            foreach (var r in oldCart.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (var c in oldCart.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }
        var breadCartAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Props/Night2_BreadCart.fbx");
        if (breadCartAsset != null)
        {
            var cart = Object.Instantiate(breadCartAsset, n2Root);
            cart.name = "Night2_BreadCart";
            cart.transform.position = new Vector3(-2.3f, 0.08f, 2.2f);
            cart.transform.rotation = Quaternion.Euler(0, 90, 0);
            ApplyPbrMaterials(cart);
            foreach (var r in cart.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            AddBoundsCollider(cart);
        }

        // 2. Night2_Shelf: replace blockout BH_F3_Room7_Rack
        var oldRack = Child("BH_F3_Room7_Rack");
        if (oldRack != null)
        {
            foreach (var r in oldRack.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (var c in oldRack.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }
        var shelfAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Props/Night2_Shelf.fbx");
        if (shelfAsset != null)
        {
            var shelf = Object.Instantiate(shelfAsset, n2Root);
            shelf.name = "Night2_Shelf";
            shelf.transform.position = new Vector3(-2.05f, 10.28f, -44.15f);
            shelf.transform.rotation = Quaternion.identity;
            ApplyPbrMaterials(shelf);
            foreach (var r in shelf.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        }

        // 3. Night2_ClampLamp
        var lampAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Props/Night2_ClampLamp.fbx");
        if (lampAsset != null)
        {
            var lamp = Object.Instantiate(lampAsset, n2Root);
            lamp.name = "Night2_ClampLamp";
            lamp.transform.position = new Vector3(-2.05f, 11.15f, -44.15f);
            lamp.transform.rotation = Quaternion.identity;
            ApplyPbrMaterials(lamp);
            foreach (var r in lamp.GetComponentsInChildren<Renderer>(true)) r.enabled = true;

            var bulb = lamp.transform.Find("Night2_ClampLamp_Bulb");
            var lightObj = new GameObject("DeskLampLight");
            lightObj.transform.SetParent(bulb != null ? bulb : lamp.transform, false);
            lightObj.transform.localPosition = Vector3.zero;
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.82f, 0.55f);
            l.intensity = 2.5f;
            l.range = 4.5f;
            l.enabled = false;
        }

        // 4. HomewareShop_ShelfKit_Box
        var oldBox = Child("HomewareShop_ShelfKit_Box");
        if (oldBox != null)
        {
            foreach (var r in oldBox.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            ApplyPbrMaterials(oldBox.gameObject);
        }
        else
        {
            var boxAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Props/HomewareShop_ShelfKit_Box.fbx");
            if (boxAsset != null)
            {
                var box = Object.Instantiate(boxAsset, n2Root);
                box.name = "HomewareShop_ShelfKit_Box";
                box.transform.position = new Vector3(5.75f, 0.08f, -2.15f);
                ApplyPbrMaterials(box);
                foreach (var r in box.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            }
        }

        SetupNightTwoEvening(parent, n2Root);

        // Night 1 must not show Night 2 props (dem-1.md 1c); NightTwoMorningWorld turns the root on when Night 2 starts.
        n2Root.gameObject.SetActive(false);
    }

    // Issue #129: buổi tối Đêm 2 (dem-2.md mục 5.4). Rice shop stove and milk tea live under the Night 2 root;
    // the evening parking spot is a plain marker that NightTwoMorningWorld moves the player's bike to.
    static void SetupNightTwoEvening(Transform parent, Transform n2Root)
    {
        // env_rice_shop (#23) đã có mặt tiền, quầy món, bàn ghế; thêm bếp ga có nồi canh và nồi cơm cạnh người bán
        // (NPC_N2_RiceSeller ở (60; -1,8)), giữa quầy món và bình gas của env_rice_shop.
        ClearRiceCounterGlass();
        var shop = PlaceProp("Night2_RiceShop", n2Root, OnGround(new Vector3(61.0f, 0, -1.7f)), Quaternion.identity);
        if (shop != null) AddBox(shop, new Vector3(0, .55f, -.31f), new Vector3(.6f, 1.1f, 1.25f));
        else missing.Add("Night2_RiceShop.fbx");

        // Bàn nhựa thấp sát chậu cây phía nam cổng, ngoài lối đi qua cổng; ly trà sữa đặt trên bàn, giấy dán quay ra hẻm.
        var tablePos = OnGround(new Vector3(-0.95f, 0, -44.55f));
        var table = PlaceProp("Night2_LienTable", n2Root, tablePos, Quaternion.identity);
        if (table != null) AddBox(table, new Vector3(0, .23f, 0), new Vector3(.5f, .46f, .5f));
        else missing.Add("Night2_LienTable.fbx");
        // Sticker and liquid keep Toon (no PBR counterpart); straw keeps Toon M_PlasticRed so it stays readable at night.
        if (PlaceProp("Night2_MilkTea", n2Root, tablePos + new Vector3(.06f, .461f, .04f), Quaternion.Euler(0, 15, 0)) == null)
            missing.Add("Night2_MilkTea.fbx");

        // Chỗ trống sát vách phải nhà xe, giữa Garage Cub 9 và 7; đầu xe (local +Z) quay ra lối đi giữa (+X).
        var spot = Child("PARK_N2_EveningBike") ?? new GameObject("PARK_N2_EveningBike").transform;
        spot.SetParent(parent, true);
        spot.SetPositionAndRotation(new Vector3(-3.15f, 0, -52.45f), Quaternion.Euler(0, 90, 0));
    }

    // env_rice_shop (#23) shares one M_GlassFrosted slot between the food counter and the upper-floor windows, and
    // that dark opaque glass hides the dishes. Move only the counter's glass triangles into a clear transparent slot.
    static readonly Bounds RiceCounterGlass = new Bounds(new Vector3(59.9f, 1.2f, -0.9f), new Vector3(4.4f, .8f, 1.3f));
    const string RiceShopMesh = "Assets/Art/MeshesPBR/env_rice_shop_counter_glass.asset";
    static void ClearRiceCounterGlass()
    {
        var renderer = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(r => r.name == "env_rice_shop");
        var filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
        if (filter == null || filter.sharedMesh == null) { missing.Add("env_rice_shop mesh"); return; }
        var mats = renderer.sharedMaterials;
        int glass = System.Array.FindIndex(mats, m => m != null && m.name == "M_GlassFrosted");
        if (glass < 0 || glass >= filter.sharedMesh.subMeshCount) { missing.Add("env_rice_shop counter glass"); return; }
        var mesh = Object.Instantiate(filter.sharedMesh);
        mesh.name = "env_rice_shop_counter_glass";
        var toWorld = renderer.transform.localToWorldMatrix;
        var v = mesh.vertices;
        var keep = new List<int>(); var counter = new List<int>();
        var tris = mesh.GetTriangles(glass);
        for (int i = 0; i < tris.Length; i += 3)
        {
            var c = toWorld.MultiplyPoint3x4((v[tris[i]] + v[tris[i + 1]] + v[tris[i + 2]]) / 3f);
            (RiceCounterGlass.Contains(c) ? counter : keep).AddRange(new[] { tris[i], tris[i + 1], tris[i + 2] });
        }
        if (counter.Count == 0) { missing.Add("env_rice_shop counter glass triangles"); return; }
        mesh.subMeshCount += 1;
        mesh.SetTriangles(keep, glass);
        mesh.SetTriangles(counter, mesh.subMeshCount - 1);
        AssetDatabase.DeleteAsset(RiceShopMesh);
        AssetDatabase.CreateAsset(mesh, RiceShopMesh);
        filter.sharedMesh = mesh;
        var collider = renderer.GetComponent<MeshCollider>();
        if (collider != null && collider.sharedMesh == filter.sharedMesh) collider.sharedMesh = mesh;
        renderer.sharedMaterials = mats.Concat(new[] { ClearGlass() }).ToArray();
    }

    static Material ClearGlass()
    {
        const string path = PbrRoot + "/M_GlassClear.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", new Color(.85f, .92f, .95f, .14f));
        m.SetFloat("_Smoothness", .92f); m.SetFloat("_Metallic", 0);
        m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject PlaceProp(string stem, Transform parent, Vector3 position, Quaternion rotation)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/Props/{stem}.fbx");
        if (asset == null) return null;
        var go = Object.Instantiate(asset, parent);
        go.name = stem;
        go.transform.SetPositionAndRotation(position, rotation);
        ApplyPbrMaterials(go);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        return go;
    }

    static void AddBox(GameObject go, Vector3 center, Vector3 size)
    {
        var box = go.AddComponent<BoxCollider>();
        box.center = center; box.size = size;
    }

    // Floor height under a point from scene colliders, so props sit on sloped alley or shop floors.
    static Vector3 OnGround(Vector3 at)
    {
        Physics.SyncTransforms();
        return Physics.Raycast(new Vector3(at.x, at.y + 3f, at.z), Vector3.down, out var hit, 6f) ? new Vector3(at.x, hit.point.y, at.z) : at;
    }

    // The FBX imports without colliders, so give the prop one box around all its meshes, in its own space.
    static void AddBoundsCollider(GameObject root)
    {
        var toRoot = root.transform.worldToLocalMatrix;
        Bounds? bounds = null;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            var m = toRoot * filter.transform.localToWorldMatrix;
            var b = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                if (bounds is Bounds current) { current.Encapsulate(corner); bounds = current; }
                else bounds = new Bounds(corner, Vector3.zero);
            }
        }
        if (bounds is not Bounds box) return;
        var collider = root.AddComponent<BoxCollider>();
        collider.center = box.center;
        collider.size = box.size;
    }

    [MenuItem("3 Gio 12/Night 1/Create Realistic (overwrites scene)")]
    public static void Create()
    {
        missing.Clear(); watchers.Clear();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(SourcePath, ScenePath)) throw new System.Exception("Cannot copy " + SourcePath);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        dark = Mat("Dark", new Color(.055f, .065f, .08f));
        skin = Mat("Observers", new Color(.55f, .4f, .35f)); paper = Mat("Paper", new Color(.86f, .84f, .78f));

        blockout = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.parent == null && t.name == "night1_3_blockout");
        if (blockout == null) throw new System.Exception("night1_3_blockout missing from " + SourcePath);
        gameplay = new GameObject("Night1 gameplay").transform;
        var detached = gameplay.gameObject.AddComponent<NavMeshModifier>(); detached.ignoreFromBuild = true; detached.applyToChildren = true;

        var controller = Object.FindFirstObjectByType<FirstPersonPlayer>();
        if (controller == null) throw new System.Exception("Player missing from " + SourcePath);
        controller.showTestHud = false;
        var story = new GameObject("NightOneDirector").AddComponent<NightOneDirector>(); story.player = controller;
        story.gameObject.AddComponent<NightOneHud>().story = story;
        if (!controller.GetComponent<NightInjuryFeedback>()) controller.gameObject.AddComponent<NightInjuryFeedback>().director = story;

        var mouth = Point("SPAWN_N1_Prologue_AlleyMouth", new Vector3(0, 0, -1));
        story.alleyStart = Marker("Checkpoint alley start", new Vector3(mouth.x, .1f, -.8f), 180);
        controller.transform.SetPositionAndRotation(story.alleyStart.position, story.alleyStart.rotation);
        var garage = Point("CHECKPOINT_N1_Garage", new Vector3(0, 0, -50));
        story.parkingSpawn = Marker("Checkpoint parking", new Vector3(garage.x, .1f, garage.z), 180);
        var floor = Box("Garage_Floor", new Vector3(-4.5f, -.1f, -58), new Vector3(4.5f, 0, -48));
        story.parkingZone = new Bounds(new Vector3(floor.center.x, 0, floor.center.z), new Vector3(floor.size.x, 4, floor.size.z));
        var room = Box("BH_F3_Room7_Mattress", new Vector3(-5.1f, 10.2f, -47.7f), new Vector3(-3.1f, 10.4f, -45.9f));
        story.roomZone = new Bounds(new Vector3(room.center.x - .33f, room.min.y + 1.5f, -44.75f), new Vector3(3.75f, 3.2f, 6.1f));

        Interact("Start street", At(new Vector3(0, 1.1f, .15f), new Vector3(2 * AlleyWidth.Half + .6f, 2.2f, .3f)), 0, NightAction.ReturnToStart, "Ra đầu hẻm");
        Interact("Police direction", At(new Vector3(-1.8f, 1.4f, -.4f), new Vector3(.1f, .6f, .6f)), .02f, NightAction.Police, "Nhìn về phía công an phường");
        Interact("Hung shop", Box("Shop_Hung_Counter", new Vector3(1.35f, 0, -3.5f), new Vector3(1.7f, 1, -1)), .05f, NightAction.Shop, "Nói chuyện với chú Hùng");
        var camera = Box("Shop_Hung_Camera", new Vector3(.9f, 2.55f, -2.5f), new Vector3(1.15f, 2.7f, -2.3f));
        Interact("Security camera", camera, .1f, NightAction.Camera, "Xem kỹ camera");
        story.securityCamera = Marker("Security camera focus", camera.center);
        var led = Box("Shop_Hung_Camera_LED", new Vector3(.88f, 2.6f, -2.42f), new Vector3(.9f, 2.64f, -2.38f));
        Lamp("Camera red LED", led.center + Vector3.left * .03f, Color.red, .1f, 1);

        var hung = Watch("Chú Hùng", new Vector3(2.6f, .05f, -2.4f), 270, 20, 5, 5, story); hung.flashlight = true;
        var stool = Box("Dishwasher_Stool", new Vector3(-1.45f, 0, -10.5f), new Vector3(-1.15f, .35f, -10.2f));
        Watch("Người phụ nữ", new Vector3(stool.center.x, 0, stool.center.z), 90, 13, 3, 3, story).irregularLook = true;
        var kids = new Vector3(1.4f, 0, -15);
        Watch("Đứa nhỏ 1", kids, 270, 12, 4, 6, story, true).patrol = true;
        Watch("Đứa nhỏ 2", kids + new Vector3(0, 0, -1.5f), 270, 12, 4, 8, story, true).patrol = true;
        var gate = Box("Lien_Gate_PostL", new Vector3(-1.85f, 0, -42), new Vector3(-1.65f, 2.4f, -41.8f));
        var lien = new Vector3(gate.center.x + .4f, 0, gate.center.z - 1.2f);
        var lienWatch = Watch("Cô Liên", lien, 180, 16, 5, 12, story); lienWatch.staysAfterPrologue = true; lienWatch.range = 10;

        var lights = new List<Light>();
        var doors = new[] { (-1.6f, -6.5f, 90f), (1.6f, -10.8f, 270f), (-1.6f, -17.5f, 90f), (1.6f, -23.8f, 270f) };
        for (int i = 0; i < doors.Length; i++)
        {
            var (x, z, yaw) = doors[i];
            var l = Lamp("Alert porch " + i, new Vector3(x - Mathf.Sign(x) * .11f, 2.6f, z), new Color(1, .68f, .36f), 3); l.enabled = false; lights.Add(l);
            var neighbor = Watch("Hàng xóm " + i, new Vector3(x, 0, z), yaw, 1, 1, 0, story); neighbor.stationary = true; neighbor.startsDormant = true;
        }
        story.alertLights = lights.ToArray();
        story.observers = watchers.ToArray();

        var bike02 = Box("Cover_ParkedBike_02", new Vector3(-1.75f, 0, -14.45f), new Vector3(-1.1f, 1.1f, -12.55f));
        var pots = Box("Cover_PlantPots_L", new Vector3(1.25f, 0, -27.2f), new Vector3(1.75f, .9f, -26));
        var cart = Box("Cover_Cart", new Vector3(-1.75f, 0, -30), new Vector3(-.95f, 1, -28.5f));
        var bike05 = Box("Cover_ParkedBike_05", new Vector3(1.1f, 0, -31.95f), new Vector3(1.75f, 1.1f, -30.05f));
        story.hidingPoints = new[]
        {
            Marker("Hiding spot", new Vector3(bike02.center.x, .06f, bike02.min.z - .45f)),
            Marker("Hiding spot", new Vector3(pots.center.x, .06f, pots.min.z - .45f)),
            Marker("Hiding spot", new Vector3(cart.center.x, .06f, cart.min.z - .45f)),
            Marker("Hiding spot", new Vector3(bike05.center.x, .06f, bike05.min.z - .45f)),
        };

        var ownSlot = Box("Garage_Bike_L02", new Vector3(2.2f, 0, -50.65f), new Vector3(4.1f, 1.1f, -49.95f));
        Interact("Own bike", ownSlot, .05f, NightAction.ParkBike, "Dựng xe / xem đồng hồ");
        var roots = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.parent == null).ToArray();
        var ownBike = roots.Where(t => t.name.StartsWith("Garage Cub ")).OrderBy(t => (t.position - ownSlot.center).sqrMagnitude).FirstOrDefault();
        var roadBike = roots.Where(t => t.name.StartsWith("Road Cub ")).OrderBy(t => (t.position - story.alleyStart.position).sqrMagnitude).FirstOrDefault();
        if (ownBike == null) missing.Add("Garage Cub (own bike)");
        else
        {
            story.ownBike = ownBike.gameObject;
            NightOneProps.DressOilSticker(ownBike.gameObject);
            if (roadBike != null)
            {
                story.mouthBike = MovableCopy(ownBike, "Linh bike at alley mouth", gameplay);
                story.mouthBike.transform.SetPositionAndRotation(roadBike.position, roadBike.rotation);
                roadBike.gameObject.SetActive(false);
            }
            else missing.Add("Road Cub (alley mouth)");
            story.pushBike = MovableCopy(ownBike, "Pushed bike", controller.transform);
            PlacePushedBike(story.pushBike.transform, controller.transform);
            story.pushBike.SetActive(false);
        }
        var wave = Box("Garage_Bike_Wave", new Vector3(-4.1f, 0, -51.45f), new Vector3(-2.2f, 1.1f, -50.75f));
        var slot = Point("PLACE_N1_PhoneUnderWave", new Vector3(-2, 0, -51));
        var waveBike = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.parent == null && t.name.StartsWith("Garage Cub "))
            .OrderBy(t => (t.position - wave.center).sqrMagnitude).FirstOrDefault();
        var waveRenderers = waveBike == null ? new Renderer[0] : waveBike.GetComponentsInChildren<Renderer>();
        var waveBounds = wave;
        if (waveRenderers.Length == 0) missing.Add("Garage Cub (Wave)");
        else { waveBounds = waveRenderers[0].bounds; foreach (var r in waveRenderers) waveBounds.Encapsulate(r.bounds); }
        var beside = new Vector3(Mathf.Max(waveBounds.max.x + .12f, slot.x), 0, waveBounds.center.z);
        Interact("Place phone beside bike", waveBounds, .08f, NightAction.PlacePhone, "Đặt / lấy chiếc máy");
        Interact("Place phone slot", At(beside + Vector3.up * .015f, new Vector3(.5f, .03f, .6f)), 0, NightAction.PlacePhone, "Đặt / lấy chiếc máy");
        if (waveBike != null) foreach (var c in waveBike.GetComponents<Collider>()) c.enabled = false;
        story.nganPhone = Interact("Ngan phone", At(beside + Vector3.up * .007f, new Vector3(.08f, .012f, .16f)), 0, NightAction.Phone, "Nhặt chiếc máy", dark);
        story.nganPhone.transform.rotation = Quaternion.Euler(0, 28, 0);
        NightOneProps.DressPhone(story);
        Interact("Parking notice", Box("Garage_Sign_Paper", new Vector3(4.18f, 1.4f, -51.55f), new Vector3(4.19f, 1.8f, -51.25f)), .08f, NightAction.Notice, "Đọc tờ giấy");
        var bulb = Box("Garage_Bulb", new Vector3(-.08f, 2.55f, -53.08f), new Vector3(.08f, 2.7f, -52.92f));
        story.parkingAlertLight = Lamp("Parking alert lamp", bulb.center + Vector3.down * .2f, new Color(1, .85f, .6f), 4, 9); story.parkingAlertLight.enabled = false;

        var passing = new List<NightPassingBike>();
        var raincoat = Mat("Raincoat", new Color(.12f, .32f, .7f));
        for (int i = 0; i < 2; i++)
        {
            var root = new GameObject("Passing bike " + i); root.transform.SetParent(gameplay, false);
            var bike = root.AddComponent<NightPassingBike>(); bike.director = story;
            float x = i == 0 ? -.55f : .55f;
            bike.from = new Vector3(x, 0, i == 0 ? 2 : -47); bike.to = new Vector3(x, 0, i == 0 ? -47 : 2); bike.departure = 15 + i * 13;
            root.transform.SetPositionAndRotation(bike.from, Quaternion.LookRotation(bike.to - bike.from));
            var frame = RealisticSceneBuilder.SpawnBike("cub_rider", i == 0 ? 2 : 4, "Passing bike with rider");
            if (frame == null) missing.Add("Passing bike with rider " + i);
            else
            {
                foreach (var c in frame.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                frame.transform.SetParent(root.transform, false); frame.transform.localPosition = Vector3.zero; frame.transform.localRotation = Quaternion.identity;
                foreach (var r in frame.GetComponentsInChildren<MeshRenderer>())
                    r.sharedMaterials = r.sharedMaterials.Select(m => m != null && m.name.StartsWith("M_ClothBlue") ? raincoat : m).ToArray();
            }
            var lamp = new GameObject("Headlight"); lamp.transform.SetParent(root.transform, false); lamp.transform.localPosition = new Vector3(0, .9f, .7f);
            bike.headlight = lamp.AddComponent<Light>(); bike.headlight.type = LightType.Spot; bike.headlight.range = 9; bike.headlight.spotAngle = 50; bike.headlight.intensity = 4; bike.headlight.enabled = false;
            passing.Add(bike);
        }
        story.passingBikes = passing.ToArray();

        var potModel = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "planter_pot_clay.002" && t.GetComponent<Renderer>() != null);
        var potRenderer = potModel == null ? null : potModel.GetComponent<Renderer>();
        var pot = potRenderer != null ? potRenderer.bounds : Box("Lien_PlantPot_02", new Vector3(-1.7f, 0, -41.1f), new Vector3(-1.3f, .25f, -40.7f));
        if (potRenderer == null) missing.Add("planter_pot_clay.002");
        else { GameObjectUtility.SetStaticEditorFlags(potModel.gameObject, 0); story.fallenPot = potModel; }
        Interact("Lien gate plant", pot, .08f, NightAction.Gate, "Phụ cô Liên dựng chậu");
        Interact("Ask Lien keep phone", At(lien + Vector3.up * 1.15f, new Vector3(.4f, .5f, .4f)), 0, NightAction.LienPhone, "Nhờ cô Liên giữ máy");
        story.greyGlove = GameObject.CreatePrimitive(PrimitiveType.Cube); story.greyGlove.name = "Grey glove found by Lien";
        Object.DestroyImmediate(story.greyGlove.GetComponent<Collider>());
        story.greyGlove.transform.SetParent(gameplay, false); story.greyGlove.transform.position = lien + new Vector3(.25f, 1.1f, 0);
        story.greyGlove.transform.localScale = new Vector3(.1f, .04f, .2f); story.greyGlove.GetComponent<Renderer>().sharedMaterial = Mat("Concrete", new Color(.28f, .32f, .35f));

        var top = Box("BH_F3_Room7_DoorTop", new Vector3(-3.25f, 12.3f, -41.7f), new Vector3(-2.35f, 12.45f, -41.6f));
        var entry = Interact("Room7 entry", At(new Vector3(top.center.x, (room.min.y + top.min.y) / 2, -41.65f), new Vector3(top.size.x, top.min.y - room.min.y, .22f)), 0, NightAction.Room, "Vào phòng 7");
        var door = entry.AddComponent<NightRoomDoor>();
        var leaf = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "env_room7_door");
        if (leaf == null) missing.Add("env_room7_door");
        else { door.leaf = leaf; door.hinge = new Vector3(top.max.x, room.min.y, -41.65f); }
        if (leaf != null && leaf.parent != null) leaf.parent.gameObject.AddComponent<BaGio12.Environment.ProbeLitOverride>();
        var roomPoint = Point("CHECKPOINT_N1_Room7", new Vector3(-3.5f, 10.2f, -44));
        story.roomSpawn = Marker("Checkpoint room7", new Vector3(roomPoint.x, room.min.y + .08f, roomPoint.z), 0);
        var doorBag = Point("PLACE_N1_BackpackDoor", new Vector3(-2.5f, 10.2f, -42));
        story.bagAtDoor = Marker("Bag beside door", new Vector3(doorBag.x - .3f, room.min.y + .25f, doorBag.z - .45f));
        story.bed = Interact("Mattress", room, .03f, NightAction.Bed, "Ngồi xuống nệm / nghỉ").transform;
        story.bag = Interact("Backpack", At(story.bagAtDoor.position, new Vector3(.4f, .5f, .3f)), 0, NightAction.Bag, "Xem kỹ ba lô", dark);
        var flap = GameObject.CreatePrimitive(PrimitiveType.Cube); flap.name = "Backpack main pocket flap"; Object.DestroyImmediate(flap.GetComponent<Collider>());
        flap.transform.SetParent(story.bag.transform, false); flap.transform.localPosition = Vector3.up * .5f; flap.transform.localScale = new Vector3(.85f, .15f, .8f);
        flap.GetComponent<Renderer>().sharedMaterial = Mat("Interact", new Color(.2f, .55f, .5f));
        var presentation = story.bag.AddComponent<NightBagPresentation>(); presentation.director = story; presentation.flap = flap.transform;
        Interact("Move bag", At(story.bagAtDoor.position + new Vector3(0, .32f, 0), new Vector3(.3f, .14f, .3f)), 0, NightAction.MoveBag, "Dời ba lô");
        var box = Box("BH_F3_Room7_PhoneBox", new Vector3(-3.75f, 10.2f, -45.65f), new Vector3(-3.5f, 10.26f, -45.5f));
        Interact("Deposit slip", At(new Vector3(box.center.x - .4f, box.min.y + .005f, box.center.z), new Vector3(.2f, .01f, .15f)), 0, NightAction.Deposit, "Xem giấy nhận cọc", paper);
        Interact("Phone box and charger", box, .06f, NightAction.Charge, "Cắm sạc máy");
        Interact("Wifi", Box("BH_F3_Room7_WifiSign", new Vector3(-2.5f, 11.6f, -41.73f), new Vector3(-2.32f, 11.9f, -41.71f)), .04f, NightAction.Wifi, "Xem bảng Wi-Fi");
        Interact("Foggy mirror", Box("BH_F3_WC_Mirror_Fogged", new Vector3(-13.55f, 11.4f, -39.1f), new Vector3(-13.5f, 12.1f, -38.5f)), .04f, NightAction.Mirror, "Xem gương mờ");

        foreach (var ghost in new[] { "BH_G_ShoeRack" })
        {
            var t = Child(ghost); var c = t == null ? null : t.GetComponent<Collider>(); var r = t == null ? null : t.GetComponent<Renderer>();
            if (c != null && c.enabled && (r == null || !r.enabled)) { c.enabled = false; Debug.Log("NIGHT1_REAL_HIDDEN_BLOCKER_OFF " + ghost); }
        }
        SetupNightTwoProps(gameplay);
        var sill = new GameObject("Night1 boarding gate sill").AddComponent<BoxCollider>();
        sill.transform.position = new Vector3(gate.center.x - .02f, -.05f, gate.center.z - 1.1f); sill.size = new Vector3(.5f, .1f, 2.2f);
        var nav = new GameObject("Night1 navigation").AddComponent<NavMeshSurface>();
        nav.transform.SetParent(gameplay, false);
        nav.collectObjects = CollectObjects.Volume; nav.center = new Vector3(-4, 6, -27); nav.size = new Vector3(24, 16, 68);
        nav.useGeometry = NavMeshCollectGeometry.PhysicsColliders; nav.layerMask = PlayerGeometry.WorldMask;
        Physics.SyncTransforms();
        nav.BuildNavMesh();
        if (nav.navMeshData == null) throw new System.Exception("NavMesh build produced nothing");
        AssetDatabase.DeleteAsset(NavPath); AssetDatabase.CreateAsset(nav.navMeshData, NavPath);
        var gateRoot = new GameObject("Night1 prologue gate").transform; gateRoot.SetParent(gameplay, false);
        var shut = GameObject.CreatePrimitive(PrimitiveType.Cube); shut.name = "Night1 prologue gate shut";
        shut.transform.SetParent(gateRoot, false);
        shut.transform.position = new Vector3(gate.center.x - .3f, 1.2f, (gate.min.z - 44.6f) / 2);
        shut.transform.localScale = new Vector3(.06f, 2.4f, gate.min.z + 44.6f);
        shut.GetComponent<Renderer>().sharedMaterial = dark;
        FoldingGrille(gateRoot, gate.center.x + .02f, gate.min.z, -44.6f, 2.35f);
        story.prologueGate = gateRoot.gameObject;

        foreach (var w in watchers)
        {
            if (NavMesh.SamplePosition(w.transform.position, out var exact, .15f, NavMesh.AllAreas)) { w.transform.position = exact.position; continue; }
            if (!NavMesh.SamplePosition(w.transform.position, out var near, 1.2f, NavMesh.AllAreas)) { missing.Add("navmesh@" + w.displayName); continue; }
            Debug.Log($"NIGHT1_REAL_SNAP {w.displayName} {w.transform.position} -> {near.position}");
            w.transform.position = near.position;
        }
        var stairs = new NavMeshPath();
        bool corridor = NavMesh.SamplePosition(new Vector3(top.center.x + .2f, room.min.y, -41), out var landing, .8f, NavMesh.AllAreas);
        if (!corridor || !NavMesh.CalculatePath(story.parkingSpawn.position, landing.position, NavMesh.AllAreas, stairs) || stairs.status != NavMeshPathStatus.PathComplete)
            missing.Add("navmesh path parking->room7 corridor");
        CharacterSetup.DressObservers(watchers);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log($"NIGHT_ONE_REALISTIC_CREATED missing=[{string.Join(",", missing)}]");
    }

    public static void Run() { Create(); EditorApplication.Exit(missing.Count == 0 ? 0 : 2); }

    public static void InspectNav()
    {
        EditorSceneManager.OpenScene(ScenePath);
        blockout = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.parent == null && t.name == "night1_3_blockout");
        var lines = new List<string>();
        for (float z = 2; z >= -58; z -= .5f)
        {
            var row = new System.Text.StringBuilder($"{z,6:0.0} ");
            for (float x = -4.4f; x <= 4.41f; x += .2f)
                row.Append(NavMesh.SamplePosition(new Vector3(x, .3f, z), out var hit, .25f, NavMesh.AllAreas) && hit.position.y < 1 ? '#' : Mathf.Abs(x) <= AlleyWidth.Half ? '.' : ' ');
            lines.Add(row.ToString());
        }
        var story = Object.FindFirstObjectByType<NightOneDirector>();
        foreach (var w in story.observers)
        {
            bool ok = NavMesh.SamplePosition(w.transform.position, out var near, 2, NavMesh.AllAreas);
            var blockers = Physics.OverlapCapsule(w.transform.position + Vector3.up * .3f, w.transform.position + Vector3.up * 1.6f, .25f, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore);
            lines.Add($"{w.displayName} at {w.transform.position} nearest={(ok ? near.position.ToString() : "none")} blockers={string.Join(",", blockers.Select(b => b.name))}");
        }
        foreach (var (label, area) in new[] { ("shop", new Bounds(new Vector3(3f, 1, -2.5f), new Vector3(5, 2, 5))), ("gate", new Bounds(new Vector3(-1.5f, 1, -43), new Vector3(2, 2, 4))), ("room7door", new Bounds(new Vector3(-2.8f, 11.2f, -41.3f), new Vector3(1.6f, 2, 1.4f))) })
            foreach (var c in Physics.OverlapBox(area.center, area.extents, Quaternion.identity, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore))
                lines.Add($"{label}: {c.name} {c.GetType().Name} min={c.bounds.min} max={c.bounds.max}");
        foreach (var probe in new[] { new Vector3(0, 0, -43), new Vector3(-2.5f, 0, -43), new Vector3(-2.7f, 0, -39.4f), new Vector3(-3.4f, .3f, -38.7f), new Vector3(-6.1f, 1.7f, -39.2f), new Vector3(-4f, 3.4f, -39.7f), new Vector3(-6.1f, 5.1f, -39.2f), new Vector3(-4f, 6.8f, -39.7f), new Vector3(-6.1f, 8.5f, -39.2f), new Vector3(-3.5f, 10.2f, -39.7f), new Vector3(-2.6f, 10.2f, -41) })
        {
            bool found = NavMesh.SamplePosition(probe, out var at, .6f, NavMesh.AllAreas);
            var route = new NavMeshPath();
            if (found) NavMesh.CalculatePath(story.parkingSpawn.position, at.position, NavMesh.AllAreas, route);
            lines.Add($"probe {probe} found={found} at={(found ? at.position.ToString() : "-")} status={(found ? route.status.ToString() : "-")} end={(found && route.corners.Length > 0 ? route.corners[^1].ToString() : "-")}");
        }
        var rack = Child("BH_G_ShoeRack");
        lines.Add($"shoerack renderer={(rack && rack.GetComponent<Renderer>() ? rack.GetComponent<Renderer>().enabled.ToString() : "none")} collider={(rack && rack.GetComponent<Collider>() ? rack.GetComponent<Collider>().enabled.ToString() : "none")}");
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.bounds.Intersects(new Bounds(new Vector3(-2.2f, .5f, -41), new Vector3(.6f, 1, 1.4f)))))
            lines.Add($"visible near rack: {r.name} min={r.bounds.min} max={r.bounds.max}");
        int ghosts = 0;
        foreach (var c in blockout.GetComponentsInChildren<Collider>().Where(c => c.enabled))
        {
            var r = c.GetComponent<Renderer>();
            if (r != null && !r.enabled && c.bounds.Intersects(new Bounds(new Vector3(-4, 6, -27), new Vector3(24, 16, 68)))) ghosts++;
        }
        lines.Add($"hidden blockout colliders in play area: {ghosts}");
        var path = new NavMeshPath();
        NavMesh.SamplePosition(new Vector3(-2.6f, 10.2f, -41), out var corridor, 1.5f, NavMesh.AllAreas);
        NavMesh.CalculatePath(story.parkingSpawn.position, corridor.position, NavMesh.AllAreas, path);
        lines.Add($"stairs corridor={corridor.position} status={path.status} corners={path.corners.Length}");
        System.IO.File.WriteAllLines(System.Environment.GetEnvironmentVariable("NAV_OUT"), lines);
        EditorApplication.Exit(0);
    }
}
