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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GameDevWare.TextTransform.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.TextTransform
{
	[InitializeOnLoad, Serializable]
	internal class AssetChangesTrigger : AssetPostprocessor
	{
		private static readonly Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>(StringComparer.Ordinal);
		private static readonly Dictionary<string, List<string>> templatePathByWatchedPaths = new Dictionary<string, List<string>>();
		private static readonly HashSet<string> changedAssets = new HashSet<string>(StringComparer.Ordinal);
		private static bool reloadWatchList = true;
		private static int doDelayedAssetRefresh = -1;

		static AssetChangesTrigger()
		{
			EditorApplication.update += Update;
		}

		public static void ReloadWatchList()
		{
			reloadWatchList = true;
			foreach (var watcher in watchers.Values)
				watcher.Dispose();
			templatePathByWatchedPaths.Clear();
			watchers.Clear();
		}

		private static void Update()
		{
			if (reloadWatchList)
			{
				CreateWatchers();
				reloadWatchList = false;
			}

			if (doDelayedAssetRefresh > 0 && --doDelayedAssetRefresh == 0)
			{
				if (Settings.Current.Verbose)
					Debug.Log("Performing forced asset refresh.");

				AssetDatabase.Refresh();
				doDelayedAssetRefresh = -1;
			}

			CheckChangedAssets();
		}
		private static void CheckChangedAssets()
		{
			var changedAssetsCopy = default(string[]);
			lock (changedAssets)
			{
				if (changedAssets.Count > 0)
				{
					changedAssetsCopy = changedAssets.ToArray();
					changedAssets.Clear();
				}
			}

			if (changedAssetsCopy == null) return;

			
			//if (Settings.Current.Verbose)
			//	Debug.Log("Watched paths: " + string.Join(", ", templatePathByWatchedPaths.Keys.ToArray()));

			var triggeredTemplatePaths = new HashSet<string>();
			foreach (var changedAsset in changedAssetsCopy)
			{
				if (!File.Exists(changedAsset))
					continue;

				//if (Settings.Current.Verbose)
				//	Debug.Log("Changed Asset: " + changedAsset);

				foreach (var watchedPath in templatePathByWatchedPaths.Keys)
					if (changedAsset.StartsWith(watchedPath, StringComparison.Ordinal))
						foreach (var templatePath in templatePathByWatchedPaths[watchedPath])
							triggeredTemplatePaths.Add(templatePath);
			}

			foreach (var templatePath in triggeredTemplatePaths)
			{
				if (Settings.Current.Verbose)
					Debug.Log(string.Format("Asset modification is triggered T4 template's generator at '{0}'.", templatePath));

				var settings = TemplateSettings.Load(templatePath);
				UnityTemplateGenerator.RunForTemplateWithDelay(templatePath, TimeSpan.FromMilliseconds(settings.TriggerDelay));
			}
		}
		private static void CreateWatchers()
		{
			if (Settings.Current.Verbose)
				Debug.Log("Recreating watchers.");

			foreach (var templatePath in TemplateSettings.ListTemplatesInProject())
			{
				var settings = TemplateSettings.Load(templatePath);
				if ((settings.Trigger & (int)TemplateSettings.Triggers.AssetChanges) == 0 || settings.WatchedAssets == null)
					continue;

				foreach (var watchedAssetPath in settings.WatchedAssets)
				{
					var watchedDirectory = Path.GetDirectoryName(Path.GetFullPath(watchedAssetPath));
					if (watchedDirectory == null)
						continue;

					var templatePathList = default(List<string>);
					if (templatePathByWatchedPaths.TryGetValue(watchedAssetPath, out templatePathList) == false)
						templatePathByWatchedPaths.Add(watchedAssetPath, templatePathList = new List<string> { templatePath });
					else
						templatePathList.Add(templatePath);

					if (watchers.ContainsKey(watchedDirectory) || Directory.Exists(watchedDirectory) == false)
						continue;

					try
					{
						var watcher = new FileSystemWatcher(watchedDirectory)
						{
							NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
							Filter = Path.GetFileName(watchedDirectory)
						};
						watcher.Changed += (sender, args) =>
						{
							var path = PathUtils.MakeProjectRelative(args.FullPath);
							lock (changedAssets)
								changedAssets.Add(path);
						};
						watchers.Add(watchedDirectory, watcher);
						watcher.EnableRaisingEvents = true;
					}
					catch (Exception e)
					{
						Debug.LogError("Failed to create FileSystemWatcher for asset " + watchedAssetPath + ": " + e);
					}
				}
			}
		}

		// ReSharper disable once UnusedMember.Local
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			lock (changedAssets)
			{
				if (importedAssets != null)
					foreach (var asset in importedAssets)
						changedAssets.Add(PathUtils.MakeProjectRelative(asset));

				if (deletedAssets != null)
					foreach (var asset in deletedAssets)
						changedAssets.Add(PathUtils.MakeProjectRelative(asset));

				if (movedAssets != null)
					foreach (var asset in movedAssets)
						changedAssets.Add(PathUtils.MakeProjectRelative(asset));

				if (movedFromAssetPaths != null)
					foreach (var asset in movedFromAssetPaths)
						changedAssets.Add(PathUtils.MakeProjectRelative(asset));
			}
		}
		public static void DoDelayedAssetRefresh()
		{
			doDelayedAssetRefresh = 100;
		}
	}
}
