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
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.TextTransform
{
	[SuppressMessage("ReSharper", "UnusedMember.Local")]
	public static class Menu
	{
		[MenuItem("Tools/T4/Verbose Logs", false, 20)]
		private static void SwitchVerboseLogs()
		{
			Settings.Current.Verbose = !Settings.Current.Verbose;
			Settings.Current.Save();
			UnityEditor.Menu.SetChecked("Tools/T4/Verbose Logs", Settings.Current.Verbose);
		}
		[MenuItem("Tools/T4/Verbose Logs", true, 20)]
		private static bool SwitchVerboseLogsCheck()
		{
			UnityEditor.Menu.SetChecked("Tools/T4/Verbose Logs", Settings.Current.Verbose);
			return true;
		}

		[MenuItem("Tools/T4/Transform All Assets", false, 1)]
		private static void T4TransformAllAssets()
		{
			foreach (var templatePath in TemplateSettings.ListTemplatesInProject())
			{
				var assetName = Path.GetFileName(templatePath);
				Debug.Log(string.Format("Running transformation for '{0}' asset...", assetName));
				var result = UnityTemplateGenerator.RunForTemplate(templatePath);
				Debug.Log(string.Format("Transformation for '{0}' asset is completed. Result: {1}", assetName, result));
			}
			AssetChangesTrigger.DoDelayedAssetRefresh();
		}
		[MenuItem("Tools/T4/Transform All Assets", true, 1)]
		private static bool T4TransformAllAssetsCheck()
		{
			return !EditorApplication.isCompiling;
		}

		[MenuItem("Tools/T4/Settings...", false, 30)]
		private static void ShowSettings()
		{
			var settingsService = typeof(UnityEditor.EditorApplication).Assembly.GetType("UnityEditor.SettingsService", throwOnError: false, ignoreCase: true);
			var preferencesWindowType = typeof(UnityEditor.EditorApplication).Assembly.GetType("UnityEditor.PreferencesWindow", throwOnError: false, ignoreCase: true);
			var settingsWindowType = typeof(UnityEditor.EditorApplication).Assembly.GetType("UnityEditor.SettingsWindow", throwOnError: false, ignoreCase: true);
			if (settingsService != null)
			{
				settingsService.InvokeMember("OpenUserPreferences", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod,
					null, null, new object[] { "Preferences/T4" });
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
	}
}
