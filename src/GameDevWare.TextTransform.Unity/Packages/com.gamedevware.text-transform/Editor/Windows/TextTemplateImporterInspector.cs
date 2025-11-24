/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "T4 Templates" Unity Asset - https://www.assetstore.unity3d.com/#!/content/63294

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY,
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.

	This source code is distributed via Unity Asset Store,
	to use it in your project you should accept Terms of Service and EULA
	https://unity3d.com/ru/legal/as_terms
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GameDevWare.TextTransform.Editor.Windows
{
	[CustomEditor(typeof(TextTemplateImporter), editorForChildClasses: true, isFallback = false)]
	internal class TextTemplateImporterInspector : ScriptedImporterEditor
	{
		private UnityObject newAssetToWatch;
		private string lastGenerationResult;

		public override void OnInspectorGUI()
		{
			if (this.assetTargets.Length == 1)
			{
				this.InspectorGUI(this, this.assetTarget);
			}
			this.ApplyRevertGUI();
		}

		// ReSharper disable once FunctionComplexityOverflow
		private void InspectorGUI(ScriptedImporterEditor editor, UnityObject assetTarget)
		{
			editor.serializedObject.Update();
			var generationOutputProperty = editor.serializedObject.FindProperty(nameof(T4TemplateImporter.generationOutput));
			var outputPathProperty = editor.serializedObject.FindProperty(nameof(T4TemplateImporter.outputPath));
			var generationTriggersProperty = editor.serializedObject.FindProperty(nameof(T4TemplateImporter.generationTriggers));
			var triggerDelayProperty = editor.serializedObject.FindProperty(nameof(T4TemplateImporter.triggerDelay));
			var watchedAssetsProperty = editor.serializedObject.FindProperty(nameof(T4TemplateImporter.watchedAssets));

			var assetPath = AssetDatabase.GetAssetPath(assetTarget);

			this.serializedObject.Update();

			GUI.enabled = true;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button("Preferences...", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
			{
				var settingsService = typeof(EditorApplication).Assembly.GetType("UnityEditor.SettingsService", throwOnError: false, ignoreCase: true);
				var preferencesWindowType = typeof(EditorApplication).Assembly.GetType("UnityEditor.PreferencesWindow", throwOnError: false, ignoreCase: true);
				var settingsWindowType = typeof(EditorApplication).Assembly.GetType("UnityEditor.SettingsWindow", throwOnError: false, ignoreCase: true);
				if (settingsService != null)
				{
					SettingsService.OpenProjectSettings("Project/T4");
				}
				else if (preferencesWindowType != null)
				{
					var settingsWindow = EditorWindow.GetWindow(preferencesWindowType);
					settingsWindow.Show();
					settingsWindow.Focus();
				}
				else if (settingsWindowType != null)
				{
					var settingsWindow = EditorWindow.GetWindow(settingsWindowType);
					settingsWindow.Show();
					settingsWindow.Focus();
				}
				else
				{
					Debug.LogWarning("Unable to locate preferences window. Please open it manually 'Edit -> Preferences...'.");
				}
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Label(Path.GetFileName(assetPath), EditorStyles.boldLabel);

			EditorGUILayout.PropertyField(generationOutputProperty, new GUIContent("Output Type"));

			var outputPath = outputPathProperty.stringValue;
			var codeAsset = !string.IsNullOrEmpty(outputPath) && File.Exists(outputPath) ? AssetDatabase.LoadAssetAtPath(outputPath, typeof(UnityObject)) : null;
			if (codeAsset != null)
			{
				outputPathProperty.stringValue = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField("Output Path", codeAsset, typeof(UnityObject), false));
			}
			else
			{
				EditorGUILayout.PropertyField(outputPathProperty, new GUIContent("Output Path"));
			}
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(triggerDelayProperty, new GUIContent("Auto-Gen Delay (Ms)"));

			var generationTrigger = (GenerationTriggers) generationTriggersProperty.enumValueFlag;
			generationTriggersProperty.intValue = (int)(GenerationTriggers)EditorGUILayout.EnumFlagsField("Auto-Gen Triggers", generationTrigger);

			if ((generationTrigger & GenerationTriggers.AssetChanges) != 0)
			{
				EditorGUILayout.Space();
				GUILayout.Label("Assets to Watch", EditorStyles.boldLabel);
				for (var i = 0; i < watchedAssetsProperty.arraySize; i++)
				{
					var watchedAssetProperty = watchedAssetsProperty.GetArrayElementAtIndex(i);
					var watchedAssetPath = watchedAssetProperty.stringValue;
					var assetExists = !string.IsNullOrEmpty(watchedAssetPath) && (File.Exists(watchedAssetPath) || Directory.Exists(watchedAssetPath));
					var watchedAsset = assetExists ? AssetDatabase.LoadMainAssetAtPath(watchedAssetPath) : null;
					if (watchedAsset != null)
						watchedAssetProperty.stringValue = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(watchedAsset.GetType().Name, watchedAsset, typeof(UnityObject), false));
					else
						watchedAssetProperty.stringValue = EditorGUILayout.TextField("Path", watchedAssetPath);
				}
				EditorGUILayout.Space();

				this.newAssetToWatch = EditorGUILayout.ObjectField("<New>", this.newAssetToWatch, typeof(UnityObject), false);
				if (Event.current.type == (EventType)7 /*repaint*/)
				{
					if (this.newAssetToWatch != null)
					{
						watchedAssetsProperty.InsertArrayElementAtIndex(watchedAssetsProperty.arraySize);
						var newWatchedAssetProperty = watchedAssetsProperty.GetArrayElementAtIndex(watchedAssetsProperty.arraySize - 1);
						newWatchedAssetProperty.stringValue = AssetDatabase.GetAssetPath(this.newAssetToWatch);

						this.newAssetToWatch = null;
						GUI.changed = true;
					}

					for (var i = 0; i < watchedAssetsProperty.arraySize; i++)
					{
						var watchedAssetProperty = watchedAssetsProperty.GetArrayElementAtIndex(i);
						if (!string.IsNullOrEmpty(watchedAssetProperty.stringValue)) continue;

						watchedAssetsProperty.DeleteArrayElementAtIndex(i);
					}
				}
			}

			EditorGUILayout.Space();
			GUILayout.Label("Actions", EditorStyles.boldLabel);

			if (EditorApplication.isCompiling)
				EditorGUILayout.HelpBox("No action could be made while Unity Editor is compiling scripts.", MessageType.Warning);

			GUI.enabled = !EditorApplication.isCompiling;
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Generate " + this.lastGenerationResult))
			{
				try
				{
					switch (UnityTemplateGenerator.RunForTemplate(assetPath))
					{
						case TransformationResult.Success:
						case TransformationResult.NoChanges:
							this.lastGenerationResult = "(Success)";
							AssetChangesTrigger.DoDelayedAssetRefresh();
							break;
						case TransformationResult.UnknownOutputType:
							this.lastGenerationResult = "(Unknown output type)";
							break;
						case TransformationResult.TemplateProcessingError:
							this.lastGenerationResult = "(Template processing error)";
							break;
						case TransformationResult.TemplateCompilationError:
							this.lastGenerationResult = "(Template compilation error)";
							break;
						default:
							this.lastGenerationResult = "(Failure)";
							break;
					}
				}
				catch
				{
					this.lastGenerationResult = "(Failure)";
					throw;
				}
			}
			EditorGUILayout.EndHorizontal();
			GUI.enabled = true;

			// Apply the changes to the serialized object
			this.serializedObject.ApplyModifiedProperties();
		}
	}
}
