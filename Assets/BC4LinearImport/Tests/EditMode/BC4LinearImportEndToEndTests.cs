// Verifies end-to-end BC4 linear import flow, exclusion gating, and cleanup boundaries.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using BC4LinearImport.Editor;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Verifies end-to-end BC4 linear import flow, exclusion gating, and cleanup boundaries.
    /// </summary>
    [TestFixture]
    public class BC4LinearImportEndToEndTests
    {
        private bool originalProjectWideEnabled;
        private List<string> originalExcludedAssetPaths;
        private BC4LinearImportTemporaryAssetScope activeScope;

        /// <summary>
        /// Captures the shared project settings before each integration test runs.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            originalProjectWideEnabled = settings.ProjectWideEnabled;
            originalExcludedAssetPaths = new List<string>(settings.ExcludedAssetPaths);

            settings.ProjectWideEnabled = true;
            settings.SetExcludedAssetPaths(Array.Empty<string>());
            settings.SaveSettings();
        }

        /// <summary>
        /// Restores the shared project settings and disposes any temporary asset scope.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            activeScope?.Dispose();
            activeScope = null;

            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = originalProjectWideEnabled;
            settings.SetExcludedAssetPaths(originalExcludedAssetPaths);
            settings.SaveSettings();
        }

        /// <summary>
        /// Verifies that supported PNG and JPEG-family assets run through the explicit Standalone BC4 import flow, preserve Kaiser filtering, and leave no stale loop guard.
        /// </summary>
        /// <param name="extension">The source extension under test.</param>
        [TestCase(".png")]
        [TestCase(".jpg")]
        [TestCase(".jpeg")]
        public void Import_ExplicitStandaloneBc4Override_ConvertsAndReleasesLoopGuard(string extension)
        {
            byte[] sourceBytes = extension == ".png"
                ? BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes()
                : BC4LinearImportFixtureUtility.CreateSrgbMetadataRampJpegBytes();

            activeScope = new BC4LinearImportTemporaryAssetScope();
            ResetLastProcessReport();
            string assetPath = activeScope.CreateTextureAsset($"srgb-ramp{extension}", sourceBytes);

            TextureImporter textureImporter = activeScope.ConfigureExplicitStandaloneBc4AndReimport(assetPath);
            BC4LinearImportTargetSnapshot snapshot = activeScope.CaptureTargetingSnapshot(assetPath);
            object report = GetLastProcessReport();

            Assert.That(textureImporter.mipmapFilter, Is.EqualTo(TextureImporterMipFilter.KaiserFilter));
            Assert.That(BC4LinearImportTargeting.HasExplicitStandaloneBc4Override(snapshot), Is.True);
            AssertReportValue(report, "AssetPath", assetPath);
            AssertReportValue(report, "DecisionKind", BC4LinearColorDecisionKind.ConvertToLinear);
            AssertReportValue(report, "DecisionSource", BC4LinearColorDecisionSource.Metadata);
            AssertReportValue(report, "Converted", true);
            Assert.That(ReadReportValue<BC4LinearConversionBackend>(report, "ConversionBackend"), Is.Not.EqualTo(BC4LinearConversionBackend.None));
            Assert.That(ReadReportValue<int>(report, "ConvertedMipCount"), Is.GreaterThan(0));

            ResetLastProcessReport();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            report = GetLastProcessReport();
            AssertReportValue(report, "AssetPath", assetPath);
            AssertReportValue(report, "DecisionKind", BC4LinearColorDecisionKind.ConvertToLinear);

            AssertLoopGuardReleased(assetPath);
        }

        /// <summary>
        /// Verifies that a visible Default-panel BC4 appearance does not convert when the Standalone override remains disabled.
        /// </summary>
        [Test]
        public void Import_StandaloneOverrideOff_DefaultPanelBc4Appearance_DoesNotConvert()
        {
            activeScope = new BC4LinearImportTemporaryAssetScope();
            ResetLastProcessReport();
            string assetPath = activeScope.CreateTextureAsset("default-only-bc4-appearance.png", BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes());

            LogAssert.Expect(
                LogType.Error,
                "Selected texture format 'R Compressed BC4' for platform 'DefaultTexturePlatform' is not valid with the current texture type 'Default'.");

            TextureImporter textureImporter = activeScope.ConfigureStandaloneOverrideOffDefaultBc4AppearanceAndReimport(assetPath);
            BC4LinearImportTargetSnapshot snapshot = activeScope.CaptureTargetingSnapshot(assetPath);
            object report = GetLastProcessReport();

            Assert.That(textureImporter.mipmapFilter, Is.EqualTo(TextureImporterMipFilter.KaiserFilter));
            Assert.That(snapshot.DefaultPlatformFormat, Is.EqualTo(TextureImporterFormat.BC4));
            Assert.That(snapshot.IsStandaloneOverrideActive, Is.False);
            Assert.That(snapshot.TextureType, Is.EqualTo(TextureImporterType.Default));
            Assert.That(BC4LinearImportTargeting.IsEligibleForBc4LinearImport(snapshot), Is.False);
            AssertProcessReportWasNotWritten(report);
            AssertLoopGuardReleased(assetPath);
        }

        /// <summary>
        /// Verifies that an explicit Standalone non-BC4 override does not convert.
        /// </summary>
        [Test]
        public void Import_ExplicitStandaloneNonBc4Override_DoesNotConvert()
        {
            activeScope = new BC4LinearImportTemporaryAssetScope();
            ResetLastProcessReport();
            string assetPath = activeScope.CreateTextureAsset("explicit-non-bc4.png", BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes());

            TextureImporter textureImporter = activeScope.ConfigureExplicitStandaloneFormatAndReimport(assetPath, TextureImporterFormat.RGBA32);
            BC4LinearImportTargetSnapshot snapshot = activeScope.CaptureTargetingSnapshot(assetPath);
            object report = GetLastProcessReport();

            Assert.That(textureImporter.mipmapFilter, Is.EqualTo(TextureImporterMipFilter.KaiserFilter));
            Assert.That(snapshot.IsStandaloneOverrideActive, Is.True);
            Assert.That(snapshot.StandalonePlatformFormat, Is.EqualTo(TextureImporterFormat.RGBA32));
            Assert.That(BC4LinearImportTargeting.IsEligibleForBc4LinearImport(snapshot), Is.False);
            AssertProcessReportWasNotWritten(report);
            AssertLoopGuardReleased(assetPath);
        }

        /// <summary>
        /// Verifies that the observed automatic Standalone BC4 path converts only when the current Unity environment exposes it deterministically.
        /// </summary>
        [Test]
        public void Import_ObservedAutomaticStandaloneBc4Path_ConvertsWhenEnvironmentSupportsObservation()
        {
            activeScope = new BC4LinearImportTemporaryAssetScope();
            ResetLastProcessReport();
            string assetPath = activeScope.CreateTextureAsset("observed-automatic-single-channel.png", BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes());

            TextureImporter textureImporter = activeScope.ConfigureObservedAutomaticSingleChannelCaseAndReimport(assetPath);
            BC4LinearImportTargetSnapshot snapshot = activeScope.CaptureTargetingSnapshot(assetPath);
            if (!BC4LinearImportTargeting.UsesObservedAutomaticStandaloneBc4Path(snapshot))
            {
                Assert.Ignore($"Automatic Standalone BC4 observation is not available in this Unity environment. AutomaticFormat={snapshot.AutomaticStandaloneFormat}; TextureType={snapshot.TextureType}; SingleChannelComponent={snapshot.SingleChannelComponent}; StandaloneOverride={snapshot.IsStandaloneOverrideActive}.");
            }

            object report = GetLastProcessReport();

            Assert.That(textureImporter.mipmapFilter, Is.EqualTo(TextureImporterMipFilter.KaiserFilter));
            Assert.That(snapshot.IsStandaloneOverrideActive, Is.False);
            Assert.That(snapshot.TextureType, Is.EqualTo(TextureImporterType.SingleChannel));
            Assert.That(ReadReportValue<string>(report, "AssetPath"), Is.EqualTo(assetPath));
            AssertReportValue(report, "DecisionKind", BC4LinearColorDecisionKind.ConvertToLinear);
            AssertReportValue(report, "DecisionSource", BC4LinearColorDecisionSource.Metadata);
            AssertReportValue(report, "Converted", true);
            Assert.That(ReadReportValue<BC4LinearConversionBackend>(report, "ConversionBackend"), Is.Not.EqualTo(BC4LinearConversionBackend.None));
            Assert.That(ReadReportValue<int>(report, "ConvertedMipCount"), Is.GreaterThan(0));
            AssertLoopGuardReleased(assetPath);
        }

        /// <summary>
        /// Verifies that the full import flow keeps embedded-profile evidence ahead of conflicting JPEG metadata.
        /// </summary>
        [Test]
        public void Import_ConflictingJpegProfileAndMetadata_HonorsEmbeddedProfileFirst()
        {
            byte[] sourceBytes = BC4LinearImportFixtureUtility.CreateLinearProfileOverridesMetadataJpegBytes();
            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect("Assets/Test/conflict.jpg", sourceBytes);

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.SkipConversion));
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.EmbeddedProfile));

            activeScope = new BC4LinearImportTemporaryAssetScope();
            ResetLastProcessReport();
            string assetPath = activeScope.CreateTextureAsset("profile-overrides.jpg", sourceBytes);

            TextureImporter textureImporter = activeScope.ConfigureExplicitStandaloneBc4AndReimport(assetPath);
            object report = GetLastProcessReport();

            Assert.That(textureImporter.mipmapFilter, Is.EqualTo(TextureImporterMipFilter.KaiserFilter));
            AssertReportValue(report, "AssetPath", assetPath);
            AssertReportValue(report, "DecisionKind", BC4LinearColorDecisionKind.SkipConversion);
            AssertReportValue(report, "DecisionSource", BC4LinearColorDecisionSource.EmbeddedProfile);
            AssertReportValue(report, "SourceMarker", "ICC APP2");
            AssertReportValue(report, "Converted", false);
            AssertReportValue(report, "ConvertedMipCount", 0);
            AssertLoopGuardReleased(assetPath);
        }

        /// <summary>
        /// Verifies that the temporary asset scope deletes generated assets even when an exception interrupts the test flow.
        /// </summary>
        [Test]
        public void TemporaryAssetScope_CleansUpAssetsAfterFailurePath()
        {
            string rootAssetPath = null;
            string absoluteRootPath = null;

            try
            {
                using var scope = new BC4LinearImportTemporaryAssetScope();
                rootAssetPath = scope.RootAssetPath;
                absoluteRootPath = scope.GetAbsolutePath(rootAssetPath);
                scope.CreateTextureAsset("failure-path.png", BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes());
                throw new InvalidOperationException("Simulated integration failure.");
            }
            catch (InvalidOperationException)
            {
            }

            Assert.That(rootAssetPath, Is.Not.Null.And.Not.Empty);
            Assert.That(AssetDatabase.IsValidFolder(rootAssetPath), Is.False);
            Assert.That(Directory.Exists(absoluteRootPath), Is.False);
        }

        /// <summary>
        /// Verifies that a canonicalized exact exclusion path stops import-time processing even when the importer remains BC4-eligible.
        /// </summary>
        [Test]
        public void Import_ExcludedCanonicalAssetPath_SkipsProcessing()
        {
            activeScope = new BC4LinearImportTemporaryAssetScope();
            string assetPath = $"{activeScope.RootAssetPath}/excluded-mask.png";

            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.SetExcludedAssetPaths(new[] { $"  {assetPath.Replace('/', '\\')}  " });
            settings.SaveSettings();

            ResetLastProcessReport();
            activeScope.CreateTextureAsset("excluded-mask.png", BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes());

            // Creating the asset performs an initial import, so clear that no-op report before verifying the explicit BC4 reimport.
            ResetLastProcessReport();
            TextureImporter textureImporter = activeScope.ConfigureExplicitStandaloneBc4AndReimport(assetPath);
            BC4LinearImportTargetSnapshot snapshot = activeScope.CaptureTargetingSnapshot(assetPath);
            object report = GetLastProcessReport();

            Assert.That(textureImporter.mipmapFilter, Is.EqualTo(TextureImporterMipFilter.KaiserFilter));
            Assert.That(BC4LinearImportTargeting.HasExplicitStandaloneBc4Override(snapshot), Is.True);
            Assert.That(settings.IsEnabledForAssetPath(assetPath), Is.False);
            AssertProcessReportWasNotWritten(report);
            AssertLoopGuardReleased(assetPath);
        }

        /// <summary>
        /// Verifies that a canonicalized excluded folder prefix stops import-time processing for assets created beneath that folder even when the importer remains BC4-eligible.
        /// </summary>
        [Test]
        public void Import_ExcludedCanonicalFolderPrefixPath_SkipsProcessing()
        {
            activeScope = new BC4LinearImportTemporaryAssetScope();
            string excludedFolderPath = $"{activeScope.RootAssetPath}/ExcludedFolder";
            string assetPath = $"{excludedFolderPath}/excluded-mask.png";

            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.SetExcludedAssetPaths(new[] { $"  {excludedFolderPath.Replace('/', '\\')}\\  " });
            settings.SaveSettings();

            ResetLastProcessReport();
            activeScope.CreateTextureAsset("ExcludedFolder/excluded-mask.png", BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes());

            // Creating the asset performs an initial import, so clear that no-op report before verifying the explicit BC4 reimport.
            ResetLastProcessReport();
            TextureImporter textureImporter = activeScope.ConfigureExplicitStandaloneBc4AndReimport(assetPath);
            BC4LinearImportTargetSnapshot snapshot = activeScope.CaptureTargetingSnapshot(assetPath);
            object report = GetLastProcessReport();

            Assert.That(textureImporter.mipmapFilter, Is.EqualTo(TextureImporterMipFilter.KaiserFilter));
            Assert.That(BC4LinearImportTargeting.HasExplicitStandaloneBc4Override(snapshot), Is.True);
            Assert.That(settings.IsEnabledForAssetPath(assetPath), Is.False);
            AssertProcessReportWasNotWritten(report);
            AssertLoopGuardReleased(assetPath);
        }

        /// <summary>
        /// Verifies that clearing exclusions restores normal import-time processing for an otherwise eligible asset.
        /// </summary>
        [Test]
        public void Import_ClearingExclusions_RestoresProcessing()
        {
            activeScope = new BC4LinearImportTemporaryAssetScope();
            string assetPath = activeScope.CreateTextureAsset("restored-mask.png", BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes());

            TextureImporter textureImporter = activeScope.ConfigureExplicitStandaloneBc4AndReimport(assetPath);
            Assert.That(textureImporter.mipmapFilter, Is.EqualTo(TextureImporterMipFilter.KaiserFilter));

            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.SetExcludedAssetPaths(new[] { $"  {assetPath.Replace('/', '\\')}  " });
            settings.SaveSettings();

            ResetLastProcessReport();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            Assert.That(settings.IsEnabledForAssetPath(assetPath), Is.False);
            AssertProcessReportWasNotWritten(GetLastProcessReport());
            AssertLoopGuardReleased(assetPath);

            settings.SetExcludedAssetPaths(Array.Empty<string>());
            settings.SaveSettings();

            ResetLastProcessReport();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            object report = GetLastProcessReport();
            Assert.That(settings.IsEnabledForAssetPath(assetPath), Is.True);
            AssertReportValue(report, "AssetPath", assetPath);
            AssertReportValue(report, "DecisionKind", BC4LinearColorDecisionKind.ConvertToLinear);
            AssertReportValue(report, "DecisionSource", BC4LinearColorDecisionSource.Metadata);
            AssertReportValue(report, "Converted", true);
            Assert.That(ReadReportValue<BC4LinearConversionBackend>(report, "ConversionBackend"), Is.Not.EqualTo(BC4LinearConversionBackend.None));
            Assert.That(ReadReportValue<int>(report, "ConvertedMipCount"), Is.GreaterThan(0));
            AssertLoopGuardReleased(assetPath);
        }

        /// <summary>
        /// Resets the last import-processing report before an integration import runs.
        /// </summary>
        private static void ResetLastProcessReport()
        {
            var resetMethod = typeof(BC4LinearTexturePostprocessor).GetMethod(
                "ResetLastProcessReport",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            if (resetMethod == null)
            {
                Assert.Fail("BC4LinearTexturePostprocessor must expose ResetLastProcessReport() for Phase 5 integration verification.");
            }

            resetMethod.Invoke(null, null);
        }

        /// <summary>
        /// Gets the last import-processing report captured by the BC4 postprocessor.
        /// </summary>
        /// <returns>The last import-processing report.</returns>
        private static object GetLastProcessReport()
        {
            var property = typeof(BC4LinearTexturePostprocessor).GetProperty(
                "LastProcessReport",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            if (property == null)
            {
                Assert.Fail("BC4LinearTexturePostprocessor must expose LastProcessReport for Phase 5 integration verification.");
            }

            return property.GetValue(null);
        }

        /// <summary>
        /// Reads a typed value from the reflected process report.
        /// </summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="report">The reflected process report.</param>
        /// <param name="propertyName">The property name to read.</param>
        /// <returns>The typed property value.</returns>
        private static T ReadReportValue<T>(object report, string propertyName)
        {
            var property = report.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (property == null)
            {
                Assert.Fail($"Process report property '{propertyName}' is missing.");
            }

            return (T)property.GetValue(report);
        }

        /// <summary>
        /// Asserts a reflected process report property value.
        /// </summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="report">The reflected process report.</param>
        /// <param name="propertyName">The property name to read.</param>
        /// <param name="expectedValue">The expected value.</param>
        private static void AssertReportValue<T>(object report, string propertyName, T expectedValue)
        {
            Assert.That(ReadReportValue<T>(report, propertyName), Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Asserts that the postprocessor did not write a report because the asset never passed the BC4 targeting gate.
        /// </summary>
        /// <param name="report">The reflected process report.</param>
        private static void AssertProcessReportWasNotWritten(object report)
        {
            Assert.That(ReadReportValue<string>(report, "AssetPath"), Is.Null.Or.Empty);
            Assert.That(ReadReportValue<bool>(report, "Converted"), Is.False);
            Assert.That(ReadReportValue<BC4LinearConversionBackend>(report, "ConversionBackend"), Is.EqualTo(BC4LinearConversionBackend.None));
            Assert.That(ReadReportValue<int>(report, "ConvertedMipCount"), Is.EqualTo(0));
        }

        /// <summary>
        /// Asserts that the one-shot SessionState loop guard is no longer held for the asset path.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        private static void AssertLoopGuardReleased(string assetPath)
        {
            Assert.That(
                BC4LinearTexturePostprocessor.TryAcquireSessionGuard(assetPath),
                Is.True,
                "The SessionState loop guard should be released after the import flow completes.");
            BC4LinearTexturePostprocessor.ReleaseSessionGuard(assetPath);
        }
    }
}
