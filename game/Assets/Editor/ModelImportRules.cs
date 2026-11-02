using UnityEditor;

public class ModelImportRules : AssetPostprocessor
{
    public const string ModelsRoot = "Assets/Art/Models/";
    public const string TexturesRoot = "Assets/Art/Textures/";

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
    }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ModelsRoot)) return;
        var importer = (ModelImporter)assetImporter;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.bakeAxisConversion = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
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
