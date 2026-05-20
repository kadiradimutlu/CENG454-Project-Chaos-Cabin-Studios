using System.IO;
using UnityEditor;
using UnityEngine;

public static class ConfigureMixamoAnimationImports
{
    private const string MixamoFolder = "Assets/Player_System/MixamoAnimations";

    [MenuItem("Tools/Configure Mixamo Animation Imports")]
    public static void Configure()
    {
        if (!AssetDatabase.IsValidFolder(MixamoFolder))
        {
            Debug.LogError("Missing folder: " + MixamoFolder);
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { MixamoFolder });

        if (guids.Length == 0)
        {
            Debug.LogError("No FBX model assets found in: " + MixamoFolder);
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer == null)
                continue;

            string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            bool isOneShot =
                fileName.Contains("jump") ||
                fileName.Contains("landing") ||
                fileName.Contains("land");

            bool shouldLoop = !isOneShot;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;

            if (clips == null || clips.Length == 0)
                clips = importer.clipAnimations;

            if (clips != null)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].name = Path.GetFileNameWithoutExtension(path);

                    clips[i].loopTime = shouldLoop;
                    clips[i].loopPose = shouldLoop;

                    clips[i].lockRootRotation = true;
                    clips[i].lockRootHeightY = true;
                    clips[i].lockRootPositionXZ = true;

                    clips[i].keepOriginalOrientation = false;
                    clips[i].keepOriginalPositionY = false;
                    clips[i].keepOriginalPositionXZ = false;

                    // This helps humanoid retargeting keep grounded poses from sinking.
                    clips[i].heightFromFeet = true;
                }

                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();

            Debug.Log($"Configured Mixamo import: {path} | Loop={shouldLoop}");
        }

        AssetDatabase.Refresh();
        Debug.Log("Mixamo animation import configuration completed.");
    }
}
