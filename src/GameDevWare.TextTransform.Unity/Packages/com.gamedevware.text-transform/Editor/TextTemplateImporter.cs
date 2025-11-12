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
using GameDevWare.TextTransform.Editor.Utils;
using UnityEditor;
using UnityEditor.AssetImporters;
using Object = UnityEngine.Object;


namespace GameDevWare.TextTransform.Editor
{
	/// <summary>
	/// T4 Template importer with it's settings.
	/// </summary>
	public abstract class TextTemplateImporter : ScriptedImporter
	{
		/// <summary>
		/// Run mode. Determine result of transformation.
		/// </summary>
		public enum GenerationOutput
		{
			/// <summary>
			/// Result is generated code/markup.
			/// </summary>
			Text,
			/// <summary>
			/// Result is C# code of template which could output <see cref="Text"/> if ran.
			/// </summary>
			TextGenerator,
		}

		/// <summary>
		/// Transformation triggers.
		/// </summary>
		[Flags]
		public enum GenerationTriggers
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
		public GenerationTriggers generationTriggers;
		/// <summary>
		/// Delay to auto-transformation run after trigger event occurs.
		/// </summary>
		public int triggerDelay;
		/// <summary>
		/// Transformation result type. Generator or code/markup.
		/// </summary>
		public GenerationOutput generationOutput;
		/// <summary>
		/// Path to place transformation result.
		/// </summary>
		public string outputPath;
		/// <summary>
		/// Project relative paths to watched assets.
		/// </summary>
		public string[] watchedAssets;

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

		public static bool TryLoad(string templatePath, out TextTemplateImporter textTemplateImporter)
		{
			textTemplateImporter = GetAtPath(templatePath) as TextTemplateImporter;
			if (textTemplateImporter != null)
			{
				textTemplateImporter.Validate();
			}
			return textTemplateImporter != null;
		}

		public void Validate()
		{
			if (this.triggerDelay < 500)
			{
				this.triggerDelay = (int)500;
			}
			this.watchedAssets ??= new string[0];
		}

		/// <summary>
		/// Determines if asset is T4 template.
		/// </summary>
		public static bool IsTemplateAsset(Object asset)
		{
			if (asset == null) throw new ArgumentNullException(nameof(asset));

			return IsTemplateAsset(AssetDatabase.GetAssetPath(asset));
		}
		/// <summary>
		/// Determines if asset is T4 template.
		/// </summary>
		public static bool IsTemplateAsset(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));

			return AssetImporter.GetAtPath(path) != null && path.EndsWith(".tt", StringComparison.OrdinalIgnoreCase);
		}
	}
}
