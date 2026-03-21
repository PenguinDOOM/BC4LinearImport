// Detects Phase 2 BC4 source color intent for PNG and JPEG-family imports.
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
using System.IO;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Describes the ordered detection stages used by the BC4 linear import workflow.
    /// </summary>
    public enum BC4LinearDetectionStage
    {
        /// <summary>
        /// Detection based on embedded color profile data.
        /// </summary>
        EmbeddedProfile,

        /// <summary>
        /// Detection based on image metadata.
        /// </summary>
        Metadata,

        /// <summary>
        /// Detection based on pixel-data heuristics.
        /// </summary>
        PixelInference
    }

    /// <summary>
    /// Detects Phase 2 BC4 source color intent for PNG and JPEG-family imports.
    /// </summary>
    public static class BC4LinearColorDetector
    {
        private static readonly string[] SupportedSourceExtensions = { ".png", ".jpg", ".jpeg" };

        private static readonly BC4LinearDetectionStage[] OrderedDetectionPrecedence =
        {
            BC4LinearDetectionStage.EmbeddedProfile,
            BC4LinearDetectionStage.Metadata,
            BC4LinearDetectionStage.PixelInference
        };

        /// <summary>
        /// Gets the currently declared set of supported source extensions.
        /// </summary>
        public static IReadOnlyList<string> SupportedExtensions => SupportedSourceExtensions;

        /// <summary>
        /// Gets the currently declared detection precedence order.
        /// </summary>
        public static IReadOnlyList<BC4LinearDetectionStage> DetectionPrecedence => OrderedDetectionPrecedence;

        /// <summary>
        /// Detects the BC4 color intent for supported PNG and JPEG-family source bytes.
        /// </summary>
        /// <param name="assetPath">The asset path used for extension gating.</param>
        /// <param name="sourceBytes">The original source bytes.</param>
        /// <returns>The detector decision together with its reason and evidence source.</returns>
        public static BC4LinearColorDecision Detect(string assetPath, byte[] sourceBytes)
        {
            if (!TryGetSupportedExtension(assetPath, out string extension))
            {
                string rejectedExtension = string.IsNullOrWhiteSpace(assetPath) ? string.Empty : Path.GetExtension(assetPath);
                return BC4LinearColorDecision.Unknown(
                    BC4LinearColorDecisionSource.ExtensionGate,
                    string.IsNullOrEmpty(rejectedExtension) ? "extension" : rejectedExtension,
                    $"BC4 linear detection only supports .png, .jpg, and .jpeg sources; received '{rejectedExtension}'.",
                    0.0f);
            }

            if (sourceBytes == null || sourceBytes.Length == 0)
            {
                return BC4LinearColorDecision.Unknown(
                    BC4LinearColorDecisionSource.FormatValidation,
                    extension,
                    $"{GetFormatName(extension)} source bytes are empty and cannot be classified.",
                    0.0f);
            }

            BC4LinearInspectionEvidence evidence;
            try
            {
                evidence = extension == ".png"
                    ? BC4LinearPngInspector.Inspect(sourceBytes)
                    : BC4LinearJpegInspector.Inspect(sourceBytes);
            }
            catch (System.Exception exception)
            {
                return BC4LinearColorDecision.Unknown(
                    BC4LinearColorDecisionSource.FormatValidation,
                    GetFormatName(extension),
                    $"{GetFormatName(extension)} inspection failed safely with {exception.GetType().Name}.",
                    0.0f);
            }

            if (evidence.HasEmbeddedProfileDecision)
            {
                return evidence.EmbeddedProfileDecision;
            }

            if (evidence.HasMetadataDecision)
            {
                return evidence.MetadataDecision;
            }

            if (evidence.FallbackDecision.Source != BC4LinearColorDecisionSource.None)
            {
                return evidence.FallbackDecision;
            }

            try
            {
                return BC4LinearPixelHeuristics.Infer(assetPath, sourceBytes);
            }
            catch (System.Exception exception)
            {
                return BC4LinearColorDecision.Unknown(
                    BC4LinearColorDecisionSource.PixelInference,
                    "PixelInference",
                    $"Pixel inference failed safely with {exception.GetType().Name}.",
                    0.0f);
            }
        }

        /// <summary>
        /// Attempts to extract a supported source extension from the asset path.
        /// </summary>
        /// <param name="assetPath">The asset path to inspect.</param>
        /// <param name="extension">The supported extension when available.</param>
        /// <returns><see langword="true"/> when a supported extension was found; otherwise, <see langword="false"/>.</returns>
        private static bool TryGetSupportedExtension(string assetPath, out string extension)
        {
            extension = string.Empty;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            string candidate = Path.GetExtension(assetPath);
            if (string.IsNullOrEmpty(candidate))
            {
                return false;
            }

            foreach (string supportedExtension in SupportedSourceExtensions)
            {
                if (string.Equals(candidate, supportedExtension, System.StringComparison.OrdinalIgnoreCase))
                {
                    extension = supportedExtension;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the display format name for the supported extension.
        /// </summary>
        /// <param name="extension">The supported extension.</param>
        /// <returns>The display format name.</returns>
        private static string GetFormatName(string extension)
        {
            return extension == ".png" ? "PNG" : "JPEG";
        }
    }
}
