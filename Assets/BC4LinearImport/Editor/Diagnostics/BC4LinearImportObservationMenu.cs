// Emits one-off BC4 importer targeting observations from the BC4 diagnostics menu.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BC4LinearImport.Editor.Diagnostics
{
    /// <summary>
    /// Emits one-off BC4 importer targeting observations from the BC4 diagnostics menu.
    /// </summary>
    internal static class BC4LinearImportObservationMenu
    {
        private const string RootAssetPath = "Assets/__BC4LinearImportObservation";
        private const string AssetPath = RootAssetPath + "/observation.png";
        private const string StandalonePlatformName = "Standalone";
        private const string MenuPath = "Tools/BC4 Linear Import/Diagnostics/Observe Targeting";

        /// <summary>
        /// Creates a temporary texture asset, logs importer targeting observations, and cleans up the temporary asset.
        /// </summary>
        [MenuItem(MenuPath)]
        internal static void Observe()
        {
            try
            {
                PrepareAsset();
                TextureImporter textureImporter = GetTextureImporter();
                LogObservation("Initial", textureImporter);

                TextureImporterSettings redSettings = new TextureImporterSettings();
                textureImporter.ReadTextureSettings(redSettings);
                textureImporter.textureType = TextureImporterType.SingleChannel;
                redSettings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                textureImporter.SetTextureSettings(redSettings);
                textureImporter.SaveAndReimport();
                textureImporter = GetTextureImporter();
                LogObservation("SingleChannelRed", textureImporter);

                TextureImporterSettings alphaSettings = new TextureImporterSettings();
                textureImporter.ReadTextureSettings(alphaSettings);
                textureImporter.textureType = TextureImporterType.SingleChannel;
                alphaSettings.singleChannelComponent = TextureImporterSingleChannelComponent.Alpha;
                textureImporter.SetTextureSettings(alphaSettings);
                textureImporter.SaveAndReimport();
                textureImporter = GetTextureImporter();
                LogObservation("SingleChannelAlpha", textureImporter);
            }
            finally
            {
                CleanupAsset();
            }
        }

        /// <summary>
        /// Creates the temporary observation asset and imports it.
        /// </summary>
        private static void PrepareAsset()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", RootAssetPath));

            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false, true);
            try
            {
                Color[] pixels = new Color[16];
                for (int index = 0; index < pixels.Length; index++)
                {
                    float value = index / 15.0f;
                    pixels[index] = new Color(value, value, value, 1.0f);
                }

                texture.SetPixels(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(GetAbsolutePath(AssetPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// Deletes the temporary observation asset tree.
        /// </summary>
        private static void CleanupAsset()
        {
            AssetDatabase.DeleteAsset(RootAssetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        /// Gets the temporary observation texture importer.
        /// </summary>
        /// <returns>The texture importer.</returns>
        private static TextureImporter GetTextureImporter()
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(AssetPath) as TextureImporter;
            if (textureImporter == null)
            {
                throw new InvalidOperationException("Failed to resolve the observation texture importer.");
            }

            return textureImporter;
        }

        /// <summary>
        /// Logs the current importer targeting state.
        /// </summary>
        /// <param name="label">The observation label.</param>
        /// <param name="textureImporter">The texture importer to inspect.</param>
        private static void LogObservation(string label, TextureImporter textureImporter)
        {
            TextureImporterPlatformSettings defaultSettings = textureImporter.GetDefaultPlatformTextureSettings();
            TextureImporterPlatformSettings standaloneSettings = textureImporter.GetPlatformTextureSettings(StandalonePlatformName);
            TextureImporterSettings importerSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(importerSettings);

            Debug.Log(
                $"[BC4Observation] label={label}; defaultFormat={defaultSettings.format}; standaloneFormat={standaloneSettings.format}; standaloneOverride={standaloneSettings.overridden}; automaticStandaloneFormat={textureImporter.GetAutomaticFormat(StandalonePlatformName)}; textureType={textureImporter.textureType}; singleChannelComponent={importerSettings.singleChannelComponent}; textureCompression={textureImporter.textureCompression}; compressionQuality={textureImporter.compressionQuality}");
        }

        /// <summary>
        /// Resolves the absolute filesystem path for the asset-relative path.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <returns>The absolute filesystem path.</returns>
        private static string GetAbsolutePath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }
    }
}
