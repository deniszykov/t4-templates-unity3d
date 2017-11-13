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

using System.Diagnostics.CodeAnalysis;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace Assets.Editor.GameDevWare.TextTransform
{
	[SuppressMessage("ReSharper", "UnusedMember.Local")]
	public static class Menu
	{
		public static bool VerboseLogs = false;


		[MenuItem("Tools/T4/Transform All Assets", false, 1)]
		private static void T4TransformAllAssets()
		{
			foreach (var templatePath in TemplateSettings.ListTemplatesInProject())
				UnityTemplateGenerator.RunForTemplate(templatePath);
		}
	}
}
