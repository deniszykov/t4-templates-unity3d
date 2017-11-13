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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Assets.Editor.GameDevWare.TextTransform.Processor;
using Assets.Editor.GameDevWare.TextTransform.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.GameDevWare.TextTransform
{
	public class UnityTemplateGenerator : TemplateGenerator
	{
		private static readonly Dictionary<string, Timer> TriggerDelays = new Dictionary<string, Timer>(StringComparer.Ordinal);
		private static readonly Queue<string> PendingTemplateGenerations = new Queue<string>();

		static UnityTemplateGenerator()
		{
			EditorApplication.update += () =>
			{
				if (PendingTemplateGenerations.Count <= 0) return;

				lock (PendingTemplateGenerations)
					if (PendingTemplateGenerations.Count > 0)
						RunForTemplate(PendingTemplateGenerations.Dequeue());
			};
		}

		public UnityTemplateGenerator()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.ReflectionOnly)
					continue;

				try
				{
					var assemblyLocation = assembly.Location;
					if (string.IsNullOrEmpty(assemblyLocation)) continue;

					this.Refs.Add(assemblyLocation);
				}
				catch { /* ignore */ }
			}

			this.ReferencePaths.Add(Path.GetDirectoryName(typeof(UnityTemplateGenerator).Assembly.Location));
			this.ReferencePaths.Add(Path.GetDirectoryName(typeof(UnityEngine.Debug).Assembly.Location));
			this.ReferencePaths.Add(Path.GetDirectoryName(typeof(UnityEditor.EditorApplication).Assembly.Location));
		}

		public static void RunForTemplateWithDelay(string templatePath, TimeSpan delay)
		{
			if (templatePath == null) throw new ArgumentNullException("templatePath");

			templatePath = FileUtils.MakeProjectRelative(templatePath);

			if (delay <= TimeSpan.Zero)
			{
				RunForTemplate(templatePath);
				return;
			}

			var timer = default(Timer);
			lock (TriggerDelays)
			{
				if (TriggerDelays.TryGetValue(templatePath, out timer) == false || timer == null)
				{
					TriggerDelays[templatePath] = timer = new Timer(path =>
					{
						lock (PendingTemplateGenerations)
							PendingTemplateGenerations.Enqueue((string)path);
					}, templatePath, Timeout.Infinite, Timeout.Infinite);
				}
			}

			timer.Change(delay, TimeSpan.FromTicks(-1));
		}
		public static bool RunForTemplate(string templatePath)
		{
			if (templatePath == null) throw new ArgumentNullException("templatePath");

			templatePath = FileUtils.MakeProjectRelative(templatePath);

			var settings = TemplateSettings.Load(templatePath);
			var generator = new UnityTemplateGenerator();
			var templateName = Path.GetFileNameWithoutExtension(templatePath);
			var templateDir = Path.GetDirectoryName(templatePath) ?? "Assets";
			var templateNamespace = string.Join(".", templateDir.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries));
			var generatorOutputDir = Path.Combine("Temp", "T4");
			var outputFile = Path.Combine(generatorOutputDir, Path.GetFileNameWithoutExtension(templatePath) + "_" + Guid.NewGuid().ToString().Replace("-", "") + ".tmp");
			var generatorOutputFile = Path.ChangeExtension(outputFile, ".gen.cs");

			var language = default(string);
			var references = default(string[]);

			if (Directory.Exists(generatorOutputDir) == false) Directory.CreateDirectory(generatorOutputDir);

			if (Menu.VerboseLogs)
				Debug.Log(string.Format("Pre-process T4 template '{0}'. Output directory: '{1}'.", templatePath, generatorOutputDir));
			if (generator.PreprocessTemplate(templatePath, templateName, templateNamespace, generatorOutputFile, Encoding.UTF8, out language, out references) == false)
			{
				Debug.LogWarning(string.Format("Failed to pre-process template '{0}'.", templatePath));
				foreach (var error in generator.Errors)
					Debug.LogWarning(error);
				return false;
			}
			if (Menu.VerboseLogs)
				Debug.Log(string.Format("Pre-process T4 template '{0}' is complete successfully. Language: '{1}', References: '{2}', Output file: '{3}'.", templatePath, language, string.Join(", ", references ?? new string[0]), generatorOutputFile));

			if (Menu.VerboseLogs)
				Debug.Log(string.Format("Process T4 template '{0}'. Output File: '{1}'.", templatePath, outputFile));
			if (generator.ProcessTemplate(templatePath, ref outputFile) == false)
			{
				Debug.LogWarning(string.Format("Failed to process template '{0}'.", templatePath));
				var warnText = new StringBuilder();
				foreach (CompilerError error in generator.Errors)
					warnText.AppendLine(error.ToString());
				if (warnText.Length > 0)
					Debug.LogWarning(warnText);
				return false;
			}
			if (Menu.VerboseLogs)
				Debug.Log(string.Format("Process T4 template '{0}' is complete successfully. Output file: '{1}'.", templatePath, outputFile));

			var sourceFile = default(string);
			switch ((TemplateSettings.OutputTypes)settings.OutputType)
			{
				case TemplateSettings.OutputTypes.Code:
					sourceFile = outputFile;
					break;
				case TemplateSettings.OutputTypes.CodeGenerator:
					sourceFile = generatorOutputFile;
					break;
				default:
					Debug.LogWarning("Invalid 'OutputType' is specified in template's settings.");
					return false;
			}
			var targetFile = settings.OutputPath;
			if (targetFile == null)
				targetFile = Path.GetFullPath(Path.ChangeExtension(templatePath, Path.GetExtension(sourceFile)));
			else
				targetFile = Path.GetFullPath(targetFile);

			if (File.Exists(targetFile) && FileUtils.ComputeMd5Hash(targetFile) == FileUtils.ComputeMd5Hash(sourceFile))
			{
				if (Menu.VerboseLogs)
					Debug.Log(string.Format("Generated file is same as existing at location '{0}'.", targetFile));
				return false;
			}

			var targetDir = Path.GetDirectoryName(targetFile);
			if (targetDir != null && Directory.Exists(targetDir) == false)
				Directory.CreateDirectory(targetDir);

			File.Copy(sourceFile, targetFile, overwrite: true);
			File.Delete(outputFile);
			File.Delete(generatorOutputFile);

			return true;
		}
	}
}
