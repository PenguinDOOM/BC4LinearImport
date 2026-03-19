// Provides shared fixture, valid-image, and source-contract helpers for BC4 linear import EditMode tests.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Shared fixture, valid-image, and source-contract helpers for BC4 linear import EditMode tests.
    /// </summary>
    internal static class BC4LinearImportFixtureUtility
    {
        /// <summary>
        /// Creates a metadata-free PNG byte stream with a grayscale ramp that follows an sRGB-encoded linear ramp.
        /// </summary>
        /// <returns>The PNG source bytes.</returns>
        internal static byte[] CreateSrgbEncodedRampPngBytes()
        {
            return CreateEncodedGrayscaleImageBytes(16, 16, value => ConvertLinearToSrgb(value), encodeToJpeg: false);
        }

        /// <summary>
        /// Creates a metadata-free JPEG byte stream with a grayscale ramp that follows an sRGB-encoded linear ramp.
        /// </summary>
        /// <returns>The JPEG source bytes.</returns>
        internal static byte[] CreateSrgbEncodedRampJpegBytes()
        {
            return CreateEncodedGrayscaleImageBytes(16, 16, value => ConvertLinearToSrgb(value), encodeToJpeg: true);
        }

        /// <summary>
        /// Creates a valid PNG byte stream whose grayscale ramp is sRGB-authored and carries an <c>sRGB</c> metadata chunk.
        /// </summary>
        /// <returns>The PNG source bytes.</returns>
        internal static byte[] CreateSrgbMetadataRampPngBytes()
        {
            byte[] encodedBytes = CreateSrgbEncodedRampPngBytes();
            byte[] srgbChunk = CreatePngChunkBytes("sRGB", new byte[] { 0 });
            return InsertPngChunksBeforeFirstIdat(encodedBytes, srgbChunk);
        }

        /// <summary>
        /// Creates a valid JPEG byte stream whose grayscale ramp is sRGB-authored and carries EXIF ColorSpace metadata.
        /// </summary>
        /// <returns>The JPEG source bytes.</returns>
        internal static byte[] CreateSrgbMetadataRampJpegBytes()
        {
            byte[] encodedBytes = CreateSrgbEncodedRampJpegBytes();
            byte[] exifSegment = CreateJpegSegmentBytes(0xE1, CreateExifColorSpaceSegmentData(1));
            return InsertJpegSegmentsAfterSoi(encodedBytes, exifSegment);
        }

        /// <summary>
        /// Creates a valid JPEG byte stream whose linear-authored ramp includes conflicting EXIF and ICC evidence so ICC wins.
        /// </summary>
        /// <returns>The JPEG source bytes.</returns>
        internal static byte[] CreateLinearProfileOverridesMetadataJpegBytes()
        {
            byte[] encodedBytes = CreateEncodedGrayscaleImageBytes(16, 16, value => value, encodeToJpeg: true);
            byte[] exifSegment = CreateJpegSegmentBytes(0xE1, CreateExifColorSpaceSegmentData(1));
            byte[] iccSegment = CreateJpegSegmentBytes(0xE2, CreateIccProfileSegmentData("Linear Gray"));
            return InsertJpegSegmentsAfterSoi(encodedBytes, exifSegment, iccSegment);
        }

        /// <summary>
        /// Creates a metadata-free flat PNG byte stream whose pixels are intentionally ambiguous.
        /// </summary>
        /// <returns>The PNG source bytes.</returns>
        internal static byte[] CreateFlatPngBytes()
        {
            return CreateEncodedGrayscaleImageBytes(8, 8, _ => 0.5f, encodeToJpeg: false);
        }

        /// <summary>
        /// Creates a metadata-free flat JPEG byte stream whose pixels are intentionally ambiguous.
        /// </summary>
        /// <returns>The JPEG source bytes.</returns>
        internal static byte[] CreateFlatJpegBytes()
        {
            return CreateEncodedGrayscaleImageBytes(8, 8, _ => 0.5f, encodeToJpeg: true);
        }

        /// <summary>
        /// Creates a minimal PNG byte stream with optional profile and metadata chunks for detector tests.
        /// </summary>
        /// <param name="embeddedProfileName">The optional embedded profile name to place into an <c>iCCP</c> chunk.</param>
        /// <param name="includeSrgbChunk"><see langword="true"/> to emit an <c>sRGB</c> chunk.</param>
        /// <param name="gammaValue">The optional <c>gAMA</c> value.</param>
        /// <param name="metadataBeforeProfile"><see langword="true"/> to emit metadata chunks before the profile chunk.</param>
        /// <returns>The constructed PNG source bytes.</returns>
        internal static byte[] CreatePngBytes(string embeddedProfileName = null, bool includeSrgbChunk = false, uint? gammaValue = null, bool metadataBeforeProfile = false)
        {
            using var stream = new MemoryStream();

            WriteBytes(stream, new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            WritePngChunk(stream, "IHDR", new byte[]
            {
                0, 0, 0, 1,
                0, 0, 0, 1,
                8,
                0,
                0,
                0,
                0
            });

            if (metadataBeforeProfile)
            {
                WriteOptionalPngMetadata(stream, includeSrgbChunk, gammaValue);
                WriteOptionalPngProfile(stream, embeddedProfileName);
            }
            else
            {
                WriteOptionalPngProfile(stream, embeddedProfileName);
                WriteOptionalPngMetadata(stream, includeSrgbChunk, gammaValue);
            }

            WritePngChunk(stream, "IDAT", Array.Empty<byte>());
            WritePngChunk(stream, "IEND", Array.Empty<byte>());
            return stream.ToArray();
        }

        /// <summary>
        /// Creates a deliberately truncated PNG byte stream.
        /// </summary>
        /// <returns>The truncated PNG source bytes.</returns>
        internal static byte[] CreateTruncatedPngBytes()
        {
            return new byte[] { 137, 80, 78, 71, 13, 10, 26 };
        }

        /// <summary>
        /// Creates a minimal JPEG byte stream with optional ICC and EXIF color metadata segments.
        /// </summary>
        /// <param name="embeddedProfileText">The optional ICC payload text.</param>
        /// <param name="exifColorSpace">The optional EXIF color-space tag value.</param>
        /// <param name="metadataBeforeProfile"><see langword="true"/> to emit EXIF before ICC.</param>
        /// <returns>The constructed JPEG source bytes.</returns>
        internal static byte[] CreateJpegBytes(string embeddedProfileText = null, ushort? exifColorSpace = null, bool metadataBeforeProfile = false)
        {
            using var stream = new MemoryStream();
            WriteBytes(stream, new byte[] { 0xFF, 0xD8 });

            if (metadataBeforeProfile)
            {
                WriteOptionalJpegMetadata(stream, exifColorSpace);
                WriteOptionalJpegProfile(stream, embeddedProfileText);
            }
            else
            {
                WriteOptionalJpegProfile(stream, embeddedProfileText);
                WriteOptionalJpegMetadata(stream, exifColorSpace);
            }

            WriteBytes(stream, new byte[] { 0xFF, 0xD9 });
            return stream.ToArray();
        }

        /// <summary>
        /// Creates a deliberately truncated JPEG byte stream.
        /// </summary>
        /// <returns>The truncated JPEG source bytes.</returns>
        internal static byte[] CreateTruncatedJpegBytes()
        {
            return new byte[] { 0xFF, 0xD8, 0xFF, 0xE2, 0x00 };
        }

        /// <summary>
        /// Resolves a project-relative asset path to an absolute filesystem path.
        /// </summary>
        /// <param name="assetRelativePath">The asset-relative path to resolve.</param>
        /// <returns>The absolute filesystem path.</returns>
        internal static string ResolveProjectPath(string assetRelativePath)
        {
            return BC4LinearImportTestPathUtility.ResolveCurrentProjectPath(assetRelativePath);
        }

        /// <summary>
        /// Writes the optional PNG profile chunk.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="embeddedProfileName">The embedded profile name.</param>
        private static void WriteOptionalPngProfile(Stream stream, string embeddedProfileName)
        {
            if (string.IsNullOrEmpty(embeddedProfileName))
            {
                return;
            }

            byte[] profileNameBytes = Encoding.ASCII.GetBytes(embeddedProfileName);
            byte[] compressedProfileBytes = Encoding.ASCII.GetBytes($"profile:{embeddedProfileName}");
            byte[] chunkData = new byte[profileNameBytes.Length + 2 + compressedProfileBytes.Length];
            Buffer.BlockCopy(profileNameBytes, 0, chunkData, 0, profileNameBytes.Length);
            chunkData[profileNameBytes.Length] = 0;
            chunkData[profileNameBytes.Length + 1] = 0;
            Buffer.BlockCopy(compressedProfileBytes, 0, chunkData, profileNameBytes.Length + 2, compressedProfileBytes.Length);
            WritePngChunk(stream, "iCCP", chunkData);
        }

        /// <summary>
        /// Writes the optional PNG metadata chunks.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="includeSrgbChunk"><see langword="true"/> to emit <c>sRGB</c>.</param>
        /// <param name="gammaValue">The optional <c>gAMA</c> value.</param>
        private static void WriteOptionalPngMetadata(Stream stream, bool includeSrgbChunk, uint? gammaValue)
        {
            if (includeSrgbChunk)
            {
                WritePngChunk(stream, "sRGB", new byte[] { 0 });
            }

            if (gammaValue.HasValue)
            {
                WritePngChunk(stream, "gAMA", GetBigEndianBytes(gammaValue.Value));
            }
        }

        /// <summary>
        /// Writes a PNG chunk without CRC validation requirements.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="chunkType">The PNG chunk type.</param>
        /// <param name="chunkData">The chunk payload.</param>
        private static void WritePngChunk(Stream stream, string chunkType, byte[] chunkData)
        {
            WriteBytes(stream, GetBigEndianBytes((uint)chunkData.Length));
            WriteBytes(stream, Encoding.ASCII.GetBytes(chunkType));
            WriteBytes(stream, chunkData);
            WriteBytes(stream, new byte[] { 0, 0, 0, 0 });
        }

        /// <summary>
        /// Writes the optional JPEG ICC profile segment.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="embeddedProfileText">The ICC payload text.</param>
        private static void WriteOptionalJpegProfile(Stream stream, string embeddedProfileText)
        {
            if (string.IsNullOrEmpty(embeddedProfileText))
            {
                return;
            }

            var segmentData = new List<byte>();
            segmentData.AddRange(Encoding.ASCII.GetBytes("ICC_PROFILE\0"));
            segmentData.Add(1);
            segmentData.Add(1);
            segmentData.AddRange(Encoding.ASCII.GetBytes(embeddedProfileText));
            WriteJpegSegment(stream, 0xE2, segmentData.ToArray());
        }

        /// <summary>
        /// Writes the optional JPEG EXIF metadata segment.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="exifColorSpace">The EXIF color-space tag value.</param>
        private static void WriteOptionalJpegMetadata(Stream stream, ushort? exifColorSpace)
        {
            if (!exifColorSpace.HasValue)
            {
                return;
            }

            var segmentData = new List<byte>();
            segmentData.AddRange(Encoding.ASCII.GetBytes("Exif\0\0"));
            segmentData.AddRange(Encoding.ASCII.GetBytes("II"));
            segmentData.AddRange(new byte[] { 42, 0 });
            segmentData.AddRange(new byte[] { 8, 0, 0, 0 });
            segmentData.AddRange(new byte[] { 1, 0 });
            segmentData.AddRange(new byte[] { 0x69, 0x87, 4, 0, 1, 0, 0, 0, 26, 0, 0, 0 });
            segmentData.AddRange(new byte[] { 0, 0, 0, 0 });
            segmentData.AddRange(new byte[] { 1, 0 });
            segmentData.AddRange(new byte[]
            {
                0x01, 0xA0,
                3, 0,
                1, 0, 0, 0,
                (byte)(exifColorSpace.Value & 0xFF),
                (byte)(exifColorSpace.Value >> 8),
                0,
                0
            });
            segmentData.AddRange(new byte[] { 0, 0, 0, 0 });
            WriteJpegSegment(stream, 0xE1, segmentData.ToArray());
        }

        /// <summary>
        /// Writes a JPEG metadata segment.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="marker">The JPEG segment marker without the <c>0xFF</c> prefix.</param>
        /// <param name="segmentData">The segment payload.</param>
        private static void WriteJpegSegment(Stream stream, byte marker, byte[] segmentData)
        {
            WriteBytes(stream, new byte[] { 0xFF, marker });
            ushort segmentLength = (ushort)(segmentData.Length + 2);
            WriteBytes(stream, new[] { (byte)(segmentLength >> 8), (byte)(segmentLength & 0xFF) });
            WriteBytes(stream, segmentData);
        }

        /// <summary>
        /// Creates a PNG chunk byte sequence with a valid CRC.
        /// </summary>
        /// <param name="chunkType">The chunk type.</param>
        /// <param name="chunkData">The chunk payload.</param>
        /// <returns>The PNG chunk bytes.</returns>
        private static byte[] CreatePngChunkBytes(string chunkType, byte[] chunkData)
        {
            byte[] chunkTypeBytes = Encoding.ASCII.GetBytes(chunkType);
            uint crc = ComputePngChunkCrc(chunkTypeBytes, chunkData);

            using var stream = new MemoryStream();
            WriteBytes(stream, GetBigEndianBytes((uint)chunkData.Length));
            WriteBytes(stream, chunkTypeBytes);
            WriteBytes(stream, chunkData);
            WriteBytes(stream, GetBigEndianBytes(crc));
            return stream.ToArray();
        }

        /// <summary>
        /// Inserts additional PNG chunks before the first <c>IDAT</c> chunk.
        /// </summary>
        /// <param name="pngBytes">The original valid PNG bytes.</param>
        /// <param name="chunks">The chunk byte sequences to insert.</param>
        /// <returns>The PNG bytes with the inserted chunks.</returns>
        private static byte[] InsertPngChunksBeforeFirstIdat(byte[] pngBytes, params byte[][] chunks)
        {
            if (pngBytes == null)
            {
                throw new ArgumentNullException(nameof(pngBytes));
            }

            int insertOffset = FindPngChunkOffset(pngBytes, "IDAT");
            if (insertOffset < 0)
            {
                throw new InvalidDataException("The valid PNG fixture does not contain an IDAT chunk.");
            }

            using var stream = new MemoryStream();
            stream.Write(pngBytes, 0, insertOffset);
            foreach (byte[] chunk in chunks)
            {
                WriteBytes(stream, chunk);
            }

            stream.Write(pngBytes, insertOffset, pngBytes.Length - insertOffset);
            return stream.ToArray();
        }

        /// <summary>
        /// Finds the offset of the requested PNG chunk.
        /// </summary>
        /// <param name="pngBytes">The PNG bytes to inspect.</param>
        /// <param name="chunkType">The chunk type to find.</param>
        /// <returns>The offset of the chunk length field, or <c>-1</c> when missing.</returns>
        private static int FindPngChunkOffset(byte[] pngBytes, string chunkType)
        {
            int offset = 8;
            while (offset + 8 <= pngBytes.Length)
            {
                int chunkOffset = offset;
                uint chunkLength = ReadUInt32BigEndian(pngBytes, offset);
                offset += 4;
                string currentChunkType = Encoding.ASCII.GetString(pngBytes, offset, 4);
                offset += 4;

                if (string.Equals(currentChunkType, chunkType, StringComparison.Ordinal))
                {
                    return chunkOffset;
                }

                offset += (int)chunkLength + 4;
            }

            return -1;
        }

        /// <summary>
        /// Inserts additional JPEG segments immediately after the SOI marker.
        /// </summary>
        /// <param name="jpegBytes">The original valid JPEG bytes.</param>
        /// <param name="segments">The JPEG segment byte sequences to insert.</param>
        /// <returns>The JPEG bytes with the inserted segments.</returns>
        private static byte[] InsertJpegSegmentsAfterSoi(byte[] jpegBytes, params byte[][] segments)
        {
            if (jpegBytes == null)
            {
                throw new ArgumentNullException(nameof(jpegBytes));
            }

            if (jpegBytes.Length < 2 || jpegBytes[0] != 0xFF || jpegBytes[1] != 0xD8)
            {
                throw new InvalidDataException("The valid JPEG fixture does not begin with an SOI marker.");
            }

            using var stream = new MemoryStream();
            stream.Write(jpegBytes, 0, 2);
            foreach (byte[] segment in segments)
            {
                WriteBytes(stream, segment);
            }

            stream.Write(jpegBytes, 2, jpegBytes.Length - 2);
            return stream.ToArray();
        }

        /// <summary>
        /// Creates the EXIF APP1 payload for the requested color-space value.
        /// </summary>
        /// <param name="exifColorSpace">The EXIF ColorSpace value.</param>
        /// <returns>The APP1 payload bytes.</returns>
        private static byte[] CreateExifColorSpaceSegmentData(ushort exifColorSpace)
        {
            var segmentData = new List<byte>();
            segmentData.AddRange(Encoding.ASCII.GetBytes("Exif\0\0"));
            segmentData.AddRange(Encoding.ASCII.GetBytes("II"));
            segmentData.AddRange(new byte[] { 42, 0 });
            segmentData.AddRange(new byte[] { 8, 0, 0, 0 });
            segmentData.AddRange(new byte[] { 1, 0 });
            segmentData.AddRange(new byte[] { 0x69, 0x87, 4, 0, 1, 0, 0, 0, 26, 0, 0, 0 });
            segmentData.AddRange(new byte[] { 0, 0, 0, 0 });
            segmentData.AddRange(new byte[] { 1, 0 });
            segmentData.AddRange(new byte[]
            {
                0x01, 0xA0,
                3, 0,
                1, 0, 0, 0,
                (byte)(exifColorSpace & 0xFF),
                (byte)(exifColorSpace >> 8),
                0,
                0
            });
            segmentData.AddRange(new byte[] { 0, 0, 0, 0 });
            return segmentData.ToArray();
        }

        /// <summary>
        /// Creates the ICC APP2 payload for the requested profile text.
        /// </summary>
        /// <param name="profileText">The profile text to encode.</param>
        /// <returns>The APP2 payload bytes.</returns>
        private static byte[] CreateIccProfileSegmentData(string profileText)
        {
            var segmentData = new List<byte>();
            segmentData.AddRange(Encoding.ASCII.GetBytes("ICC_PROFILE\0"));
            segmentData.Add(1);
            segmentData.Add(1);
            segmentData.AddRange(Encoding.ASCII.GetBytes(profileText));
            return segmentData.ToArray();
        }

        /// <summary>
        /// Creates a complete JPEG segment byte sequence.
        /// </summary>
        /// <param name="marker">The JPEG segment marker without the <c>0xFF</c> prefix.</param>
        /// <param name="segmentData">The segment payload.</param>
        /// <returns>The JPEG segment bytes.</returns>
        private static byte[] CreateJpegSegmentBytes(byte marker, byte[] segmentData)
        {
            using var stream = new MemoryStream();
            WriteJpegSegment(stream, marker, segmentData);
            return stream.ToArray();
        }

        /// <summary>
        /// Converts a 32-bit value to a big-endian byte array.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <returns>The big-endian byte array.</returns>
        private static byte[] GetBigEndianBytes(uint value)
        {
            return new[]
            {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
        }

        /// <summary>
        /// Writes raw bytes to the target stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="bytes">The bytes to write.</param>
        private static void WriteBytes(Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Computes the PNG CRC32 for a chunk type and payload.
        /// </summary>
        /// <param name="chunkTypeBytes">The ASCII chunk type bytes.</param>
        /// <param name="chunkData">The chunk payload.</param>
        /// <returns>The CRC32 value.</returns>
        private static uint ComputePngChunkCrc(byte[] chunkTypeBytes, byte[] chunkData)
        {
            const uint Polynomial = 0xEDB88320u;

            uint crc = 0xFFFFFFFFu;
            crc = UpdatePngCrc(crc, chunkTypeBytes, Polynomial);
            crc = UpdatePngCrc(crc, chunkData, Polynomial);
            return crc ^ 0xFFFFFFFFu;
        }

        /// <summary>
        /// Updates an in-progress PNG CRC32 value.
        /// </summary>
        /// <param name="crc">The in-progress CRC.</param>
        /// <param name="bytes">The bytes to apply.</param>
        /// <param name="polynomial">The CRC polynomial.</param>
        /// <returns>The updated CRC value.</returns>
        private static uint UpdatePngCrc(uint crc, byte[] bytes, uint polynomial)
        {
            foreach (byte value in bytes)
            {
                crc ^= value;
                for (int bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 1u) != 0u ? (crc >> 1) ^ polynomial : crc >> 1;
                }
            }

            return crc;
        }

        /// <summary>
        /// Reads a 32-bit big-endian PNG value.
        /// </summary>
        /// <param name="bytes">The source bytes.</param>
        /// <param name="offset">The value offset.</param>
        /// <returns>The decoded 32-bit value.</returns>
        private static uint ReadUInt32BigEndian(byte[] bytes, int offset)
        {
            return ((uint)bytes[offset] << 24)
                   | ((uint)bytes[offset + 1] << 16)
                   | ((uint)bytes[offset + 2] << 8)
                   | bytes[offset + 3];
        }

        /// <summary>
        /// Creates encoded grayscale image bytes from a normalized value generator.
        /// </summary>
        /// <param name="width">The texture width.</param>
        /// <param name="height">The texture height.</param>
        /// <param name="valueSelector">Selects the normalized grayscale value for each sample index.</param>
        /// <param name="encodeToJpeg"><see langword="true"/> to encode as JPEG; otherwise PNG.</param>
        /// <returns>The encoded image bytes.</returns>
        private static byte[] CreateEncodedGrayscaleImageBytes(int width, int height, Func<float, float> valueSelector, bool encodeToJpeg)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                var pixels = new Color[width * height];
                int lastIndex = pixels.Length - 1;
                for (int index = 0; index < pixels.Length; index++)
                {
                    float normalized = lastIndex <= 0 ? 0.0f : (float)index / lastIndex;
                    float value = Mathf.Clamp01(valueSelector(normalized));
                    pixels[index] = new Color(value, value, value, 1.0f);
                }

                texture.SetPixels(pixels);
                texture.Apply(false, false);
                return encodeToJpeg ? texture.EncodeToJPG(100) : texture.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// Converts a normalized linear value into sRGB space.
        /// </summary>
        /// <param name="linearValue">The normalized linear-space value.</param>
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
    }
}
