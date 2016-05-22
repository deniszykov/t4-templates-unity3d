using System;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;

namespace Assets.Editor.GameDevWare.TextTranform
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
