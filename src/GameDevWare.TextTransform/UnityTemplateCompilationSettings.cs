using System;
using System.IO;
using UnityEditor;

namespace GameDevWare.TextTransform
{
	public static class UnityTemplateCompilationSettings
	{
		public static string RoslynCompilerLocation;

		static UnityTemplateCompilationSettings()
		{
			var roslynCompilerDirectory = string.Empty;
			try
			{
				var editorDirectory = Path.GetDirectoryName(EditorApplication.applicationPath);
				roslynCompilerDirectory = Path.Combine(editorDirectory, @"Data\Tools\Roslyn");
				if (File.Exists(Path.Combine(roslynCompilerDirectory, "csc.exe")))
				{
					RoslynCompilerLocation = roslynCompilerDirectory;
				}
			}
			catch
			{
				if (Settings.Current.Verbose)
				{
					UnityEngine.Debug.LogWarning($"Failed to locate Roslyn compiler (csc.exe) at expected location '{roslynCompilerDirectory}'.");
				}
			}
		}
	}
}
