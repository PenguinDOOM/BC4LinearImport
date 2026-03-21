// Stores project-scoped settings for the BC4 linear import editor workflow.
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
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Stores project-scoped settings for the BC4 linear import editor scaffold.
    /// </summary>
    [FilePath(
        "ProjectSettings/BC4LinearImportSettings.asset",
        FilePathAttribute.Location.ProjectFolder
    )]
    public sealed class BC4LinearImportSettings : ScriptableSingleton<BC4LinearImportSettings>
    {
        [SerializeField]
        private bool projectWideEnabled = true;

        [SerializeField]
        private List<string> excludedAssetPaths = new();

        /// <summary>
        /// Gets or sets a value indicating whether the BC4 linear import workflow is enabled project-wide.
        /// </summary>
        public bool ProjectWideEnabled
        {
            get => projectWideEnabled;
            set => projectWideEnabled = value;
        }

        /// <summary>
        /// Gets the asset-path exclusions configured for the workflow.
        /// </summary>
        public IReadOnlyList<string> ExcludedAssetPaths => excludedAssetPaths;

        /// <summary>
        /// Replaces the configured exclusion paths after canonicalizing valid project asset paths.
        /// </summary>
        /// <param name="assetPaths">The exclusion paths to canonicalize and store.</param>
        public void SetExcludedAssetPaths(IEnumerable<string> assetPaths)
        {
            excludedAssetPaths = SanitizeExcludedAssetPaths(assetPaths);
        }

        /// <summary>
        /// Determines whether the workflow is enabled for the provided asset path.
        /// </summary>
        /// <param name="assetPath">The asset path to inspect.</param>
        /// <returns><see langword="true"/> when the workflow is enabled and the asset path is not excluded; otherwise, <see langword="false"/>.</returns>
        public bool IsEnabledForAssetPath(string assetPath)
        {
            return ProjectWideEnabled && !IsAssetPathExcluded(assetPath);
        }

        /// <summary>
        /// Determines whether the provided asset path matches any configured exclusion.
        /// </summary>
        /// <param name="assetPath">The asset path to inspect.</param>
        /// <returns><see langword="true"/> when the asset path is excluded; otherwise, <see langword="false"/>.</returns>
        public bool IsAssetPathExcluded(string assetPath)
        {
            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            if (string.IsNullOrEmpty(normalizedAssetPath))
            {
                return false;
            }

            foreach (string excludedAssetPath in excludedAssetPaths)
            {
                // Stored paths are sanitized on write, but keep this normalization as a defensive guard
                // for legacy serialized values or direct field mutations that bypass the public API.
                string normalizedExcludedAssetPath = NormalizeAssetPath(excludedAssetPath);
                if (string.IsNullOrEmpty(normalizedExcludedAssetPath))
                {
                    continue;
                }

                if (
                    string.Equals(
                        normalizedAssetPath,
                        normalizedExcludedAssetPath,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }

                string directoryPrefix = normalizedExcludedAssetPath + "/";
                if (
                    normalizedAssetPath.StartsWith(
                        directoryPrefix,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Saves the current settings to the project settings asset.
        /// </summary>
        public void SaveSettings()
        {
            excludedAssetPaths = SanitizeExcludedAssetPaths(excludedAssetPaths);
            Save(true);
        }

        /// <summary>
        /// Canonicalizes an exclusion path and rejects entries outside the Unity project asset root.
        /// </summary>
        /// <param name="assetPath">The raw asset path to inspect.</param>
        /// <param name="sanitizedAssetPath">The canonical asset path when the input is valid; otherwise, an empty string.</param>
        /// <returns><see langword="true"/> when the asset path is valid for exclusion storage; otherwise, <see langword="false"/>.</returns>
        internal static bool TrySanitizeExcludedAssetPath(string assetPath, out string sanitizedAssetPath)
        {
            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            if (string.IsNullOrEmpty(normalizedAssetPath))
            {
                sanitizedAssetPath = string.Empty;
                return false;
            }

            if (
                string.Equals(normalizedAssetPath, "Assets", System.StringComparison.Ordinal)
                || normalizedAssetPath.StartsWith("Assets/", System.StringComparison.Ordinal)
            )
            {
                sanitizedAssetPath = normalizedAssetPath;
                return true;
            }

            sanitizedAssetPath = string.Empty;
            return false;
        }

        /// <summary>
        /// Normalizes an asset path for exclusion comparisons.
        /// </summary>
        /// <param name="assetPath">The asset path to normalize.</param>
        /// <returns>The normalized asset path.</returns>
        private static string NormalizeAssetPath(string assetPath)
        {
            return string.IsNullOrWhiteSpace(assetPath)
                ? string.Empty
                : assetPath.Replace('\\', '/').Trim().TrimEnd('/');
        }

        /// <summary>
        /// Canonicalizes and deduplicates exclusion paths while preserving the first valid project path casing.
        /// </summary>
        /// <param name="assetPaths">The raw exclusion paths to sanitize.</param>
        /// <returns>The sanitized exclusion paths.</returns>
        private static List<string> SanitizeExcludedAssetPaths(IEnumerable<string> assetPaths)
        {
            if (assetPaths == null)
            {
                return new List<string>();
            }

            var sanitizedAssetPaths = new List<string>();
            var seenAssetPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (string assetPath in assetPaths.Where(path => path != null))
            {
                if (!TrySanitizeExcludedAssetPath(assetPath, out string sanitizedAssetPath))
                {
                    continue;
                }

                if (seenAssetPaths.Add(sanitizedAssetPath))
                {
                    sanitizedAssetPaths.Add(sanitizedAssetPath);
                }
            }

            return sanitizedAssetPaths;
        }
    }
}
