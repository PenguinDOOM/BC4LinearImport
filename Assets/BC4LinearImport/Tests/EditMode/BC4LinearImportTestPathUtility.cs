// Resolves current-project BC4 test paths for the BC4 Linear Import EditMode test scaffold.
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