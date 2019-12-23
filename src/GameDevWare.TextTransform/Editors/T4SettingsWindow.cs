using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using GameDevWare.TextTransform.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.TextTransform.Editors
{
	public static class T4SettingsWindow
	{
		private static Vector2 scrollPosition;

		private static HashSet<AssemblyName> assemblyIncludeList;
		private static HashSet<AssemblyName> assemblyExcludeList;
		private static HashSet<string> includePaths;
		private static HashSet<string> excludePaths;

		private static int loadedAssemblyCount;

		[PreferenceItem("T4")]
		public static void PreferencesGUI()
		{
			GUILayout.Space(5);
			GUILayout.Label("Project-wide T4 Template Settings:", EditorStyles.boldLabel);
			GUILayout.Space(10);
			GUILayout.Label("Referenced Assemblies:", EditorStyles.boldLabel);

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			foreach (var assemblyName in GetAllAssemblies().OrderByDescending(v => v, AssemblyNameOnlyComparer.Default))
			{
				var isChecked = assemblyIncludeList.Contains(assemblyName);
				if (EditorGUILayout.ToggleLeft(assemblyName.Name, isChecked) != isChecked)
				{
					(isChecked ? assemblyIncludeList : assemblyExcludeList).Remove(assemblyName);
					(isChecked ? assemblyExcludeList : assemblyIncludeList).Add(assemblyName);
				}
			}

			EditorGUILayout.EndScrollView();

			GUILayout.Space(10);
			GUILayout.Label("Include Paths:", EditorStyles.boldLabel);
			var includePathToRemove = default(string);
			var includePathToAdd = default(string);
			foreach (var path in GetIncludePaths())
			{
				if (EditorGUILayout.ToggleLeft(path, true) == false)
				{
					includePathToRemove = path;
				}
			}
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5);
			if (GUILayout.Button("Include File...", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
			{
				var selectedFile = EditorUtility.OpenFilePanel("Include File...", null, "dll");
				if (!string.IsNullOrEmpty(selectedFile))
				{
					includePathToAdd = PathUtils.MakeProjectRelative(selectedFile);
				}
			}
			GUILayout.Space(5);
			if (GUILayout.Button("Include Directory...", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
			{
				var selectedDirectory = EditorUtility.OpenFolderPanel("Include Directory...", null, null);
				if (!string.IsNullOrEmpty(selectedDirectory))
				{
					includePathToAdd = PathUtils.MakeProjectRelative(selectedDirectory);
				}
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
			GUILayout.Label("Exclude Paths:", EditorStyles.boldLabel);
			var excludePathToRemove = default(string);
			var excludePathToAdd = default(string);
			foreach (var path in GetExcludePaths())
			{
				if (EditorGUILayout.ToggleLeft(path, true) == false)
				{
					excludePathToRemove = path;
				}
			}
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5);
			if (GUILayout.Button("Exclude File...", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
			{
				var selectedFile = EditorUtility.OpenFilePanel("Exclude File...", null, "dll");
				if (!string.IsNullOrEmpty(selectedFile))
				{
					excludePathToAdd = PathUtils.MakeProjectRelative(selectedFile);
				}
			}
			GUILayout.Space(5);
			if (GUILayout.Button("Exclude Directory...", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
			{
				var selectedDirectory = EditorUtility.OpenFolderPanel("Exclude Directory...", null, null);
				if (!string.IsNullOrEmpty(selectedDirectory))
				{
					excludePathToAdd = PathUtils.MakeProjectRelative(selectedDirectory);
				}
			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(5);

			if (!string.IsNullOrEmpty(includePathToAdd) && includePaths.Add(includePathToAdd))
			{
				GUI.changed = true;
			}
			if (!string.IsNullOrEmpty(includePathToRemove) && includePaths.Remove(includePathToRemove))
			{
				GUI.changed = true;
			}
			if (!string.IsNullOrEmpty(excludePathToAdd) && excludePaths.Add(excludePathToAdd))
			{
				GUI.changed = true;
			}
			if (!string.IsNullOrEmpty(excludePathToRemove) && excludePaths.Remove(excludePathToRemove))
			{
				GUI.changed = true;
			}

			if (GUI.changed)
			{
				Settings.Current.IncludePaths = includePaths.ToArray();
				Settings.Current.ExcludePaths = excludePaths.ToArray();
				Settings.Current.ExcludeAssemblies = assemblyExcludeList.Select(n => n.FullName).ToArray();
				Settings.Current.Save();
			}
		}

		private static IEnumerable<AssemblyName> GetAllAssemblies()
		{
			var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			if (loadedAssemblyCount == currentAssemblies.Length && assemblyIncludeList != null && assemblyExcludeList != null)
			{
				return assemblyExcludeList.Concat(assemblyIncludeList);
			}

			assemblyExcludeList = new HashSet<AssemblyName>(AssemblyNameOnlyComparer.Default);
			assemblyIncludeList = new HashSet<AssemblyName>(AssemblyNameOnlyComparer.Default);
			foreach (var assembly in currentAssemblies)
			{
				try
				{
					if (assembly.ReflectionOnly) continue;
					var assemblyLocation = assembly.Location;
					if (string.IsNullOrEmpty(assemblyLocation)) continue;
					if (File.Exists(assemblyLocation) == false) continue;
				}
				catch { /* ignore */ }
				assemblyIncludeList.Add(assembly.GetName());
			}

			loadedAssemblyCount = currentAssemblies.Length;

			foreach (var excludeAssemblyName in Settings.Current.GetExcludeAssemblyNames())
			{
				assemblyIncludeList.Remove(excludeAssemblyName);
				assemblyExcludeList.Add(excludeAssemblyName);
			}

			return assemblyExcludeList.Concat(assemblyIncludeList);
		}
		private static IEnumerable<string> GetIncludePaths()
		{
			if (includePaths != null)
			{
				return includePaths;
			}
			includePaths = new HashSet<string>(Settings.Current.IncludePaths);
			return includePaths;
		}
		private static IEnumerable<string> GetExcludePaths()
		{
			if (excludePaths != null)
			{
				return excludePaths;
			}
			excludePaths = new HashSet<string>(Settings.Current.ExcludePaths);
			return excludePaths;
		}

	}
}
