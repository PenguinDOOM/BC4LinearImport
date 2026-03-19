// Verifies Phase 3 metadata-free BC4 detector heuristics and conservative fallback behavior.
using NUnit.Framework;
using BC4LinearImport.Editor;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Verifies Phase 3 metadata-free BC4 detector heuristics and conservative fallback behavior.
    /// </summary>
    [TestFixture]
    public class BC4LinearImportHeuristicTests
    {
        /// <summary>
        /// Verifies that metadata-free PNG sources can use pixel inference when the sampled ramp strongly matches sRGB-authored content.
        /// </summary>
        [Test]
        public void Detect_MetadataFreePng_UsesPixelInferenceWhenSignatureIsStrong()
        {
            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect(
                "Assets/Test/pixel-inference.png",
                BC4LinearImportFixtureUtility.CreateSrgbEncodedRampPngBytes());

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.ConvertToLinear));
            Assert.That(decision.ShouldConvert, Is.True);
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.PixelInference));
            Assert.That(decision.Reason, Does.Contain("pixel").IgnoreCase);
        }

        /// <summary>
        /// Verifies that metadata-free JPEG sources can use pixel inference when the sampled ramp strongly matches sRGB-authored content.
        /// </summary>
        [Test]
        public void Detect_MetadataFreeJpeg_UsesPixelInferenceWhenSignatureIsStrong()
        {
            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect(
                "Assets/Test/pixel-inference.jpg",
                BC4LinearImportFixtureUtility.CreateSrgbEncodedRampJpegBytes());

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.ConvertToLinear));
            Assert.That(decision.ShouldConvert, Is.True);
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.PixelInference));
            Assert.That(decision.Reason, Does.Contain("pixel").IgnoreCase);
        }

        /// <summary>
        /// Verifies that ambiguous metadata-free PNG sources fall back safely to an unknown no-op decision.
        /// </summary>
        [Test]
        public void Detect_MetadataFreeFlatPng_FallsBackToUnknownSafely()
        {
            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect(
                "Assets/Test/flat.png",
                BC4LinearImportFixtureUtility.CreateFlatPngBytes());

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.Unknown));
            Assert.That(decision.ShouldConvert, Is.False);
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.PixelInference));
        }

        /// <summary>
        /// Verifies that ambiguous metadata-free JPEG sources fall back safely to an unknown no-op decision.
        /// </summary>
        [Test]
        public void Detect_MetadataFreeFlatJpeg_FallsBackToUnknownSafely()
        {
            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect(
                "Assets/Test/flat.jpeg",
                BC4LinearImportFixtureUtility.CreateFlatJpegBytes());

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.Unknown));
            Assert.That(decision.ShouldConvert, Is.False);
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.PixelInference));
        }
    }
}
