// Resolves current-project BC4 test paths for the BC4 Linear Import EditMode test scaffold.
using System.IO;
using UnityEngine;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Resolves current-project BC4 test paths for the BC4 Linear Import EditMode test scaffold.
    /// </summary>
    internal static class BC4LinearImportTestPathUtility
    {
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
        /// Normalizes an asset-relative path for stable comparisons.
        /// </summary>
        /// <param name="assetRelativePath">The asset-relative path to normalize.</param>
        /// <returns>The normalized asset-relative path using forward slashes.</returns>
        internal static string NormalizeAssetPath(string assetRelativePath)
        {
            return assetRelativePath.Replace('\\', '/').Trim();
        }

        /// <summary>
        /// Gets the current BC4LinearImport project root.
        /// </summary>
        /// <returns>The current project root path.</returns>
        private static string GetCurrentProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
    }
}