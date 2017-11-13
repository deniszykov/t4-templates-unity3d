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
using System.Collections.Generic;
using System.Linq;
using Assets.Editor.GameDevWare.TextTransform.Json;
using Assets.Editor.GameDevWare.TextTransform.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace Assets.Editor.GameDevWare.TextTransform
{
	internal sealed class TemplateSettings
	{
		public enum OutputTypes
		{
			CodeGenerator,
			Code
		}
		[Flags]
		public enum Triggers
		{
			CodeCompilation = 0x1 << 0,
			AssetChanges = 0x2 << 0,
		}

		public int Trigger;
		public int TriggerDelay;
		public int OutputType;
		public string OutputPath;
		public string[] WatchedAssets;

		public static TemplateSettings CreateDefault(string templatePath)
		{
			if (templatePath == null) throw new ArgumentNullException("templatePath");

			var settings = new TemplateSettings();
			settings.Trigger = (int)0;
			settings.TriggerDelay = (int)500;
			settings.OutputType = (int)OutputTypes.Code;
			settings.WatchedAssets = new string[0];
			return settings;
		}
		public static TemplateSettings Load(UnityEngine.Object templateAsset)
		{
			if (templateAsset == null) throw new NullReferenceException("templateAsset");

			var gameDataPath = AssetDatabase.GetAssetPath(templateAsset);
			return Load(gameDataPath);
		}
		public static TemplateSettings Load(string templatePath)
		{
			if (templatePath == null) throw new NullReferenceException("templatePath");

			var templateSettings = default(TemplateSettings);
			try
			{
				var gameDataSettingsJson = AssetImporter.GetAtPath(templatePath).userData;
				if (string.IsNullOrEmpty(gameDataSettingsJson) == false)
					templateSettings = JsonObject.Parse(gameDataSettingsJson).As<TemplateSettings>();

				if (templateSettings != null)
				{
					templateSettings.OutputPath = FileUtils.MakeProjectRelative(templateSettings.OutputPath);
					if (templateSettings.WatchedAssets.Any(string.IsNullOrEmpty))
						templateSettings.WatchedAssets = templateSettings.WatchedAssets.Where(s => !string.IsNullOrEmpty(s)).ToArray();
				}
			}
			catch (Exception e) { Debug.LogError("Failed to load template's settings: " + e); }

			if (templateSettings == null)
				templateSettings = CreateDefault(templatePath);

			return templateSettings;
		}

		public void Save(string templatePath)
		{
			if (templatePath == null) throw new ArgumentNullException("templatePath");

			try
			{
				if (this.WatchedAssets == null) this.WatchedAssets = new string[0];
				
				var importer = AssetImporter.GetAtPath(templatePath);
				importer.userData = JsonObject.From(this).Stringify();
#if UNITY_5
				importer.SaveAndReimport();
#else
				EditorUtility.SetDirty(importer);
				AssetDatabase.SaveAssets();
#endif
			}
			catch (Exception e) { Debug.LogError("Failed to save template's settings: " + e); }
		}

		public static List<string> ListTemplatesInProject()
		{
			var allTemplates = (from id in AssetDatabase.FindAssets("t:DefaultAsset").Union(AssetDatabase.FindAssets("t:TextAsset"))
								let path = FileUtils.MakeProjectRelative(AssetDatabase.GUIDToAssetPath(id))
								where path != null && IsTemplateAsset(path)
								select path).ToList();
			return allTemplates;
		}

		public static bool IsTemplateAsset(Object asset)
		{
			if (asset == null) throw new ArgumentNullException("asset");

			return IsTemplateAsset(AssetDatabase.GetAssetPath(asset));
		}
		public static bool IsTemplateAsset(string path)
		{
			if (path == null) throw new ArgumentNullException("path");

			return path.EndsWith(".tt", StringComparison.OrdinalIgnoreCase);
		}
	}
}
