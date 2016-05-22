/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "T4 Transform" Unity Asset - https://www.assetstore.unity3d.com/en/#!/content/56706
	
	This source code is distributed via Unity Asset Store, 
	to use it in your project you should accept Terms of Service and EULA 
	https://unity3d.com/ru/legal/as_terms
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Editor.GameDevWare.TextTranform.Utils;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Assets.Editor.GameDevWare.TextTranform.Editors
{
	internal class TemplateInspector : UnityEditor.Editor
	{
		private Object lastAsset;
		private Object newAssetToWatch;
		private TemplateSettings templateSettings;

		static TemplateInspector()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		public static void OnSelectionChanged()
		{
			if (Selection.activeObject == null || TemplateSettings.IsTemplateAsset(Selection.activeObject) == false)
				return;

			try
			{
				var templateAsset = Selection.activeObject.GetType();
				var inspectorWindowType = typeof(PopupWindow).Assembly.GetType("UnityEditor.InspectorWindow");
				var inspectorWindow = EditorWindow.GetWindow(inspectorWindowType);
				var activeEditorTracker = inspectorWindow.GetFieldValue("m_Tracker");
				var customEditorAttributesType = typeof(PopupWindow).Assembly.GetType("UnityEditor.CustomEditorAttributes");
				var customEditorsList = (System.Collections.IList)customEditorAttributesType.GetFieldValue("kSCustomEditors");
				foreach (var customEditor in customEditorsList)
				{
					if ((Type)customEditor.GetFieldValue("m_InspectedType") != templateAsset) continue;

					var originalInspectorType = (Type)customEditor.GetFieldValue("m_InspectorType");
					// override inspector
					customEditor.SetFieldValue("m_InspectorType", typeof(TemplateInspector));
					// force rebuild editor list
					activeEditorTracker.Invoke("ForceRebuild");
					inspectorWindow.Invoke("Repaint");
					// restore original inspector
					customEditor.SetFieldValue("m_InspectorType", originalInspectorType);
				}

			}
			catch (Exception e)
			{
				Debug.LogError(e);
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
			var codeAsset = !string.IsNullOrEmpty(this.templateSettings.OutputPath) && File.Exists(this.templateSettings.OutputPath) ? AssetDatabase.LoadAssetAtPath<Object>(this.templateSettings.OutputPath) : null;
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
			if (GUILayout.Button("Generate"))
			{
				UnityTemplateGenerator.RunForTemplate(assetPath);
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
