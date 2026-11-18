using System.Linq;
using BaGio12.Story;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class NightOneProps
{
    const string PhoneModel = "Assets/Art/Models/Environment/env_phone_ngan.fbx";
    const string GloveModel = "Assets/Art/Models/Environment/env_glove_left.fbx";
    const string MatDir = "Assets/Art/Models/Environment/Materials/";
    const string StickerTex = "Assets/Art/Decals/D_OilSticker.png";
    const string StickerMat = "Assets/Art/MaterialsPBR/M_OilSticker.mat";
    public static readonly Vector3 HeldPosition = new Vector3(-.16f, -.19f, .34f);
    public static readonly Quaternion HeldRotation = Quaternion.Euler(0, 180 - 18, 0) * Quaternion.Euler(52, 0, -8);

    public static void Materials()
    {
        Lit("M_NganPhoneBody", new Color(.62f, .64f, .66f), .7f, .85f);
        Lit("M_NganPhoneScreen", new Color(.012f, .014f, .02f), .92f, 0);
        Lit("M_NganPhoneBezel", new Color(.01f, .01f, .012f), .8f, 0);
        Lit("M_NganPhoneLens", new Color(.02f, .02f, .03f), .95f, 0);
        Lit("M_NganPhoneFabric", Color.white, .08f, 0, "Assets/Art/Textures/T_PhoneFabric.png");
        Lit("M_GloveKnit", Color.white, .05f, 0, "Assets/Art/Textures/T_TileGloveKnit.png", new Vector2(1, 1));
        var clear = Lit("M_NganPhoneCase", new Color(.92f, .95f, .97f, .12f), .9f, 0);
        clear.SetFloat("_Surface", 1); clear.SetFloat("_Blend", 0);
        clear.SetOverrideTag("RenderType", "Transparent");
        clear.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); clear.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        clear.SetFloat("_ZWrite", 0); clear.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        clear.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(clear);
        AssetDatabase.SaveAssets();
    }

    static Material Lit(string name, Color c, float smooth, float metal, string tex = null, Vector2? tiling = null)
    {
        var path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
        m.SetTexture("_BaseMap", tex == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
        m.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Spawn(string path, Transform parent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new System.Exception("NightOneProps missing " + path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        return go;
    }

    public static void DressPhone(NightOneDirector story)
    {
        Materials();
        var box = story.nganPhone.transform;
        var r = box.GetComponent<MeshRenderer>(); if (r != null) r.enabled = false;
        var phone = Spawn(PhoneModel, box);
        phone.name = "Ngan phone model";
        var s = box.localScale;
        phone.transform.localScale = new Vector3(1 / s.x, 1 / s.y, 1 / s.z);
        phone.transform.localRotation = Quaternion.identity;
        phone.transform.localPosition = new Vector3(0, -.5f + .0008f / s.y, 0);

        var eyes = story.player.eyes.transform;
        var held = new GameObject("Held phone (Ngân)");
        held.transform.SetParent(eyes, false);
        var view = new GameObject("View");
        view.transform.SetParent(held.transform, false);
        view.transform.localPosition = HeldPosition;
        view.transform.localRotation = HeldRotation;
        Spawn(PhoneModel, view.transform).name = "Phone";
        Spawn(GloveModel, view.transform).name = "Glove left";
        foreach (var mr in view.GetComponentsInChildren<MeshRenderer>()) { mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; }
        var c = held.AddComponent<HeldPhoneView>(); c.director = story; c.view = view;
        view.SetActive(false);
    }

    public static void DressOilSticker(GameObject bike)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(StickerTex);
        var ti = (TextureImporter)AssetImporter.GetAtPath(StickerTex);
        if (ti.alphaIsTransparency == false || ti.wrapMode != TextureWrapMode.Clamp) { ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(StickerMat);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, StickerMat); }
        mat.shader = Shader.Find("Universal Render Pipeline/Lit");
        mat.SetTexture("_BaseMap", tex); mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_AlphaClip", 1); mat.SetFloat("_Cutoff", .5f); mat.EnableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Smoothness", .55f);
        mat.renderQueue = (int)RenderQueue.AlphaTest;
        EditorUtility.SetDirty(mat);

        MeshRenderer best = null; Mesh mesh = null; Vector3 centre = Vector3.zero, normal = Vector3.zero; float area = 0;
        foreach (var r in bike.GetComponentsInChildren<MeshRenderer>())
        {
            int sub = System.Array.FindIndex(r.sharedMaterials, m => m != null && m.name.Contains("Gauge"));
            var mf = r.GetComponent<MeshFilter>();
            if (sub < 0 || mf == null || mf.sharedMesh == null || sub >= mf.sharedMesh.subMeshCount) continue;
            var tris = mf.sharedMesh.GetTriangles(sub); var v = mf.sharedMesh.vertices; var t = r.transform;
            Vector3 c = Vector3.zero, n = Vector3.zero; float total = 0;
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = t.TransformPoint(v[tris[i]]), b = t.TransformPoint(v[tris[i + 1]]), d = t.TransformPoint(v[tris[i + 2]]);
                var x = Vector3.Cross(b - a, d - a);
                n += x; c += (a + b + d) / 3 * x.magnitude; total += x.magnitude;
            }
            if (total / 2 <= area) continue;
            best = r; mesh = mf.sharedMesh; area = total / 2; centre = c / Mathf.Max(total, 1e-9f); normal = n.normalized;
        }
        if (best == null) { Debug.LogError("NIGHT1_STICKER no gauge on " + bike.name); return; }
        var gauge = (r: best, sub: 0);
        var tr = best.transform;
        if (normal.y < 0) normal = -normal;
        var up = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
        var side = Vector3.Cross(up, normal).normalized;

        var probe = gauge.r.gameObject.AddComponent<MeshCollider>();
        probe.sharedMesh = mesh;
        Vector3 hitPoint = Vector3.zero, hitNormal = normal; bool found = false;
        foreach (var offset in new[] { .105f, -.105f, .09f, -.09f })
        {
            var from = centre + side * offset + normal * .15f;
            if (probe.Raycast(new Ray(from, -normal), out var hit, .3f) && Vector3.Dot(hit.normal, normal) > .6f)
            { hitPoint = hit.point; hitNormal = hit.normal; found = true; break; }
        }
        Object.DestroyImmediate(probe);
        if (!found) { hitPoint = centre - up * .045f; hitNormal = normal; }

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.name = "Oil change sticker";
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
        quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        var stickerUp = Vector3.ProjectOnPlane(Vector3.up, hitNormal).normalized;
        quad.transform.SetPositionAndRotation(hitPoint + hitNormal * .0015f, Quaternion.LookRotation(-hitNormal, stickerUp));
        quad.transform.SetParent(tr, true);
        var world = new Vector3(.072f, .042f, 1);
        var ls = tr.lossyScale;
        quad.transform.localScale = new Vector3(world.x / ls.x, world.y / ls.y, 1 / ls.z);
        Debug.Log($"NIGHT1_STICKER bike={bike.name} gauge={gauge.r.name} hit={found} at={hitPoint.ToString("F3")} normal={hitNormal.ToString("F2")} area={area:F4}");
    }
}
