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
using System.Linq;
using System.Text;
using System.Threading;
using GameDevWare.TextTransform.Editor.Processor;
using GameDevWare.TextTransform.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.TextTransform.Editor
{
	/// <summary>
	///     T4 Template based generator. Use <see cref="UnityTemplateGenerator.RunForTemplate(string)" /> method to run
	///     transformation.
	/// </summary>
	public class UnityTemplateGenerator : TemplateGenerator
	{
		private static readonly Queue<string> PendingTemplateGenerations = new();
		private static readonly Dictionary<string, Timer> TriggerDelays = new(StringComparer.Ordinal);

		public string RoslynCompilerPath => TextTemplateToolSettings.Current.templateCompiler == TextTemplateToolSettings.TemplateCompiler.Roslyn &&
			File.Exists(TextTemplateToolSettings.Current.roslynCompilerPath ?? "nil") ? TextTemplateToolSettings.Current.roslynCompilerPath : null;

		static UnityTemplateGenerator()
		{
			EditorApplication.update += () =>
			{
				if (PendingTemplateGenerations.Count <= 0) return;

				lock (PendingTemplateGenerations)
				{
					if (PendingTemplateGenerations.Count > 0)
						RunForTemplate(PendingTemplateGenerations.Dequeue());
				}
			};
		}
		/// <summary>
		///     Create instance of <see cref="UnityTemplateGenerator" />.
		/// </summary>
		public UnityTemplateGenerator()
		{
			var excludedAssemblyNames = new HashSet<string>(TextTemplateToolSettings.Current.GetExcludeAssemblyNames().Select(n => n.Name), StringComparer.Ordinal);
			var excludedPaths = new HashSet<string>(TextTemplateToolSettings.Current.GetExcludePaths(), StringComparer.OrdinalIgnoreCase);

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.ReflectionOnly)
					continue;

				var assemblyName = assembly.GetName().Name;

				if (excludedAssemblyNames.Contains(assemblyName))
					continue; // excluded

				try
				{
					var assemblyLocation = PathUtils.Normalize(assembly.Location);
					if (string.IsNullOrEmpty(assemblyLocation)) continue;
					if (!File.Exists(assemblyLocation)) continue;
					if (excludedPaths.Contains(assemblyLocation)) continue;

					this.Refs.Add(assemblyLocation);
				}
				catch
				{
					/* ignore */
				}
			}

			foreach (var includePath in TextTemplateToolSettings.Current.GetIncludePaths())
			{
				if (File.Exists(includePath))
					this.Refs.Add(includePath);
				else if (Directory.Exists(includePath))
					this.ReferencePaths.Add(includePath);
				else if (TextTemplateToolSettings.Current.verbose)
					Debug.LogWarning($"Unable to locate assembly file or directory at '{includePath}' for inclusion in T4 template generation.");
			}

			this.ReferencePaths.Add(Path.GetDirectoryName(typeof(int).Assembly.Location));
			this.ReferencePaths.Add(Path.GetDirectoryName(typeof(UnityTemplateGenerator).Assembly.Location));
			this.ReferencePaths.Add(Path.GetDirectoryName(typeof(Debug).Assembly.Location));
			this.ReferencePaths.Add(Path.GetDirectoryName(typeof(EditorApplication).Assembly.Location));
		}

		/// <summary>
		///     Run T4 template transformation at <paramref name="templatePath" /> after <paramref name="delay" />.
		/// </summary>
		/// <param name="templatePath">Path to T4 template.</param>
		/// <param name="delay">Defer value.</param>
		public static void RunForTemplateWithDelay(string templatePath, TimeSpan delay)
		{
			if (templatePath == null) throw new ArgumentNullException(nameof(templatePath));

			templatePath = PathUtils.MakeProjectRelative(templatePath);

			if (delay <= TimeSpan.Zero)
			{
				RunForTemplate(templatePath);
				return;
			}

			var timer = default(Timer);
			lock (TriggerDelays)
			{
				if (!TriggerDelays.TryGetValue(templatePath, out timer) || timer == null)
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
		/// <summary>
		///     Run T4 template transformation with at <paramref name="templatePath" /> with default settings.
		/// </summary>
		/// <param name="templatePath">Path to T4 template.</param>
		/// <returns>Result of transformation.</returns>
		public static TransformationResult RunForTemplate(string templatePath)
		{
			return RunForTemplate(templatePath, default);
		}
		/// <summary>
		///     Run T4 template transformation with at <paramref name="templatePath" /> with additional settings.
		/// </summary>
		/// <param name="templatePath">Path to T4 template.</param>
		/// <param name="outputPath">
		///     Output path. It will override <see cref="TextTemplateImporter.outputPath" /> from
		///     <paramref name="importer" /> parameter.
		/// </param>
		/// <param name="importer">Settings override for this run.</param>
		/// <param name="parameters">
		///     Additional template parameters. They could be retrieved with following code.
		///     <code>this.Host.ResolveParameterValue("-", "-", "someKey");</code>.
		/// </param>
		/// <param name="assemblyReferences">
		///     Additional assemblies to load during transformation. Could be assembly name or full
		///     path to assembly.
		/// </param>
		/// <param name="assemblyReferencesLookupPaths">
		///     Additional assembly lookup paths. Used during referenced assemblies
		///     resolution.
		/// </param>
		/// <param name="includeLookupPaths">Additional lookup path for &lt;#=include#&gt; directives.</param>
		/// <returns>Result of transformation.</returns>
		public static TransformationResult RunForTemplate
		(
			string templatePath,
			string outputPath,
			Dictionary<string, string> parameters = null,
			List<string> assemblyReferences = null,
			List<string> assemblyReferencesLookupPaths = null,
			List<string> includeLookupPaths = null
		)
		{
			if (templatePath == null) throw new ArgumentNullException(nameof(templatePath));

			templatePath = PathUtils.MakeProjectRelative(templatePath);

			if (!TextTemplateImporter.TryLoad(templatePath, out var importer))
			{
				importer = new T4TemplateImporter();
				importer.Validate();
			}

			var generator = new UnityTemplateGenerator();
			var templateName = Path.GetFileNameWithoutExtension(templatePath);
			var templateDir = Path.GetDirectoryName(templatePath) ?? "Assets";
			var templateNamespace = string.Join(".", templateDir.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries));
			var generatorOutputDir = Path.Combine("Temp", "T4");
			var outputFile = Path.Combine(generatorOutputDir,
				Path.GetFileNameWithoutExtension(templatePath) + "_" + Guid.NewGuid().ToString().Replace("-", "") + ".cs");
			var generatorOutputFile = Path.ChangeExtension(outputFile, ".gen.cs");

			if (!Directory.Exists(generatorOutputDir)) Directory.CreateDirectory(generatorOutputDir);

			if (TextTemplateToolSettings.Current.verbose)
				Debug.Log($"Pre-process T4 template '{templatePath}'. Output directory: '{generatorOutputDir}'.");
			if (!generator.PreprocessTemplate(templatePath, templateName, templateNamespace, generatorOutputFile, Encoding.UTF8, out var language,
					out var references))
			{
				Debug.LogWarning($"Failed to pre-process template '{templatePath}'.");
				foreach (var error in generator.Errors)
				{
					var log = (Action<object>)Debug.LogError;
					log.BeginInvoke(error, null, null);
				}

				FocusConsoleWindow();
				return TransformationResult.TemplateCompilationError;
			}

			if (TextTemplateToolSettings.Current.verbose)
			{
				Debug.Log(string.Format(
					"Pre-process T4 template '{0}' is complete successfully. Language: '{1}', References: '{2}', Reference location paths: {3}, Include paths: {4}, Output file: '{5}'.",
					templatePath, language, string.Join(", ", references ?? new string[0]),
					string.Join(", ", generator.ReferencePaths.ToArray()), string.Join(", ", generator.IncludePaths.ToArray()), generatorOutputFile));
			}

			if (assemblyReferences != null) generator.Refs.AddRange(assemblyReferences);

			if (assemblyReferencesLookupPaths != null) generator.ReferencePaths.AddRange(assemblyReferencesLookupPaths);

			if (includeLookupPaths != null) generator.IncludePaths.AddRange(includeLookupPaths);

			if (parameters != null)
			{
				foreach (var kv in parameters)
				{
					generator.AddParameter("-", "-", kv.Key, kv.Value);
					generator.AddParameter(null, null, kv.Key, kv.Value);
				}
			}

			if (TextTemplateToolSettings.Current.verbose)
				Debug.Log($"Process T4 template '{templatePath}'. Output File: '{outputFile}'.");

			if (!generator.ProcessTemplate(templatePath, ref outputFile))
			{
				Debug.LogWarning($"Failed to process template '{templatePath}'.");
				foreach (CompilerError error in generator.Errors)
				{
					var log = (Action<object>)Debug.LogError;
					log.BeginInvoke(error, null, null);
				}

				FocusConsoleWindow();
				return TransformationResult.TemplateProcessingError;
			}

			if (TextTemplateToolSettings.Current.verbose)
				Debug.Log($"Process T4 template '{templatePath}' is complete successfully. Output file: '{outputFile}'.");

			var sourceFile = default(string);
			switch (importer.generationOutput)
			{
				case GenerationOutput.Text:
					sourceFile = outputFile;
					break;
				case GenerationOutput.TextGenerator:
					sourceFile = generatorOutputFile;
					break;
				default:
					Debug.LogWarning("Invalid 'OutputType' is specified in template's settings.");
					return TransformationResult.UnknownOutputType;
			}

			var targetFile = outputPath ?? importer.outputPath;
			if (string.IsNullOrEmpty(targetFile))
				targetFile = Path.GetFullPath(Path.ChangeExtension(templatePath, Path.GetExtension(sourceFile)), PathUtils.ProjectPath);
			else
				targetFile = Path.GetFullPath(targetFile, PathUtils.ProjectPath);

			if (File.Exists(targetFile) && PathUtils.ComputeMd5Hash(targetFile) == PathUtils.ComputeMd5Hash(sourceFile))
			{
				if (TextTemplateToolSettings.Current.verbose)
					Debug.Log($"Generated file is same as existing at location '{targetFile}'.");
				return TransformationResult.NoChanges;
			}

			var targetDir = Path.GetDirectoryName(targetFile);
			if (targetDir != null && !Directory.Exists(targetDir))
				Directory.CreateDirectory(targetDir);

			File.Copy(sourceFile, targetFile, true);
			File.Delete(outputFile);
			File.Delete(generatorOutputFile);

			return TransformationResult.Success;
		}

		private static void FocusConsoleWindow()
		{
			var consoleWindowType = typeof(SceneView).Assembly.GetType("UnityEditor.ConsoleWindow", false);
			if (consoleWindowType == null)
				return;

			var consoleWindow = EditorWindow.GetWindow(consoleWindowType);
			consoleWindow.Focus();
		}
	}
}
