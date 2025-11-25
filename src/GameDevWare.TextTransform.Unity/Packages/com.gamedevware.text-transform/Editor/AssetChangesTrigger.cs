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
using GameDevWare.TextTransform.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.TextTransform.Editor
{
	[InitializeOnLoad, Serializable]
	internal class AssetChangesTrigger : AssetPostprocessor
	{
		private static readonly Dictionary<string, FileSystemWatcher> Watchers = new(StringComparer.Ordinal);
		private static readonly Dictionary<string, List<string>> TemplatePathByWatchedPaths = new(StringComparer.Ordinal);
		private static readonly HashSet<string> ChangedAssets = new(StringComparer.Ordinal);
		private static bool reloadWatchList = true;
		private static int doDelayedAssetRefresh = -1;

		static AssetChangesTrigger()
		{
			EditorApplication.update += Update;
		}

		// ReSharper disable once UnusedMember.Local
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			lock (ChangedAssets)
			{
				if (importedAssets != null)
				{
					foreach (var asset in importedAssets)
					{
						ChangedAssets.Add(PathUtils.MakeProjectRelative(asset));
					}
				}

				if (deletedAssets != null)
				{
					foreach (var asset in deletedAssets)
					{
						ChangedAssets.Add(PathUtils.MakeProjectRelative(asset));
					}
				}

				if (movedAssets != null)
				{
					foreach (var asset in movedAssets)
					{
						ChangedAssets.Add(PathUtils.MakeProjectRelative(asset));
					}
				}

				if (movedFromAssetPaths != null)
				{
					foreach (var asset in movedFromAssetPaths)
					{
						ChangedAssets.Add(PathUtils.MakeProjectRelative(asset));
					}
				}
			}
		}

		public static void ReloadWatchList()
		{
			reloadWatchList = true;
			foreach (var watcher in Watchers.Values)
			{
				watcher.Dispose();
			}

			TemplatePathByWatchedPaths.Clear();
			Watchers.Clear();
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
				if (TextTemplateToolSettings.Current.verbose)
					Debug.Log("Performing forced asset refresh.");

				AssetDatabase.Refresh();
				doDelayedAssetRefresh = -1;
			}

			CheckChangedAssets();
		}
		private static void CheckChangedAssets()
		{
			var changedAssetsCopy = default(string[]);
			lock (ChangedAssets)
			{
				if (ChangedAssets.Count > 0)
				{
					changedAssetsCopy = ChangedAssets.ToArray();
					ChangedAssets.Clear();
				}
			}

			if (changedAssetsCopy == null) return;

			var triggeredTemplatePaths = new HashSet<string>(StringComparer.Ordinal);
			foreach (var changedAsset in changedAssetsCopy.Select(PathUtils.Normalize))
			{
				if (!File.Exists(Path.GetFullPath(changedAsset, PathUtils.ProjectPath))) continue;

				foreach (var watchedPath in TemplatePathByWatchedPaths.Keys)
				{
					if (!changedAsset.StartsWith(watchedPath, StringComparison.Ordinal)) continue;

					foreach (var templatePath in TemplatePathByWatchedPaths[watchedPath])
					{
						triggeredTemplatePaths.Add(templatePath);
					}
				}
			}

			foreach (var templatePath in triggeredTemplatePaths)
			{
				if (TextTemplateToolSettings.Current.verbose)
					Debug.Log($"Asset modification is triggered T4 template's generator at '{templatePath}'.");

				if (!TextTemplateImporter.TryLoad(templatePath, out var textTemplateImporter)) continue;

				UnityTemplateGenerator.RunForTemplateWithDelay(templatePath, TimeSpan.FromMilliseconds(textTemplateImporter.triggerDelay));
			}
		}
		private static void CreateWatchers()
		{
			if (TextTemplateToolSettings.Current.verbose) Debug.Log("Recreating text template asset watchers.");

			foreach (var templatePath in TextTemplateImporter.ListTemplatesInProject())
			{
				if (!TextTemplateImporter.TryLoad(templatePath, out var textTemplateImporter))
				{
					if (TextTemplateToolSettings.Current.verbose) Debug.Log($"Found Text Template file with invalid ScriptedImporter at {templatePath}.");
					continue;
				}

				if ((textTemplateImporter.generationTriggers & GenerationTriggers.AssetChanges) == 0) continue;

				var watchedAssets = new HashSet<string>(textTemplateImporter.watchedAssets ?? Array.Empty<string>()) {
					templatePath
				};

				foreach (var watchedAssetPath in watchedAssets.Select(PathUtils.Normalize))
				{
					var watchedDirectory = Path.GetDirectoryName(Path.GetFullPath(watchedAssetPath, PathUtils.ProjectPath));
					if (watchedDirectory == null)
						continue;

					if (TextTemplateToolSettings.Current.verbose) Debug.Log($"Adding asset '{watchedAssetPath}' to watched assets list for Text Templates.");

					if (!TemplatePathByWatchedPaths.TryGetValue(watchedAssetPath, out var templatePathList))
						TemplatePathByWatchedPaths.Add(watchedAssetPath, templatePathList = new List<string> { templatePath });
					else
						templatePathList.Add(templatePath);

					if (Watchers.ContainsKey(watchedDirectory) || !Directory.Exists(watchedDirectory)) continue;

					try
					{
						var watcher = new FileSystemWatcher(watchedDirectory) {
							NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
							Filter = Path.GetFileName(watchedDirectory)
						};
						watcher.Changed += (_, args) =>
						{
							var path = PathUtils.MakeProjectRelative(args.FullPath);
							lock (ChangedAssets)
								ChangedAssets.Add(path);
						};
						Watchers.Add(watchedDirectory, watcher);
						watcher.EnableRaisingEvents = true;
					}
					catch (Exception e)
					{
						Debug.LogError("Failed to create FileSystemWatcher for asset " + watchedAssetPath + ": " + e);
					}
				}
			}
		}
		public static void DoDelayedAssetRefresh()
		{
			doDelayedAssetRefresh = 100;
		}
	}
}
