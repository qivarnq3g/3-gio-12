using UnityEditor;

public class ModelImportRules : AssetPostprocessor
{
    public const string ModelsRoot = "Assets/Art/Models/";

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
