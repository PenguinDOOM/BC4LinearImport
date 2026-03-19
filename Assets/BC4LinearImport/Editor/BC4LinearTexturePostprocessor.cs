// Provides the import-hook scaffold for the BC4 linear texture workflow.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Captures the final decision and conversion result from a BC4 import pass.
    /// </summary>
    internal readonly struct BC4LinearImportProcessReport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BC4LinearImportProcessReport"/> struct.
        /// </summary>
        /// <param name="assetPath">The imported asset path.</param>
        /// <param name="decisionKind">The final detector decision kind.</param>
        /// <param name="decisionSource">The final detector decision source.</param>
        /// <param name="sourceMarker">The decision source marker.</param>
        /// <param name="converted">A value indicating whether the texture was converted.</param>
        /// <param name="conversionBackend">The conversion backend that ran.</param>
        /// <param name="convertedMipCount">The number of converted mip levels.</param>
        internal BC4LinearImportProcessReport(
            string assetPath,
            BC4LinearColorDecisionKind decisionKind,
            BC4LinearColorDecisionSource decisionSource,
            string sourceMarker,
            bool converted,
            BC4LinearConversionBackend conversionBackend,
            int convertedMipCount)
        {
            AssetPath = assetPath ?? string.Empty;
            DecisionKind = decisionKind;
            DecisionSource = decisionSource;
            SourceMarker = sourceMarker ?? string.Empty;
            Converted = converted;
            ConversionBackend = conversionBackend;
            ConvertedMipCount = convertedMipCount;
        }

        /// <summary>
        /// Gets the imported asset path.
        /// </summary>
        internal string AssetPath { get; }

        /// <summary>
        /// Gets the final detector decision kind.
        /// </summary>
        internal BC4LinearColorDecisionKind DecisionKind { get; }

        /// <summary>
        /// Gets the final detector decision source.
        /// </summary>
        internal BC4LinearColorDecisionSource DecisionSource { get; }

        /// <summary>
        /// Gets the final detector source marker.
        /// </summary>
        internal string SourceMarker { get; }

        /// <summary>
        /// Gets a value indicating whether conversion ran.
        /// </summary>
        internal bool Converted { get; }

        /// <summary>
        /// Gets the conversion backend that ran.
        /// </summary>
        internal BC4LinearConversionBackend ConversionBackend { get; }

        /// <summary>
        /// Gets the number of converted mip levels.
        /// </summary>
        internal int ConvertedMipCount { get; }
    }

    /// <summary>
    /// Provides the import-hook scaffold for the BC4 linear texture workflow.
    /// </summary>
    public sealed class BC4LinearTexturePostprocessor : AssetPostprocessor
    {
        private const string SessionStateKeyPrefix = "BC4LinearImport.Editor.SessionGuard.";
        private static BC4LinearImportProcessReport lastProcessReport;

        /// <summary>
        /// Defines the declared postprocess order for the BC4 linear workflow.
        /// </summary>
        /// <remarks>
        /// This must stay greater than the VRCSDK <c>PerceptualPostProcessor</c> effective order.
        /// VRCSDK does not currently override <c>GetPostprocessOrder()</c>, so its effective order is the implicit default of <c>0</c>.
        /// Keeping this at <c>1</c> ensures BC4 linearization runs after DPID/perceptual mip generation and preserves that final mip chain.
        /// </remarks>
        public const int DeclaredPostprocessOrder = 1;

        /// <summary>
        /// Gets the last import-processing report captured by the postprocessor.
        /// </summary>
        internal static BC4LinearImportProcessReport LastProcessReport => lastProcessReport;

        /// <summary>
        /// Resets the last import-processing report.
        /// </summary>
        internal static void ResetLastProcessReport()
        {
            lastProcessReport = default;
        }

        /// <summary>
        /// Returns the current postprocess order for the BC4 import scaffold.
        /// </summary>
        /// <returns>The current postprocess order value.</returns>
        public override int GetPostprocessOrder()
        {
            return DeclaredPostprocessOrder;
        }

        /// <summary>
        /// Determines whether the asset path matches the currently declared supported extensions.
        /// </summary>
        /// <param name="assetPath">The asset path to inspect.</param>
        /// <returns><see langword="true"/> when the path uses a declared supported extension; otherwise, <see langword="false"/>.</returns>
        public static bool SupportsAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            string extension = Path.GetExtension(assetPath);
            foreach (string supportedExtension in BC4LinearColorDetector.SupportedExtensions)
            {
                if (string.Equals(extension, supportedExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to acquire the one-shot SessionState guard for the given asset path.
        /// </summary>
        /// <param name="assetPath">The asset path that is being processed.</param>
        /// <returns><see langword="true"/> when the guard was acquired; otherwise, <see langword="false"/>.</returns>
        public static bool TryAcquireSessionGuard(string assetPath)
        {
            string key = GetSessionGuardKey(assetPath);
            if (SessionState.GetBool(key, false))
            {
                return false;
            }

            SessionState.SetBool(key, true);
            return true;
        }

        /// <summary>
        /// Releases the one-shot SessionState guard for the given asset path.
        /// </summary>
        /// <param name="assetPath">The asset path that finished processing.</param>
        public static void ReleaseSessionGuard(string assetPath)
        {
            SessionState.SetBool(GetSessionGuardKey(assetPath), false);
        }

        /// <summary>
        /// Performs the preprocess gate checks for the BC4 linear import workflow.
        /// </summary>
        private void OnPreprocessTexture()
        {
        }

        /// <summary>
        /// Performs the postprocess BC4 linearization workflow.
        /// </summary>
        /// <param name="texture">The imported texture.</param>
        private void OnPostprocessTexture(Texture2D texture)
        {
            if (texture == null || !ShouldProcessCurrentAsset())
            {
                return;
            }

            if (!TryAcquireSessionGuard(assetPath))
            {
                lastProcessReport = new BC4LinearImportProcessReport(
                    assetPath,
                    BC4LinearColorDecisionKind.Unknown,
                    BC4LinearColorDecisionSource.None,
                    "Session guard already active",
                    converted: false,
                    BC4LinearConversionBackend.None,
                    convertedMipCount: 0);
                return;
            }

            try
            {
                byte[] sourceBytes = TryReadSourceBytes(assetPath);
                if (sourceBytes.Length == 0)
                {
                    lastProcessReport = new BC4LinearImportProcessReport(
                        assetPath,
                        BC4LinearColorDecisionKind.Unknown,
                        BC4LinearColorDecisionSource.FormatValidation,
                        "Source bytes unavailable",
                        converted: false,
                        BC4LinearConversionBackend.None,
                        convertedMipCount: 0);
                    return;
                }

                BC4LinearColorDecision decision = BC4LinearColorDetector.Detect(assetPath, sourceBytes);
                if (!decision.ShouldConvert)
                {
                    lastProcessReport = new BC4LinearImportProcessReport(
                        assetPath,
                        decision.DecisionKind,
                        decision.Source,
                        decision.SourceMarker,
                        converted: false,
                        BC4LinearConversionBackend.None,
                        convertedMipCount: 0);
                    return;
                }

                BC4LinearTextureConversionResult conversionResult = BC4LinearTextureConverter.ConvertInPlace(texture);
                lastProcessReport = new BC4LinearImportProcessReport(
                    assetPath,
                    decision.DecisionKind,
                    decision.Source,
                    decision.SourceMarker,
                    converted: true,
                    conversionResult.Backend,
                    conversionResult.ConvertedMipCount);
            }
            finally
            {
                ReleaseSessionGuard(assetPath);
            }
        }

        /// <summary>
        /// Determines whether the current asset satisfies the shared BC4 targeting and settings gates.
        /// </summary>
        /// <returns><see langword="true"/> when the current asset should be processed; otherwise, <see langword="false"/>.</returns>
        private bool ShouldProcessCurrentAsset()
        {
            if (!SupportsAssetPath(assetPath))
            {
                return false;
            }

            if (!(assetImporter is TextureImporter textureImporter))
            {
                return false;
            }

            if (!BC4LinearImportSettings.instance.IsEnabledForAssetPath(assetPath))
            {
                return false;
            }

            return BC4LinearImportTargeting.IsEligibleForBc4LinearImport(textureImporter);
        }

        /// <summary>
        /// Reads the original source bytes for the imported asset.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <returns>The source bytes, or an empty array when the source file is unavailable.</returns>
        private static byte[] TryReadSourceBytes(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return Array.Empty<byte>();
            }

            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            return File.Exists(absolutePath) ? File.ReadAllBytes(absolutePath) : Array.Empty<byte>();
        }

        /// <summary>
        /// Builds the one-shot SessionState key for the provided asset path.
        /// </summary>
        /// <param name="assetPath">The asset-relative path.</param>
        /// <returns>The SessionState key.</returns>
        private static string GetSessionGuardKey(string assetPath)
        {
            string normalizedAssetPath = string.IsNullOrWhiteSpace(assetPath)
                ? string.Empty
                : assetPath.Replace('\\', '/').Trim();
            string guid = string.IsNullOrEmpty(normalizedAssetPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(normalizedAssetPath);
            string stableKeySuffix = string.IsNullOrEmpty(guid) ? normalizedAssetPath : guid;
            return SessionStateKeyPrefix + stableKeySuffix;
        }
    }
}
