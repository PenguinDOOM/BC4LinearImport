// Defines the Phase 2 BC4 color-decision model and shared inspection evidence.
using System;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Represents the detector's convert / skip / unknown decision.
    /// </summary>
    public enum BC4LinearColorDecisionKind
    {
        /// <summary>
        /// The detector could not classify the source deterministically.
        /// </summary>
        Unknown,

        /// <summary>
        /// The detector determined that the source should be converted to linear.
        /// </summary>
        ConvertToLinear,

        /// <summary>
        /// The detector determined that the source should stay as-is.
        /// </summary>
        SkipConversion
    }

    /// <summary>
    /// Identifies which evidence source produced the detector decision.
    /// </summary>
    public enum BC4LinearColorDecisionSource
    {
        /// <summary>
        /// No specific evidence source produced the final decision.
        /// </summary>
        None,

        /// <summary>
        /// The decision came from embedded profile evidence.
        /// </summary>
        EmbeddedProfile,

        /// <summary>
        /// The decision came from image metadata.
        /// </summary>
        Metadata,

        /// <summary>
        /// The decision came from sampled pixel-data inference.
        /// </summary>
        PixelInference,

        /// <summary>
        /// The decision came from extension gating.
        /// </summary>
        ExtensionGate,

        /// <summary>
        /// The decision came from safe format validation or malformed input handling.
        /// </summary>
        FormatValidation
    }

    /// <summary>
    /// Describes the Phase 2 detector outcome together with its evidence source and reason.
    /// </summary>
    public sealed class BC4LinearColorDecision
    {
        private BC4LinearColorDecision(
            BC4LinearColorDecisionKind decisionKind,
            BC4LinearColorDecisionSource source,
            string sourceMarker,
            string reason,
            float confidence)
        {
            DecisionKind = decisionKind;
            Source = source;
            SourceMarker = sourceMarker ?? string.Empty;
            Reason = reason ?? string.Empty;
            Confidence = confidence;
        }

        /// <summary>
        /// Gets the detector's convert / skip / unknown decision.
        /// </summary>
        public BC4LinearColorDecisionKind DecisionKind { get; }

        /// <summary>
        /// Gets a value indicating whether the detector chose to convert the source to linear.
        /// </summary>
        public bool ShouldConvert => DecisionKind == BC4LinearColorDecisionKind.ConvertToLinear;

        /// <summary>
        /// Gets the detector confidence for the chosen decision.
        /// </summary>
        public float Confidence { get; }

        /// <summary>
        /// Gets the human-readable reason for the decision.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Gets the evidence source that produced the decision.
        /// </summary>
        public BC4LinearColorDecisionSource Source { get; }

        /// <summary>
        /// Gets the specific marker or metadata field that produced the decision.
        /// </summary>
        public string SourceMarker { get; }

        /// <summary>
        /// Creates a conversion decision.
        /// </summary>
        /// <param name="source">The evidence source.</param>
        /// <param name="sourceMarker">The specific marker or field that was used.</param>
        /// <param name="reason">The decision reason.</param>
        /// <param name="confidence">The decision confidence.</param>
        /// <returns>The conversion decision.</returns>
        public static BC4LinearColorDecision Convert(BC4LinearColorDecisionSource source, string sourceMarker, string reason, float confidence)
        {
            return new BC4LinearColorDecision(BC4LinearColorDecisionKind.ConvertToLinear, source, sourceMarker, reason, confidence);
        }

        /// <summary>
        /// Creates a skip-conversion decision.
        /// </summary>
        /// <param name="source">The evidence source.</param>
        /// <param name="sourceMarker">The specific marker or field that was used.</param>
        /// <param name="reason">The decision reason.</param>
        /// <param name="confidence">The decision confidence.</param>
        /// <returns>The skip-conversion decision.</returns>
        public static BC4LinearColorDecision Skip(BC4LinearColorDecisionSource source, string sourceMarker, string reason, float confidence)
        {
            return new BC4LinearColorDecision(BC4LinearColorDecisionKind.SkipConversion, source, sourceMarker, reason, confidence);
        }

        /// <summary>
        /// Creates an unknown decision.
        /// </summary>
        /// <param name="source">The evidence source.</param>
        /// <param name="sourceMarker">The specific marker or field that was used.</param>
        /// <param name="reason">The decision reason.</param>
        /// <param name="confidence">The decision confidence.</param>
        /// <returns>The unknown decision.</returns>
        public static BC4LinearColorDecision Unknown(BC4LinearColorDecisionSource source, string sourceMarker, string reason, float confidence)
        {
            return new BC4LinearColorDecision(BC4LinearColorDecisionKind.Unknown, source, sourceMarker, reason, confidence);
        }
    }

    /// <summary>
    /// Captures embedded-profile, metadata, and fallback evidence from a source inspector.
    /// </summary>
    internal sealed class BC4LinearInspectionEvidence
    {
        private BC4LinearInspectionEvidence(
            BC4LinearColorDecision embeddedProfileDecision,
            BC4LinearColorDecision metadataDecision,
            BC4LinearColorDecision fallbackDecision)
        {
            EmbeddedProfileDecision = embeddedProfileDecision;
            MetadataDecision = metadataDecision;
            FallbackDecision = fallbackDecision ?? throw new ArgumentNullException(nameof(fallbackDecision));
        }

        /// <summary>
        /// Gets the embedded-profile decision when one was classified.
        /// </summary>
        internal BC4LinearColorDecision EmbeddedProfileDecision { get; }

        /// <summary>
        /// Gets the metadata decision when one was classified.
        /// </summary>
        internal BC4LinearColorDecision MetadataDecision { get; }

        /// <summary>
        /// Gets the safe fallback decision used when no higher-priority evidence classified the source.
        /// </summary>
        internal BC4LinearColorDecision FallbackDecision { get; }

        /// <summary>
        /// Gets a value indicating whether embedded-profile evidence was classified.
        /// </summary>
        internal bool HasEmbeddedProfileDecision => EmbeddedProfileDecision != null;

        /// <summary>
        /// Gets a value indicating whether metadata evidence was classified.
        /// </summary>
        internal bool HasMetadataDecision => MetadataDecision != null;

        /// <summary>
        /// Creates an inspection-evidence container.
        /// </summary>
        /// <param name="fallbackDecision">The safe fallback decision.</param>
        /// <param name="embeddedProfileDecision">The embedded-profile decision.</param>
        /// <param name="metadataDecision">The metadata decision.</param>
        /// <returns>The evidence container.</returns>
        internal static BC4LinearInspectionEvidence Create(
            BC4LinearColorDecision fallbackDecision,
            BC4LinearColorDecision embeddedProfileDecision = null,
            BC4LinearColorDecision metadataDecision = null)
        {
            return new BC4LinearInspectionEvidence(embeddedProfileDecision, metadataDecision, fallbackDecision);
        }
    }

    /// <summary>
    /// Classifies profile text into deterministic BC4 detector decisions.
    /// </summary>
    internal static class BC4LinearProfileClassifier
    {
        /// <summary>
        /// Creates a deterministic embedded-profile decision from profile text.
        /// </summary>
        /// <param name="profileText">The embedded profile text to classify.</param>
        /// <param name="formatName">The format name used in the reason text.</param>
        /// <param name="sourceMarker">The source marker used in the reason text.</param>
        /// <returns>The classified decision, or <see langword="null"/> when the text is unsupported.</returns>
        internal static BC4LinearColorDecision CreateDecisionFromProfileText(string profileText, string formatName, string sourceMarker)
        {
            if (string.IsNullOrWhiteSpace(profileText))
            {
                return null;
            }

            string normalized = profileText.Trim().ToLowerInvariant();
            if (ContainsAny(normalized, "srgb", "iec61966", "iec 61966"))
            {
                return BC4LinearColorDecision.Convert(
                    BC4LinearColorDecisionSource.EmbeddedProfile,
                    sourceMarker,
                    $"{formatName} {sourceMarker} profile '{profileText}' indicates sRGB-authored content.",
                    1.0f);
            }

            if (ContainsAny(normalized, "linear", "gamma 1.0", "gamma1.0"))
            {
                return BC4LinearColorDecision.Skip(
                    BC4LinearColorDecisionSource.EmbeddedProfile,
                    sourceMarker,
                    $"{formatName} {sourceMarker} profile '{profileText}' indicates linear-authored content.",
                    1.0f);
            }

            return null;
        }

        /// <summary>
        /// Determines whether the normalized profile text contains any supported tokens.
        /// </summary>
        /// <param name="normalized">The normalized profile text.</param>
        /// <param name="tokens">The supported tokens to match.</param>
        /// <returns><see langword="true"/> when any token is present; otherwise, <see langword="false"/>.</returns>
        private static bool ContainsAny(string normalized, params string[] tokens)
        {
            foreach (string token in tokens)
            {
                if (normalized.Contains(token))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
