// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace GameDevWare.TextTransform.Editor.Processor.RoslynCodeProvider.Util
{
	internal static class CompilationUtil
	{
		private const int DefaultCompilerServerTTL = 10; // 10 seconds
		private const int DefaultCompilerServerTTLInDevEnvironment = 60 * 15; // 15 minutes

		public static IProviderOptions CSC2 { get; }

		internal static bool IsDebuggerAttached => Debugger.IsAttached;

		static CompilationUtil()
		{
			CSC2 = GetProviderOptionsFor(".cs");

			if (IsDebuggerAttached) Environment.SetEnvironmentVariable("IN_DEBUG_MODE", "1", EnvironmentVariableTarget.Process);
		}

		public static IProviderOptions GetProviderOptionsFor(string fileExt)
		{
			//
			// AllOptions
			//
			var options = GetProviderOptionsCollection(fileExt);

			//
			// CompilerFullPath
			//
			var compilerFullPath = string.Empty;
			if (string.IsNullOrEmpty(compilerFullPath))
				compilerFullPath = Environment.GetEnvironmentVariable("ROSLYN_COMPILER_LOCATION");
			if (string.IsNullOrEmpty(compilerFullPath))
				options.TryGetValue("CompilerLocation", out compilerFullPath);
			if (string.IsNullOrEmpty(compilerFullPath))
				compilerFullPath = CompilerFullPath(@"bin\roslyn");

			if (fileExt.Equals(".cs", StringComparison.InvariantCultureIgnoreCase)) compilerFullPath = Path.Combine(compilerFullPath, "csc.exe");

			//
			// CompilerServerTimeToLive - default 10 seconds in production, 15 minutes in dev environment.
			//
			int ttl;
			var ttlstr = Environment.GetEnvironmentVariable("VBCSCOMPILER_TTL");
			if (string.IsNullOrEmpty(ttlstr))
				options.TryGetValue("CompilerServerTTL", out ttlstr);
			if (!int.TryParse(ttlstr, out ttl))
			{
				ttl = DefaultCompilerServerTTL;

				if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEV_ENVIRONMENT")) ||
					!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IN_DEBUG_MODE")) ||
					IsDebuggerAttached)
					ttl = DefaultCompilerServerTTLInDevEnvironment;
			}

			//
			// CompilerVersion - if this is null, we don't care.
			//
			string compilerVersion;
			options.TryGetValue("CompilerVersion", out compilerVersion); // Failure to parse sets to null

			//
			// WarnAsError - default false.
			//
			var warnAsError = false;
			if (options.TryGetValue("WarnAsError", out var sWAE)) bool.TryParse(sWAE, out warnAsError); // Failure to parse sets to 'false'

			//
			// UseAspNetSettings - default true. This was meant to be an ASP.Net support package first and foremost.
			//
			var useAspNetSettings = true;
			if (options.TryGetValue("UseAspNetSettings", out var sUANS))
			{
				// Failure to parse sets to 'false', but we want to keep the default 'true'.
				if (!bool.TryParse(sUANS, out useAspNetSettings))
					useAspNetSettings = true;
			}

			var providerOptions = new ProviderOptions {
				CompilerFullPath = compilerFullPath,
				CompilerServerTimeToLive = ttl,
				CompilerVersion = compilerVersion,
				WarnAsError = warnAsError,
				UseAspNetSettings = useAspNetSettings,
				AllOptions = options
			};

			return providerOptions;
		}

		internal static IDictionary<string, string> GetProviderOptionsCollection(string fileExt)
		{
			var opts = new Dictionary<string, string>();

			if (!CodeDomProvider.IsDefinedExtension(fileExt))
				return new Dictionary<string, string>(opts);

			var ci = CodeDomProvider.GetCompilerInfo(CodeDomProvider.GetLanguageFromExtension(fileExt));

			if (ci == null)
				return new Dictionary<string, string>(opts);

			// There is a fun little comment about this property in the framework code about making it
			// public after 3.5. Guess that didn't happen. Oh well. :)
			var pi = ci.GetType().GetProperty("ProviderOptions",
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
			if (pi == null)
				return new Dictionary<string, string>(opts);

			return new Dictionary<string, string>((IDictionary<string, string>)pi.GetValue(ci, null));
		}

		internal static void PrependCompilerOption(CompilerParameters compilParams, string compilerOptions)
		{
			if (compilParams.CompilerOptions == null)
				compilParams.CompilerOptions = compilerOptions;
			else
				compilParams.CompilerOptions = compilerOptions + " " + compilParams.CompilerOptions;
		}

		internal static string CompilerFullPath(string relativePath)
		{
			var compilerFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
			return compilerFullPath;
		}
	}
}
