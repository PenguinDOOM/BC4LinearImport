// Scans project textures and explicitly reimports only BC4-eligible PNG/JPG/JPEG assets.
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
using UnityEditor;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Scans project textures and explicitly reimports only BC4-eligible PNG/JPG/JPEG assets.
    /// </summary>
    public static class BC4LinearImportReimportUtility
    {
        private const string TextureSearchFilter = "t:Texture2D";

        /// <summary>
        /// Collects the supported, non-excluded asset paths that satisfy the provided eligibility predicate.
        /// </summary>
        /// <param name="candidateAssetPaths">The candidate asset paths to inspect.</param>
        /// <param name="settings">The project settings to apply.</param>
        /// <param name="eligibilityPredicate">The shared eligibility predicate to evaluate per asset path.</param>
        /// <returns>The eligible asset paths in input order.</returns>
        public static IReadOnlyList<string> CollectEligibleAssetPaths(
            IEnumerable<string> candidateAssetPaths,
            BC4LinearImportSettings settings,
            Func<string, bool> eligibilityPredicate)
        {
            if (candidateAssetPaths == null)
            {
                throw new ArgumentNullException(nameof(candidateAssetPaths));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (eligibilityPredicate == null)
            {
                throw new ArgumentNullException(nameof(eligibilityPredicate));
            }

            if (!settings.ProjectWideEnabled)
            {
                return Array.Empty<string>();
            }

            var eligibleAssetPaths = new List<string>();
            var seenAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string candidateAssetPath in candidateAssetPaths)
            {
                if (string.IsNullOrWhiteSpace(candidateAssetPath) || !seenAssetPaths.Add(candidateAssetPath))
                {
                    continue;
                }

                if (!BC4LinearTexturePostprocessor.SupportsAssetPath(candidateAssetPath))
                {
                    continue;
                }

                if (!settings.IsEnabledForAssetPath(candidateAssetPath))
                {
                    continue;
                }

                if (!eligibilityPredicate(candidateAssetPath))
                {
                    continue;
                }

                eligibleAssetPaths.Add(candidateAssetPath);
            }

            return eligibleAssetPaths;
        }

        /// <summary>
        /// Finds the supported, non-excluded BC4-eligible asset paths in the current project.
        /// </summary>
        /// <param name="settings">The optional settings override.</param>
        /// <returns>The eligible asset paths.</returns>
        public static IReadOnlyList<string> FindEligibleAssetPaths(BC4LinearImportSettings settings = null)
        {
            BC4LinearImportSettings resolvedSettings = settings ?? BC4LinearImportSettings.instance;
            string[] textureGuids = AssetDatabase.FindAssets(TextureSearchFilter);
            var candidateAssetPaths = new List<string>(textureGuids.Length);
            foreach (string textureGuid in textureGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(textureGuid);
                if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    candidateAssetPaths.Add(assetPath);
                }
            }

            return CollectEligibleAssetPaths(
                candidateAssetPaths,
                resolvedSettings,
                IsEligibleForBc4LinearImport);
        }

        /// <summary>
        /// Reimports all currently eligible assets.
        /// </summary>
        /// <param name="settings">The optional settings override.</param>
        /// <returns>The number of reimported assets.</returns>
        public static int ReimportEligibleAssets(BC4LinearImportSettings settings = null)
        {
            IReadOnlyList<string> eligibleAssetPaths = FindEligibleAssetPaths(settings);
            foreach (string eligibleAssetPath in eligibleAssetPaths)
            {
                AssetDatabase.ImportAsset(eligibleAssetPath, ImportAssetOptions.ForceUpdate);
            }

            return eligibleAssetPaths.Count;
        }

        /// <summary>
        /// Determines whether the asset path resolves to a texture importer that qualifies for BC4 linear import processing.
        /// </summary>
        /// <param name="assetPath">The asset path to inspect.</param>
        /// <returns><see langword="true"/> when the resolved texture importer qualifies for BC4 linear import processing; otherwise, <see langword="false"/>.</returns>
        private static bool IsEligibleForBc4LinearImport(string assetPath)
        {
            return TryGetTextureImporter(assetPath, out TextureImporter textureImporter)
                   && BC4LinearImportTargeting.IsEligibleForBc4LinearImport(textureImporter);
        }

        /// <summary>
        /// Attempts to resolve the texture importer for the asset path.
        /// </summary>
        /// <param name="assetPath">The asset path to inspect.</param>
        /// <param name="textureImporter">The resolved texture importer when available.</param>
        /// <returns><see langword="true"/> when a texture importer was resolved; otherwise, <see langword="false"/>.</returns>
        private static bool TryGetTextureImporter(string assetPath, out TextureImporter textureImporter)
        {
            textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            return textureImporter != null;
        }
    }
}
