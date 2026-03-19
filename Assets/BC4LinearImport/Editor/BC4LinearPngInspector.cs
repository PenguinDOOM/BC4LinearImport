// Inspects PNG source bytes for BC4 color intent using profile-first and metadata-second evidence.
using System;
using System.Text;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Inspects PNG source bytes for BC4 color intent using profile-first and metadata-second evidence.
    /// </summary>
    internal static class BC4LinearPngInspector
    {
        private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        /// <summary>
        /// Inspects PNG source bytes for embedded-profile and metadata evidence.
        /// </summary>
        /// <param name="sourceBytes">The PNG source bytes.</param>
        /// <returns>The inspection evidence for the detector.</returns>
        internal static BC4LinearInspectionEvidence Inspect(byte[] sourceBytes)
        {
            if (sourceBytes == null || sourceBytes.Length < PngSignature.Length)
            {
                return BC4LinearInspectionEvidence.Create(
                    BC4LinearColorDecision.Unknown(
                        BC4LinearColorDecisionSource.FormatValidation,
                        "PNG",
                        "PNG source bytes are truncated before the file signature.",
                        0.1f));
            }

            if (!HasPngSignature(sourceBytes))
            {
                return BC4LinearInspectionEvidence.Create(
                    BC4LinearColorDecision.Unknown(
                        BC4LinearColorDecisionSource.FormatValidation,
                        "PNG",
                        "Source bytes do not match the PNG file signature.",
                        0.1f));
            }

            BC4LinearColorDecision embeddedProfileDecision = null;
            BC4LinearColorDecision metadataDecision = null;
            BC4LinearColorDecision fallbackDecision = BC4LinearColorDecision.Unknown(
                BC4LinearColorDecisionSource.None,
                "PNG",
                "PNG contains no supported iCCP, sRGB, or gAMA color evidence.",
                0.0f);
            int metadataPriority = 0;
            int offset = PngSignature.Length;

            while (offset < sourceBytes.Length)
            {
                if (sourceBytes.Length - offset < 8)
                {
                    return BC4LinearInspectionEvidence.Create(
                        BC4LinearColorDecision.Unknown(
                            BC4LinearColorDecisionSource.FormatValidation,
                            "PNG",
                            "PNG chunk header is truncated.",
                            0.1f),
                        embeddedProfileDecision,
                        metadataDecision);
                }

                uint chunkLength = ReadUInt32BigEndian(sourceBytes, offset);
                offset += 4;
                string chunkType = Encoding.ASCII.GetString(sourceBytes, offset, 4);
                offset += 4;

                if (chunkLength > int.MaxValue || sourceBytes.Length - offset < chunkLength + 4)
                {
                    return BC4LinearInspectionEvidence.Create(
                        BC4LinearColorDecision.Unknown(
                            BC4LinearColorDecisionSource.FormatValidation,
                            chunkType,
                            $"PNG {chunkType} chunk is truncated.",
                            0.1f),
                        embeddedProfileDecision,
                        metadataDecision);
                }

                int chunkDataOffset = offset;
                int chunkDataLength = (int)chunkLength;

                switch (chunkType)
                {
                    case "iCCP":
                        if (embeddedProfileDecision == null)
                        {
                            embeddedProfileDecision = TryCreateEmbeddedProfileDecision(sourceBytes, chunkDataOffset, chunkDataLength);
                            if (embeddedProfileDecision == null)
                            {
                                fallbackDecision = BC4LinearColorDecision.Unknown(
                                    BC4LinearColorDecisionSource.FormatValidation,
                                    "iCCP",
                                    "PNG iCCP chunk was present but did not expose a supported profile classification.",
                                    0.2f);
                            }
                        }

                        break;

                    case "sRGB":
                        metadataDecision = SelectMetadataDecision(
                            metadataDecision,
                            ref metadataPriority,
                            BC4LinearColorDecision.Convert(
                                BC4LinearColorDecisionSource.Metadata,
                                "sRGB",
                                "PNG sRGB chunk indicates sRGB-authored content.",
                                0.9f),
                            2);
                        break;

                    case "gAMA":
                        BC4LinearColorDecision gammaDecision = TryCreateGammaDecision(sourceBytes, chunkDataOffset, chunkDataLength);
                        if (gammaDecision != null)
                        {
                            metadataDecision = SelectMetadataDecision(metadataDecision, ref metadataPriority, gammaDecision, 1);
                        }

                        break;
                }

                offset += chunkDataLength + 4;
                if (chunkType == "IEND")
                {
                    break;
                }
            }

            return BC4LinearInspectionEvidence.Create(fallbackDecision, embeddedProfileDecision, metadataDecision);
        }

        /// <summary>
        /// Determines whether the source bytes start with the PNG file signature.
        /// </summary>
        /// <param name="sourceBytes">The source bytes to inspect.</param>
        /// <returns><see langword="true"/> when the signature matches; otherwise, <see langword="false"/>.</returns>
        private static bool HasPngSignature(byte[] sourceBytes)
        {
            for (int index = 0; index < PngSignature.Length; index++)
            {
                if (sourceBytes[index] != PngSignature[index])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to create an embedded-profile decision from an <c>iCCP</c> chunk.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="chunkDataOffset">The chunk data offset.</param>
        /// <param name="chunkDataLength">The chunk data length.</param>
        /// <returns>The classified embedded-profile decision, or <see langword="null"/> when unsupported.</returns>
        private static BC4LinearColorDecision TryCreateEmbeddedProfileDecision(byte[] sourceBytes, int chunkDataOffset, int chunkDataLength)
        {
            int profileNameEnd = Array.IndexOf(sourceBytes, (byte)0, chunkDataOffset, chunkDataLength);
            if (profileNameEnd < 0)
            {
                return null;
            }

            if (profileNameEnd + 1 >= chunkDataOffset + chunkDataLength)
            {
                return null;
            }

            string profileName = Encoding.ASCII.GetString(sourceBytes, chunkDataOffset, profileNameEnd - chunkDataOffset).Trim();
            return BC4LinearProfileClassifier.CreateDecisionFromProfileText(profileName, "PNG", "iCCP");
        }

        /// <summary>
        /// Attempts to create a metadata decision from a <c>gAMA</c> chunk.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="chunkDataOffset">The chunk data offset.</param>
        /// <param name="chunkDataLength">The chunk data length.</param>
        /// <returns>The classified metadata decision, or <see langword="null"/> when unsupported.</returns>
        private static BC4LinearColorDecision TryCreateGammaDecision(byte[] sourceBytes, int chunkDataOffset, int chunkDataLength)
        {
            if (chunkDataLength != 4)
            {
                return null;
            }

            uint gammaValue = ReadUInt32BigEndian(sourceBytes, chunkDataOffset);
            if (gammaValue == 45455u)
            {
                return BC4LinearColorDecision.Convert(
                    BC4LinearColorDecisionSource.Metadata,
                    "gAMA",
                    "PNG gAMA=45455 indicates sRGB-authored content.",
                    0.75f);
            }

            if (gammaValue == 100000u)
            {
                return BC4LinearColorDecision.Skip(
                    BC4LinearColorDecisionSource.Metadata,
                    "gAMA",
                    "PNG gAMA=100000 indicates linear-authored content.",
                    0.75f);
            }

            return null;
        }

        /// <summary>
        /// Selects the higher-priority PNG metadata decision.
        /// </summary>
        /// <param name="currentDecision">The current metadata decision.</param>
        /// <param name="currentPriority">The current metadata priority.</param>
        /// <param name="candidateDecision">The candidate metadata decision.</param>
        /// <param name="candidatePriority">The candidate metadata priority.</param>
        /// <returns>The selected metadata decision.</returns>
        private static BC4LinearColorDecision SelectMetadataDecision(
            BC4LinearColorDecision currentDecision,
            ref int currentPriority,
            BC4LinearColorDecision candidateDecision,
            int candidatePriority)
        {
            if (candidateDecision == null)
            {
                return currentDecision;
            }

            // When metadata candidates have the same priority, the later chunk wins so the detector keeps
            // the most recently parsed equally ranked marker instead of preserving the earlier one.
            if (currentDecision == null || candidatePriority >= currentPriority)
            {
                currentPriority = candidatePriority;
                return candidateDecision;
            }

            return currentDecision;
        }

        /// <summary>
        /// Reads an unsigned 32-bit big-endian integer.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="offset">The value offset.</param>
        /// <returns>The decoded unsigned 32-bit value.</returns>
        private static uint ReadUInt32BigEndian(byte[] sourceBytes, int offset)
        {
            return ((uint)sourceBytes[offset] << 24)
                   | ((uint)sourceBytes[offset + 1] << 16)
                   | ((uint)sourceBytes[offset + 2] << 8)
                   | sourceBytes[offset + 3];
        }
    }
}
