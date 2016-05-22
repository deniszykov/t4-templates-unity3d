/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "T4 Transform" Unity Asset - https://www.assetstore.unity3d.com/#!/content/63294
	
	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND 
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE 
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY, 
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE 
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.
	
	This source code is distributed via Unity Asset Store, 
	to use it in your project you should accept Terms of Service and EULA 
	https://unity3d.com/ru/legal/as_terms
*/
#if !(UNITY_5 || UNITY_4 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5)

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Editor.GameDevWare.TextTranform.Processor;

// ReSharper disable UnusedMember.Local
// ReSharper disable once CheckNamespace
namespace GameDevWare.TextTranform
{
	class Program
	{
		public static void Main(string[] args)
		{
			var result = 0;
			try
			{
				result = CommandLine.Run<Program>(CommandLine.Arguments, "Help");

				if (args.Any(a => a == "--pause"))
				{
					Console.WriteLine();
					Console.WriteLine("Press any key to continue...");
					Console.ReadKey();
				}

			}
			catch (MissingMethodException)
			{
				Console.WriteLine(CommandLine.Describe<Program>());
				result = 1;
			}

			if (result != 0)
				Environment.Exit(result);
		}

		[Description("Transform specified T4 template.")]
		private static int Transform
		(
			[Description("Path to T4 template.")]
			string templatePath,
			[Description("Path where generated content will be saved.")]
			string outputPath = null,
			[Description("List of referenced assemblies used in template. Full path to assemblies should be specified.")]
			string[] references = null,
			[Description("List of namespaces used in template.")]
			string[] namespaces = null,
			[Description("List of included sub-templates used in template.")]
			string[] includes = null,
			[Description("List of locations to search for referenced assemblies.")]
			string[] referencePaths = null,
			[Description("Flag indicating that generator based on template will be created instead of content.")]
			bool createGenerator = false,
			[Description("Flag indicating that verbose logs is required.")]
			bool verbose = false
		)
		{
			var generator = new TemplateGenerator();
			foreach (var rf in references ?? new string[0])
				generator.Refs.Add(rf);
			foreach (var ns in namespaces ?? new string[0])
				generator.Imports.Add(ns);
			foreach (var inc in includes ?? new string[0])
				generator.IncludePaths.Add(inc);
			foreach (var rfp in referencePaths ?? new string[0])
				generator.ReferencePaths.Add(rfp);

			var templateName = Path.GetFileNameWithoutExtension(templatePath);
			var templateDir = Path.GetDirectoryName(templatePath) ?? Environment.CurrentDirectory;
			var templateNamespace = string.Join(".", templateDir.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries));
			var generatorOutputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			var contentOutputFile = Path.Combine(generatorOutputDir, Path.GetFileNameWithoutExtension(templatePath) + "_" + Guid.NewGuid().ToString().Replace("-", "") + ".tmp");
			var generatorOutputFile = Path.ChangeExtension(contentOutputFile, ".gen.cs");

			var language = default(string);
			if (Directory.Exists(generatorOutputDir) == false) Directory.CreateDirectory(generatorOutputDir);

			if (verbose)
				Console.WriteLine("Pre-process T4 template '{0}'. Output directory: '{1}'.", templatePath, generatorOutputDir);
			if (generator.PreprocessTemplate(templatePath, templateName, templateNamespace, generatorOutputFile, Encoding.UTF8, out language, out references) == false)
			{
				Console.Error.WriteLine("Failed to pre-process template '{0}'.", templatePath);
				foreach (var error in generator.Errors)
					Console.Error.WriteLine(error);
				return 1;
			}
			if (verbose)
				Console.WriteLine("Pre-process T4 template '{0}' is complete successfully. Language: '{1}', References: '{2}', Output file: '{3}'.", templatePath, language, string.Join(", ", references ?? new string[0]), generatorOutputFile);

			if (verbose)
				Console.WriteLine("Process T4 template '{0}'. Output File: '{1}'.", templatePath, contentOutputFile);
			if (generator.ProcessTemplate(templatePath, ref contentOutputFile) == false)
			{
				Console.Error.WriteLine("Failed to process template '{0}'.", templatePath);
				foreach (var error in generator.Errors)
					Console.Error.WriteLine(error);
				return 2;
			}
			if (verbose)
				Console.WriteLine("Process T4 template '{0}' is complete successfully. Output file: '{1}'.", templatePath, contentOutputFile);

			var sourceFile = createGenerator ? generatorOutputFile : contentOutputFile;
			var targetFile = outputPath;
			targetFile = Path.GetFullPath(targetFile ?? Path.ChangeExtension(templatePath, Path.GetExtension(sourceFile)));

			var targetDir = Path.GetDirectoryName(targetFile);
			if (targetDir != null && Directory.Exists(targetDir) == false)
				Directory.CreateDirectory(targetDir);

			if (verbose)
				Console.WriteLine("Copying file '{0}' to it's new location '{1}'.", sourceFile, targetFile);

			File.Copy(sourceFile, targetFile, overwrite: true);
			
			Directory.Delete(generatorOutputDir, recursive: true);

			return 0;
		}

		[Description("Print version.")]
		private static int Version()
		{
			Console.WriteLine(typeof(Program).Assembly.GetName().Version);
			return 0;
		}

		[Description("Print help.")]
		private static int Help(string action = null)
		{
			return CommandLine.Describe<Program>(action);
		}
	}
}
#endif
