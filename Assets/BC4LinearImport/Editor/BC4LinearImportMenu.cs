// Adds explicit editor menu actions for BC4 linear import maintenance.
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
