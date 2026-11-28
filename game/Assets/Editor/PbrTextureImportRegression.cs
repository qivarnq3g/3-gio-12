using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PbrTextureImportRegression
{
    const string Root = "Assets/Art/TexturesPBR";

    public static void Run()
    {
        var failures = new List<string>();
        int scanned = 0;
        int normals = 0;
        int masks = 0;
        string tempFolder = null;
        try
        {
            if (!AssetDatabase.IsValidFolder(Root))
            {
                failures.Add("Missing folder " + Root);
            }
            else
            {
                var paths = new HashSet<string>(StringComparer.Ordinal);
                foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { Root }))
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
                var sorted = new List<string>(paths);
                sorted.Sort(StringComparer.Ordinal);

                string sampleNormal = null;
                string sampleMask = null;
                foreach (var path in sorted)
                {
                    scanned++;
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (name.EndsWith("_normal"))
                    {
                        normals++;
                        if (sampleNormal == null) sampleNormal = path;
                        ValidateTexture(path, failures);
                    }
                    else if (name.EndsWith("_mask"))
                    {
                        masks++;
                        if (sampleMask == null) sampleMask = path;
                        ValidateTexture(path, failures);
                    }
                }

                if (sampleNormal == null) failures.Add("No *_normal texture found under " + Root);
                if (sampleMask == null) failures.Add("No *_mask texture found under " + Root);

                if (sampleNormal != null && sampleMask != null)
                {
                    string folderGuid = AssetDatabase.CreateFolder(Root, "TempImportCheck_" + Guid.NewGuid().ToString("N"));
                    tempFolder = AssetDatabase.GUIDToAssetPath(folderGuid);
                    if (string.IsNullOrEmpty(tempFolder))
                    {
                        failures.Add("Failed to create temporary folder under " + Root);
                    }
                    else
                    {
                        string dstNormal = $"{tempFolder}/probe_normal{Path.GetExtension(sampleNormal)}";
                        string dstMask = $"{tempFolder}/probe_mask{Path.GetExtension(sampleMask)}";
                        File.Copy(DiskPath(sampleNormal), DiskPath(dstNormal));
                        File.Copy(DiskPath(sampleMask), DiskPath(dstMask));
                        AssetDatabase.ImportAsset(dstNormal, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                        AssetDatabase.ImportAsset(dstMask, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                        ValidateTexture(dstNormal, failures);
                        ValidateTexture(dstMask, failures);
                    }
                }
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            failures.Add(error.Message);
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempFolder) && AssetDatabase.IsValidFolder(tempFolder) && !AssetDatabase.DeleteAsset(tempFolder))
                failures.Add("Failed to delete temporary folder " + tempFolder);
        }

        foreach (var failure in failures)
            Debug.LogError($"[PbrTextureImportRegression] {failure}");
        if (failures.Count == 0)
            Debug.Log($"[PbrTextureImportRegression] PASS scanned={scanned} normals={normals} masks={masks}");
        else
            Debug.LogError($"[PbrTextureImportRegression] FAIL scanned={scanned} normals={normals} masks={masks} failures={failures.Count}");
        EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
    }

    static void ValidateTexture(string path, List<string> failures)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            failures.Add($"{path}: missing TextureImporter");
            return;
        }
        var name = Path.GetFileNameWithoutExtension(path);
        if (name.EndsWith("_normal") && (importer.textureType != TextureImporterType.NormalMap || importer.sRGBTexture))
            failures.Add($"{path}: expected NormalMap with sRGB off, got textureType={importer.textureType} sRGB={importer.sRGBTexture}");
        if (name.EndsWith("_mask") && importer.sRGBTexture)
            failures.Add($"{path}: expected sRGB off for mask texture");
    }

    static string DiskPath(string assetPath) =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));
}
