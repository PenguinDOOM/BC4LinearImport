// Inspects JPEG source bytes for BC4 color intent using ICC-first and EXIF-second evidence.
using System;
using System.Text;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Inspects JPEG source bytes for BC4 color intent using ICC-first and EXIF-second evidence.
    /// </summary>
    internal static class BC4LinearJpegInspector
    {
        /// <summary>
        /// Inspects JPEG source bytes for embedded-profile and metadata evidence.
        /// </summary>
        /// <param name="sourceBytes">The JPEG source bytes.</param>
        /// <returns>The inspection evidence for the detector.</returns>
        internal static BC4LinearInspectionEvidence Inspect(byte[] sourceBytes)
        {
            if (sourceBytes == null || sourceBytes.Length < 4)
            {
                return BC4LinearInspectionEvidence.Create(
                    BC4LinearColorDecision.Unknown(
                        BC4LinearColorDecisionSource.FormatValidation,
                        "JPEG",
                        "JPEG source bytes are truncated before the file signature.",
                        0.1f));
            }

            if (sourceBytes[0] != 0xFF || sourceBytes[1] != 0xD8)
            {
                return BC4LinearInspectionEvidence.Create(
                    BC4LinearColorDecision.Unknown(
                        BC4LinearColorDecisionSource.FormatValidation,
                        "JPEG",
                        "Source bytes do not match the JPEG SOI marker.",
                        0.1f));
            }

            BC4LinearColorDecision embeddedProfileDecision = null;
            BC4LinearColorDecision metadataDecision = null;
            BC4LinearColorDecision fallbackDecision = BC4LinearColorDecision.Unknown(
                BC4LinearColorDecisionSource.None,
                "JPEG",
                "JPEG contains no supported ICC or EXIF color evidence.",
                0.0f);
            int offset = 2;

            while (offset < sourceBytes.Length)
            {
                if (!HasRemainingBytes(offset, sourceBytes.Length, 2))
                {
                    return BC4LinearInspectionEvidence.Create(
                        BC4LinearColorDecision.Unknown(
                            BC4LinearColorDecisionSource.FormatValidation,
                            "JPEG",
                            "JPEG marker stream is truncated.",
                            0.1f),
                        embeddedProfileDecision,
                        metadataDecision);
                }

                if (sourceBytes[offset] != 0xFF)
                {
                    return BC4LinearInspectionEvidence.Create(
                        BC4LinearColorDecision.Unknown(
                            BC4LinearColorDecisionSource.FormatValidation,
                            "JPEG",
                            $"JPEG marker stream became invalid at offset {offset}.",
                            0.1f),
                        embeddedProfileDecision,
                        metadataDecision);
                }

                while (offset < sourceBytes.Length && sourceBytes[offset] == 0xFF)
                {
                    offset++;
                }

                if (offset >= sourceBytes.Length)
                {
                    return BC4LinearInspectionEvidence.Create(
                        BC4LinearColorDecision.Unknown(
                            BC4LinearColorDecisionSource.FormatValidation,
                            "JPEG",
                            "JPEG marker stream is truncated after a marker prefix.",
                            0.1f),
                        embeddedProfileDecision,
                        metadataDecision);
                }

                byte marker = sourceBytes[offset++];
                if (marker == 0xD9 || marker == 0xDA)
                {
                    break;
                }

                if (IsStandaloneMarker(marker))
                {
                    continue;
                }

                if (!HasRemainingBytes(offset, sourceBytes.Length, 2))
                {
                    return BC4LinearInspectionEvidence.Create(
                        BC4LinearColorDecision.Unknown(
                            BC4LinearColorDecisionSource.FormatValidation,
                            "JPEG",
                            $"JPEG segment 0x{marker:X2} is missing its declared length.",
                            0.1f),
                        embeddedProfileDecision,
                        metadataDecision);
                }

                int segmentLength = (sourceBytes[offset] << 8) | sourceBytes[offset + 1];
                offset += 2;
                if (segmentLength < 2)
                {
                    return BC4LinearInspectionEvidence.Create(
                        BC4LinearColorDecision.Unknown(
                            BC4LinearColorDecisionSource.FormatValidation,
                            $"0x{marker:X2}",
                            $"JPEG segment 0x{marker:X2} declares an invalid length.",
                            0.1f),
                        embeddedProfileDecision,
                        metadataDecision);
                }

                int segmentDataLength = segmentLength - 2;
                if (sourceBytes.Length - offset < segmentDataLength)
                {
                    return BC4LinearInspectionEvidence.Create(
                        BC4LinearColorDecision.Unknown(
                            BC4LinearColorDecisionSource.FormatValidation,
                            $"0x{marker:X2}",
                            $"JPEG segment 0x{marker:X2} is truncated.",
                            0.1f),
                        embeddedProfileDecision,
                        metadataDecision);
                }

                if (marker == 0xE2 && embeddedProfileDecision == null)
                {
                    BC4LinearColorDecision iccDecision = TryCreateIccDecision(sourceBytes, offset, segmentDataLength);
                    if (iccDecision != null)
                    {
                        embeddedProfileDecision = iccDecision;
                    }
                }
                else if (marker == 0xE1 && metadataDecision == null)
                {
                    BC4LinearColorDecision exifDecision = TryCreateExifDecision(sourceBytes, offset, segmentDataLength);
                    if (exifDecision != null)
                    {
                        metadataDecision = exifDecision;
                    }
                }

                offset += segmentDataLength;
            }

            return BC4LinearInspectionEvidence.Create(fallbackDecision, embeddedProfileDecision, metadataDecision);
        }

        /// <summary>
        /// Determines whether the marker is a standalone JPEG marker with no length field.
        /// </summary>
        /// <param name="marker">The JPEG marker.</param>
        /// <returns><see langword="true"/> when the marker is standalone; otherwise, <see langword="false"/>.</returns>
        private static bool IsStandaloneMarker(byte marker)
        {
            return marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7);
        }

        /// <summary>
        /// Attempts to create an ICC-profile decision from an APP2 segment.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="segmentOffset">The segment offset.</param>
        /// <param name="segmentDataLength">The segment data length.</param>
        /// <returns>The classified embedded-profile decision, or <see langword="null"/> when unsupported.</returns>
        private static BC4LinearColorDecision TryCreateIccDecision(byte[] sourceBytes, int segmentOffset, int segmentDataLength)
        {
            const string Identifier = "ICC_PROFILE\0";
            int identifierLength = Identifier.Length;
            if (segmentDataLength < identifierLength + 2)
            {
                return null;
            }

            if (!MatchesAscii(sourceBytes, segmentOffset, Identifier))
            {
                return null;
            }

            int profileDataOffset = segmentOffset + identifierLength + 2;
            int profileDataLength = segmentDataLength - identifierLength - 2;
            if (profileDataLength <= 0)
            {
                return null;
            }

            // Phase 2 only interprets the ICC payload as ASCII because the current byte fixtures embed
            // descriptive test text instead of a real ICC profile body. Follow-up work should replace this
            // with proper ICC profile description parsing from the binary payload.
            string profileText = Encoding.ASCII.GetString(sourceBytes, profileDataOffset, profileDataLength).Trim('\0', ' ');
            return BC4LinearProfileClassifier.CreateDecisionFromProfileText(profileText, "JPEG", "ICC APP2");
        }

        /// <summary>
        /// Attempts to create an EXIF metadata decision from an APP1 segment.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="segmentOffset">The segment offset.</param>
        /// <param name="segmentDataLength">The segment data length.</param>
        /// <returns>The classified metadata decision, or <see langword="null"/> when unsupported.</returns>
        private static BC4LinearColorDecision TryCreateExifDecision(byte[] sourceBytes, int segmentOffset, int segmentDataLength)
        {
            if (segmentDataLength < 14 || !MatchesAscii(sourceBytes, segmentOffset, "Exif\0\0"))
            {
                return null;
            }

            int tiffOffset = segmentOffset + 6;
            bool? bigEndian = TryReadEndianFlag(sourceBytes, tiffOffset);
            if (!bigEndian.HasValue)
            {
                return null;
            }

            int segmentEnd = segmentOffset + segmentDataLength;
            if (!TryReadUInt16(sourceBytes, tiffOffset + 2, segmentEnd, bigEndian.Value, out ushort magic) || magic != 42)
            {
                return null;
            }

            if (!TryReadUInt32(sourceBytes, tiffOffset + 4, segmentEnd, bigEndian.Value, out uint ifd0Offset))
            {
                return null;
            }

            int ifd0AbsoluteOffset = tiffOffset + (int)ifd0Offset;
            if (TryReadExifColorSpace(sourceBytes, tiffOffset, ifd0AbsoluteOffset, segmentEnd, bigEndian.Value, out ushort colorSpace))
            {
                if (colorSpace == 1)
                {
                    return BC4LinearColorDecision.Convert(
                        BC4LinearColorDecisionSource.Metadata,
                        "EXIF ColorSpace",
                        "JPEG EXIF ColorSpace=1 indicates sRGB-authored content.",
                        0.85f);
                }
            }

            return null;
        }

        /// <summary>
        /// Attempts to read the EXIF color-space tag from IFD0 or the Exif IFD.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="tiffOffset">The TIFF header offset.</param>
        /// <param name="ifd0AbsoluteOffset">The IFD0 absolute offset.</param>
        /// <param name="segmentEnd">The segment end offset.</param>
        /// <param name="bigEndian">The TIFF endianness.</param>
        /// <param name="colorSpace">The decoded color-space value.</param>
        /// <returns><see langword="true"/> when the color-space tag was read; otherwise, <see langword="false"/>.</returns>
        private static bool TryReadExifColorSpace(
            byte[] sourceBytes,
            int tiffOffset,
            int ifd0AbsoluteOffset,
            int segmentEnd,
            bool bigEndian,
            out ushort colorSpace)
        {
            colorSpace = 0;
            if (TryReadShortTagValue(sourceBytes, ifd0AbsoluteOffset, segmentEnd, bigEndian, 0xA001, out colorSpace))
            {
                return true;
            }

            if (!TryReadUInt32TagValue(sourceBytes, ifd0AbsoluteOffset, segmentEnd, bigEndian, 0x8769, out uint exifIfdOffset))
            {
                return false;
            }

            int exifIfdAbsoluteOffset = tiffOffset + (int)exifIfdOffset;
            return TryReadShortTagValue(sourceBytes, exifIfdAbsoluteOffset, segmentEnd, bigEndian, 0xA001, out colorSpace);
        }

        /// <summary>
        /// Attempts to read a TIFF SHORT tag value.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="directoryOffset">The TIFF directory offset.</param>
        /// <param name="segmentEnd">The segment end offset.</param>
        /// <param name="bigEndian">The TIFF endianness.</param>
        /// <param name="tagId">The TIFF tag identifier.</param>
        /// <param name="value">The decoded value.</param>
        /// <returns><see langword="true"/> when the tag value was read; otherwise, <see langword="false"/>.</returns>
        private static bool TryReadShortTagValue(
            byte[] sourceBytes,
            int directoryOffset,
            int segmentEnd,
            bool bigEndian,
            ushort tagId,
            out ushort value)
        {
            value = 0;
            if (!TryReadDirectoryEntryOffset(sourceBytes, directoryOffset, segmentEnd, bigEndian, tagId, out int entryOffset))
            {
                return false;
            }

            if (!TryReadUInt16(sourceBytes, entryOffset + 2, segmentEnd, bigEndian, out ushort type) || type != 3)
            {
                return false;
            }

            if (!TryReadUInt32(sourceBytes, entryOffset + 4, segmentEnd, bigEndian, out uint count) || count != 1)
            {
                return false;
            }

            return TryReadUInt16(sourceBytes, entryOffset + 8, segmentEnd, bigEndian, out value);
        }

        /// <summary>
        /// Attempts to read a TIFF LONG tag value.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="directoryOffset">The TIFF directory offset.</param>
        /// <param name="segmentEnd">The segment end offset.</param>
        /// <param name="bigEndian">The TIFF endianness.</param>
        /// <param name="tagId">The TIFF tag identifier.</param>
        /// <param name="value">The decoded value.</param>
        /// <returns><see langword="true"/> when the tag value was read; otherwise, <see langword="false"/>.</returns>
        private static bool TryReadUInt32TagValue(
            byte[] sourceBytes,
            int directoryOffset,
            int segmentEnd,
            bool bigEndian,
            ushort tagId,
            out uint value)
        {
            value = 0;
            if (!TryReadDirectoryEntryOffset(sourceBytes, directoryOffset, segmentEnd, bigEndian, tagId, out int entryOffset))
            {
                return false;
            }

            if (!TryReadUInt16(sourceBytes, entryOffset + 2, segmentEnd, bigEndian, out ushort type) || type != 4)
            {
                return false;
            }

            if (!TryReadUInt32(sourceBytes, entryOffset + 4, segmentEnd, bigEndian, out uint count) || count != 1)
            {
                return false;
            }

            return TryReadUInt32(sourceBytes, entryOffset + 8, segmentEnd, bigEndian, out value);
        }

        /// <summary>
        /// Attempts to locate a TIFF directory entry for the given tag.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="directoryOffset">The TIFF directory offset.</param>
        /// <param name="segmentEnd">The segment end offset.</param>
        /// <param name="bigEndian">The TIFF endianness.</param>
        /// <param name="tagId">The TIFF tag identifier.</param>
        /// <param name="entryOffset">The matched entry offset.</param>
        /// <returns><see langword="true"/> when the tag entry was found; otherwise, <see langword="false"/>.</returns>
        private static bool TryReadDirectoryEntryOffset(
            byte[] sourceBytes,
            int directoryOffset,
            int segmentEnd,
            bool bigEndian,
            ushort tagId,
            out int entryOffset)
        {
            entryOffset = 0;
            if (!TryReadUInt16(sourceBytes, directoryOffset, segmentEnd, bigEndian, out ushort entryCount))
            {
                return false;
            }

            int currentEntryOffset = directoryOffset + 2;
            for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
            {
                if (currentEntryOffset + 12 > segmentEnd)
                {
                    return false;
                }

                if (!TryReadUInt16(sourceBytes, currentEntryOffset, segmentEnd, bigEndian, out ushort currentTagId))
                {
                    return false;
                }

                if (currentTagId == tagId)
                {
                    entryOffset = currentEntryOffset;
                    return true;
                }

                currentEntryOffset += 12;
            }

            return false;
        }

        /// <summary>
        /// Attempts to read the TIFF endianness flag.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="offset">The TIFF header offset.</param>
        /// <returns><see langword="true"/> for big-endian, <see langword="false"/> for little-endian, or <see langword="null"/> when invalid.</returns>
        private static bool? TryReadEndianFlag(byte[] sourceBytes, int offset)
        {
            if (!HasRemainingBytes(offset, sourceBytes.Length, 2))
            {
                return null;
            }

            if (sourceBytes[offset] == 'I' && sourceBytes[offset + 1] == 'I')
            {
                return false;
            }

            if (sourceBytes[offset] == 'M' && sourceBytes[offset + 1] == 'M')
            {
                return true;
            }

            return null;
        }

        /// <summary>
        /// Attempts to read a 16-bit TIFF value.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="offset">The value offset.</param>
        /// <param name="segmentEnd">The segment end offset.</param>
        /// <param name="bigEndian">The TIFF endianness.</param>
        /// <param name="value">The decoded value.</param>
        /// <returns><see langword="true"/> when the value was read; otherwise, <see langword="false"/>.</returns>
        private static bool TryReadUInt16(byte[] sourceBytes, int offset, int segmentEnd, bool bigEndian, out ushort value)
        {
            value = 0;
            if (!HasRemainingBytes(offset, segmentEnd, 2))
            {
                return false;
            }

            value = bigEndian
                ? (ushort)((sourceBytes[offset] << 8) | sourceBytes[offset + 1])
                : (ushort)(sourceBytes[offset] | (sourceBytes[offset + 1] << 8));
            return true;
        }

        /// <summary>
        /// Attempts to read a 32-bit TIFF value.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="offset">The value offset.</param>
        /// <param name="segmentEnd">The segment end offset.</param>
        /// <param name="bigEndian">The TIFF endianness.</param>
        /// <param name="value">The decoded value.</param>
        /// <returns><see langword="true"/> when the value was read; otherwise, <see langword="false"/>.</returns>
        private static bool TryReadUInt32(byte[] sourceBytes, int offset, int segmentEnd, bool bigEndian, out uint value)
        {
            value = 0;
            if (!HasRemainingBytes(offset, segmentEnd, 4))
            {
                return false;
            }

            value = bigEndian
                ? ((uint)sourceBytes[offset] << 24)
                  | ((uint)sourceBytes[offset + 1] << 16)
                  | ((uint)sourceBytes[offset + 2] << 8)
                  | sourceBytes[offset + 3]
                : sourceBytes[offset]
                  | ((uint)sourceBytes[offset + 1] << 8)
                  | ((uint)sourceBytes[offset + 2] << 16)
                  | ((uint)sourceBytes[offset + 3] << 24);
            return true;
        }

        /// <summary>
        /// Determines whether the source bytes match the expected ASCII text at the given offset.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="offset">The comparison offset.</param>
        /// <param name="text">The expected ASCII text.</param>
        /// <returns><see langword="true"/> when the text matches; otherwise, <see langword="false"/>.</returns>
        private static bool MatchesAscii(byte[] sourceBytes, int offset, string text)
        {
            if (offset < 0 || offset + text.Length > sourceBytes.Length)
            {
                return false;
            }

            for (int index = 0; index < text.Length; index++)
            {
                if (sourceBytes[offset + index] != text[index])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the requested byte count remains available within the current bounds.
        /// </summary>
        /// <param name="offset">The read offset.</param>
        /// <param name="limitExclusive">The exclusive upper bound for the readable range.</param>
        /// <param name="requiredByteCount">The number of bytes required.</param>
        /// <returns><see langword="true"/> when the requested bytes remain available; otherwise, <see langword="false"/>.</returns>
        private static bool HasRemainingBytes(int offset, int limitExclusive, int requiredByteCount)
        {
            return offset >= 0
                && requiredByteCount >= 0
                && offset <= limitExclusive
                && limitExclusive - offset >= requiredByteCount;
        }
    }
}
