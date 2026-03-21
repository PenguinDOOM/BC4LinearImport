// Verifies BC4 settings persistence, exclusion matching, and explicit reimport selection contracts.
/*
// -----------------------------------------------------------------------------
    BC4LinearImport - Automatically converts the color space of textures imported for BC4 from sRGB to linear.
    Copyright (C) 2026  Penguin

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
// -----------------------------------------------------------------------------
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using BC4LinearImport.Editor;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Verifies BC4 settings persistence, exclusion matching, and explicit reimport selection contracts.
    /// </summary>
    [TestFixture]
    public class BC4LinearImportSettingsTests
    {
        private bool originalProjectWideEnabled;
        private List<string> originalExcludedAssetPaths;

        /// <summary>
        /// Captures the current project settings so each test can restore them.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            originalProjectWideEnabled = settings.ProjectWideEnabled;
            originalExcludedAssetPaths = ReadExcludedAssetPaths(settings).ToList();
        }

        /// <summary>
        /// Restores the project settings that were captured before each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = originalProjectWideEnabled;
            settings.SetExcludedAssetPaths(originalExcludedAssetPaths);
            settings.SaveSettings();
        }

        /// <summary>
        /// Verifies that the shared path-exclusion matcher supports exact asset paths and folder prefixes.
        /// </summary>
        [Test]
        public void IsAssetPathExcluded_SupportsExactAssetPathsAndFolderPrefixes()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.SetExcludedAssetPaths(new[]
            {
                "Assets/Textures/IgnoreFolder",
                "Assets/Textures/Exact/exact-mask.png"
            });

            MethodInfo exclusionMethod = typeof(BC4LinearImportSettings).GetMethod("IsAssetPathExcluded");
            if (exclusionMethod == null)
            {
                Assert.Fail("Phase 4 settings must expose IsAssetPathExcluded(string) for shared exclusion matching.");
            }

            Assert.That((bool)exclusionMethod.Invoke(settings, new object[] { "Assets/Textures/IgnoreFolder/nested/mask.png" }), Is.True);
            Assert.That((bool)exclusionMethod.Invoke(settings, new object[] { "Assets/Textures/Exact/exact-mask.png" }), Is.True);
            Assert.That((bool)exclusionMethod.Invoke(settings, new object[] { "Assets/Textures/Keep/mask.png" }), Is.False);
        }

        /// <summary>
        /// Verifies that the project-wide enable switch disables reimport eligibility selection.
        /// </summary>
        [Test]
        public void ProjectWideDisabled_ReimportSelectionReturnsNoAssets()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = false;
            settings.SetExcludedAssetPaths(Array.Empty<string>());

            IReadOnlyList<string> eligibleAssetPaths = InvokeCollectEligibleAssetPaths(
                new[]
                {
                    "Assets/Textures/Enabled/linear-mask.png",
                    "Assets/Textures/Enabled/linear-mask.jpg"
                },
                settings,
                _ => true);

            Assert.That(eligibleAssetPaths, Is.Empty);
        }

        /// <summary>
        /// Verifies that the ScriptableSingleton persists its serialized state under <c>ProjectSettings</c>.
        /// </summary>
        [Test]
        public void SaveSettings_PersistsProjectSettingsAsset()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = false;
            settings.SetExcludedAssetPaths(new[] { "Assets/Textures/ExcludedFolder" });

            settings.SaveSettings();

            string serializedSettings = File.ReadAllText(ResolveProjectSettingsPath());
            Assert.That(serializedSettings, Does.Contain("projectWideEnabled: 0"));
            Assert.That(serializedSettings, Does.Contain("Assets/Textures/ExcludedFolder"));
        }

        /// <summary>
        /// Verifies that exclusion writes canonicalize separators, trim surrounding whitespace, remove trailing separators, and collapse duplicates.
        /// </summary>
        [Test]
        public void SetExcludedAssetPaths_CanonicalizesAndDeduplicatesProjectPaths()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;

            settings.SetExcludedAssetPaths(
                new[]
                {
                    "  Assets\\Textures\\IgnoreFolder\\  ",
                    "Assets/Textures/IgnoreFolder/",
                    " Assets\\Textures\\Exact\\mask.PNG ",
                    "Assets/Textures/Exact/mask.PNG"
                });

            settings.SaveSettings();

            Assert.That(
                ReadExcludedAssetPaths(settings),
                Is.EqualTo(
                    new[]
                    {
                        "Assets/Textures/IgnoreFolder",
                        "Assets/Textures/Exact/mask.PNG"
                    }));

            string serializedSettings = File.ReadAllText(ResolveProjectSettingsPath());
            Assert.That(serializedSettings, Does.Contain("Assets/Textures/IgnoreFolder"));
            Assert.That(serializedSettings, Does.Contain("Assets/Textures/Exact/mask.PNG"));
            Assert.That(serializedSettings, Does.Not.Contain("Assets\\Textures\\IgnoreFolder\\"));
        }

        /// <summary>
        /// Verifies that exclusion writes reject blank inputs and non-project paths while preserving canonical <c>Assets/...</c> entries.
        /// </summary>
        [Test]
        public void SetExcludedAssetPaths_RejectsBlankAndNonProjectPaths()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;

            settings.SetExcludedAssetPaths(
                new[]
                {
                    null,
                    string.Empty,
                    "   ",
                    "Packages/com.example.tooling/Editor/icon.png",
                    @"C:\repo\BC4LinearImport\Assets\Textures\absolute-mask.png",
                    "../Assets/Textures/traversal-mask.png",
                    "Assets/Textures/Keep/AllowedFolder/",
                    "Assets/Textures/Keep/AllowedMask.png"
                });

            settings.SaveSettings();

            Assert.That(
                ReadExcludedAssetPaths(settings),
                Is.EqualTo(
                    new[]
                    {
                        "Assets/Textures/Keep/AllowedFolder",
                        "Assets/Textures/Keep/AllowedMask.png"
                    }));

            string serializedSettings = File.ReadAllText(ResolveProjectSettingsPath());
            Assert.That(serializedSettings, Does.Contain("Assets/Textures/Keep/AllowedFolder"));
            Assert.That(serializedSettings, Does.Contain("Assets/Textures/Keep/AllowedMask.png"));
            Assert.That(serializedSettings, Does.Not.Contain("Packages/com.example.tooling/Editor/icon.png"));
            Assert.That(serializedSettings, Does.Not.Contain("C:\\repo\\BC4LinearImport\\Assets\\Textures\\absolute-mask.png"));
            Assert.That(serializedSettings, Does.Not.Contain("../Assets/Textures/traversal-mask.png"));
        }

        /// <summary>
        /// Verifies that explicit reimport selection allows an explicit Standalone BC4 override.
        /// </summary>
        [Test]
        public void CollectEligibleAssetPaths_AllowsExplicitStandaloneBc4Override()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = true;
            settings.SetExcludedAssetPaths(Array.Empty<string>());

            IReadOnlyList<string> eligibleAssetPaths = InvokeCollectEligibleAssetPaths(
                new[] { "Assets/Textures/Keep/explicit-bc4.png" },
                settings,
                CreateEligibilityPredicate(
                    ("Assets/Textures/Keep/explicit-bc4.png", CreateSnapshot(
                        defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        standalonePlatformFormat: TextureImporterFormat.BC4,
                        isStandaloneOverrideActive: true,
                        automaticStandaloneFormat: TextureImporterFormat.DXT1))));

            Assert.That(eligibleAssetPaths, Is.EqualTo(new[] { "Assets/Textures/Keep/explicit-bc4.png" }));
        }

        /// <summary>
        /// Verifies that explicit reimport selection rejects a Default-panel BC4 appearance when the Standalone override is inactive.
        /// </summary>
        [Test]
        public void CollectEligibleAssetPaths_RejectsDefaultPanelBc4Appearance_WhenStandaloneOverrideIsInactive()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = true;
            settings.SetExcludedAssetPaths(Array.Empty<string>());

            IReadOnlyList<string> eligibleAssetPaths = InvokeCollectEligibleAssetPaths(
                new[] { "Assets/Textures/Keep/default-only-bc4.png" },
                settings,
                CreateEligibilityPredicate(
                    ("Assets/Textures/Keep/default-only-bc4.png", CreateSnapshot(
                        defaultPlatformFormat: TextureImporterFormat.BC4,
                        standalonePlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        isStandaloneOverrideActive: false,
                        automaticStandaloneFormat: TextureImporterFormat.DXT1))));

            Assert.That(eligibleAssetPaths, Is.Empty);
        }

        /// <summary>
        /// Verifies that explicit reimport selection rejects an explicit Standalone non-BC4 override.
        /// </summary>
        [Test]
        public void CollectEligibleAssetPaths_RejectsExplicitStandaloneNonBc4Override()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = true;
            settings.SetExcludedAssetPaths(Array.Empty<string>());

            IReadOnlyList<string> eligibleAssetPaths = InvokeCollectEligibleAssetPaths(
                new[] { "Assets/Textures/Keep/explicit-non-bc4.png" },
                settings,
                CreateEligibilityPredicate(
                    ("Assets/Textures/Keep/explicit-non-bc4.png", CreateSnapshot(
                        defaultPlatformFormat: TextureImporterFormat.BC4,
                        standalonePlatformFormat: TextureImporterFormat.DXT1,
                        isStandaloneOverrideActive: true,
                        automaticStandaloneFormat: TextureImporterFormat.DXT1))));

            Assert.That(eligibleAssetPaths, Is.Empty);
        }

        /// <summary>
        /// Verifies that explicit reimport selection allows the observed automatic Standalone BC4 path.
        /// </summary>
        [Test]
        public void CollectEligibleAssetPaths_AllowsObservedAutomaticStandaloneBc4Path()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = true;
            settings.SetExcludedAssetPaths(Array.Empty<string>());

            IReadOnlyList<string> eligibleAssetPaths = InvokeCollectEligibleAssetPaths(
                new[] { "Assets/Textures/Keep/automatic-bc4.png" },
                settings,
                CreateEligibilityPredicate(
                    ("Assets/Textures/Keep/automatic-bc4.png", CreateSnapshot(
                        defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        standalonePlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        isStandaloneOverrideActive: false,
                        automaticStandaloneFormat: TextureImporterFormat.BC4,
                        textureType: TextureImporterType.SingleChannel,
                        singleChannelComponent: TextureImporterSingleChannelComponent.Red))));

            Assert.That(eligibleAssetPaths, Is.EqualTo(new[] { "Assets/Textures/Keep/automatic-bc4.png" }));
        }

        /// <summary>
        /// Verifies that explicit reimport selection rejects the observed single-channel case when automatic Standalone format is not BC4.
        /// </summary>
        [Test]
        public void CollectEligibleAssetPaths_RejectsObservedSingleChannelCase_WhenAutomaticStandaloneFormatIsNotBc4()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = true;
            settings.SetExcludedAssetPaths(Array.Empty<string>());

            IReadOnlyList<string> eligibleAssetPaths = InvokeCollectEligibleAssetPaths(
                new[] { "Assets/Textures/Keep/automatic-not-bc4.png" },
                settings,
                CreateEligibilityPredicate(
                    ("Assets/Textures/Keep/automatic-not-bc4.png", CreateSnapshot(
                        defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        standalonePlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        isStandaloneOverrideActive: false,
                        automaticStandaloneFormat: TextureImporterFormat.DXT1,
                        textureType: TextureImporterType.SingleChannel,
                        singleChannelComponent: TextureImporterSingleChannelComponent.Red))));

            Assert.That(eligibleAssetPaths, Is.Empty);
        }

        /// <summary>
        /// Verifies that a canonicalized excluded folder prefix removes matching reimport candidates while keeping other BC4-eligible assets selectable.
        /// </summary>
        [Test]
        public void CollectEligibleAssetPaths_ExcludedCanonicalFolderPrefix_RemovesMatchingCandidates_AndKeepsOtherEligibleAssets()
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = true;
            settings.SetExcludedAssetPaths(new[] { "  Assets\\Textures\\IgnoreFolder\\  " });

            IReadOnlyList<string> eligibleAssetPaths = InvokeCollectEligibleAssetPaths(
                new[]
                {
                    "Assets/Textures/IgnoreFolder/excluded-bc4.png",
                    "Assets/Textures/IgnoreFolder/nested/excluded-bc4.jpg",
                    "Assets/Textures/Keep/eligible-bc4.png",
                    "Assets/Textures/Keep/automatic-not-bc4.png",
                    "Assets/Textures/Keep/not-supported.tga"
                },
                settings,
                CreateEligibilityPredicate(
                    ("Assets/Textures/IgnoreFolder/excluded-bc4.png", CreateSnapshot(
                        defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        standalonePlatformFormat: TextureImporterFormat.BC4,
                        isStandaloneOverrideActive: true,
                        automaticStandaloneFormat: TextureImporterFormat.DXT1)),
                    ("Assets/Textures/IgnoreFolder/nested/excluded-bc4.jpg", CreateSnapshot(
                        defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        standalonePlatformFormat: TextureImporterFormat.BC4,
                        isStandaloneOverrideActive: true,
                        automaticStandaloneFormat: TextureImporterFormat.DXT1)),
                    ("Assets/Textures/Keep/eligible-bc4.png", CreateSnapshot(
                        defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        standalonePlatformFormat: TextureImporterFormat.BC4,
                        isStandaloneOverrideActive: true,
                        automaticStandaloneFormat: TextureImporterFormat.DXT1)),
                    ("Assets/Textures/Keep/automatic-not-bc4.png", CreateSnapshot(
                        defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        standalonePlatformFormat: TextureImporterFormat.AutomaticCompressed,
                        isStandaloneOverrideActive: false,
                        automaticStandaloneFormat: TextureImporterFormat.DXT1,
                        textureType: TextureImporterType.SingleChannel,
                        singleChannelComponent: TextureImporterSingleChannelComponent.Red))));

            Assert.That(ReadExcludedAssetPaths(settings), Is.EqualTo(new[] { "Assets/Textures/IgnoreFolder" }));
            Assert.That(eligibleAssetPaths, Is.EqualTo(new[] { "Assets/Textures/Keep/eligible-bc4.png" }));
        }

        /// <summary>
        /// Verifies that clearing exclusions restores explicit reimport selection for a previously excluded eligible asset.
        /// </summary>
        [Test]
        public void CollectEligibleAssetPaths_ClearingExclusions_RestoresPreviouslyExcludedCandidate()
        {
            const string CandidateAssetPath = "Assets/Textures/IgnoreFolder/restored-bc4.png";

            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            settings.ProjectWideEnabled = true;
            settings.SetExcludedAssetPaths(new[] { " Assets\\Textures\\IgnoreFolder\\ " });

            Func<string, bool> eligibilityPredicate = CreateEligibilityPredicate(
                (CandidateAssetPath, CreateSnapshot(
                    defaultPlatformFormat: TextureImporterFormat.AutomaticCompressed,
                    standalonePlatformFormat: TextureImporterFormat.BC4,
                    isStandaloneOverrideActive: true,
                    automaticStandaloneFormat: TextureImporterFormat.DXT1)));

            IReadOnlyList<string> excludedSelection = InvokeCollectEligibleAssetPaths(
                new[] { CandidateAssetPath },
                settings,
                eligibilityPredicate);

            settings.SetExcludedAssetPaths(Array.Empty<string>());

            IReadOnlyList<string> restoredSelection = InvokeCollectEligibleAssetPaths(
                new[] { CandidateAssetPath },
                settings,
                eligibilityPredicate);

            Assert.That(excludedSelection, Is.Empty);
            Assert.That(restoredSelection, Is.EqualTo(new[] { CandidateAssetPath }));
        }

        /// <summary>
        /// Verifies that the project settings UI is project-scoped and surfaces the explicit reimport action.
        /// </summary>
        [Test]
        public void SettingsProvider_UsesDragAndDropExclusionsWorkflow_AndSurfacesExplicitReimportAction()
        {
            SettingsProvider provider = BC4LinearImportSettingsProvider.CreateProvider();

            Assert.That(provider.settingsPath, Is.EqualTo("Project/BC4 Linear Import"));

            string providerSource = File.ReadAllText(
                BC4LinearImportFixtureUtility.ResolveProjectPath("Assets/BC4LinearImport/Editor/BC4LinearImportSettingsProvider.cs"));
            Assert.That(
                providerSource,
                Does.Contain("FindProperty(\"projectWideEnabled\")"),
                "The custom settings UI should keep the project-wide enable toggle bound through a serialized property so persistence behavior remains intact.");
            Assert.That(
                providerSource,
                Does.Contain("ResolveDroppedAssetPathsWithReport"),
                "Phase 3 should route drop intake through the dedicated helper instead of embedding asset-resolution logic directly in IMGUI event handling.");
            Assert.That(
                providerSource,
                Does.Contain("SetExcludedAssetPaths("),
                "Phase 3 should route exclusion add/remove/clear mutations through the shared sanitize API.");
            Assert.That(
                providerSource,
                Does.Contain("Undo.RecordObject"),
                "Phase 3 should record exclusion mutations for Undo support.");
            Assert.That(
                providerSource,
                Does.Contain("Undo.undoRedoPerformed"),
                "Phase 3 should explicitly persist undo and redo operations for the settings singleton.");
            Assert.That(
                providerSource.Contains("PropertyField(exclusionsProperty", StringComparison.Ordinal),
                Is.False,
                "Phase 3 should replace the default freeform exclusions PropertyField with the custom drag-and-drop workflow.");
            Assert.That(
                providerSource,
                Does.Contain("BC4LinearImportReimportUtility"),
                "The project settings UI should surface the explicit reimport action in Phase 4.");
            Assert.That(
                providerSource,
                Does.Contain("Drag project assets or folders here to exclude them from BC4 linear import."),
                "The exclusions help copy should clearly explain the drag-and-drop workflow in approachable terms.");
            Assert.That(
                providerSource,
                Does.Contain("run the explicit reimport action to repair existing assets"),
                "The top-level help copy should explain how exclusions and explicit reimport work together.");

            string menuSourcePath = BC4LinearImportFixtureUtility.ResolveProjectPath("Assets/BC4LinearImport/Editor/BC4LinearImportMenu.cs");
            Assert.That(File.Exists(menuSourcePath), Is.True, "Phase 4 should add an explicit menu entry for reimport tooling.");
        }

        /// <summary>
        /// Verifies that import-time flow uses the shared project settings gate.
        /// </summary>
        [Test]
        public void Postprocessor_UsesSharedSettingsGateForImportTimeFlow()
        {
            string postprocessorSource = File.ReadAllText(
                BC4LinearImportFixtureUtility.ResolveProjectPath("Assets/BC4LinearImport/Editor/BC4LinearTexturePostprocessor.cs"));

            Assert.That(
                postprocessorSource,
                Does.Contain("BC4LinearImportSettings.instance.IsEnabledForAssetPath(assetPath)"),
                "Import-time processing should share the project-wide enable switch and exclusions gate.");
        }

        /// <summary>
        /// Verifies that explicit reimport selection binds the shared importer classifier through the predicate seam.
        /// </summary>
        [Test]
        public void FindEligibleAssetPaths_UsesSharedEligibilityPredicate()
        {
            string utilitySource = File.ReadAllText(
                BC4LinearImportFixtureUtility.ResolveProjectPath("Assets/BC4LinearImport/Editor/BC4LinearImportReimportUtility.cs"));

            Assert.That(
                utilitySource,
                Does.Contain("CollectEligibleAssetPaths("),
                "Explicit reimport selection should continue to funnel project candidates through the shared predicate seam.");
            Assert.That(
                utilitySource,
                Does.Contain("IsEligibleForBc4LinearImport"),
                "Explicit reimport selection should bind the shared importer classifier instead of reconstructing its own BC4 format gate.");
            Assert.That(
                utilitySource.Contains("ResolveDefaultPlatformSettings", StringComparison.Ordinal),
                Is.False,
                "Explicit reimport selection should remove the obsolete default-platform resolver helper once the shared classifier owns eligibility.");
            Assert.That(
                utilitySource.Contains("ResolveStandalonePlatformSettings", StringComparison.Ordinal),
                Is.False,
                "Explicit reimport selection should remove the obsolete Standalone-platform resolver helper once the shared classifier owns eligibility.");
        }

        /// <summary>
        /// Reads the serialized exclusion list directly from the singleton backing field.
        /// </summary>
        /// <param name="settings">The settings singleton instance.</param>
        /// <returns>The exclusion paths.</returns>
        private static IReadOnlyList<string> ReadExcludedAssetPaths(BC4LinearImportSettings settings)
        {
            FieldInfo exclusionsField = typeof(BC4LinearImportSettings).GetField("excludedAssetPaths", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(exclusionsField, Is.Not.Null, "Settings should keep the serialized exclusion list in a private field.");
            return ((List<string>)exclusionsField.GetValue(settings)).ToArray();
        }

        /// <summary>
        /// Invokes the explicit reimport selection helper through reflection so the snapshot-based Phase 1 seam can compile before implementation.
        /// </summary>
        /// <param name="candidateAssetPaths">The candidate asset paths to evaluate.</param>
        /// <param name="settings">The settings instance to apply.</param>
        /// <param name="eligibilityPredicate">The eligibility predicate to evaluate.</param>
        /// <returns>The eligible asset paths returned by the utility.</returns>
        private static IReadOnlyList<string> InvokeCollectEligibleAssetPaths(
            IEnumerable<string> candidateAssetPaths,
            BC4LinearImportSettings settings,
            Func<string, bool> eligibilityPredicate)
        {
            Type utilityType = Type.GetType("BC4LinearImport.Editor.BC4LinearImportReimportUtility, BC4LinearImport.Editor");
            if (utilityType == null)
            {
                Assert.Fail("Expected BC4LinearImportReimportUtility in the BC4LinearImport.Editor assembly so the shared CollectEligibleAssetPaths(IEnumerable<string>, BC4LinearImportSettings, Func<string, bool>) overload can be verified.");
            }

            MethodInfo collectMethod = utilityType.GetMethod(
                "CollectEligibleAssetPaths",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[]
                {
                    typeof(IEnumerable<string>),
                    typeof(BC4LinearImportSettings),
                    typeof(Func<string, bool>)
                },
                null);

            if (collectMethod == null)
            {
                Assert.Fail("Expected CollectEligibleAssetPaths(IEnumerable<string>, BC4LinearImportSettings, Func<string, bool>) so Phase 1 can verify the required shared eligibility-predicate overload.");
            }

            object result = collectMethod.Invoke(
                null,
                new object[]
                {
                    candidateAssetPaths.ToArray(),
                    settings,
                    eligibilityPredicate
                });

            return ((IEnumerable<string>)result).ToArray();
        }

        /// <summary>
        /// Creates a shared eligibility predicate from snapshot fixtures.
        /// </summary>
        /// <param name="snapshots">The asset-path snapshots to expose through the predicate.</param>
        /// <returns>The predicate.</returns>
        private static Func<string, bool> CreateEligibilityPredicate(params (string assetPath, BC4LinearImportTargetSnapshot snapshot)[] snapshots)
        {
            var snapshotLookup = snapshots.ToDictionary(entry => entry.assetPath, entry => entry.snapshot, StringComparer.Ordinal);
            return assetPath => snapshotLookup.TryGetValue(assetPath, out BC4LinearImportTargetSnapshot snapshot)
                && BC4LinearImportTargeting.IsEligibleForBc4LinearImport(snapshot);
        }

        /// <summary>
        /// Creates a BC4 targeting snapshot for explicit reimport selection tests.
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
            TextureImporterSingleChannelComponent singleChannelComponent = TextureImporterSingleChannelComponent.Alpha)
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
        /// Resolves the project settings asset path used by the ScriptableSingleton.
        /// </summary>
        /// <returns>The absolute project settings asset path.</returns>
        private static string ResolveProjectSettingsPath()
        {
            return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "ProjectSettings/BC4LinearImportSettings.asset"));
        }
    }
}
