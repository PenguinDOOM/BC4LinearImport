// Verifies BC4 postprocessor gating, guard behavior, and conversion expectations.
using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using BC4LinearImport.Editor;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Verifies BC4 postprocessor gating, guard behavior, and conversion expectations.
    /// </summary>
    [TestFixture]
    public class BC4LinearImportPostprocessorTests
    {
        /// <summary>
        /// Verifies that the postprocessor only supports the Phase 3 source extensions.
        /// </summary>
        [Test]
        public void SupportsAssetPath_AllowsOnlyPngAndJpegFamily()
        {
            Assert.That(BC4LinearTexturePostprocessor.SupportsAssetPath("Assets/Test/file.png"), Is.True);
            Assert.That(BC4LinearTexturePostprocessor.SupportsAssetPath("Assets/Test/file.jpg"), Is.True);
            Assert.That(BC4LinearTexturePostprocessor.SupportsAssetPath("Assets/Test/file.jpeg"), Is.True);
            Assert.That(BC4LinearTexturePostprocessor.SupportsAssetPath("Assets/Test/file.tga"), Is.False);
        }

        /// <summary>
        /// Verifies that the postprocessor order is explicit and runs after the VRCSDK default order.
        /// </summary>
        [Test]
        public void PostprocessOrder_IsExplicitlyAfterVrcsdkDefaultOrder()
        {
            Assert.That(
                BC4LinearTexturePostprocessor.DeclaredPostprocessOrder,
                Is.EqualTo(1),
                "The BC4 postprocessor must keep the explicit Phase 3 order value that runs after VRCSDK's default postprocess order.");
        }

        /// <summary>
        /// Verifies that postprocessing uses source bytes only for detection and converts the imported texture in place.
        /// </summary>
        [Test]
        public void OnPostprocessTexture_UsesSourceBytesOnlyForDetectionAndConvertsImportedTextureInPlace()
        {
            string methodSource = ExtractMethodSourceOrFail("private void OnPostprocessTexture(Texture2D texture)");

            Assert.That(
                methodSource,
                Does.Contain("BC4LinearColorDetector.Detect(assetPath, sourceBytes)"),
                "OnPostprocessTexture should keep source bytes only for detector input.");
            Assert.That(
                methodSource,
                Does.Contain("BC4LinearTextureConverter.ConvertInPlace(texture)"),
                "OnPostprocessTexture should linearize the already-postprocessed imported texture in place so VRCSDK-produced mip content is preserved.");
            Assert.That(
                methodSource.Contains("LoadReadableSourceTexture", StringComparison.Ordinal),
                Is.False,
                "OnPostprocessTexture must not rebuild a separate source texture after VRCSDK has already produced the final mip chain.");
            Assert.That(
                methodSource.Contains("CopyTexturePixels", StringComparison.Ordinal),
                Is.False,
                "OnPostprocessTexture must not overwrite the imported mip chain with pixels copied from a re-decoded source texture.");
        }

        /// <summary>
        /// Verifies that compute-path failures log a warning before the converter falls back to CPU.
        /// </summary>
        [Test]
        public void ConvertInPlace_ComputeFailurePath_LogsWarningBeforeCpuFallback()
        {
            string converterSource = File.ReadAllText(
                BC4LinearImportFixtureUtility.ResolveProjectPath("Assets/BC4LinearImport/Editor/BC4LinearTextureConverter.cs"));

            Assert.That(
                converterSource,
                Does.Contain("catch (Exception ex)"),
                "The compute conversion path should capture the exception so the fallback warning can retain failure context.");
            Assert.That(
                converterSource,
                Does.Contain("Debug.LogWarning("),
                "The compute conversion path should emit a warning before it falls back to the deterministic CPU path.");
        }

        /// <summary>
        /// Verifies that the classifier allows an explicit Standalone BC4 override.
        /// </summary>
        [Test]
        public void Classifier_AllowsExplicitStandaloneBc4Override()
        {
            BC4LinearImportTargetSnapshot snapshot = CreateSnapshot(
                defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                standalonePlatformFormat: TextureImporterFormat.BC4,
                isStandaloneOverrideActive: true,
                automaticStandaloneFormat: TextureImporterFormat.DXT1);

            Assert.That(BC4LinearImportTargeting.IsEligibleForBc4LinearImport(snapshot), Is.True);
        }

        /// <summary>
        /// Verifies that the classifier rejects a Default-panel BC4 appearance when the Standalone override is inactive.
        /// </summary>
        [Test]
        public void Classifier_RejectsDefaultPanelBc4Appearance_WhenStandaloneOverrideIsInactive()
        {
            BC4LinearImportTargetSnapshot snapshot = CreateSnapshot(
                defaultPlatformFormat: TextureImporterFormat.BC4,
                standalonePlatformFormat: TextureImporterFormat.AutomaticCompressed,
                isStandaloneOverrideActive: false,
                automaticStandaloneFormat: TextureImporterFormat.DXT1);

            Assert.That(BC4LinearImportTargeting.IsEligibleForBc4LinearImport(snapshot), Is.False);
        }

        /// <summary>
        /// Verifies that the classifier rejects an explicit Standalone non-BC4 override even when Default still appears as BC4.
        /// </summary>
        [Test]
        public void Classifier_RejectsExplicitStandaloneNonBc4Override()
        {
            BC4LinearImportTargetSnapshot snapshot = CreateSnapshot(
                defaultPlatformFormat: TextureImporterFormat.BC4,
                standalonePlatformFormat: TextureImporterFormat.DXT1,
                isStandaloneOverrideActive: true,
                automaticStandaloneFormat: TextureImporterFormat.DXT1);

            Assert.That(BC4LinearImportTargeting.IsEligibleForBc4LinearImport(snapshot), Is.False);
        }

        /// <summary>
        /// Verifies that the classifier allows the observed automatic Standalone BC4 path for the snapshot-based single-channel case.
        /// </summary>
        [Test]
        public void Classifier_AllowsObservedAutomaticStandaloneBc4Path_ForObservedSingleChannelCase()
        {
            BC4LinearImportTargetSnapshot snapshot = CreateSnapshot(
                defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                standalonePlatformFormat: TextureImporterFormat.AutomaticCompressed,
                isStandaloneOverrideActive: false,
                automaticStandaloneFormat: TextureImporterFormat.BC4,
                textureType: TextureImporterType.SingleChannel,
                singleChannelComponent: TextureImporterSingleChannelComponent.Red);

            Assert.That(BC4LinearImportTargeting.IsEligibleForBc4LinearImport(snapshot), Is.True);
        }

        /// <summary>
        /// Verifies that the observed automatic Standalone BC4 path remains accepted for the current observed-neutral Single Channel / Alpha case.
        /// </summary>
        [Test]
        public void UsesObservedAutomaticStandaloneBc4Path_AllowsObservedSingleChannelAlphaCase()
        {
            BC4LinearImportTargetSnapshot snapshot = CreateSnapshot(
                defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                standalonePlatformFormat: TextureImporterFormat.AutomaticCompressed,
                isStandaloneOverrideActive: false,
                automaticStandaloneFormat: TextureImporterFormat.BC4,
                textureType: TextureImporterType.SingleChannel,
                singleChannelComponent: TextureImporterSingleChannelComponent.Alpha);

            Assert.That(BC4LinearImportTargeting.UsesObservedAutomaticStandaloneBc4Path(snapshot), Is.True);
            Assert.That(BC4LinearImportTargeting.IsEligibleForBc4LinearImport(snapshot), Is.True);
        }

        /// <summary>
        /// Verifies that the classifier rejects the observed single-channel case when the automatic Standalone format is not BC4.
        /// </summary>
        [Test]
        public void Classifier_RejectsObservedSingleChannelCase_WhenAutomaticStandaloneFormatIsNotBc4()
        {
            BC4LinearImportTargetSnapshot snapshot = CreateSnapshot(
                defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                standalonePlatformFormat: TextureImporterFormat.AutomaticCompressed,
                isStandaloneOverrideActive: false,
                automaticStandaloneFormat: TextureImporterFormat.DXT1,
                textureType: TextureImporterType.SingleChannel,
                singleChannelComponent: TextureImporterSingleChannelComponent.Red);

            Assert.That(BC4LinearImportTargeting.IsEligibleForBc4LinearImport(snapshot), Is.False);
        }

        /// <summary>
        /// Verifies that import-time gating routes through the shared targeting classifier instead of the legacy format-only shortcut.
        /// </summary>
        [Test]
        public void ShouldProcessCurrentAsset_UsesSharedTargetingClassifier()
        {
            string methodSource = ExtractMethodSourceOrFail("private bool ShouldProcessCurrentAsset()");

            Assert.That(
                methodSource,
                Does.Contain("BC4LinearImportTargeting.IsEligibleForBc4LinearImport(textureImporter)"),
                "ShouldProcessCurrentAsset should route importer eligibility through the shared BC4 targeting classifier.");
            Assert.That(
                methodSource.Contains("IsBc4TargetFormat(", StringComparison.Ordinal),
                Is.False,
                "ShouldProcessCurrentAsset must not keep the legacy format-only BC4 shortcut inline.");
        }

        /// <summary>
        /// Verifies that the SessionState loop guard is one-shot per asset until released.
        /// </summary>
        [Test]
        public void SessionGuard_IsOneShotUntilReleased()
        {
            const string AssetPath = "Assets/Test/guarded.png";

            BC4LinearTexturePostprocessor.ReleaseSessionGuard(AssetPath);
            Assert.That(BC4LinearTexturePostprocessor.TryAcquireSessionGuard(AssetPath), Is.True);
            Assert.That(BC4LinearTexturePostprocessor.TryAcquireSessionGuard(AssetPath), Is.False);

            BC4LinearTexturePostprocessor.ReleaseSessionGuard(AssetPath);
            Assert.That(BC4LinearTexturePostprocessor.TryAcquireSessionGuard(AssetPath), Is.True);
            BC4LinearTexturePostprocessor.ReleaseSessionGuard(AssetPath);
        }

        /// <summary>
        /// Verifies that the converter falls back to the deterministic CPU path and converts every mip when compute is unavailable.
        /// </summary>
        [Test]
        public void ConvertInPlace_WhenComputeIsUnavailable_UsesCpuFallbackAndConvertsAllMips()
        {
            Texture2D texture = CreateMipmappedGrayscaleTexture();
            try
            {
                float expectedMip0 = QuantizeToRgba32(BC4LinearTextureConverter.ConvertSrgbChannelToLinear(texture.GetPixel(1, 0, 0).r));
                float expectedMip1 = QuantizeToRgba32(BC4LinearTextureConverter.ConvertSrgbChannelToLinear(texture.GetPixels(1)[0].r));

                BC4LinearTextureConversionResult result = BC4LinearTextureConverter.ConvertInPlace(
                    texture,
                    preferCompute: true,
                    supportsComputeOverride: false,
                    computeShader: null);

                Assert.That(result.Backend, Is.EqualTo(BC4LinearConversionBackend.CpuFallback));
                Assert.That(result.ConvertedMipCount, Is.EqualTo(texture.mipmapCount));
                Assert.That(texture.GetPixel(1, 0, 0).r, Is.EqualTo(expectedMip0).Within(0.0001f));
                Assert.That(texture.GetPixel(1, 0, 0).g, Is.EqualTo(expectedMip0).Within(0.0001f));
                Assert.That(texture.GetPixels(1)[0].r, Is.EqualTo(expectedMip1).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// Verifies that the converter prefers the compute backend when a compute shader is available and compute is supported.
        /// </summary>
        [Test]
        public void ConvertInPlace_WhenComputeIsSupported_PrefersComputeBackend()
        {
            ComputeShader computeShader = BC4LinearTextureConverter.LoadComputeShader();
            if (!BC4LinearTextureConverter.CanUseComputeBackend(computeShader))
            {
                Assert.Ignore("Compute shaders are not available in this Unity environment.");
            }

            Texture2D texture = CreateMipmappedGrayscaleTexture();
            try
            {
                BC4LinearTextureConversionResult result = BC4LinearTextureConverter.ConvertInPlace(
                    texture,
                    preferCompute: true,
                    supportsComputeOverride: true,
                    computeShader: computeShader);

                Assert.That(result.Backend, Is.EqualTo(BC4LinearConversionBackend.ComputeShader));
                Assert.That(result.ConvertedMipCount, Is.EqualTo(texture.mipmapCount));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// Creates a readable mipmapped grayscale texture for conversion tests.
        /// </summary>
        /// <returns>The configured test texture.</returns>
        private static Texture2D CreateMipmappedGrayscaleTexture()
        {
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, true, true);
            texture.SetPixels(new[]
            {
                new Color(0.0f, 0.0f, 0.0f, 1.0f),
                new Color(0.5f, 0.5f, 0.5f, 1.0f),
                new Color(1.0f, 1.0f, 1.0f, 1.0f),
                new Color(0.75f, 0.75f, 0.75f, 1.0f),
                new Color(0.25f, 0.25f, 0.25f, 1.0f),
                new Color(0.5f, 0.5f, 0.5f, 1.0f),
                new Color(0.75f, 0.75f, 0.75f, 1.0f),
                new Color(1.0f, 1.0f, 1.0f, 1.0f),
                new Color(0.0f, 0.0f, 0.0f, 1.0f),
                new Color(0.25f, 0.25f, 0.25f, 1.0f),
                new Color(0.5f, 0.5f, 0.5f, 1.0f),
                new Color(0.75f, 0.75f, 0.75f, 1.0f),
                new Color(1.0f, 1.0f, 1.0f, 1.0f),
                new Color(0.75f, 0.75f, 0.75f, 1.0f),
                new Color(0.5f, 0.5f, 0.5f, 1.0f),
                new Color(0.25f, 0.25f, 0.25f, 1.0f)
            }, 0);

            texture.SetPixels(new[]
            {
                new Color(0.5f, 0.5f, 0.5f, 1.0f),
                new Color(0.25f, 0.25f, 0.25f, 1.0f),
                new Color(0.75f, 0.75f, 0.75f, 1.0f),
                new Color(1.0f, 1.0f, 1.0f, 1.0f)
            }, 1);

            texture.SetPixels(new[]
            {
                new Color(0.5f, 0.5f, 0.5f, 1.0f)
            }, 2);

            texture.Apply(false, false);
            return texture;
        }

        /// <summary>
        /// Quantizes a normalized channel value to the precision stored by <see cref="TextureFormat.RGBA32"/>.
        /// </summary>
        /// <param name="value">The normalized channel value.</param>
        /// <returns>The quantized channel value.</returns>
        private static float QuantizeToRgba32(float value)
        {
            return Mathf.Round(Mathf.Clamp01(value) * 255.0f) / 255.0f;
        }

        /// <summary>
        /// Creates a BC4 targeting snapshot for classifier-focused tests.
        /// </summary>
        /// <param name="defaultPlatformFormat">The default-platform format.</param>
        /// <param name="standalonePlatformFormat">The Standalone-platform format.</param>
        /// <param name="isStandaloneOverrideActive"><see langword="true"/> when the Standalone override is active.</param>
        /// <param name="automaticStandaloneFormat">The automatic Standalone format.</param>
        /// <param name="textureType">The texture type.</param>
        /// <param name="singleChannelComponent">The single-channel component.</param>
        /// <returns>The configured snapshot.</returns>
        private static BC4LinearImportTargetSnapshot CreateSnapshot(
            TextureImporterFormat defaultPlatformFormat,
            TextureImporterFormat standalonePlatformFormat,
            bool isStandaloneOverrideActive,
            TextureImporterFormat automaticStandaloneFormat,
            TextureImporterType textureType = TextureImporterType.Default,
            TextureImporterSingleChannelComponent singleChannelComponent = TextureImporterSingleChannelComponent.Red)
        {
            return new BC4LinearImportTargetSnapshot(
                defaultPlatformFormat,
                standalonePlatformFormat,
                isStandaloneOverrideActive,
                automaticStandaloneFormat,
                textureType,
                singleChannelComponent,
                TextureImporterCompression.Compressed,
                50,
                TextureImporterCompression.Compressed,
                50);
        }

        /// <summary>
        /// Extracts a method body from the BC4 postprocessor source text.
        /// </summary>
        /// <param name="methodSignature">The method signature to locate.</param>
        /// <returns>The extracted method source.</returns>
        private static string ExtractMethodSourceOrFail(string methodSignature)
        {
            string source = File.ReadAllText(
                BC4LinearImportFixtureUtility.ResolveProjectPath("Assets/BC4LinearImport/Editor/BC4LinearTexturePostprocessor.cs"));

            int signatureIndex = source.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.That(
                signatureIndex,
                Is.GreaterThanOrEqualTo(0),
                $"Method signature not found in postprocessor source: {methodSignature}");

            int openingBraceIndex = source.IndexOf('{', signatureIndex);
            Assert.That(
                openingBraceIndex,
                Is.GreaterThan(signatureIndex),
                $"Opening brace not found for method: {methodSignature}");

            int depth = 0;
            for (int index = openingBraceIndex; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(signatureIndex, index - signatureIndex + 1);
                    }
                }
            }

            Assert.Fail($"Closing brace not found for method: {methodSignature}");
            return string.Empty;
        }
    }
}
