// Validates BC4 ownership and migration boundaries after moving the BC4-specific EditMode suite into the BC4LinearImport project.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Validates BC4 ownership and migration boundaries after moving the BC4-specific EditMode suite into the BC4LinearImport project.
    /// </summary>
    [TestFixture]
    public class BC4LinearImportContractTests
    {
        private const string LegacyRamuneBc4TestRootAssetPath = "Assets/DoFShader/Tests/EditMode";

        /// <summary>
        /// Verifies that the BC4 implementation remains owned by the target project under <c>Assets/BC4LinearImport/Editor</c>.
        /// </summary>
        [Test]
        public void ImplementationFiles_LiveUnderAssetsBc4LinearImportEditor()
        {
            var failures = new List<string>();

            foreach (string assetPath in BC4LinearImportTestPathUtility.TargetOwnedImplementationAssetPaths)
            {
                if (!BC4LinearImportTestPathUtility.IsUnderTargetImplementationTree(assetPath))
                {
                    failures.Add($"Target-owned BC4 implementation path must stay under Assets/BC4LinearImport/Editor: {assetPath}");
                }

                if (BC4LinearImportTestPathUtility.IsUnderDoFTree(assetPath))
                {
                    failures.Add($"Target-owned BC4 implementation path must stay outside Assets/DoFShader: {assetPath}");
                }

                string absolutePath = BC4LinearImportTestPathUtility.ResolveCurrentProjectPath(assetPath);
                if (!File.Exists(absolutePath))
                {
                    failures.Add($"Target-owned BC4 implementation file is missing: {absolutePath}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, failures));
            }
        }

        /// <summary>
        /// Verifies that the authoritative BC4-specific EditMode tests live under the target BC4 test tree.
        /// </summary>
        [Test]
        public void MigratedBc4SpecificTests_LiveUnderAssetsBc4LinearImportTestsEditMode()
        {
            var failures = new List<string>();

            foreach (string assetPath in BC4LinearImportTestPathUtility.TargetOwnedMigratedEditModeTestAssetPaths)
            {
                if (!BC4LinearImportTestPathUtility.IsUnderTargetEditModeTestTree(assetPath))
                {
                    failures.Add($"Migrated BC4 test path must stay under Assets/BC4LinearImport/Tests/EditMode: {assetPath}");
                }

                if (BC4LinearImportTestPathUtility.IsUnderDoFTree(assetPath))
                {
                    failures.Add($"Migrated BC4 test path must stay outside Assets/DoFShader: {assetPath}");
                }

                string absolutePath = BC4LinearImportTestPathUtility.ResolveCurrentProjectPath(assetPath);
                if (!File.Exists(absolutePath))
                {
                    failures.Add($"Migrated BC4 test file is missing from the target project: {absolutePath}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, failures));
            }
        }

        /// <summary>
        /// Verifies that Ramune no longer keeps authoritative BC4-specific EditMode tests under <c>Assets/DoFShader/Tests/EditMode</c>.
        /// </summary>
        [Test]
        public void LegacyRamuneBc4SpecificTests_AreRemovedFromAssetsDoFShaderTestsEditMode()
        {
            var failures = new List<string>();

            foreach (string assetPath in BC4LinearImportTestPathUtility.TargetOwnedMigratedEditModeTestAssetPaths)
            {
                string fileName = Path.GetFileName(assetPath);
                string legacyAssetPath = $"{LegacyRamuneBc4TestRootAssetPath}/{fileName}";

                if (!BC4LinearImportTestPathUtility.IsUnderDoFTree(legacyAssetPath))
                {
                    failures.Add($"Legacy BC4 source path must stay under Assets/DoFShader for the removal check: {legacyAssetPath}");
                }

                string absoluteLegacyPath = BC4LinearImportTestPathUtility.ResolveSiblingRamuneProjectPath(legacyAssetPath);
                if (File.Exists(absoluteLegacyPath))
                {
                    failures.Add($"Legacy Ramune BC4 test file should be removed once the target copy is authoritative: {absoluteLegacyPath}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, failures));
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
                    failures.Add($"Sibling Ramune project is missing the required DoF production directory: {absolutePath}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, failures));
            }
        }
    }
}
