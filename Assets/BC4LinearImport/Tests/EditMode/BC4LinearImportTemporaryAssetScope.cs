// Creates and cleans up temporary BC4 linear import test assets.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BC4LinearImport.Editor;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Creates and cleans up temporary BC4 linear import test assets.
    /// </summary>
    internal sealed class BC4LinearImportTemporaryAssetScope : IDisposable
    {
        private const string StandalonePlatformName = "Standalone";
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BC4LinearImportTemporaryAssetScope"/> class.
        /// </summary>
        internal BC4LinearImportTemporaryAssetScope()
        {
            RootAssetPath = $"Assets/BC4LinearImport/Tests/EditMode/__BC4LinearImportTemp_{Guid.NewGuid():N}";
            Directory.CreateDirectory(GetAbsolutePath(RootAssetPath));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        /// Gets the unique root asset path for this temporary scope.
        /// </summary>
        internal string RootAssetPath { get; }

        /// <summary>
        /// Creates a texture asset inside the scope and performs the initial import.
        /// </summary>
        /// <param name="fileName">The file name to create.</param>
        /// <param name="sourceBytes">The source bytes to write.</param>
        /// <returns>The asset-relative path.</returns>
        internal string CreateTextureAsset(string fileName, byte[] sourceBytes)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("A file name is required.", nameof(fileName));
            }

            if (sourceBytes == null || sourceBytes.Length == 0)
            {
                throw new ArgumentException("Source bytes are required.", nameof(sourceBytes));
            }

            string assetPath = $"{RootAssetPath}/{fileName}";
            string absolutePath = GetAbsolutePath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? throw new InvalidOperationException("Failed to resolve the temporary asset directory."));
            File.WriteAllBytes(absolutePath, sourceBytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return assetPath;
        }

        /// <summary>
        /// Configures the texture importer for an explicit Standalone BC4 override and reimports the asset.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <returns>The resolved texture importer.</returns>
        internal TextureImporter ConfigureBc4AndReimport(string assetPath)
        {
            return ConfigureExplicitStandaloneBc4AndReimport(assetPath);
        }

        /// <summary>
        /// Configures the texture importer for an explicit Standalone BC4 override and reimports the asset.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <returns>The resolved texture importer.</returns>
        internal TextureImporter ConfigureExplicitStandaloneBc4AndReimport(string assetPath)
        {
            return ConfigureExplicitStandaloneFormatAndReimport(assetPath, TextureImporterFormat.BC4);
        }

        /// <summary>
        /// Configures the texture importer for an explicit Standalone format override and reimports the asset.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <param name="standaloneFormat">The explicit Standalone format to apply.</param>
        /// <returns>The resolved texture importer.</returns>
        internal TextureImporter ConfigureExplicitStandaloneFormatAndReimport(string assetPath, TextureImporterFormat standaloneFormat)
        {
            TextureImporter textureImporter = GetTextureImporter(assetPath);
            PrepareImporterBaseline(textureImporter);

            TextureImporterPlatformSettings standaloneSettings = textureImporter.GetPlatformTextureSettings(StandalonePlatformName);
            standaloneSettings.name = StandalonePlatformName;
            standaloneSettings.overridden = true;
            standaloneSettings.format = standaloneFormat;
            standaloneSettings.maxTextureSize = 2048;
            textureImporter.SetPlatformTextureSettings(standaloneSettings);
            textureImporter.SaveAndReimport();
            return textureImporter;
        }

        /// <summary>
        /// Configures the texture importer so only the Default platform presents BC4 while Standalone override remains disabled.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <returns>The resolved texture importer.</returns>
        internal TextureImporter ConfigureStandaloneOverrideOffDefaultBc4AppearanceAndReimport(string assetPath)
        {
            TextureImporter textureImporter = GetTextureImporter(assetPath);
            PrepareImporterBaseline(textureImporter);

            TextureImporterPlatformSettings defaultSettings = textureImporter.GetDefaultPlatformTextureSettings();
            defaultSettings.format = TextureImporterFormat.BC4;
            defaultSettings.maxTextureSize = 2048;
            textureImporter.SetPlatformTextureSettings(defaultSettings);

            TextureImporterPlatformSettings standaloneSettings = textureImporter.GetPlatformTextureSettings(StandalonePlatformName);
            standaloneSettings.name = StandalonePlatformName;
            standaloneSettings.overridden = false;
            textureImporter.SetPlatformTextureSettings(standaloneSettings);

            textureImporter.SaveAndReimport();
            return textureImporter;
        }

        /// <summary>
        /// Configures the texture importer for the observed automatic Standalone single-channel BC4 candidate path and reimports the asset.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <param name="singleChannelComponent">The single-channel component to configure.</param>
        /// <returns>The resolved texture importer.</returns>
        internal TextureImporter ConfigureObservedAutomaticSingleChannelCaseAndReimport(
            string assetPath,
            TextureImporterSingleChannelComponent singleChannelComponent = TextureImporterSingleChannelComponent.Red)
        {
            TextureImporter textureImporter = GetTextureImporter(assetPath);
            PrepareImporterBaseline(textureImporter);
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            textureImporter.compressionQuality = 100;
            textureImporter.textureType = TextureImporterType.SingleChannel;

            TextureImporterSettings importerSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(importerSettings);
            importerSettings.singleChannelComponent = singleChannelComponent;
            textureImporter.SetTextureSettings(importerSettings);

            TextureImporterPlatformSettings standaloneSettings = textureImporter.GetPlatformTextureSettings(StandalonePlatformName);
            standaloneSettings.name = StandalonePlatformName;
            standaloneSettings.overridden = false;
            textureImporter.SetPlatformTextureSettings(standaloneSettings);

            textureImporter.SaveAndReimport();
            return textureImporter;
        }

        /// <summary>
        /// Captures the current BC4 targeting snapshot for the asset at the requested path.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <returns>The targeting snapshot.</returns>
        internal BC4LinearImportTargetSnapshot CaptureTargetingSnapshot(string assetPath)
        {
            return BC4LinearImportTargeting.BuildSnapshot(GetTextureImporter(assetPath));
        }

        /// <summary>
        /// Loads the imported texture asset at the requested path.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <returns>The imported texture.</returns>
        internal Texture2D LoadTexture(string assetPath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                throw new InvalidOperationException($"Failed to load the imported texture at '{assetPath}'.");
            }

            return texture;
        }

        /// <summary>
        /// Resolves the texture importer at the requested path.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <returns>The texture importer.</returns>
        internal TextureImporter GetTextureImporter(string assetPath)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter == null)
            {
                throw new InvalidOperationException($"Failed to resolve the texture importer at '{assetPath}'.");
            }

            return textureImporter;
        }

        /// <summary>
        /// Resolves the absolute filesystem path for the given asset-relative path.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <returns>The absolute filesystem path.</returns>
        internal string GetAbsolutePath(string assetPath)
        {
            return BC4LinearImportFixtureUtility.ResolveProjectPath(assetPath);
        }

        /// <summary>
        /// Deletes the temporary asset scope and any generated files.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            CleanupScope();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Deletes the scope through Unity first and then falls back to filesystem cleanup if needed.
        /// </summary>
        private void CleanupScope()
        {
            string absoluteRootPath = GetAbsolutePath(RootAssetPath);

            AssetDatabase.DeleteAsset(RootAssetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (Directory.Exists(absoluteRootPath))
            {
                FileUtil.DeleteFileOrDirectory(absoluteRootPath);
            }

            string metaPath = absoluteRootPath + ".meta";
            if (File.Exists(metaPath))
            {
                FileUtil.DeleteFileOrDirectory(metaPath);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        /// Applies the shared baseline importer settings used by BC4 targeting integration tests.
        /// </summary>
        /// <param name="textureImporter">The texture importer to prepare.</param>
        private static void PrepareImporterBaseline(TextureImporter textureImporter)
        {
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.isReadable = true;
            textureImporter.mipmapEnabled = true;
            textureImporter.mipmapFilter = TextureImporterMipFilter.KaiserFilter;

            TextureImporterSettings importerSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(importerSettings);
            importerSettings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
            textureImporter.SetTextureSettings(importerSettings);
        }
    }
}
