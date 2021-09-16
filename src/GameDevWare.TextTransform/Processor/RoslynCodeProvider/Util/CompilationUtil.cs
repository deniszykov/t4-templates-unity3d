// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform
{
	internal static class CompilationUtil
	{
		private const int DefaultCompilerServerTTL = 10; // 10 seconds
		private const int DefaultCompilerServerTTLInDevEnvironment = 60 * 15; // 15 minutes

		static CompilationUtil()
		{
			CSC2 = GetProviderOptionsFor(".cs");

			if (IsDebuggerAttached)
			{
				Environment.SetEnvironmentVariable("IN_DEBUG_MODE", "1", EnvironmentVariableTarget.Process);
			}
		}

		public static IProviderOptions CSC2 { get; }

		public static IProviderOptions GetProviderOptionsFor(string fileExt)
		{
			//
			// AllOptions
			//
			IDictionary<string, string> options = GetProviderOptionsCollection(fileExt);

			//
			// CompilerFullPath
			//
			string compilerFullPath =
#if TOOL
				default(string);
#else
				GameDevWare.TextTransform.UnityTemplateCompilationSettings.RoslynCompilerLocation;
#endif
			if (String.IsNullOrEmpty(compilerFullPath))
				compilerFullPath = Environment.GetEnvironmentVariable("ROSLYN_COMPILER_LOCATION");
			if (String.IsNullOrEmpty(compilerFullPath))
				options.TryGetValue("CompilerLocation", out compilerFullPath);
			if (String.IsNullOrEmpty(compilerFullPath))
				compilerFullPath = CompilerFullPath(@"bin\roslyn");

			if (fileExt.Equals(".cs", StringComparison.InvariantCultureIgnoreCase))
				compilerFullPath = Path.Combine(compilerFullPath, "csc.exe");
			else if (fileExt.Equals(".vb", StringComparison.InvariantCultureIgnoreCase))
				compilerFullPath = Path.Combine(compilerFullPath, "vbc.exe");


			//
			// CompilerServerTimeToLive - default 10 seconds in production, 15 minutes in dev environment.
			//
			int ttl;
			string ttlstr = Environment.GetEnvironmentVariable("VBCSCOMPILER_TTL");
			if (String.IsNullOrEmpty(ttlstr))
				options.TryGetValue("CompilerServerTTL", out ttlstr);
			if (!Int32.TryParse(ttlstr, out ttl))
			{
				ttl = DefaultCompilerServerTTL;

				if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEV_ENVIRONMENT")) ||
					!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("IN_DEBUG_MODE")) ||
					CompilationUtil.IsDebuggerAttached)
				{
					ttl = DefaultCompilerServerTTLInDevEnvironment;
				}
			}

			//
			// CompilerVersion - if this is null, we don't care.
			//
			string compilerVersion;
			options.TryGetValue("CompilerVersion", out compilerVersion);    // Failure to parse sets to null

			//
			// WarnAsError - default false.
			//
			bool warnAsError = false;
			if (options.TryGetValue("WarnAsError", out string sWAE))
			{
				Boolean.TryParse(sWAE, out warnAsError); // Failure to parse sets to 'false'
			}

			//
			// UseAspNetSettings - default true. This was meant to be an ASP.Net support package first and foremost.
			//
			bool useAspNetSettings = true;
			if (options.TryGetValue("UseAspNetSettings", out string sUANS))
			{
				// Failure to parse sets to 'false', but we want to keep the default 'true'.
				if (!Boolean.TryParse(sUANS, out useAspNetSettings))
					useAspNetSettings = true;
			}

			ProviderOptions providerOptions = new ProviderOptions()
			{
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
			Dictionary<string, string> opts = new Dictionary<string, string>();

			if (!CodeDomProvider.IsDefinedExtension(fileExt))
				return new Dictionary<string, string>(opts);

			CompilerInfo ci = CodeDomProvider.GetCompilerInfo(CodeDomProvider.GetLanguageFromExtension(fileExt));

			if (ci == null)
				return new Dictionary<string, string>(opts);

			// There is a fun little comment about this property in the framework code about making it
			// public after 3.5. Guess that didn't happen. Oh well. :)
			PropertyInfo pi = ci.GetType().GetProperty("ProviderOptions",
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
			if (pi == null)
				return new Dictionary<string, string>(opts);

			return new Dictionary<string, string>((IDictionary<string, string>)pi.GetValue(ci, null));
		}

		internal static void PrependCompilerOption(CompilerParameters compilParams, string compilerOptions)
		{
			if (compilParams.CompilerOptions == null)
			{
				compilParams.CompilerOptions = compilerOptions;
			}
			else
			{
				compilParams.CompilerOptions = compilerOptions + " " + compilParams.CompilerOptions;
			}
		}

		internal static string CompilerFullPath(string relativePath)
		{
			string compilerFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
			return compilerFullPath;
		}

		internal static bool IsDebuggerAttached
		{
			get
			{
				return Debugger.IsAttached;
			}
		}
	}
}
