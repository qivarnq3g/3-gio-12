using UnityEditor;

public class ModelImportRules : AssetPostprocessor
{
    public const string ModelsRoot = "Assets/Art/Models/";
    public const string TexturesRoot = "Assets/Art/Textures/";
    public const string FpsArmsModel = ModelsRoot + "Characters/char_fps_arms.fbx";

    public static bool IsTiling(string path) => System.IO.Path.GetFileName(path).StartsWith("T_Tile");

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(TexturesRoot)) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.filterMode = UnityEngine.FilterMode.Trilinear;
        importer.anisoLevel = 8;
        importer.wrapMode = IsTiling(assetPath) ? UnityEngine.TextureWrapMode.Repeat : UnityEngine.TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (name.EndsWith("_MetallicSmoothness")) importer.sRGBTexture = false;
        if (name.EndsWith("_Normal")) { importer.textureType = TextureImporterType.NormalMap; importer.sRGBTexture = false; }
        if (name.EndsWith("Leaves")) { importer.alphaIsTransparency = true; importer.mipMapsPreserveCoverage = true; importer.alphaTestReferenceValue = .4f; }
    }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ModelsRoot)) return;
        var importer = (ModelImporter)assetImporter;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.bakeAxisConversion = false;
        // The first-person model carries the reference transform for pickup camera motion.
        importer.importCameras = assetPath == FpsArmsModel;
        importer.importLights = false;
        importer.importVisibility = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        // Preserve the explicit material remaps of the animated FPS model.
        if (assetPath == FpsArmsModel) return;
        importer.materialLocation = ModelImporterMaterialLocation.External;
        importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
        importer.materialSearch = ModelImporterMaterialSearch.Everywhere;
        if (assetPath.StartsWith(ModelsRoot + "Props/") || assetPath.StartsWith(ModelsRoot + "Environment/"))
        {
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
        }
    }
}
