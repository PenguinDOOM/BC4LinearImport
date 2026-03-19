// Verifies dropped-object resolution contracts for the BC4 exclusion drag-and-drop helper.
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using BC4LinearImport.Editor;

namespace BC4LinearImport.Tests.EditMode
{
    /// <summary>
    /// Verifies dropped-object resolution contracts for the BC4 exclusion drag-and-drop helper.
    /// </summary>
    [TestFixture]
    public class BC4LinearImportExcludedPathDragAndDropUtilityTests
    {
        private BC4LinearImportTemporaryAssetScope activeScope;
        private GameObject sceneOnlyObject;

        /// <summary>
        /// Disposes the temporary asset scope and destroys any scene-only test object.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (sceneOnlyObject != null)
            {
                Object.DestroyImmediate(sceneOnlyObject);
                sceneOnlyObject = null;
            }

            activeScope?.Dispose();
            activeScope = null;
        }

        /// <summary>
        /// Verifies that valid dropped assets and folders resolve to canonical project paths in input order without duplicates.
        /// </summary>
        [Test]
        public void ResolveDroppedAssetPaths_ReturnsCanonicalProjectAssetAndFolderPaths_InDropOrderWithoutDuplicates()
        {
            activeScope = new BC4LinearImportTemporaryAssetScope();
            string textureAssetPath = activeScope.CreateTextureAsset("dragged-mask.png", BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes());
            string folderGuid = AssetDatabase.CreateFolder(activeScope.RootAssetPath, "DroppedFolder");
            string folderAssetPath = AssetDatabase.GUIDToAssetPath(folderGuid);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Texture2D textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
            DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderAssetPath);

            Assert.That(textureAsset, Is.Not.Null, "Expected the temporary texture asset to load for drag-and-drop contract verification.");
            Assert.That(folderAsset, Is.Not.Null, "Expected the temporary folder asset to load for drag-and-drop contract verification.");

            var resolvedAssetPaths = BC4LinearImportExcludedPathDragAndDropUtility.ResolveDroppedAssetPaths(
                new Object[]
                {
                    textureAsset,
                    folderAsset,
                    textureAsset
                });

            Assert.That(
                resolvedAssetPaths,
                Is.EqualTo(new[] { textureAssetPath, folderAssetPath }));
        }

        /// <summary>
        /// Verifies that scene-only and non-project objects are ignored while valid project assets still resolve.
        /// </summary>
        [Test]
        public void ResolveDroppedAssetPaths_IgnoresSceneOnlyAndNonProjectObjects()
        {
            activeScope = new BC4LinearImportTemporaryAssetScope();
            string textureAssetPath = activeScope.CreateTextureAsset("kept-mask.png", BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes());
            Texture2D textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
            sceneOnlyObject = new GameObject("BC4LinearImportSceneOnlyDrop");

            Assert.That(textureAsset, Is.Not.Null, "Expected the temporary texture asset to load for drag-and-drop contract verification.");

            var resolvedAssetPaths = BC4LinearImportExcludedPathDragAndDropUtility.ResolveDroppedAssetPaths(
                new Object[]
                {
                    null,
                    sceneOnlyObject,
                    Texture2D.whiteTexture,
                    textureAsset
                });

            Assert.That(resolvedAssetPaths, Is.EqualTo(new[] { textureAssetPath }));
        }

        /// <summary>
        /// Verifies that drag-and-drop diagnostics count ignored objects while preserving canonical valid paths.
        /// </summary>
        [Test]
        public void ResolveDroppedAssetPathsWithReport_CountsIgnoredObjectsWhileKeepingValidProjectPaths()
        {
            activeScope = new BC4LinearImportTemporaryAssetScope();
            string textureAssetPath = activeScope.CreateTextureAsset("reported-mask.png", BC4LinearImportFixtureUtility.CreateSrgbMetadataRampPngBytes());
            Texture2D textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
            sceneOnlyObject = new GameObject("BC4LinearImportIgnoredDrop");

            Assert.That(textureAsset, Is.Not.Null, "Expected the temporary texture asset to load for drag-and-drop diagnostic verification.");

            BC4LinearImportExcludedPathDropResolutionResult dropResult = BC4LinearImportExcludedPathDragAndDropUtility.ResolveDroppedAssetPathsWithReport(
                new Object[]
                {
                    null,
                    sceneOnlyObject,
                    Texture2D.whiteTexture,
                    textureAsset,
                    textureAsset
                });

            Assert.That(dropResult.AssetPaths, Is.EqualTo(new[] { textureAssetPath }));
            Assert.That(dropResult.IgnoredObjectCount, Is.EqualTo(3));
        }
    }
}