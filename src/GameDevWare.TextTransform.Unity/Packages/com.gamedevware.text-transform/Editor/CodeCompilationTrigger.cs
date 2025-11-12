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
using GameDevWare.TextTransform.Editor.Windows;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.TextTransform.Editor
{
	[InitializeOnLoad]
	internal static class CodeCompilationTrigger
	{
		private static readonly EditorApplication.CallbackFunction InitializeCallback = Initialize;

		static CodeCompilationTrigger()
		{
			EditorApplication.update += InitializeCallback;
		}

		private static void Initialize()
		{
			EditorApplication.update -= InitializeCallback;

			System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(TextTemplateImporterInspector).TypeHandle);

			foreach (var templatePath in TextTemplateImporter.ListTemplatesInProject())
			{
				if (!TextTemplateImporter.TryLoad(templatePath, out var textTemplateImporter))
				{
					continue;
				}

				if ((textTemplateImporter.generationTriggers & TextTemplateImporter.GenerationTriggers.CodeCompilation) == 0)
				{
					continue;
				}

				if (TextTemplateToolSettings.Current.verbose)
					Debug.Log($"Code compilation in project is triggered T4 template's generator at '{templatePath}'.");

				UnityTemplateGenerator.RunForTemplateWithDelay(templatePath, TimeSpan.FromMilliseconds(textTemplateImporter.triggerDelay));
			}
		}
	}
}
