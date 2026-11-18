using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaGio12.Stealth;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class NightOnePropsCapture
{
    const string Scene = "Assets/Scenes/Night01/NightOneRealistic.unity";

    public static void Capture()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-shots");
        var dir = i >= 0 ? args[i + 1] : "Logs/props";
        Directory.CreateDirectory(dir);
        EditorSceneManager.OpenScene(Scene);
        var story = Object.FindFirstObjectByType<NightOneDirector>();
        var eyes = story.player.eyes;
        var held = eyes.GetComponentInChildren<HeldPhoneView>(true);
        held.view.SetActive(true);
        Pose(Object.FindObjectsByType<Animator>(FindObjectsSortMode.None));

        var cam = new GameObject("Capture cam").AddComponent<Camera>();
        cam.nearClipPlane = .02f; cam.fieldOfView = eyes.fieldOfView;
        var shots = new List<(string name, Vector3 pos, Quaternion rot, float fov)>();
        story.player.transform.SetPositionAndRotation(new Vector3(.4f, 0, -6), Quaternion.Euler(0, 180, 0));
        eyes.transform.localRotation = Quaternion.Euler(8, 0, 0);
        shots.Add(("held_alley", eyes.transform.position, eyes.transform.rotation, eyes.fieldOfView));
        story.player.transform.SetPositionAndRotation(new Vector3(-.6f, 0, -46), Quaternion.Euler(0, 180, 0));
        eyes.transform.localRotation = Quaternion.Euler(22, 0, 0);
        shots.Add(("held_garage", eyes.transform.position, eyes.transform.rotation, eyes.fieldOfView));
        var sticker = GameObject.Find("Oil change sticker");
        if (sticker != null)
        {
            var n = -sticker.transform.forward; var up = sticker.transform.up;
            var p = sticker.transform.position + n * .28f - up * .12f;
            shots.Add(("sticker", p, Quaternion.LookRotation(sticker.transform.position - p, Vector3.up), 40));
            var far = sticker.transform.position + n * .9f - up * .35f + Vector3.up * .3f;
            shots.Add(("sticker_bike", far, Quaternion.LookRotation(sticker.transform.position - far, Vector3.up), 50));
        }
        var phone = story.nganPhone.transform;
        var pp = phone.position + new Vector3(.25f, .35f, .2f);
        shots.Add(("phone_floor", pp, Quaternion.LookRotation(phone.position - pp), 45));
        foreach (var o in Object.FindObjectsByType<NightObserver>(FindObjectsSortMode.None).OrderBy(o => o.displayName))
        {
            var t = o.transform;
            var p = t.position + t.forward * 2.4f + Vector3.up * 1.35f;
            shots.Add(("person_" + Slug(o.displayName), p, Quaternion.LookRotation(t.position + Vector3.up * .85f - p), 50));
        }

        Shoot(cam, shots, dir, "Night");
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) l.enabled = false;
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)) v.enabled = false;
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.45f, .45f, .45f); RenderSettings.fog = false;
        foreach (var e in new[] { new Vector3(40, 30, 0), new Vector3(35, 200, 0), new Vector3(60, 110, 0) })
        {
            var l = new GameObject("Flat light").AddComponent<Light>(); l.type = LightType.Directional; l.intensity = .8f; l.transform.rotation = Quaternion.Euler(e);
        }
        Shoot(cam, shots, dir, "Flat");
        var clay = new Material(Shader.Find("Universal Render Pipeline/Lit")); clay.SetColor("_BaseColor", new Color(.55f, .55f, .55f)); clay.SetFloat("_Smoothness", .2f);
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r is MeshRenderer || r is SkinnedMeshRenderer) r.sharedMaterials = r.sharedMaterials.Select(_ => clay).ToArray();
        Shoot(cam, shots, dir, "Clay");
        AnimationMode.StopAnimationMode();
        Debug.Log("NIGHT1_PROPS_CAPTURE " + shots.Count + " -> " + dir);
        EditorApplication.Exit(0);
    }

    static string Slug(string s) => new string(s.Normalize(System.Text.NormalizationForm.FormD).Where(c => c < 128 && (char.IsLetterOrDigit(c) || c == ' ')).ToArray()).Trim().Replace(' ', '_').ToLowerInvariant();

    static void Pose(IEnumerable<Animator> animators)
    {
        AnimationMode.StartAnimationMode();
        foreach (var a in animators)
        {
            var clip = a.runtimeAnimatorController?.animationClips.FirstOrDefault(c => c.name.EndsWith("Idle_Loop"));
            if (clip == null) continue;
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(a.gameObject, clip, .6f);
            AnimationMode.EndSampling();
        }
    }

    static void Shoot(Camera cam, List<(string name, Vector3 pos, Quaternion rot, float fov)> shots, string dir, string prefix)
    {
        var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        foreach (var s in shots)
        {
            cam.transform.SetPositionAndRotation(s.pos, s.rot); cam.fieldOfView = s.fov;
            cam.targetTexture = rt; for (int k = 0; k < 8; k++) cam.Render(); RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes($"{dir}/{prefix}_{s.name}.png", tex.EncodeToPNG());
        }
        cam.targetTexture = null; RenderTexture.active = null;
    }
}
