// Seeds migration ownership contracts for moving BC4 Linear Import into the sibling BC4LinearImport Unity project.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Seeds migration ownership contracts for moving BC4 Linear Import into the sibling BC4LinearImport Unity project.
    /// </summary>
    [TestFixture]
    public class BC4LinearImportMigrationContractTests
    {
        /// <summary>
        /// Verifies that the Phase 1 seed files already exist under the target EditMode test tree.
        /// </summary>
        [Test]
        public void SeededMigrationContractFiles_ExistUnderTargetEditModeTree()
        {
            var failures = new List<string>();

            foreach (string assetPath in BC4LinearImportTestPathUtility.SeededEditModeContractAssetPaths)
            {
                if (!BC4LinearImportTestPathUtility.IsUnderTargetEditModeTestTree(assetPath))
                {
                    failures.Add($"Seeded migration file must live under Assets/BC4LinearImport/Tests/EditMode: {assetPath}");
                }

                string absolutePath = BC4LinearImportTestPathUtility.ResolveCurrentProjectPath(assetPath);
                if (!File.Exists(absolutePath))
                {
                    failures.Add($"Seeded migration file is missing: {absolutePath}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, failures));
            }
        }

        /// <summary>
        /// Verifies that the target project owns the migrated BC4 production assets.
        /// </summary>
        [Test]
        public void TargetProject_OwnsBc4ProductionAssets()
        {
            var failures = new List<string>();

            foreach (string assetPath in BC4LinearImportTestPathUtility.TargetOwnedImplementationAssetPaths)
            {
                if (!BC4LinearImportTestPathUtility.IsUnderTargetImplementationTree(assetPath))
                {
                    failures.Add($"Target-owned BC4 production path must live under Assets/BC4LinearImport/Editor: {assetPath}");
                }

                if (BC4LinearImportTestPathUtility.IsUnderDoFTree(assetPath))
                {
                    failures.Add($"Target-owned BC4 production path must stay outside Assets/DoFShader: {assetPath}");
                }

                string absolutePath = BC4LinearImportTestPathUtility.ResolveCurrentProjectPath(assetPath);
                if (!File.Exists(absolutePath))
                {
                    failures.Add($"Target project does not own the BC4 production asset yet: {absolutePath}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, failures));
            }
        }

        /// <summary>
        /// Verifies that the target project owns the moved BC4 observation menu.
        /// </summary>
        [Test]
        public void TargetProject_OwnsMovedBc4ObservationMenu()
        {
            const string expectedAssetPath = "Assets/BC4LinearImport/Editor/Diagnostics/BC4LinearImportObservationMenu.cs";

            Assert.That(
                BC4LinearImportTestPathUtility.TargetObservationMenuAssetPath,
                Is.EqualTo(expectedAssetPath),
                "Observation menu contract path drifted from the completed migration boundary.");

            Assert.That(
                BC4LinearImportTestPathUtility.IsUnderTargetImplementationTree(BC4LinearImportTestPathUtility.TargetObservationMenuAssetPath),
                Is.True,
                $"Target-owned BC4 observation menu must live under Assets/BC4LinearImport/Editor: {BC4LinearImportTestPathUtility.TargetObservationMenuAssetPath}");

            Assert.That(
                BC4LinearImportTestPathUtility.IsUnderDoFTree(BC4LinearImportTestPathUtility.TargetObservationMenuAssetPath),
                Is.False,
                $"Target-owned BC4 observation menu must stay outside Assets/DoFShader: {BC4LinearImportTestPathUtility.TargetObservationMenuAssetPath}");

            string absolutePath = BC4LinearImportTestPathUtility.ResolveCurrentProjectPath(BC4LinearImportTestPathUtility.TargetObservationMenuAssetPath);
            if (!File.Exists(absolutePath))
            {
                Assert.Fail($"Target project does not own the moved BC4 observation menu: {absolutePath}");
            }
        }

        /// <summary>
        /// Verifies that the target project owns the migrated BC4-specific EditMode tests.
        /// </summary>
        [Test]
        public void TargetProject_OwnsBc4SpecificEditModeTests()
        {
            var failures = new List<string>();

            foreach (string assetPath in BC4LinearImportTestPathUtility.TargetOwnedMigratedEditModeTestAssetPaths)
            {
                if (!BC4LinearImportTestPathUtility.IsUnderTargetEditModeTestTree(assetPath))
                {
                    failures.Add($"Target-owned BC4 test path must live under Assets/BC4LinearImport/Tests/EditMode: {assetPath}");
                }

                if (BC4LinearImportTestPathUtility.IsUnderDoFTree(assetPath))
                {
                    failures.Add($"Target-owned BC4 test path must stay outside Assets/DoFShader: {assetPath}");
                }

                string absolutePath = BC4LinearImportTestPathUtility.ResolveCurrentProjectPath(assetPath);
                if (!File.Exists(absolutePath))
                {
                    failures.Add($"Target project does not own the BC4-specific EditMode test yet: {absolutePath}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, failures));
            }
        }

        /// <summary>
        /// Verifies that the sibling Ramune project no longer keeps legacy BC4 production source files under the old editor tree.
        /// </summary>
        [Test]
        public void SiblingRamuneProject_NoLongerOwnsLegacyBc4ProductionSources()
        {
            var failures = new List<string>();

            foreach (string assetPath in BC4LinearImportTestPathUtility.TargetOwnedImplementationAssetPaths)
            {
                string absoluteLegacyPath = BC4LinearImportTestPathUtility.ResolveSiblingRamuneProjectPath(assetPath);
                if (File.Exists(absoluteLegacyPath))
                {
                    failures.Add($"Legacy Ramune BC4 production source should be removed after cleanup: {absoluteLegacyPath}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, failures));
            }
        }

        /// <summary>
        /// Verifies that the sibling Ramune project no longer keeps the temporary BC4 observation menu.
        /// </summary>
        [Test]
        public void SiblingRamuneProject_NoLongerKeepsLegacyBc4ObservationMenu()
        {
            string absoluteLegacyPath = BC4LinearImportTestPathUtility.ResolveSiblingRamuneProjectPath(BC4LinearImportTestPathUtility.LegacyRamuneObservationMenuAssetPath);
            if (File.Exists(absoluteLegacyPath))
            {
                Assert.Fail($"Legacy Ramune BC4 observation menu should be removed after cleanup: {absoluteLegacyPath}");
            }
        }

        /// <summary>
        /// Verifies that the sibling Ramune DoF EditMode asmdef no longer references the BC4 editor assembly.
        /// </summary>
        [Test]
        public void SiblingRamuneProject_DoFEditModeAsmdef_NoLongerReferencesBc4LinearImportEditor()
        {
            string absoluteAsmdefPath = BC4LinearImportTestPathUtility.ResolveSiblingRamuneProjectPath(BC4LinearImportTestPathUtility.LegacyRamuneDoFEditModeAsmdefAssetPath);
            if (!File.Exists(absoluteAsmdefPath))
            {
                Assert.Fail($"Sibling Ramune DoF EditMode asmdef is missing: {absoluteAsmdefPath}");
            }

            string asmdefContents = File.ReadAllText(absoluteAsmdefPath);
            if (asmdefContents.Contains("\"BC4LinearImport.Editor\"", StringComparison.Ordinal))
            {
                Assert.Fail($"Sibling Ramune DoF EditMode asmdef must not reference BC4LinearImport.Editor after cleanup: {absoluteAsmdefPath}");
            }
        }

        /// <summary>
        /// Verifies that DoF production directories remain owned by the sibling Ramune project.
        /// </summary>
        [Test]
        public void DoFProductionDirectories_RemainOwnedBySiblingRamuneProject()
        {
            var failures = new List<string>();

            foreach (string assetPath in BC4LinearImportTestPathUtility.RamuneDoFProductionDirectoryPaths)
            {
                if (!BC4LinearImportTestPathUtility.IsUnderDoFTree(assetPath))
                {
                    failures.Add($"DoF production path must stay under Assets/DoFShader: {assetPath}");
                }

                string absolutePath = BC4LinearImportTestPathUtility.ResolveSiblingRamuneProjectPath(assetPath);
                if (!Directory.Exists(absolutePath))
                {
                    failures.Add($"Sibling Ramune project is missing required DoF production directory: {absolutePath}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, failures));
            }
        }
    }
}