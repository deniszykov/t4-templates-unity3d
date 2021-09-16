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
			try
			{
				var editorDirectory = Path.GetDirectoryName(EditorApplication.applicationPath);
				var roslynCompilerDirectory = Path.Combine(editorDirectory, @"Data\Tools\Roslyn");
				if (File.Exists(Path.Combine(roslynCompilerDirectory, "csc.exe")))
				{
					RoslynCompilerLocation = roslynCompilerDirectory;
				}
			}
			catch
			{
				/* ignore */
			}
		}
	}
}
