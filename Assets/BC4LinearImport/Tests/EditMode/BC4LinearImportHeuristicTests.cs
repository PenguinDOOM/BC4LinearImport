// Verifies Phase 3 metadata-free BC4 detector heuristics and conservative fallback behavior.
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
