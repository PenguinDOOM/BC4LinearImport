// Converts BC4-targeted grayscale textures from sRGB-authored source data to linear grayscale output.
using System;
using UnityEngine;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Identifies which backend executed the BC4 linearization conversion.
    /// </summary>
    public enum BC4LinearConversionBackend
    {
        /// <summary>
        /// No conversion backend ran.
        /// </summary>
        None,

        /// <summary>
        /// The compute-shader backend ran.
        /// </summary>
        ComputeShader,

        /// <summary>
        /// The deterministic CPU fallback ran.
        /// </summary>
        CpuFallback
    }

    /// <summary>
    /// Reports which backend executed and how many mip levels were processed.
    /// </summary>
    public readonly struct BC4LinearTextureConversionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BC4LinearTextureConversionResult"/> struct.
        /// </summary>
        /// <param name="backend">The backend that executed.</param>
        /// <param name="convertedMipCount">The number of converted mip levels.</param>
        public BC4LinearTextureConversionResult(BC4LinearConversionBackend backend, int convertedMipCount)
        {
            Backend = backend;
            ConvertedMipCount = convertedMipCount;
        }

        /// <summary>
        /// Gets the backend that executed.
        /// </summary>
        public BC4LinearConversionBackend Backend { get; }

        /// <summary>
        /// Gets the number of converted mip levels.
        /// </summary>
        public int ConvertedMipCount { get; }
    }

    /// <summary>
    /// Converts BC4-targeted grayscale textures from sRGB-authored source data to linear grayscale output.
    /// </summary>
    public static class BC4LinearTextureConverter
    {
        private const string ComputeShaderResourceName = "BC4Linearize";
        private const string KernelName = "BC4Linearize";

        /// <summary>
        /// Converts a normalized sRGB-authored channel value into linear space.
        /// </summary>
        /// <param name="srgbValue">The normalized sRGB-authored value.</param>
        /// <returns>The corresponding linear-space value.</returns>
        public static float ConvertSrgbChannelToLinear(float srgbValue)
        {
            srgbValue = Mathf.Clamp01(srgbValue);
            if (srgbValue <= 0.04045f)
            {
                return srgbValue / 12.92f;
            }

            return Mathf.Pow((srgbValue + 0.055f) / 1.055f, 2.4f);
        }

        /// <summary>
        /// Determines whether the compute backend can be used.
        /// </summary>
        /// <param name="computeShader">The compute shader resource.</param>
        /// <param name="supportsComputeOverride">An optional support override for tests.</param>
        /// <returns><see langword="true"/> when the compute backend can be used; otherwise, <see langword="false"/>.</returns>
        public static bool CanUseComputeBackend(ComputeShader computeShader, bool? supportsComputeOverride = null)
        {
            return computeShader != null && (supportsComputeOverride ?? SystemInfo.supportsComputeShaders);
        }

        /// <summary>
        /// Loads the BC4 linearization compute shader resource when available.
        /// </summary>
        /// <returns>The compute shader resource, or <see langword="null"/> when unavailable.</returns>
        public static ComputeShader LoadComputeShader()
        {
            return Resources.Load<ComputeShader>(ComputeShaderResourceName);
        }

        /// <summary>
        /// Converts every mip level of the provided texture in place.
        /// </summary>
        /// <param name="texture">The texture to convert.</param>
        /// <param name="preferCompute">A value indicating whether compute should be preferred.</param>
        /// <param name="supportsComputeOverride">An optional support override for tests.</param>
        /// <param name="computeShader">An optional compute shader override.</param>
        /// <returns>The conversion result.</returns>
        public static BC4LinearTextureConversionResult ConvertInPlace(
            Texture2D texture,
            bool preferCompute = true,
            bool? supportsComputeOverride = null,
            ComputeShader computeShader = null)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            ComputeShader resolvedComputeShader = computeShader != null ? computeShader : LoadComputeShader();
            int mipCount = Math.Max(1, texture.mipmapCount);

            if (preferCompute
                && CanUseComputeBackend(resolvedComputeShader, supportsComputeOverride)
                && SupportsComputeSourceTexture(texture)
                && TryConvertWithCompute(texture, resolvedComputeShader, mipCount))
            {
                return new BC4LinearTextureConversionResult(BC4LinearConversionBackend.ComputeShader, mipCount);
            }

            ConvertWithCpu(texture, mipCount);
            return new BC4LinearTextureConversionResult(BC4LinearConversionBackend.CpuFallback, mipCount);
        }

        /// <summary>
        /// Converts the texture deterministically on the CPU.
        /// </summary>
        /// <param name="texture">The texture to convert.</param>
        /// <param name="mipCount">The number of mip levels to convert.</param>
        private static void ConvertWithCpu(Texture2D texture, int mipCount)
        {
            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                Color[] pixels = texture.GetPixels(mipLevel);
                for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
                {
                    pixels[pixelIndex] = ConvertTexel(pixels[pixelIndex]);
                }

                texture.SetPixels(pixels, mipLevel);
            }

            texture.Apply(false, false);
        }

        /// <summary>
        /// Determines whether the imported texture format can be copied safely into the compute staging texture.
        /// </summary>
        /// <param name="texture">The texture to inspect.</param>
        /// <returns><see langword="true"/> when the compute path can stage the texture without format-mismatch errors; otherwise, <see langword="false"/>.</returns>
        private static bool SupportsComputeSourceTexture(Texture2D texture)
        {
            return texture.format == TextureFormat.RGBA32
                   || texture.format == TextureFormat.ARGB32
                   || texture.format == TextureFormat.BGRA32;
        }

        /// <summary>
        /// Attempts to convert the texture with the compute-shader backend.
        /// </summary>
        /// <param name="texture">The texture to convert.</param>
        /// <param name="computeShader">The compute shader resource.</param>
        /// <param name="mipCount">The number of mip levels to convert.</param>
        /// <returns><see langword="true"/> when the compute conversion succeeded; otherwise, <see langword="false"/>.</returns>
        private static bool TryConvertWithCompute(Texture2D texture, ComputeShader computeShader, int mipCount)
        {
            try
            {
                int kernelIndex = computeShader.FindKernel(KernelName);
                for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
                {
                    ConvertMipWithCompute(texture, computeShader, kernelIndex, mipLevel);
                }

                texture.Apply(false, false);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"BC4 linear compute conversion failed for texture '{texture.name}' and will fall back to the CPU path. {ex.GetType().Name}: {ex.Message}",
                    texture);
                return false;
            }
        }

        /// <summary>
        /// Converts a single mip level with the compute-shader backend.
        /// </summary>
        /// <param name="texture">The texture to convert.</param>
        /// <param name="computeShader">The compute shader resource.</param>
        /// <param name="kernelIndex">The compute kernel index.</param>
        /// <param name="mipLevel">The mip level to convert.</param>
        private static void ConvertMipWithCompute(Texture2D texture, ComputeShader computeShader, int kernelIndex, int mipLevel)
        {
            int width = Math.Max(1, texture.width >> mipLevel);
            int height = Math.Max(1, texture.height >> mipLevel);
            RenderTexture sourceRenderTexture = default;
            RenderTexture destinationRenderTexture = default;
            Texture2D stagingTexture = null;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                sourceRenderTexture = CreateRenderTexture(width, height, enableRandomWrite: false);
                destinationRenderTexture = CreateRenderTexture(width, height, enableRandomWrite: true);
                stagingTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);

                Graphics.CopyTexture(texture, 0, mipLevel, sourceRenderTexture, 0, 0);
                computeShader.SetTexture(kernelIndex, "_Source", sourceRenderTexture);
                computeShader.SetTexture(kernelIndex, "_Destination", destinationRenderTexture);
                computeShader.SetInts("_TextureSize", width, height);
                computeShader.Dispatch(kernelIndex, (width + 7) / 8, (height + 7) / 8, 1);

                RenderTexture.active = destinationRenderTexture;
                stagingTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                stagingTexture.Apply(false, false);
                texture.SetPixels(stagingTexture.GetPixels(), mipLevel);
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (stagingTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(stagingTexture);
                }

                if (destinationRenderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(destinationRenderTexture);
                }

                if (sourceRenderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(sourceRenderTexture);
                }
            }
        }

        /// <summary>
        /// Creates a temporary render texture for the requested conversion stage.
        /// </summary>
        /// <param name="width">The texture width.</param>
        /// <param name="height">The texture height.</param>
        /// <param name="enableRandomWrite"><see langword="true"/> for the compute destination texture.</param>
        /// <returns>The temporary render texture.</returns>
        private static RenderTexture CreateRenderTexture(int width, int height, bool enableRandomWrite)
        {
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0)
            {
                sRGB = false,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = enableRandomWrite
            };

            RenderTexture renderTexture = RenderTexture.GetTemporary(descriptor);
            renderTexture.filterMode = FilterMode.Point;
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            return renderTexture;
        }

        /// <summary>
        /// Converts a single grayscale texel using the same formula as the compute shader.
        /// </summary>
        /// <param name="sourceTexel">The source texel.</param>
        /// <returns>The converted destination texel.</returns>
        private static Color ConvertTexel(Color sourceTexel)
        {
            float linearValue = ConvertSrgbChannelToLinear(sourceTexel.r);
            return new Color(linearValue, linearValue, linearValue, sourceTexel.a);
        }
    }
}
