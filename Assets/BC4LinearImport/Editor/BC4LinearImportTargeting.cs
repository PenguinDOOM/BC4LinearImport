// Captures BC4 import targeting snapshots and exposes the future shared eligibility seam.
using System;
using UnityEditor;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Captures the plain importer state that the shared BC4 targeting classifier will evaluate.
    /// </summary>
    /// <remarks>
    /// Some snapshot members are intentionally retained as raw observed importer state even when the current Phase 2 classifier does not consume them directly.
    /// Keeping those observations available supports future classifier refinements and diagnostics without re-reading Unity importer APIs.
    /// </remarks>
    public readonly struct BC4LinearImportTargetSnapshot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BC4LinearImportTargetSnapshot"/> struct.
        /// </summary>
        /// <param name="defaultPlatformFormat">The resolved default-platform format.</param>
        /// <param name="standalonePlatformFormat">The resolved Standalone-platform format.</param>
        /// <param name="isStandaloneOverrideActive"><see langword="true"/> when the Standalone override is active.</param>
        /// <param name="automaticStandaloneFormat">The resolved automatic Standalone format.</param>
        /// <param name="textureType">The importer texture type.</param>
        /// <param name="singleChannelComponent">The configured single-channel component.</param>
        /// <param name="defaultTextureCompression">The default-platform texture compression mode.</param>
        /// <param name="defaultCompressionQuality">The default-platform compression quality.</param>
        /// <param name="standaloneTextureCompression">The Standalone-platform texture compression mode.</param>
        /// <param name="standaloneCompressionQuality">The Standalone-platform compression quality.</param>
        public BC4LinearImportTargetSnapshot(
            TextureImporterFormat defaultPlatformFormat,
            TextureImporterFormat standalonePlatformFormat,
            bool isStandaloneOverrideActive,
            TextureImporterFormat automaticStandaloneFormat,
            TextureImporterType textureType,
            TextureImporterSingleChannelComponent singleChannelComponent,
            TextureImporterCompression defaultTextureCompression,
            int defaultCompressionQuality,
            TextureImporterCompression standaloneTextureCompression,
            int standaloneCompressionQuality)
        {
            DefaultPlatformFormat = defaultPlatformFormat;
            StandalonePlatformFormat = standalonePlatformFormat;
            IsStandaloneOverrideActive = isStandaloneOverrideActive;
            AutomaticStandaloneFormat = automaticStandaloneFormat;
            TextureType = textureType;
            SingleChannelComponent = singleChannelComponent;
            DefaultTextureCompression = defaultTextureCompression;
            DefaultCompressionQuality = defaultCompressionQuality;
            StandaloneTextureCompression = standaloneTextureCompression;
            StandaloneCompressionQuality = standaloneCompressionQuality;
        }

        /// <summary>
        /// Gets the resolved default-platform format.
        /// </summary>
        public TextureImporterFormat DefaultPlatformFormat { get; }

        /// <summary>
        /// Gets the resolved Standalone-platform format.
        /// </summary>
        public TextureImporterFormat StandalonePlatformFormat { get; }

        /// <summary>
        /// Gets a value indicating whether the Standalone override is active.
        /// </summary>
        public bool IsStandaloneOverrideActive { get; }

        /// <summary>
        /// Gets the resolved automatic Standalone format.
        /// </summary>
        public TextureImporterFormat AutomaticStandaloneFormat { get; }

        /// <summary>
        /// Gets the importer texture type.
        /// </summary>
        public TextureImporterType TextureType { get; }

        /// <summary>
        /// Gets the configured single-channel component.
        /// </summary>
        public TextureImporterSingleChannelComponent SingleChannelComponent { get; }

        /// <summary>
        /// Gets the default-platform texture compression mode.
        /// </summary>
        public TextureImporterCompression DefaultTextureCompression { get; }

        /// <summary>
        /// Gets the default-platform compression quality.
        /// </summary>
        public int DefaultCompressionQuality { get; }

        /// <summary>
        /// Gets the Standalone-platform texture compression mode.
        /// </summary>
        public TextureImporterCompression StandaloneTextureCompression { get; }

        /// <summary>
        /// Gets the Standalone-platform compression quality.
        /// </summary>
        public int StandaloneCompressionQuality { get; }
    }

    /// <summary>
    /// Builds BC4 targeting snapshots and exposes the shared eligibility seam.
    /// </summary>
    public static class BC4LinearImportTargeting
    {
        private const string StandalonePlatformName = "Standalone";

        /// <summary>
        /// Determines whether the provided texture importer qualifies for BC4 linear import processing.
        /// </summary>
        /// <param name="textureImporter">The texture importer to inspect.</param>
        /// <returns><see langword="true"/> when the importer qualifies for BC4 linear import processing; otherwise, <see langword="false"/>.</returns>
        public static bool IsEligibleForBc4LinearImport(TextureImporter textureImporter)
        {
            if (textureImporter == null)
            {
                throw new ArgumentNullException(nameof(textureImporter));
            }

            BC4LinearImportTargetSnapshot snapshot = BuildSnapshot(textureImporter);
            return IsEligibleForBc4LinearImport(snapshot);
        }

        /// <summary>
        /// Builds a plain-data targeting snapshot from the provided texture importer.
        /// </summary>
        /// <param name="textureImporter">The texture importer to inspect.</param>
        /// <returns>The captured targeting snapshot.</returns>
        public static BC4LinearImportTargetSnapshot BuildSnapshot(TextureImporter textureImporter)
        {
            if (textureImporter == null)
            {
                throw new ArgumentNullException(nameof(textureImporter));
            }

            TextureImporterPlatformSettings defaultPlatformSettings = textureImporter.GetDefaultPlatformTextureSettings();
            TextureImporterPlatformSettings standalonePlatformSettings = textureImporter.GetPlatformTextureSettings(StandalonePlatformName);
            TextureImporterSettings textureImporterSettings = ReadTextureImporterSettings(textureImporter);

            return new BC4LinearImportTargetSnapshot(
                defaultPlatformSettings.format,
                standalonePlatformSettings.format,
                standalonePlatformSettings.overridden,
                textureImporter.GetAutomaticFormat(StandalonePlatformName),
                textureImporter.textureType,
                textureImporterSettings.singleChannelComponent,
                textureImporter.textureCompression,
                textureImporter.compressionQuality,
                standalonePlatformSettings.textureCompression,
                standalonePlatformSettings.compressionQuality);
        }

        /// <summary>
        /// Reads the importer settings required by the future shared classifier.
        /// </summary>
        /// <param name="textureImporter">The texture importer to inspect.</param>
        /// <returns>The populated importer settings.</returns>
        public static TextureImporterSettings ReadTextureImporterSettings(TextureImporter textureImporter)
        {
            if (textureImporter == null)
            {
                throw new ArgumentNullException(nameof(textureImporter));
            }

            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            return textureImporterSettings;
        }

        /// <summary>
        /// Determines whether the snapshot contains an explicit Standalone BC4 override.
        /// </summary>
        /// <param name="snapshot">The snapshot to inspect.</param>
        /// <returns><see langword="true"/> when the snapshot encodes an explicit Standalone BC4 override; otherwise, <see langword="false"/>.</returns>
        public static bool HasExplicitStandaloneBc4Override(BC4LinearImportTargetSnapshot snapshot)
        {
            return snapshot.IsStandaloneOverrideActive
                   && snapshot.StandalonePlatformFormat == TextureImporterFormat.BC4;
        }

        /// <summary>
        /// Determines whether the snapshot appears to follow the observed automatic Standalone BC4 path.
        /// </summary>
        /// <param name="snapshot">The snapshot to inspect.</param>
        /// <returns><see langword="true"/> when the snapshot matches the observed automatic Standalone BC4 path; otherwise, <see langword="false"/>.</returns>
        public static bool UsesObservedAutomaticStandaloneBc4Path(BC4LinearImportTargetSnapshot snapshot)
        {
            return !snapshot.IsStandaloneOverrideActive
                   && snapshot.AutomaticStandaloneFormat == TextureImporterFormat.BC4
                   && snapshot.TextureType == TextureImporterType.SingleChannel
                   && UsesObservedSingleChannelComponent(snapshot);
        }

        /// <summary>
        /// Determines whether the snapshot qualifies for BC4 linear import processing.
        /// </summary>
        /// <param name="snapshot">The snapshot to inspect.</param>
        /// <returns><see langword="true"/> when the snapshot qualifies for BC4 linear import processing; otherwise, <see langword="false"/>.</returns>
        public static bool IsEligibleForBc4LinearImport(BC4LinearImportTargetSnapshot snapshot)
        {
            return HasExplicitStandaloneBc4Override(snapshot)
                   || UsesObservedAutomaticStandaloneBc4Path(snapshot);
        }

        /// <summary>
        /// Determines whether the snapshot uses an observed single-channel component supported by the current Unity observation pass.
        /// </summary>
        /// <param name="snapshot">The snapshot to inspect.</param>
        /// <returns><see langword="true"/> when the snapshot uses an observed supported single-channel component; otherwise, <see langword="false"/>.</returns>
        private static bool UsesObservedSingleChannelComponent(BC4LinearImportTargetSnapshot snapshot)
        {
            return snapshot.SingleChannelComponent == TextureImporterSingleChannelComponent.Red
                   || snapshot.SingleChannelComponent == TextureImporterSingleChannelComponent.Alpha;
        }
    }
}
