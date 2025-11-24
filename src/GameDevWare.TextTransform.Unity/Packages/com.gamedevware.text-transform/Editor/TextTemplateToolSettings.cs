/*
	Copyright (c) 2017 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GameDevWare.TextTransform.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameDevWare.TextTransform.Editor
{
	[Serializable]
	internal class TextTemplateToolSettings
	{
		public enum TemplateCompiler
		{
			[Description("Build-in (C# 7.0)")]
			BuildIn,
			[Description("Roslyn (latest C#)")]
			Roslyn,
			[Description("dotnet tool t4 (latest C#)"), Obsolete("Not Implemented")]
			DotnetT4
		}

		public static readonly string SettingsPath;

		public static readonly Encoding DefaultEncoding = Encoding.UTF8;
		public static readonly TextTemplateToolSettings Current;

		private ReadOnlyCollection<AssemblyName> excludeAssemblyNames;
		private ReadOnlyCollection<string> includeFullPaths;
		private ReadOnlyCollection<string> excludeFullPaths;

		[FormerlySerializedAs("IncludePaths")]
		public string[] includePaths;
		[FormerlySerializedAs("ExcludePaths")]
		public string[] excludePaths;
		[FormerlySerializedAs("ExcludeAssemblies")]
		public string[] excludeAssemblies;
		[FormerlySerializedAs("Verbose")]
		public bool verbose;
		public string roslynCompilerPath;
		public string dotnetToolPath;
		public TemplateCompiler templateCompiler;

		static TextTemplateToolSettings()
		{
			var basePath = Path.GetDirectoryName(typeof(TextTemplateToolSettings).Assembly.Location);
			if (basePath == null || typeof(TextTemplateToolSettings).Assembly.GetName().Name != "GameDevWare.TextTransform")
				basePath = Path.GetFullPath("Assets/Editor/GameDevWare.TextTransform", PathUtils.ProjectPath);

			SettingsPath = Path.Combine(basePath, "GameDevWare.TextTransform.Settings.json");

			Current = Load();
		}

		private static TextTemplateToolSettings Load()
		{
			var settings = default(TextTemplateToolSettings);
			try
			{
				if (File.Exists(SettingsPath))
				{
					var json = File.ReadAllText(SettingsPath, DefaultEncoding);
					settings = JsonUtility.FromJson<TextTemplateToolSettings>(json);
				}
			}
			catch (Exception readError)
			{
				Debug.LogWarning("Failed to load settings for T4 Transform: " + readError.Message + ". A new file will be created with standard settings.");
			}

			if (settings == null)
			{
				settings = new TextTemplateToolSettings
				{
					includePaths = new string[0],
					excludePaths = new string[0],
					excludeAssemblies = new string[] { typeof(int).Assembly.GetName().FullName },
					verbose = false
				};

				try
				{
					var json = JsonUtility.ToJson(settings, prettyPrint: true);
					var directory = Path.GetDirectoryName(SettingsPath);
					if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
					{
						Directory.CreateDirectory(directory);
					}
					File.WriteAllText(SettingsPath, json, DefaultEncoding);
				}
				catch { /* ignore */ }
			}
			settings.Validate();

			return settings;
		}

		internal void Save()
		{
			this.Validate();

			try
			{
				var json = JsonUtility.ToJson(this, prettyPrint: true);
				var directory = Path.GetDirectoryName(SettingsPath);
				if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				if (File.Exists(SettingsPath) && File.ReadAllText(SettingsPath, DefaultEncoding) == json)
				{
					return; // not changed
				}

				File.WriteAllText(SettingsPath, json, DefaultEncoding);
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to save settings for T4 Transform in file '{SettingsPath}' due error.");
				Debug.LogError(e);
			}
		}

		public static string GetRoslynLocation()
		{
			var editorDirectory = Path.GetDirectoryName(EditorApplication.applicationPath) ?? EditorApplication.applicationPath;
			var compilerFullPath = Path.Combine(editorDirectory, @"Data\Tools\Roslyn\csc.exe");
			if (File.Exists(compilerFullPath))
			{
				return compilerFullPath;
			}
			else
			{
				return null;
			}
		}

		private void Validate()
		{
			this.includePaths = this.includePaths ?? new string[0];
			this.excludePaths = this.excludePaths ?? new string[0];
			this.excludeAssemblies = this.excludeAssemblies ?? new string[0];
			this.excludeAssemblyNames = null;
			this.includeFullPaths = null;
			this.excludeFullPaths = null;
			this.dotnetToolPath ??= "dotnet";
			this.roslynCompilerPath ??= GetRoslynLocation();
		}

		public ReadOnlyCollection<AssemblyName> GetExcludeAssemblyNames()
		{
			if (this.excludeAssemblyNames != null)
			{
				return this.excludeAssemblyNames;
			}

			var assemblyNames = new List<AssemblyName>();
			foreach (var assemblyNameString in this.excludeAssemblies ?? Enumerable.Empty<string>())
			{
				try
				{
					assemblyNames.Add(new AssemblyName(assemblyNameString));
				}
				catch (Exception error)
				{
					if (this.verbose)
					{
						Debug.LogWarning($"Failed to parse to string '{assemblyNameString}' as assembly name.");
						Debug.LogWarning(error);
					}
				}
			}
			return this.excludeAssemblyNames = assemblyNames.AsReadOnly();
		}
		public ReadOnlyCollection<string> GetExcludePaths()
		{
			if (this.excludeFullPaths != null)
			{
				return this.excludeFullPaths;
			}

			this.excludeFullPaths = new ReadOnlyCollection<string>(Array.ConvertAll(this.excludePaths, PathUtils.MakeProjectAbsolute));
			return this.excludeFullPaths;
		}
		public ReadOnlyCollection<string> GetIncludePaths()
		{
			if (this.includeFullPaths != null)
			{
				return this.includeFullPaths;
			}

			this.includeFullPaths = new ReadOnlyCollection<string>(Array.ConvertAll(this.includePaths, PathUtils.MakeProjectAbsolute));
			return this.includeFullPaths;
		}

		public override string ToString()
		{
			return
				$"Include Paths: {string.Join(", ", this.includePaths)}, Exclude Paths: {string.Join(", ", this.excludePaths)}, Exclude Assemblies: {string.Join(", ", this.excludeAssemblies)}, Verbose: {this.verbose}";
		}

	}
}

