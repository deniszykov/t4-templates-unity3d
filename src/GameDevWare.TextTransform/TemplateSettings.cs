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
using GameDevWare.TextTransform.Json;
using GameDevWare.TextTransform.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace GameDevWare.TextTransform
{
	/// <summary>
	/// T4 Template run settings.
	/// </summary>
	public sealed class TemplateSettings
	{
		/// <summary>
		/// Run mode. Determine result of transformation.
		/// </summary>
		public enum OutputTypes
		{
			/// <summary>
			/// Result is C# code of template which could output <see cref="Text"/> if ran.
			/// </summary>
			TextGenerator,
			/// <summary>
			/// Result is generated code/markup.
			/// </summary>
			Text
		}

		/// <summary>
		/// Transformation triggers.
		/// </summary>
		[Flags]
		public enum Triggers
		{
			/// <summary>
			/// Each Unity's editor code compilation will trigger transformation.
			/// </summary>
			CodeCompilation = 0x1 << 0,
			/// <summary>
			/// Each change in watched assets will trigger transformation.
			/// </summary>
			AssetChanges = 0x2 << 0,
		}

		/// <summary>
		/// Auto-transformation triggers.
		/// </summary>
		public int Trigger;
		/// <summary>
		/// Delay to auto-transformation run after trigger event occurs.
		/// </summary>
		public int TriggerDelay;
		/// <summary>
		/// Transformation result type. Generator or code/markup.
		/// </summary>
		public int OutputType;
		/// <summary>
		/// Path to place transformation result.
		/// </summary>
		public string OutputPath;
		/// <summary>
		/// Project relative paths to watched assets.
		/// </summary>
		public string[] WatchedAssets;

		/// <summary>
		/// Create default settings for template at <paramref name="templatePath"/>.
		/// </summary>
		public static TemplateSettings CreateDefault(string templatePath)
		{
			if (templatePath == null) throw new ArgumentNullException("templatePath");

			var settings = new TemplateSettings();
			settings.Trigger = (int)0;
			settings.TriggerDelay = (int)500;
			settings.OutputType = (int)OutputTypes.Text;
			settings.WatchedAssets = new string[0];
			return settings;
		}
		/// <summary>
		/// Load settings for <paramref name="templateAsset"/>.
		/// </summary>
		public static TemplateSettings Load(UnityEngine.Object templateAsset)
		{
			if (templateAsset == null) throw new NullReferenceException("templateAsset");

			var gameDataPath = AssetDatabase.GetAssetPath(templateAsset);
			return Load(gameDataPath);
		}
		/// <summary>
		/// Load settings for <paramref name="templatePath"/>.
		/// </summary>
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
					templateSettings.OutputPath = PathUtils.MakeProjectRelative(templateSettings.OutputPath);
					if (templateSettings.WatchedAssets.Any(string.IsNullOrEmpty))
						templateSettings.WatchedAssets = templateSettings.WatchedAssets.Where(s => !string.IsNullOrEmpty(s)).ToArray();
				}
			}
			catch (Exception e) { Debug.LogError("Failed to load template's settings: " + e); }

			if (templateSettings == null)
				templateSettings = CreateDefault(templatePath);

			return templateSettings;
		}

		/// <summary>
		/// Save settings for <paramref name="templatePath"/>.
		/// </summary>
		public void Save(string templatePath)
		{
			if (templatePath == null) throw new ArgumentNullException("templatePath");

			try
			{
				if (this.WatchedAssets == null) this.WatchedAssets = new string[0];
				
				var importer = AssetImporter.GetAtPath(templatePath);
				importer.userData = JsonObject.From(this).Stringify();
				importer.SaveAndReimport();
			}
			catch (Exception e) { Debug.LogError("Failed to save template's settings: " + e); }
		}

		/// <summary>
		/// List all T4 templates in current project.
		/// </summary>
		/// <returns></returns>
		public static List<string> ListTemplatesInProject()
		{
			var allTemplates = (from id in AssetDatabase.FindAssets("t:DefaultAsset").Union(AssetDatabase.FindAssets("t:TextAsset"))
								let path = PathUtils.MakeProjectRelative(AssetDatabase.GUIDToAssetPath(id))
								where path != null && IsTemplateAsset(path)
								select path).ToList();
			return allTemplates;
		}

		/// <summary>
		/// Determines if asset is T4 template.
		/// </summary>
		public static bool IsTemplateAsset(Object asset)
		{
			if (asset == null) throw new ArgumentNullException("asset");

			return IsTemplateAsset(AssetDatabase.GetAssetPath(asset));
		}
		/// <summary>
		/// Determines if asset is T4 template.
		/// </summary>
		public static bool IsTemplateAsset(string path)
		{
			if (path == null) throw new ArgumentNullException("path");

			return path.EndsWith(".tt", StringComparison.OrdinalIgnoreCase);
		}
	}
}
