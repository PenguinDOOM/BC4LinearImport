// Verifies Phase 2 BC4 detector precedence, extension gating, and safe degradation behavior.
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
    /// Verifies Phase 2 BC4 detector precedence, extension gating, and safe degradation behavior.
    /// </summary>
    [TestFixture]
    public class BC4LinearImportDetectorTests
    {
        /// <summary>
        /// Verifies that PNG embedded profile evidence outranks PNG metadata even when metadata appears first.
        /// </summary>
        [Test]
        public void Detect_PngEmbeddedProfile_BeatsMetadata()
        {
            byte[] sourceBytes = BC4LinearImportFixtureUtility.CreatePngBytes(
                embeddedProfileName: "Linear Gray",
                includeSrgbChunk: true,
                metadataBeforeProfile: true);

            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect("Assets/Test/profile-first.png", sourceBytes);

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.SkipConversion));
            Assert.That(decision.ShouldConvert, Is.False);
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.EmbeddedProfile));
            Assert.That(decision.SourceMarker, Is.EqualTo("iCCP"));
            Assert.That(decision.Reason, Does.Contain("Linear Gray"));
        }

        /// <summary>
        /// Verifies that PNG metadata is used when no embedded profile is available.
        /// </summary>
        [Test]
        public void Detect_PngMetadata_IsUsedWhenProfileIsMissing()
        {
            byte[] sourceBytes = BC4LinearImportFixtureUtility.CreatePngBytes(includeSrgbChunk: true);

            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect("Assets/Test/metadata-only.png", sourceBytes);

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.ConvertToLinear));
            Assert.That(decision.ShouldConvert, Is.True);
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.Metadata));
            Assert.That(decision.SourceMarker, Is.EqualTo("sRGB"));
        }

        /// <summary>
        /// Verifies that JPEG ICC profile evidence outranks EXIF metadata even when metadata appears first.
        /// </summary>
        [Test]
        public void Detect_JpegEmbeddedProfile_BeatsMetadata()
        {
            byte[] sourceBytes = BC4LinearImportFixtureUtility.CreateJpegBytes(
                embeddedProfileText: "Linear Gray",
                exifColorSpace: 1,
                metadataBeforeProfile: true);

            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect("Assets/Test/profile-first.jpg", sourceBytes);

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.SkipConversion));
            Assert.That(decision.ShouldConvert, Is.False);
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.EmbeddedProfile));
            Assert.That(decision.SourceMarker, Is.EqualTo("ICC APP2"));
            Assert.That(decision.Reason, Does.Contain("Linear Gray"));
        }

        /// <summary>
        /// Verifies that JPEG EXIF metadata is used when no ICC profile is available.
        /// </summary>
        [Test]
        public void Detect_JpegMetadata_IsUsedWhenProfileIsMissing()
        {
            byte[] sourceBytes = BC4LinearImportFixtureUtility.CreateJpegBytes(exifColorSpace: 1);

            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect("Assets/Test/metadata-only.jpeg", sourceBytes);

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.ConvertToLinear));
            Assert.That(decision.ShouldConvert, Is.True);
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.Metadata));
            Assert.That(decision.SourceMarker, Is.EqualTo("EXIF ColorSpace"));
        }

        /// <summary>
        /// Verifies that unsupported extensions are ignored explicitly.
        /// </summary>
        [Test]
        public void Detect_UnsupportedExtension_ReturnsUnknownWithReason()
        {
            byte[] sourceBytes = BC4LinearImportFixtureUtility.CreatePngBytes(includeSrgbChunk: true);

            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect("Assets/Test/unsupported.tga", sourceBytes);

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.Unknown));
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.ExtensionGate));
            Assert.That(decision.Reason, Does.Contain(".tga"));
        }

        /// <summary>
        /// Verifies that truncated PNG bytes degrade safely without throwing.
        /// </summary>
        [Test]
        public void Detect_TruncatedPng_ReturnsUnknownSafely()
        {
            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect(
                "Assets/Test/truncated.png",
                BC4LinearImportFixtureUtility.CreateTruncatedPngBytes());

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.Unknown));
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.FormatValidation));
            Assert.That(decision.Reason, Does.Contain("PNG"));
        }

        /// <summary>
        /// Verifies that truncated JPEG bytes degrade safely without throwing.
        /// </summary>
        [Test]
        public void Detect_TruncatedJpeg_ReturnsUnknownSafely()
        {
            BC4LinearColorDecision decision = BC4LinearColorDetector.Detect(
                "Assets/Test/truncated.jpg",
                BC4LinearImportFixtureUtility.CreateTruncatedJpegBytes());

            Assert.That(decision.DecisionKind, Is.EqualTo(BC4LinearColorDecisionKind.Unknown));
            Assert.That(decision.Source, Is.EqualTo(BC4LinearColorDecisionSource.FormatValidation));
            Assert.That(decision.Reason, Does.Contain("JPEG"));
        }
    }
}
