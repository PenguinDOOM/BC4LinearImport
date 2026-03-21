// Adds explicit editor menu actions for BC4 linear import maintenance.
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
using UnityEditor;
using UnityEngine;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Adds explicit editor menu actions for BC4 linear import maintenance.
    /// </summary>
    public static class BC4LinearImportMenu
    {
        private const string ReimportMenuPath = "Tools/BC4 Linear Import/Reimport Eligible Textures";

        /// <summary>
        /// Reimports the currently eligible BC4 linear import assets.
        /// </summary>
        [MenuItem(ReimportMenuPath)]
        public static void ReimportEligibleTextures()
        {
            int reimportedAssetCount = BC4LinearImportReimportUtility.ReimportEligibleAssets();
            Debug.Log($"BC4 Linear Import reimported {reimportedAssetCount} eligible texture(s).");
        }

        /// <summary>
        /// Validates whether the explicit reimport action is currently available.
        /// </summary>
        /// <returns><see langword="true"/> when the menu action can run; otherwise, <see langword="false"/>.</returns>
        [MenuItem(ReimportMenuPath, true)]
        public static bool ValidateReimportEligibleTextures()
        {
            return !EditorApplication.isCompiling && !EditorApplication.isUpdating;
        }
    }
}
