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
using UnityEngine;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Describes the result of resolving dropped Unity objects into project exclusion paths.
    /// </summary>
    public readonly struct BC4LinearImportExcludedPathDropResolutionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BC4LinearImportExcludedPathDropResolutionResult"/> struct.
        /// </summary>
        /// <param name="assetPaths">The resolved canonical project asset paths.</param>
        /// <param name="ignoredObjectCount">The number of dropped objects that could not be resolved to valid project asset paths.</param>
        public BC4LinearImportExcludedPathDropResolutionResult(IReadOnlyList<string> assetPaths, int ignoredObjectCount)
        {
            AssetPaths = assetPaths ?? Array.Empty<string>();
            IgnoredObjectCount = ignoredObjectCount;
        }

        /// <summary>
        /// Gets the canonical project asset paths resolved from the current drop operation.
        /// </summary>
        public IReadOnlyList<string> AssetPaths { get; }

        /// <summary>
        /// Gets the number of dropped objects that were ignored because they were invalid or outside the project asset root.
        /// </summary>
        public int IgnoredObjectCount { get; }
    }

    /// <summary>
    /// Resolves dropped Unity objects into project asset paths for the BC4 exclusion UI workflow.
    /// </summary>
    public static class BC4LinearImportExcludedPathDragAndDropUtility
    {
        /// <summary>
        /// Resolves dropped Unity objects into canonical project asset paths.
        /// </summary>
        /// <param name="droppedObjects">The dropped Unity objects to inspect, or <see langword="null"/> to resolve no paths.</param>
        /// <returns>The canonical project asset paths in input order, or an empty collection when <paramref name="droppedObjects"/> is <see langword="null"/>.</returns>
        public static IReadOnlyList<string> ResolveDroppedAssetPaths(IEnumerable<UnityEngine.Object> droppedObjects)
        {
            return ResolveDroppedAssetPathsWithReport(droppedObjects).AssetPaths;
        }

        /// <summary>
        /// Resolves dropped Unity objects into canonical project asset paths and reports how many objects were ignored.
        /// </summary>
        /// <param name="droppedObjects">The dropped Unity objects to inspect, or <see langword="null"/> to resolve no paths.</param>
        /// <returns>The canonical project asset paths plus ignored-object diagnostics for the current drop operation.</returns>
        public static BC4LinearImportExcludedPathDropResolutionResult ResolveDroppedAssetPathsWithReport(IEnumerable<UnityEngine.Object> droppedObjects)
        {
            if (droppedObjects == null)
            {
                return new BC4LinearImportExcludedPathDropResolutionResult(Array.Empty<string>(), 0);
            }

            var resolvedAssetPaths = new List<string>();
            var seenAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int ignoredObjectCount = 0;

            foreach (UnityEngine.Object droppedObject in droppedObjects)
            {
                if (!TryResolveDroppedAssetPath(droppedObject, out string resolvedAssetPath))
                {
                    ignoredObjectCount++;
                    continue;
                }

                if (seenAssetPaths.Add(resolvedAssetPath))
                {
                    resolvedAssetPaths.Add(resolvedAssetPath);
                }
            }

            return new BC4LinearImportExcludedPathDropResolutionResult(resolvedAssetPaths, ignoredObjectCount);
        }

        /// <summary>
        /// Attempts to resolve a dropped object into a canonical project asset path.
        /// </summary>
        /// <param name="droppedObject">The dropped object to inspect.</param>
        /// <param name="assetPath">The canonical project asset path when resolution succeeds; otherwise, an empty string.</param>
        /// <returns><see langword="true"/> when the dropped object resolves to a valid project asset path; otherwise, <see langword="false"/>.</returns>
        internal static bool TryResolveDroppedAssetPath(UnityEngine.Object droppedObject, out string assetPath)
        {
            if (droppedObject == null)
            {
                assetPath = string.Empty;
                return false;
            }

            string droppedAssetPath = AssetDatabase.GetAssetPath(droppedObject);
            if (!BC4LinearImportSettings.TrySanitizeExcludedAssetPath(droppedAssetPath, out string sanitizedAssetPath))
            {
                assetPath = string.Empty;
                return false;
            }

            assetPath = sanitizedAssetPath;
            return true;
        }
    }
}