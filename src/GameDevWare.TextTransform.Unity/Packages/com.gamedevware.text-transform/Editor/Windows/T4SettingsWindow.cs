using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GameDevWare.TextTransform.Editor.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.TextTransform.Editor.Windows
{
	public static class T4SettingsWindow
	{
		private static HashSet<AssemblyName> assemblyExcludeList;

		private static HashSet<AssemblyName> assemblyIncludeList;
		private static HashSet<string> excludePaths;
		private static HashSet<string> includePaths;

		private static int loadedAssemblyCount;
		private static Vector2 scrollPosition;
		private static bool showReferencedAssemblies;
		private static bool showTestResult;
		private static Task<string> testResult;

		[SettingsProvider]
		public static SettingsProvider CreateCharonSettingsProvider()
		{
			var provider = new SettingsProvider("Project/T4", SettingsScope.Project) {
				label = "T4",

				// Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
				guiHandler = PreferencesGUI,

				// Populate the search keywords to enable smart search filtering and label highlighting:
				keywords = new HashSet<string>(new[] {
					"T4",
					"Templates",
					"Code Generation"
				})
			};
			return provider;
		}

		[UsedImplicitly]
		private static void PreferencesGUI(string searchContext)
		{
			// padding
			GUILayout.BeginVertical();
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			;
			GUILayout.Space(20);
			GUILayout.BeginVertical();

			//

			GUILayout.Space(5);
			GUILayout.Label("Project-wide T4 Template Settings:", EditorStyles.boldLabel);
			GUILayout.Space(10);

			// template compiler type
			GUILayout.Label("Template Compiler:", EditorStyles.boldLabel);
			TextTemplateToolSettings.Current.templateCompiler =
				(TextTemplateToolSettings.TemplateCompiler)EditorGUILayout.EnumPopup(TextTemplateToolSettings.Current.templateCompiler);
			GUILayout.Space(5);

			switch (TextTemplateToolSettings.Current.templateCompiler)
			{
				case TextTemplateToolSettings.TemplateCompiler.DotnetT4:
					GUILayout.Label("Dotnet Tool Path:", EditorStyles.boldLabel);
					GUILayout.BeginHorizontal();
					TextTemplateToolSettings.Current.dotnetToolPath = EditorGUILayout.TextField(TextTemplateToolSettings.Current.dotnetToolPath);
					if (GUILayout.Button("Check", EditorStyles.toolbarButton, GUILayout.Width(60), GUILayout.Height(18)))
						testResult = RunToolAsync($"\"{TextTemplateToolSettings.Current.dotnetToolPath}\" --info ");
					GUILayout.EndHorizontal();
					GUILayout.Space(5);
					break;
				case TextTemplateToolSettings.TemplateCompiler.Roslyn:
					GUILayout.Label("Roslyn Path:", EditorStyles.boldLabel);
					GUILayout.BeginHorizontal();
					TextTemplateToolSettings.Current.roslynCompilerPath = EditorGUILayout.TextField(TextTemplateToolSettings.Current.roslynCompilerPath);
					if (GUILayout.Button("Check", EditorStyles.toolbarButton, GUILayout.Width(60), GUILayout.Height(18)))
						testResult = RunToolAsync($"\"{TextTemplateToolSettings.Current.roslynCompilerPath}\" -help ");
					GUILayout.EndHorizontal();
					GUILayout.Space(5);
					break;
				case TextTemplateToolSettings.TemplateCompiler.BuildIn:
				default: break;
			}

			if (testResult != null && testResult.IsCompleted)
			{
				GUILayout.Space(10);
				if (testResult.Exception != null)
					EditorGUILayout.HelpBox(LimitText(testResult.Exception.Message, 300), MessageType.Error);
				else
					EditorGUILayout.HelpBox(LimitText(testResult.Result, 300), MessageType.Info);
				GUILayout.Space(10);
			}

			// assembly list
			GUILayout.Label("Referenced Assemblies:", EditorStyles.boldLabel);

			if (!showReferencedAssemblies && GUILayout.Button("Show", EditorStyles.toolbarButton, GUILayout.Width(60), GUILayout.Height(18)))
				showReferencedAssemblies = true;
			if (showReferencedAssemblies)
			{
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

				var allAssemblyNames = GetAllAssemblies().Where(NotRuntimeAssembly).OrderByDescending(v => v, AssemblyNameOnlyComparer.Default).ToList();
				foreach (var assemblyName in allAssemblyNames)
				{
					var isChecked = assemblyIncludeList.Contains(assemblyName);
					if (EditorGUILayout.ToggleLeft(new GUIContent(assemblyName.Name, assemblyName.CodeBase), isChecked) != isChecked)
					{
						(isChecked ? assemblyIncludeList : assemblyExcludeList).Remove(assemblyName);
						(isChecked ? assemblyExcludeList : assemblyIncludeList).Add(assemblyName);
					}
				}

				EditorGUILayout.EndScrollView();
				GUILayout.Space(5);
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(5);
				if (GUILayout.Button("Hide", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18))) showReferencedAssemblies = false;
				if (GUILayout.Button("Exclude All", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
				{
					assemblyIncludeList.Clear();
					allAssemblyNames.ForEach(assemblyName => assemblyExcludeList.Add(assemblyName));
				}

				GUILayout.Space(5);
				if (GUILayout.Button("Include All", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
				{
					assemblyExcludeList.Clear();
					allAssemblyNames.ForEach(assemblyName => assemblyIncludeList.Add(assemblyName));
				}

				EditorGUILayout.EndHorizontal();
				GUILayout.Space(10);
			}

			GUILayout.Space(10);

			// include paths
			GUILayout.Label("Include Paths:", EditorStyles.boldLabel);
			var includePathToRemove = default(string);
			var includePathToAdd = default(string);
			foreach (var path in GetIncludePaths())
			{
				if (!EditorGUILayout.ToggleLeft(path, true)) includePathToRemove = path;
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5);
			if (GUILayout.Button("Include File...", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
			{
				var selectedFile = EditorUtility.OpenFilePanel("Include File...", null, "dll");
				if (!string.IsNullOrEmpty(selectedFile)) includePathToAdd = PathUtils.MakeProjectRelative(selectedFile);
			}

			GUILayout.Space(5);
			if (GUILayout.Button("Include Directory...", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
			{
				var selectedDirectory = EditorUtility.OpenFolderPanel("Include Directory...", null, null);
				if (!string.IsNullOrEmpty(selectedDirectory)) includePathToAdd = PathUtils.MakeProjectRelative(selectedDirectory);
			}

			EditorGUILayout.EndHorizontal();
			GUILayout.Space(10);

			// exclude paths
			GUILayout.Label("Exclude Paths:", EditorStyles.boldLabel);
			var excludePathToRemove = default(string);
			var excludePathToAdd = default(string);
			foreach (var path in GetExcludePaths())
			{
				if (!EditorGUILayout.ToggleLeft(path, true)) excludePathToRemove = path;
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5);
			if (GUILayout.Button("Exclude File...", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
			{
				var selectedFile = EditorUtility.OpenFilePanel("Exclude File...", null, "dll");
				if (!string.IsNullOrEmpty(selectedFile)) excludePathToAdd = PathUtils.MakeProjectRelative(selectedFile);
			}

			GUILayout.Space(5);
			if (GUILayout.Button("Exclude Directory...", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
			{
				var selectedDirectory = EditorUtility.OpenFolderPanel("Exclude Directory...", null, null);
				if (!string.IsNullOrEmpty(selectedDirectory)) excludePathToAdd = PathUtils.MakeProjectRelative(selectedDirectory);
			}

			EditorGUILayout.EndHorizontal();
			GUILayout.Space(5);

			if (!string.IsNullOrEmpty(includePathToAdd) && includePaths.Add(includePathToAdd)) GUI.changed = true;
			if (!string.IsNullOrEmpty(includePathToRemove) && includePaths.Remove(includePathToRemove)) GUI.changed = true;
			if (!string.IsNullOrEmpty(excludePathToAdd) && excludePaths.Add(excludePathToAdd)) GUI.changed = true;
			if (!string.IsNullOrEmpty(excludePathToRemove) && excludePaths.Remove(excludePathToRemove)) GUI.changed = true;

			if (GUI.changed)
			{
				TextTemplateToolSettings.Current.includePaths = includePaths.ToArray();
				TextTemplateToolSettings.Current.excludePaths = excludePaths.ToArray();
				if (assemblyExcludeList != null) TextTemplateToolSettings.Current.excludeAssemblies = assemblyExcludeList.Select(n => n.FullName).ToArray();
				TextTemplateToolSettings.Current.Save();
			}

			// padding
			GUILayout.EndVertical();
			GUILayout.Space(20);
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
			GUILayout.EndVertical();

			//
		}

		private static IEnumerable<AssemblyName> GetAllAssemblies()
		{
			var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			if (loadedAssemblyCount == currentAssemblies.Length && assemblyIncludeList != null && assemblyExcludeList != null)
				return assemblyExcludeList.Concat(assemblyIncludeList);

			assemblyExcludeList = new HashSet<AssemblyName>(AssemblyNameOnlyComparer.Default);
			assemblyIncludeList = new HashSet<AssemblyName>(AssemblyNameOnlyComparer.Default);
			foreach (var assembly in currentAssemblies)
			{
				try
				{
					if (assembly.ReflectionOnly) continue;

					var assemblyLocation = assembly.Location;
					if (string.IsNullOrEmpty(assemblyLocation)) continue;
					if (!File.Exists(assemblyLocation)) continue;
				}
				catch
				{
					/* ignore */
				}

				assemblyIncludeList.Add(assembly.GetName());
			}

			loadedAssemblyCount = currentAssemblies.Length;

			foreach (var excludeAssemblyName in TextTemplateToolSettings.Current.GetExcludeAssemblyNames())
			{
				assemblyIncludeList.Remove(excludeAssemblyName);
				assemblyExcludeList.Add(excludeAssemblyName);
			}

			return assemblyExcludeList.Concat(assemblyIncludeList);
		}
		private static IEnumerable<string> GetIncludePaths()
		{
			if (includePaths != null) return includePaths;

			includePaths = new HashSet<string>(TextTemplateToolSettings.Current.includePaths);
			return includePaths;
		}
		private static IEnumerable<string> GetExcludePaths()
		{
			if (excludePaths != null) return excludePaths;

			excludePaths = new HashSet<string>(TextTemplateToolSettings.Current.excludePaths);
			return excludePaths;
		}
		private static bool NotRuntimeAssembly(AssemblyName assemblyName)
		{
			return !string.Equals(assemblyName.Name, "mscorlib", StringComparison.Ordinal) &&
				!string.Equals(assemblyName.Name, "System.Runtime", StringComparison.Ordinal);
		}
		private static async Task<string> RunToolAsync(string cmd)
		{
			await Task.Yield();

			var tempFiles = new TempFileCollection(Path.GetFullPath("Temp", PathUtils.ProjectPath));
			var outputName = tempFiles.AddExtension("out");
			var errorName = tempFiles.AddExtension("error");
			try
			{
				var exitCode = Executor.ExecWaitWithCapture(
					cmd,
					Environment.CurrentDirectory,
					tempFiles,
					ref outputName,
					ref errorName
				);

				if (exitCode != 0)
				{
					throw new InvalidOperationException(
						await File.ReadAllTextAsync(outputName) +
						await File.ReadAllTextAsync(errorName)
					);
				}

				return await File.ReadAllTextAsync(outputName);
			}
			finally
			{
				tempFiles.Delete();
			}
		}

		private static string LimitText(string message, int maxLength)
		{
			if (message.Length > maxLength) return message.Substring(0, maxLength);

			return message;
		}
	}
}
