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
using GameDevWare.TextTransform.Json;
using GameDevWare.TextTransform.Utils;
using UnityEngine;

namespace GameDevWare.TextTransform
{
	[Serializable]
	internal class Settings
	{
		public static readonly string SettingsPath;

		public static readonly Encoding DefaultEncoding = Encoding.UTF8;
		public static readonly Settings Current;

		private ReadOnlyCollection<AssemblyName> excludeAssemblyNames;
		private ReadOnlyCollection<string> includeFullPaths;
		private ReadOnlyCollection<string> excludeFullPaths;

		public string[] IncludePaths;
		public string[] ExcludePaths;
		public string[] ExcludeAssemblies;
		public bool Verbose;
		
		static Settings()
		{
			var basePath = Path.GetDirectoryName(typeof(Settings).Assembly.Location);
			if (basePath == null || typeof(Settings).Assembly.GetName().Name != "GameDevWare.TextTransform")
				basePath = Path.GetFullPath("Assets/Editor/GameDevWare.TextTransform");

			SettingsPath = Path.Combine(basePath, "GameDevWare.TextTransform.Settings.json");
			
			Current = Load();
		}

		private static Settings Load()
		{

			var settings = default(Settings);
			try { settings = JsonValue.Parse(File.ReadAllText(SettingsPath, DefaultEncoding)).As<Settings>(); }
			catch (Exception readError) { Debug.LogWarning("Failed to read settings for T4 Transform: " + readError.Message); }

			if (settings == null)
			{
				settings = new Settings
				{
					IncludePaths = new string[0],
					ExcludePaths = new string[0],
					ExcludeAssemblies = new string[] { typeof(int).Assembly.GetName().FullName },
					Verbose = false
				};

				try { File.WriteAllText(SettingsPath, JsonObject.From(settings).Stringify(), DefaultEncoding); }
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
				var content = JsonObject.From(this).Stringify();
				var currentContent = File.Exists(SettingsPath) ? File.ReadAllText(SettingsPath, DefaultEncoding) : null;
				if (string.Equals(content, currentContent, StringComparison.OrdinalIgnoreCase))
					return; // no changes

				File.WriteAllText(SettingsPath, content, DefaultEncoding);
			}
			catch (Exception e)
			{
				Debug.LogError(string.Format("Failed to save settings for Charon in file '{0}'.", SettingsPath));
				Debug.LogError(e);
			}
		}

		private void Validate()
		{
			this.IncludePaths = this.IncludePaths ?? new string[0];
			this.ExcludePaths = this.ExcludePaths ?? new string[0];
			this.ExcludeAssemblies = this.ExcludeAssemblies ?? new string[0];
			this.excludeAssemblyNames = null;
			this.includeFullPaths = null;
			this.excludeFullPaths = null;
		}

		public ReadOnlyCollection<AssemblyName> GetExcludeAssemblyNames()
		{
			if (this.excludeAssemblyNames != null)
			{
				return this.excludeAssemblyNames;
			}

			var assemblyNames = new List<AssemblyName>();
			foreach (var assemblyNameString in this.ExcludeAssemblies ?? Enumerable.Empty<string>())
			{
				try
				{
					assemblyNames.Add(new AssemblyName(assemblyNameString));
				}
				catch (Exception error)
				{
					if (this.Verbose)
					{
						Debug.LogWarning(string.Format("Failed to parse to string '{0}' as assembly name.", assemblyNameString));
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

			this.excludeFullPaths = new ReadOnlyCollection<string>(Array.ConvertAll(this.ExcludePaths, PathUtils.MakeProjectAbsolute));
			return this.excludeFullPaths;
		}
		public ReadOnlyCollection<string> GetIncludePaths()
		{
			if (this.includeFullPaths != null)
			{
				return this.includeFullPaths;
			}

			this.includeFullPaths = new ReadOnlyCollection<string>(Array.ConvertAll(this.IncludePaths, PathUtils.MakeProjectAbsolute));
			return this.includeFullPaths;
		}
		
		public override string ToString()
		{
			return string.Format("Include Paths: {0}, Exclude Paths: {1}, Exclude Assemblies: {2}, Verbose: {3}", string.Join(", ", this.IncludePaths), string.Join(", ", this.ExcludePaths), string.Join(", ", this.ExcludeAssemblies), this.Verbose);
		}

	}
}

