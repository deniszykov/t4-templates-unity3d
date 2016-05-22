﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Assets.Editor.GameDevWare.TextTranform.Processor;
using Assets.Editor.GameDevWare.TextTranform.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.GameDevWare.TextTranform
{
	public class UnityTemplateGenerator : TemplateGenerator
	{
		private static readonly Dictionary<string, Timer> TriggerDelays = new Dictionary<string, Timer>(StringComparer.Ordinal);
		private static readonly Queue<string> PendingTemplateGenerations = new Queue<string>();

		public UnityTemplateGenerator()
		{
			this.Refs.Add(typeof(UnityEngine.Debug).Assembly.Location);
			this.Refs.Add(typeof(UnityEditor.EditorApplication).Assembly.Location);

			var currentAssemblyLocation = Path.GetDirectoryName(typeof(UnityTemplateGenerator).Assembly.Location);
			this.ReferencePaths.Add(currentAssemblyLocation);

			EditorApplication.update += () =>
			{
				if (PendingTemplateGenerations.Count <= 0) return;

				lock (PendingTemplateGenerations)
					if (PendingTemplateGenerations.Count > 0)
						RunForTemplate(PendingTemplateGenerations.Dequeue());
			};
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
		public static void RunForTemplate(string templatePath)
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
				return;
			}
			if (Menu.VerboseLogs)
				Debug.Log(string.Format("Pre-process T4 template '{0}' is complete successfully. Language: '{1}', References: '{2}', Output file: '{3}'.", templatePath, language, string.Join(", ", references ?? new string[0]), generatorOutputFile));

			if (Menu.VerboseLogs)
				Debug.Log(string.Format("Process T4 template '{0}'. Output File: '{1}'.", templatePath, outputFile));
			if (generator.ProcessTemplate(templatePath, ref outputFile) == false)
			{
				Debug.LogWarning(string.Format("Failed to process template '{0}'.", templatePath));
				foreach (var error in generator.Errors)
					Debug.LogWarning(error);
				return;
			}
			if (Menu.VerboseLogs)
				Debug.Log(string.Format("Process T4 template '{0}' is complete successfully. Output file: '{1}'.", templatePath, outputFile));

			var sourceFile = default(string);
			switch ((TemplateSettings.OutputTypes)settings.OutputType)
			{
				case TemplateSettings.OutputTypes.Content:
					sourceFile = outputFile;
					break;
				case TemplateSettings.OutputTypes.Generator:
					sourceFile = generatorOutputFile;
					break;
				default:
					Debug.LogWarning("Invalid 'OutputType' is specified in template's settings.");
					return;
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
				return;
			}

			var targetDir = Path.GetDirectoryName(targetFile);
			if (targetDir != null && Directory.Exists(targetDir) == false)
				Directory.CreateDirectory(targetDir);

			File.Copy(sourceFile, targetFile, overwrite: true);
			File.Delete(outputFile);
			File.Delete(generatorOutputFile);
		}
	}
}
