// Resolves cross-project migration contract paths for the BC4 Linear Import EditMode test scaffold.
using System;
using System.IO;
using UnityEngine;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Resolves cross-project migration contract paths for the BC4 Linear Import EditMode test scaffold.
    /// </summary>
    internal static class BC4LinearImportTestPathUtility
    {
        /// <summary>
        /// Gets the target-project observation menu asset path that must exist after the move.
        /// </summary>
        internal const string TargetObservationMenuAssetPath = "Assets/BC4LinearImport/Editor/Diagnostics/BC4LinearImportObservationMenu.cs";

        /// <summary>
        /// Gets the sibling Ramune DoF EditMode asmdef asset path that must stop referencing the BC4 editor assembly after cleanup.
        /// </summary>
        internal const string LegacyRamuneDoFEditModeAsmdefAssetPath = "Assets/DoFShader/Tests/EditMode/DoFShader.Tests.EditMode.asmdef";

        /// <summary>
        /// Gets the sibling Ramune observation menu asset path that must be removed after cleanup.
        /// </summary>
        internal const string LegacyRamuneObservationMenuAssetPath = "Assets/Editor/BC4LinearImportObservationMenu.cs";

        /// <summary>
        /// Gets the EditMode test asset paths that are seeded by Phase 1 in the target project.
        /// </summary>
        internal static readonly string[] SeededEditModeContractAssetPaths =
        {
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImport.Tests.EditMode.asmdef",
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImportMigrationContractTests.cs",
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImportTestPathUtility.cs"
        };

        /// <summary>
        /// Gets the BC4 production asset paths that the target project is expected to own after migration.
        /// Keep this list in sync with the sibling Ramune fixture contract list.
        /// </summary>
        internal static readonly string[] TargetOwnedImplementationAssetPaths =
        {
            "Assets/BC4LinearImport/Editor/BC4LinearImport.Editor.asmdef",
            "Assets/BC4LinearImport/Editor/BC4LinearImportTargeting.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearImportSettings.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearImportSettingsProvider.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearImportReimportUtility.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearImportMenu.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearColorDecision.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearColorDetector.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearPngInspector.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearJpegInspector.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearPixelHeuristics.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearTextureConverter.cs",
            "Assets/BC4LinearImport/Editor/BC4LinearTexturePostprocessor.cs",
            "Assets/BC4LinearImport/Editor/Resources/BC4Linearize.compute"
        };

        /// <summary>
        /// Gets the BC4-specific EditMode test asset paths that the target project is expected to own after migration.
        /// </summary>
        internal static readonly string[] TargetOwnedMigratedEditModeTestAssetPaths =
        {
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImportContractTests.cs",
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImportDetectorTests.cs",
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImportHeuristicTests.cs",
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImportPostprocessorTests.cs",
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImportSettingsTests.cs",
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImportEndToEndTests.cs",
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImportFixtureUtility.cs",
            "Assets/BC4LinearImport/Tests/EditMode/BC4LinearImportTemporaryAssetScope.cs"
        };

        /// <summary>
        /// Gets the DoF production directory paths that must remain owned by the sibling Ramune project.
        /// </summary>
        internal static readonly string[] RamuneDoFProductionDirectoryPaths =
        {
            "Assets/DoFShader/Editor",
            "Assets/DoFShader/Runtime",
            "Assets/DoFShader/Shaders",
            "Assets/DoFShader/Materials",
            "Assets/DoFShader/Textures"
        };

        /// <summary>
        /// Resolves an asset-relative path under the current BC4LinearImport project root.
        /// </summary>
        /// <param name="assetRelativePath">The asset-relative path to resolve.</param>
        /// <returns>The absolute filesystem path.</returns>
        internal static string ResolveCurrentProjectPath(string assetRelativePath)
        {
            return Path.GetFullPath(Path.Combine(GetCurrentProjectRoot(), NormalizeAssetPath(assetRelativePath)));
        }

        /// <summary>
        /// Resolves an asset-relative path under the sibling Ramune project root.
        /// </summary>
        /// <param name="assetRelativePath">The asset-relative path to resolve.</param>
        /// <returns>The absolute filesystem path.</returns>
        internal static string ResolveSiblingRamuneProjectPath(string assetRelativePath)
        {
            return Path.GetFullPath(Path.Combine(GetSiblingRamuneProjectRoot(), NormalizeAssetPath(assetRelativePath)));
        }

        /// <summary>
        /// Normalizes an asset-relative path for stable comparisons.
        /// </summary>
        /// <param name="assetRelativePath">The asset-relative path to normalize.</param>
        /// <returns>The normalized asset-relative path using forward slashes.</returns>
        internal static string NormalizeAssetPath(string assetRelativePath)
        {
            return assetRelativePath.Replace('\\', '/').Trim();
        }

        /// <summary>
        /// Determines whether an asset path belongs to the target BC4 implementation tree.
        /// </summary>
        /// <param name="assetRelativePath">The asset-relative path to inspect.</param>
        /// <returns><see langword="true"/> when the path is under <c>Assets/BC4LinearImport/Editor</c>; otherwise, <see langword="false"/>.</returns>
        internal static bool IsUnderTargetImplementationTree(string assetRelativePath)
        {
            return NormalizeAssetPath(assetRelativePath).StartsWith("Assets/BC4LinearImport/Editor/", StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether an asset path belongs to the target BC4 EditMode test tree.
        /// </summary>
        /// <param name="assetRelativePath">The asset-relative path to inspect.</param>
        /// <returns><see langword="true"/> when the path is under <c>Assets/BC4LinearImport/Tests/EditMode</c>; otherwise, <see langword="false"/>.</returns>
        internal static bool IsUnderTargetEditModeTestTree(string assetRelativePath)
        {
            return NormalizeAssetPath(assetRelativePath).StartsWith("Assets/BC4LinearImport/Tests/EditMode/", StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether an asset path points into the DoF tree.
        /// </summary>
        /// <param name="assetRelativePath">The asset-relative path to inspect.</param>
        /// <returns><see langword="true"/> when the path is under <c>Assets/DoFShader</c>; otherwise, <see langword="false"/>.</returns>
        internal static bool IsUnderDoFTree(string assetRelativePath)
        {
            return NormalizeAssetPath(assetRelativePath).StartsWith("Assets/DoFShader/", StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the current BC4LinearImport project root.
        /// </summary>
        /// <returns>The current project root path.</returns>
        private static string GetCurrentProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        /// <summary>
        /// Gets the sibling Ramune project root.
        /// </summary>
        /// <returns>The sibling Ramune project root path.</returns>
        private static string GetSiblingRamuneProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(GetCurrentProjectRoot(), "..", "Ramune"));
        }
    }
}