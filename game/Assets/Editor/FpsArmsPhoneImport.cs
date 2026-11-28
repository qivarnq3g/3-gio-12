using UnityEditor;
using UnityEngine;

sealed class FpsArmsPhoneImport : AssetPostprocessor
{
    const string ModelPath = ModelImportRules.FpsArmsModel;
    const string PhonePath = "FPS_Arms_Rig/root/upperarm.R/forearm.R/hand.R/phone_socket.R/prop_ngan_phone";

    void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        if (assetPath != ModelPath) return;

        var visibility = EditorCurveBinding.DiscreteCurve(PhonePath, typeof(MeshRenderer), "m_Enabled");
        float enabled = clip.name == "HoldPhone" ? 1f : 0f;
        AnimationUtility.SetEditorCurve(clip, visibility, AnimationCurve.Constant(0f, clip.length, enabled));
    }
}
