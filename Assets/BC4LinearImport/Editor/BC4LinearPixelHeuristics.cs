// Provides the metadata-free pixel-inference fallback for BC4 linear import detection.
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
using System.IO;
using UnityEngine;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Provides the metadata-free pixel-inference fallback for BC4 linear import detection.
    /// </summary>
    internal static class BC4LinearPixelHeuristics
    {
        /// <summary>
        /// Infers the source color intent from sampled pixel data when no higher-priority evidence exists.
        /// </summary>
        /// <param name="assetPath">The source asset path.</param>
        /// <param name="sourceBytes">The original source bytes.</param>
        /// <returns>The conservative pixel-inference decision.</returns>
        internal static BC4LinearColorDecision Infer(string assetPath, byte[] sourceBytes)
        {
            if (sourceBytes == null || sourceBytes.Length == 0)
            {
                return BC4LinearColorDecision.Unknown(
                    BC4LinearColorDecisionSource.PixelInference,
                    "PixelInference",
                    "Pixel inference skipped because the source bytes are empty.",
                    0.0f);
            }

            Texture2D sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!ImageConversion.LoadImage(sourceTexture, sourceBytes, false))
                {
                    return BC4LinearColorDecision.Unknown(
                        BC4LinearColorDecisionSource.PixelInference,
                        "PixelInference",
                        "Pixel inference could not decode the metadata-free source image.",
                        0.0f);
                }

                return AnalyzeTexture(assetPath, sourceTexture);
            }
            catch (Exception exception)
            {
                return BC4LinearColorDecision.Unknown(
                    BC4LinearColorDecisionSource.PixelInference,
                    "PixelInference",
                    $"Pixel inference failed safely with {exception.GetType().Name}.",
                    0.0f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceTexture);
            }
        }

        /// <summary>
        /// Analyzes the decoded texture and infers whether the sampled grayscale shape strongly resembles an sRGB-authored ramp.
        /// </summary>
        /// <param name="assetPath">The source asset path.</param>
        /// <param name="texture">The decoded source texture.</param>
        /// <returns>The conservative pixel-inference decision.</returns>
        private static BC4LinearColorDecision AnalyzeTexture(string assetPath, Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            if (pixels == null || pixels.Length < 16)
            {
                return BC4LinearColorDecision.Unknown(
                    BC4LinearColorDecisionSource.PixelInference,
                    "PixelInference",
                    "Pixel inference requires at least sixteen readable pixels.",
                    0.0f);
            }

            var grayscaleSamples = new List<float>(pixels.Length);
            float minValue = 1.0f;
            float maxValue = 0.0f;
            float accumulatedChannelDelta = 0.0f;

            for (int index = 0; index < pixels.Length; index++)
            {
                Color pixel = pixels[index];
                accumulatedChannelDelta += Mathf.Max(Mathf.Abs(pixel.r - pixel.g), Mathf.Abs(pixel.r - pixel.b));
                grayscaleSamples.Add(pixel.r);
                minValue = Mathf.Min(minValue, pixel.r);
                maxValue = Mathf.Max(maxValue, pixel.r);
            }

            float averageChannelDelta = accumulatedChannelDelta / pixels.Length;
            if (averageChannelDelta > 0.02f)
            {
                return BC4LinearColorDecision.Unknown(
                    BC4LinearColorDecisionSource.PixelInference,
                    "PixelInference",
                    "Pixel heuristic stayed conservative because the sampled image is not grayscale-dominant.",
                    0.15f);
            }

            float dynamicRange = maxValue - minValue;
            if (dynamicRange < 0.35f)
            {
                return BC4LinearColorDecision.Unknown(
                    BC4LinearColorDecisionSource.PixelInference,
                    "PixelInference",
                    "Pixel heuristic stayed conservative because the sampled grayscale range is too small for a confident inference.",
                    0.2f);
            }

            grayscaleSamples.Sort();
            double linearRampError = CalculateRampFitError(grayscaleSamples, useSrgbEncodedRamp: false);
            double srgbRampError = CalculateRampFitError(grayscaleSamples, useSrgbEncodedRamp: true);
            double fitMargin = linearRampError - srgbRampError;
            string formatName = GetFormatName(assetPath);

            if (srgbRampError <= 0.02d && fitMargin >= 0.01d)
            {
                float confidence = Mathf.Clamp01((float)(0.55d + (fitMargin * 6.0d)));
                return BC4LinearColorDecision.Convert(
                    BC4LinearColorDecisionSource.PixelInference,
                    "SortedRampFit",
                    $"Metadata-free {formatName} pixel heuristic detected an sRGB-like grayscale ramp signature (sRGB fit {srgbRampError:F4}, linear fit {linearRampError:F4}).",
                    confidence);
            }

            return BC4LinearColorDecision.Unknown(
                BC4LinearColorDecisionSource.PixelInference,
                "SortedRampFit",
                $"Metadata-free {formatName} pixel heuristic was inconclusive (sRGB fit {srgbRampError:F4}, linear fit {linearRampError:F4}).",
                Mathf.Clamp01((float)Math.Max(0.0d, fitMargin)));
        }

        /// <summary>
        /// Calculates the mean-squared fit error between the sorted grayscale samples and an expected ramp.
        /// </summary>
        /// <param name="sortedSamples">The sorted grayscale samples.</param>
        /// <param name="useSrgbEncodedRamp"><see langword="true"/> to compare against an sRGB-encoded linear ramp; otherwise a linear ramp.</param>
        /// <returns>The mean-squared fit error.</returns>
        private static double CalculateRampFitError(IReadOnlyList<float> sortedSamples, bool useSrgbEncodedRamp)
        {
            double error = 0.0d;
            int lastIndex = sortedSamples.Count - 1;
            for (int index = 0; index < sortedSamples.Count; index++)
            {
                float normalized = lastIndex <= 0 ? 0.0f : (float)index / lastIndex;
                float expectedValue = useSrgbEncodedRamp ? ConvertLinearToSrgb(normalized) : normalized;
                float delta = sortedSamples[index] - expectedValue;
                error += delta * delta;
            }

            return error / Math.Max(1, sortedSamples.Count);
        }

        /// <summary>
        /// Converts a normalized linear value into sRGB space.
        /// </summary>
        /// <param name="linearValue">The linear-space value.</param>
        /// <returns>The sRGB-encoded value.</returns>
        private static float ConvertLinearToSrgb(float linearValue)
        {
            linearValue = Mathf.Clamp01(linearValue);
            if (linearValue <= 0.0031308f)
            {
                return linearValue * 12.92f;
            }

            return (1.055f * Mathf.Pow(linearValue, 1.0f / 2.4f)) - 0.055f;
        }

        /// <summary>
        /// Gets the display format name for the asset path.
        /// </summary>
        /// <param name="assetPath">The source asset path.</param>
        /// <returns>The display format name.</returns>
        private static string GetFormatName(string assetPath)
        {
            string extension = string.IsNullOrWhiteSpace(assetPath) ? string.Empty : Path.GetExtension(assetPath);
            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ? "PNG" : "JPEG";
        }
    }
}
