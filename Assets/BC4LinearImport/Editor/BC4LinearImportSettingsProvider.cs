// Exposes the BC4 linear import project settings in the Unity Project Settings window.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BC4LinearImport.Editor
{
    /// <summary>
    /// Exposes the BC4 linear import project settings in the Unity Project Settings window.
    /// </summary>
    public sealed class BC4LinearImportSettingsProvider : SettingsProvider
    {
        private const float ExclusionDropAreaHeight = 52f;
        private const float RemoveButtonWidth = 72f;

        private string exclusionWarningMessage = string.Empty;

        private BC4LinearImportSettingsProvider(string path, SettingsScope scopes)
            : base(path, scopes)
        {
        }

        /// <summary>
        /// Subscribes to undo and redo notifications so exclusion changes stay persisted when reverted from the editor.
        /// </summary>
        /// <param name="searchContext">The active settings search context.</param>
        /// <param name="rootElement">The root visual element for the settings page.</param>
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;
        }

        /// <summary>
        /// Unsubscribes from undo and redo notifications when the settings page closes.
        /// </summary>
        public override void OnDeactivate()
        {
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
        }

        /// <summary>
        /// Draws the current project settings UI.
        /// </summary>
        /// <param name="searchContext">The active settings search context.</param>
        public override void OnGUI(string searchContext)
        {
            BC4LinearImportSettings settings = BC4LinearImportSettings.instance;
            SerializedObject serializedObject = new SerializedObject(settings);
            SerializedProperty enabledProperty = serializedObject.FindProperty("projectWideEnabled");

            EditorGUILayout.LabelField("BC4 Linear Import", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Convert BC4-targeted PNG/JPG/JPEG grayscale sources to linear data. Use exclusions for exact asset paths or folder prefixes, then run the explicit reimport action to repair existing assets.",
                MessageType.Info);

            serializedObject.Update();
            EditorGUILayout.PropertyField(enabledProperty, new GUIContent("Enable BC4 linear import"));

            if (serializedObject.ApplyModifiedProperties())
            {
                settings.SaveSettings();
            }

            EditorGUILayout.Space();
            DrawExcludedAssetPathsSection(settings);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isUpdating))
            {
                if (GUILayout.Button("Reimport Eligible PNG/JPG/JPEG Textures"))
                {
                    int reimportedAssetCount = BC4LinearImportReimportUtility.ReimportEligibleAssets();
                    Debug.Log($"BC4 Linear Import reimported {reimportedAssetCount} eligible texture(s).");
                }
            }
        }

        /// <summary>
        /// Draws the custom exclusions workflow with drag-and-drop intake and canonical path management controls.
        /// </summary>
        /// <param name="settings">The settings singleton to mutate.</param>
        private void DrawExcludedAssetPathsSection(BC4LinearImportSettings settings)
        {
            EditorGUILayout.LabelField("Excluded assets and folders", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Drag project assets or folders here to exclude them from BC4 linear import. Dropped items are stored as canonical project paths and affect exact assets plus everything inside excluded folders.",
                MessageType.None);

            Rect dropArea = GUILayoutUtility.GetRect(0f, ExclusionDropAreaHeight, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop project assets or folders here", EditorStyles.helpBox);
            HandleExcludedAssetPathDrop(dropArea, settings);

            if (!string.IsNullOrEmpty(exclusionWarningMessage))
            {
                EditorGUILayout.HelpBox(exclusionWarningMessage, MessageType.Warning);
            }

            IReadOnlyList<string> excludedAssetPaths = settings.ExcludedAssetPaths;
            if (excludedAssetPaths.Count == 0)
            {
                EditorGUILayout.HelpBox("No exclusions configured.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Stored exclusion paths", EditorStyles.miniBoldLabel);

            foreach (string excludedAssetPath in excludedAssetPaths.ToList())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(
                        excludedAssetPath,
                        EditorStyles.textField,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight));

                    if (GUILayout.Button("Remove", GUILayout.Width(RemoveButtonWidth)))
                    {
                        UpdateExcludedAssetPaths(
                            settings,
                            excludedAssetPaths.Where(path => !string.Equals(path, excludedAssetPath, System.StringComparison.Ordinal)),
                            "Remove BC4 linear import exclusion");
                        exclusionWarningMessage = string.Empty;
                        GUIUtility.ExitGUI();
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear all", GUILayout.Width(RemoveButtonWidth)))
                {
                    UpdateExcludedAssetPaths(settings, System.Array.Empty<string>(), "Clear BC4 linear import exclusions");
                    exclusionWarningMessage = string.Empty;
                    GUIUtility.ExitGUI();
                }
            }
        }

        /// <summary>
        /// Handles drag-and-drop intake for the exclusions drop area.
        /// </summary>
        /// <param name="dropArea">The on-screen drop target rectangle.</param>
        /// <param name="settings">The settings singleton to mutate.</param>
        private void HandleExcludedAssetPathDrop(Rect dropArea, BC4LinearImportSettings settings)
        {
            Event currentEvent = Event.current;
            if (!dropArea.Contains(currentEvent.mousePosition))
            {
                return;
            }

            switch (currentEvent.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    currentEvent.Use();
                    break;
                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();

                    BC4LinearImportExcludedPathDropResolutionResult dropResult = BC4LinearImportExcludedPathDragAndDropUtility.ResolveDroppedAssetPathsWithReport(DragAndDrop.objectReferences);
                    if (dropResult.AssetPaths.Count > 0)
                    {
                        UpdateExcludedAssetPaths(
                            settings,
                            settings.ExcludedAssetPaths.Concat(dropResult.AssetPaths),
                            "Add BC4 linear import exclusions");
                    }

                    exclusionWarningMessage = dropResult.IgnoredObjectCount > 0
                        ? $"Ignored {dropResult.IgnoredObjectCount} dropped item(s) because only project assets and folders can be excluded here."
                        : string.Empty;

                    currentEvent.Use();
                    break;
            }
        }

        /// <summary>
        /// Applies exclusion mutations through the shared sanitize API while recording editor undo state.
        /// </summary>
        /// <param name="settings">The settings singleton to mutate.</param>
        /// <param name="assetPaths">The asset paths to store through the sanitize API.</param>
        /// <param name="undoLabel">The user-facing undo label for the mutation.</param>
        private static void UpdateExcludedAssetPaths(
            BC4LinearImportSettings settings,
            IEnumerable<string> assetPaths,
            string undoLabel)
        {
            Undo.RecordObject(settings, undoLabel);
            settings.SetExcludedAssetPaths(assetPaths);
            EditorUtility.SetDirty(settings);
            settings.SaveSettings();
        }

        /// <summary>
        /// Persists undo and redo state changes for the project settings singleton.
        /// </summary>
        private static void HandleUndoRedoPerformed()
        {
            BC4LinearImportSettings.instance.SaveSettings();
        }

        /// <summary>
        /// Creates the project settings provider for the BC4 linear import workflow.
        /// </summary>
        /// <returns>The project settings provider instance.</returns>
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new BC4LinearImportSettingsProvider("Project/BC4 Linear Import", SettingsScope.Project)
            {
                keywords = new HashSet<string>(new[] { "BC4", "Linear", "Import", "Texture", "Exclude", "Reimport" })
            };
        }
    }
}
