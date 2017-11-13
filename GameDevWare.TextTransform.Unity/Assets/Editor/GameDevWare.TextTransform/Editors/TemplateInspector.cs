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

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Editor.GameDevWare.TextTransform.Utils;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace Assets.Editor.GameDevWare.TextTransform.Editors
{
	internal class TemplateInspector : UnityEditor.Editor
	{
		private Object lastAsset;
		private Object newAssetToWatch;
		private TemplateSettings templateSettings;
		private string lastGenerationResult;

		static TemplateInspector()
		{
#if UNITY_5
			Selection.selectionChanged += OnSelectionChanged;
#else
			var selection = default(Object);
			EditorApplication.update += () =>
			{
				if (selection == Selection.activeObject)
					return;
				selection = Selection.activeObject;
				OnSelectionChanged();
			};
#endif
		}

		public static void OnSelectionChanged()
		{
			if (Selection.activeObject == null || TemplateSettings.IsTemplateAsset(Selection.activeObject) == false)
				return;

			try
			{
				var selectedAssetType = Selection.activeObject.GetType();
				var inspectorWindowType = typeof(EditorApplication).Assembly.GetType("UnityEditor.InspectorWindow");
				var inspectorWindow = EditorWindow.GetWindow(inspectorWindowType);
				var activeEditorTracker = (ActiveEditorTracker)(inspectorWindow.HasProperty("tracker") ?
					inspectorWindow.GetPropertyValue("tracker") :
					inspectorWindow.GetFieldValue("m_Tracker"));
				var customEditorAttributesType = typeof(EditorApplication).Assembly.GetType("UnityEditor.CustomEditorAttributes");
				var monoEditorType = customEditorAttributesType.GetNestedType("MonoEditorType", BindingFlags.NonPublic);
				var customEditorsList = (System.Collections.IList)customEditorAttributesType.GetFieldValue("kSCustomEditors");
				var cachedCustomEditorsList = customEditorAttributesType.HasField("kCachedEditorForType") ?
					(Dictionary<Type, Type>)customEditorAttributesType.GetFieldValue("kCachedEditorForType") :
					null;

				foreach (var customEditor in customEditorsList)
				{
					if (customEditor == null || (Type)customEditor.GetFieldValue("m_InspectedType") != selectedAssetType)
						continue;

					var originalInspectorType = (Type)customEditor.GetFieldValue("m_InspectorType");
					// override inspector
					customEditor.SetFieldValue("m_InspectorType", typeof(TemplateInspector));
					if (cachedCustomEditorsList != null)
						cachedCustomEditorsList[selectedAssetType] = typeof(TemplateInspector);
					// force rebuild editor list
					activeEditorTracker.ForceRebuild();
					inspectorWindow.Repaint();
					// restore original inspector
					customEditor.SetFieldValue("m_InspectorType", originalInspectorType);
					if (cachedCustomEditorsList != null)
						cachedCustomEditorsList.Remove(selectedAssetType);

					return;
				}

				var newMonoEditorType = Activator.CreateInstance(monoEditorType);
				newMonoEditorType.SetFieldValue("m_InspectedType", selectedAssetType);
				newMonoEditorType.SetFieldValue("m_InspectorType", typeof(TemplateInspector));
				newMonoEditorType.SetFieldValue("m_EditorForChildClasses", false);
				if (monoEditorType.HasField("m_IsFallback"))
					newMonoEditorType.SetFieldValue("m_IsFallback", false);

				// override inspector
				customEditorsList.Insert(0, newMonoEditorType);
				if (cachedCustomEditorsList != null)
					cachedCustomEditorsList[selectedAssetType] = typeof(TemplateInspector);
				// force rebuild editor list
				activeEditorTracker.ForceRebuild();
				inspectorWindow.Repaint();

				// restore original inspector
				customEditorsList.Remove(newMonoEditorType);
				if (cachedCustomEditorsList != null)
					cachedCustomEditorsList.Remove(selectedAssetType);
			}
			catch (Exception updateEditorError)
			{
				Debug.LogError(updateEditorError);
			}
		}

		public override void OnInspectorGUI()
		{
			var asset = (Object)this.target;
			var assetPath = FileUtils.MakeProjectRelative(AssetDatabase.GetAssetPath(asset));
			if (assetPath == null || assetPath.EndsWith(".tt") == false)
			{
				this.DrawDefaultInspector();
				return;
			}

			if (this.lastAsset != asset || this.templateSettings == null)
			{
				this.templateSettings = TemplateSettings.Load(assetPath);
				this.lastAsset = asset;
			}
			GUI.enabled = true;
			GUILayout.Label(Path.GetFileName(assetPath), EditorStyles.boldLabel);
			this.templateSettings.OutputType = (int)(TemplateSettings.OutputTypes)EditorGUILayout.EnumPopup("Output Type", (TemplateSettings.OutputTypes)this.templateSettings.OutputType);
			var codeAsset = !string.IsNullOrEmpty(this.templateSettings.OutputPath) && File.Exists(this.templateSettings.OutputPath) ? AssetDatabase.LoadAssetAtPath(this.templateSettings.OutputPath, typeof(Object)) : null;
			if (codeAsset != null)
				this.templateSettings.OutputPath = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField("Output Path", codeAsset, typeof(Object), false));
			else
				this.templateSettings.OutputPath = EditorGUILayout.TextField("Output Path", this.templateSettings.OutputPath);
			this.templateSettings.Trigger = (int)(TemplateSettings.Triggers)EditorGUILayout.EnumMaskField("Auto-Gen Triggers", (TemplateSettings.Triggers)templateSettings.Trigger);
			this.templateSettings.TriggerDelay = (int)EditorGUILayout.IntField("Auto-Gen Delay (Ms)", templateSettings.TriggerDelay);

			if ((this.templateSettings.Trigger & (int)TemplateSettings.Triggers.AssetChanges) != 0)
			{
				EditorGUILayout.Space();
				GUILayout.Label("Assets to Watch", EditorStyles.boldLabel);
				for (var i = 0; i < this.templateSettings.WatchedAssets.Length; i++)
				{
					var watchedAssetPath = this.templateSettings.WatchedAssets[i];
					var assetExists = !string.IsNullOrEmpty(watchedAssetPath) && (File.Exists(watchedAssetPath) || Directory.Exists(watchedAssetPath));
					var watchedAsset = assetExists ? AssetDatabase.LoadMainAssetAtPath(watchedAssetPath) : null;
					if (watchedAsset != null)
						this.templateSettings.WatchedAssets[i] = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(watchedAsset.GetType().Name, watchedAsset, typeof(Object), false));
					else
						this.templateSettings.WatchedAssets[i] = EditorGUILayout.TextField("Path", watchedAssetPath);
				}
				EditorGUILayout.Space();
				this.newAssetToWatch = EditorGUILayout.ObjectField("<New>", this.newAssetToWatch, typeof(Object), false);
				if (Event.current.type == EventType.repaint && newAssetToWatch != null)
				{
					var watchedAssets = new HashSet<string>(this.templateSettings.WatchedAssets);
					watchedAssets.Remove("");
					watchedAssets.Add(AssetDatabase.GetAssetPath(this.newAssetToWatch));
					this.templateSettings.WatchedAssets = watchedAssets.ToArray();
					this.newAssetToWatch = null;
					GUI.changed = true;
				}
			}

			EditorGUILayout.Space();
			GUILayout.Label("Actions", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Generate " + this.lastGenerationResult))
			{
				if (UnityTemplateGenerator.RunForTemplate(assetPath) == false)
					this.lastGenerationResult = "(Failure)";
				else
					this.lastGenerationResult = "(Success)";
			}
			EditorGUILayout.EndHorizontal();

			if (GUI.changed)
			{
				this.templateSettings.Save(assetPath);
				if ((this.templateSettings.Trigger & (int)TemplateSettings.Triggers.AssetChanges) != 0)
					AssetChangesTrigger.ReloadWatchList();
			}
		}
	}
}
