// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using GameDevWare.TextTransform.Editor.Processor.RoslynCodeProvider.Util;

namespace GameDevWare.TextTransform.Editor.Processor.RoslynCodeProvider
{
	internal abstract class Compiler : ICodeCompiler
	{
		private const string CLR_PROFILING_SETTING = "COR_ENABLE_PROFILING";
		private const string DISABLE_PROFILING = "0";
		private readonly CodeDomProvider codeDomProvider;
		protected readonly IProviderOptions ProviderOptions;
		private string compilerFullPath;

		protected abstract string FileExtension { get; }

		protected virtual string CompilerName
		{
			get
			{
				if (null == this.compilerFullPath)
				{
					this.compilerFullPath = this.ProviderOptions.CompilerFullPath;

					// Try opening the file to make sure the compiler exist.  This will throw an exception
					// if it doesn't
					using (var str = File.OpenRead(this.compilerFullPath))
					{
					}
				}

				return this.compilerFullPath;
			}
		}

		protected abstract string FullPathsOption { get; }

		public Compiler(CodeDomProvider codeDomProvider, IProviderOptions providerOptions)
		{
			this.codeDomProvider = codeDomProvider;
			this.ProviderOptions = providerOptions;
		}

		protected abstract void ProcessCompilerOutputLine(CompilerResults results, string line);

		protected abstract string CmdArgsFromParameters(CompilerParameters options);

		protected virtual void FixUpCompilerParameters(CompilerParameters options)
		{
			this.FixTreatWarningsAsErrors(options);
		}

		private string GetCompilationArgumentString(CompilerParameters options)
		{
			this.FixUpCompilerParameters(options);

			return this.CmdArgsFromParameters(options);
		}

		// CodeDom sets TreatWarningAsErrors to true whenever warningLevel is non-zero.
		// However, TreatWarningAsErrors should be false by default.
		// And users should be able to set the value by set the value of option "WarnAsError".
		// ASP.Net does fix this option in a like named function, but only for old CodeDom providers (CSharp/VB).
		// The old ASP.Net fix was to set TreatWarningAsErrors to false anytime '/warnaserror' was
		// detected in the compiler command line options, thus allowing the user-specified
		// option to prevail. In these CodeDom providers though, users have control through
		// the 'WarnAsError' provider option as well as manual control over the command
		// line args. 'WarnAsError' will default to false but can be set by the user.
		// So just go with the 'WarnAsError' provider option here.
		private void FixTreatWarningsAsErrors(CompilerParameters parameters)
		{
			parameters.TreatWarningsAsErrors = this.ProviderOptions.WarnAsError;
		}

		private CompilerResults FromSourceBatch(CompilerParameters options, string[] sources)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (sources == null) throw new ArgumentNullException(nameof(sources));

			new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();

			var filenames = new string[sources.Length];
			CompilerResults results = null;

			// the extra try-catch is here to mitigate exception filter injection attacks.
			for (var i = 0; i < sources.Length; i++)
			{
				var name = options.TempFiles.AddExtension(i + this.FileExtension);
				var temp = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.Read);
				try
				{
					using (var sw = new StreamWriter(temp, Encoding.UTF8))
					{
						sw.Write(sources[i]);
						sw.Flush();
					}
				}
				finally
				{
					temp.Close();
				}

				filenames[i] = name;
			}

			results = this.FromFileBatch(options, filenames);

			return results;
		}

		private CompilerResults FromFileBatch(CompilerParameters options, string[] fileNames)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (fileNames == null) throw new ArgumentNullException(nameof(fileNames));

			new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();

			string outputFile = null;
			var retValue = 0;
			var results = new CompilerResults(options.TempFiles);
			var perm1 = new SecurityPermission(SecurityPermissionFlag.ControlEvidence);
			perm1.Assert();
			try
			{
#pragma warning disable 618
				results.Evidence = options.Evidence;
#pragma warning restore 618
			}
			finally
			{
				SecurityPermission.RevertAssert();
			}

			var createdEmptyAssembly = false;
			if (options.OutputAssembly == null || options.OutputAssembly.Length == 0)
			{
				var extension = options.GenerateExecutable ? "exe" : "dll";
				options.OutputAssembly = results.TempFiles.AddExtension(extension, !options.GenerateInMemory);

				// Create an empty assembly.  This is so that the file will have permissions that
				// we can later access with our current credential. If we don't do this, the compiler
				// could end up creating an assembly that we cannot open.
				new FileStream(options.OutputAssembly, FileMode.Create, FileAccess.ReadWrite).Close();
				createdEmptyAssembly = true;
			}

			var pdbname = "pdb";

			// Don't delete pdbs when debug=false but they have specified pdbonly.
			if (options.CompilerOptions != null &&
				-1 != CultureInfo.InvariantCulture.CompareInfo.IndexOf(options.CompilerOptions, "/debug:pdbonly", CompareOptions.IgnoreCase))
				results.TempFiles.AddExtension(pdbname, true);
			else
				results.TempFiles.AddExtension(pdbname);

			var args = this.GetCompilationArgumentString(options) + " " + JoinStringArray(fileNames, " ");

			// Use a response file if the compiler supports it
			var responseFileArgs = this.GetResponseFileCmdArgs(options, args);
			string trueArgs = null;
			if (responseFileArgs != null)
			{
				trueArgs = args;
				args = responseFileArgs;
			}

			// Appending TTL to the command line arguments.
			if (this.ProviderOptions.CompilerServerTimeToLive > 0)
				args = string.Format("/shared /keepalive:\"{0}\" {1}", this.ProviderOptions.CompilerServerTimeToLive, args);

			this.Compile(options,
				this.CompilerName,
				args,
				ref outputFile,
				ref retValue);

			results.NativeCompilerReturnValue = retValue;

			// only look for errors/warnings if the compile failed or the caller set the warning level
			if (retValue != 0 || options.WarningLevel > 0)
			{
				// The output of the compiler is in UTF8
				var lines = ReadAllLines(outputFile, Encoding.UTF8, FileShare.ReadWrite);
				var replacedArgs = false;
				foreach (var line in lines)
				{
					if (!replacedArgs && trueArgs != null && line.Contains(args))
					{
						replacedArgs = true;
						var outputLine = string.Format("{0}>{1} {2}",
							Environment.CurrentDirectory,
							this.CompilerName,
							trueArgs);
						results.Output.Add(outputLine);
					}
					else
						results.Output.Add(line);

					this.ProcessCompilerOutputLine(results, line);
				}

				// Delete the empty assembly if we created one
				if (retValue != 0 && createdEmptyAssembly) File.Delete(options.OutputAssembly);
			}

			if (retValue != 0 || results.Errors.HasErrors || !options.GenerateInMemory)
			{
				results.PathToAssembly = options.OutputAssembly;
				return results;
			}

			// Read assembly into memory:
			var assemblyBuff = File.ReadAllBytes(options.OutputAssembly);

			// Read symbol file into memory and ignore any errors that may be encountered:
			// (This functionality was added in NetFx 4.5, errors must be ignored to ensure compatibility)
			byte[] symbolsBuff = null;
			try
			{
				var symbFileName = options.TempFiles.BasePath + "." + pdbname;

				if (File.Exists(symbFileName)) symbolsBuff = File.ReadAllBytes(symbFileName);
			}
			catch
			{
				symbolsBuff = null;
			}

#pragma warning disable 618 // Load with evidence is obsolete - this warning is passed on via the options.Evidence property
			results.CompiledAssembly = Assembly.Load(assemblyBuff, symbolsBuff, options.Evidence);
#pragma warning restore 618

			return results;
		}

		private static void ReImpersonate(WindowsImpersonationContext impersonation)
		{
			impersonation.Undo();
		}

		private static string[] ReadAllLines(string file, Encoding encoding, FileShare share)
		{
			using (var stream = File.Open(file, FileMode.Open, FileAccess.Read, share))
			{
				string line;
				var lines = new List<string>();

				using (var sr = new StreamReader(stream, encoding))
				{
					while ((line = sr.ReadLine()) != null)
					{
						lines.Add(line);
					}
				}

				return lines.ToArray();
			}
		}

		private void Compile
		(
			CompilerParameters options,
			string compilerFullPath,
			string arguments,
			ref string outputFile,
			ref int nativeReturnValue)
		{
			string errorFile = null;
			var cmdLine = "\"" + compilerFullPath + "\" " + arguments;
			outputFile = options.TempFiles.AddExtension("out");

			var profilingSettingIsUpdated = false;
			string originalClrProfilingSetting = null;

			nativeReturnValue = Executor.ExecWaitWithCapture(
				cmdLine,
				Environment.CurrentDirectory,
				options.TempFiles,
				ref outputFile,
				ref errorFile);

			if (profilingSettingIsUpdated) Environment.SetEnvironmentVariable(CLR_PROFILING_SETTING, originalClrProfilingSetting, EnvironmentVariableTarget.Process);
		}

		private string GetResponseFileCmdArgs(CompilerParameters options, string cmdArgs)
		{
			var responseFileName = options.TempFiles.AddExtension("cmdline");
			var responseFileStream = new FileStream(responseFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
			try
			{
				using (var sw = new StreamWriter(responseFileStream, Encoding.UTF8))
				{
					sw.Write(cmdArgs);
					sw.Flush();
				}
			}
			finally
			{
				responseFileStream.Close();
			}

			// Always specify the /noconfig flag (outside of the response file)
			return "/noconfig " + this.FullPathsOption + "@\"" + responseFileName + "\"";
		}

		private static string JoinStringArray(string[] sa, string separator)
		{
			if (sa == null || sa.Length == 0) return string.Empty;

			if (sa.Length == 1) return "\"" + sa[0] + "\"";

			var sb = new StringBuilder();
			for (var i = 0; i < sa.Length - 1; i++)
			{
				sb.Append("\"");
				sb.Append(sa[i]);
				sb.Append("\"");
				sb.Append(separator);
			}

			sb.Append("\"");
			sb.Append(sa[sa.Length - 1]);
			sb.Append("\"");

			return sb.ToString();
		}

		public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (compilationUnit == null) throw new ArgumentNullException(nameof(compilationUnit));

			return this.CompileAssemblyFromDomBatch(options, new[] { compilationUnit });
		}

		public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (compilationUnits == null) throw new ArgumentNullException(nameof(compilationUnits));

			try
			{
				var sources = compilationUnits.Select(c =>
				{
					using (var writer = new StringWriter())
					{
						this.codeDomProvider.GenerateCodeFromCompileUnit(c, writer, new CodeGeneratorOptions());
						return writer.ToString();
					}
				});

				return this.FromSourceBatch(options, sources.ToArray());
			}
			finally
			{
				options.TempFiles.Delete();
			}
		}

		public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (fileName == null) throw new ArgumentNullException(nameof(fileName));

			return this.CompileAssemblyFromFileBatch(options, new[] { fileName });
		}

		public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (fileNames == null) throw new ArgumentNullException(nameof(fileNames));

			try
			{
				// Try opening the files to make sure they exists.  This will throw an exception
				// if it doesn't
				foreach (var fileName in fileNames)
				{
					using (var str = File.OpenRead(fileName))
					{
					}
				}

				return this.FromFileBatch(options, fileNames);
			}
			finally
			{
				options.TempFiles.Delete();
			}
		}

		public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (source == null) throw new ArgumentNullException(nameof(source));

			return this.CompileAssemblyFromSourceBatch(options, new[] { source });
		}

		public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (sources == null) throw new ArgumentNullException(nameof(sources));

			try
			{
				return this.FromSourceBatch(options, sources);
			}
			finally
			{
				options.TempFiles.Delete();
			}
		}
	}
}
