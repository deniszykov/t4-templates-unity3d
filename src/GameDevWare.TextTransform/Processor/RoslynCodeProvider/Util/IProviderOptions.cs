// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform
{

#pragma warning disable CS0618

	/// <summary>
	/// Provides settings for the C# and VB CodeProviders
	/// </summary>
	public interface IProviderOptions : ICompilerSettings
	{

		// These come from ICompilerSettings.
		///// <summary>
		///// The full path to csc.exe or vbc.exe
		///// </summary>
		//string CompilerFullPath { get; }

		///// <summary>
		///// TTL in seconds
		///// </summary>
		//int CompilerServerTimeToLive { get; }

		/// <summary>
		/// A string representing the in-box .Net Framework compiler version to be used.
		/// Not applicable to this Roslyn-based package which contains it's own compiler.
		/// </summary>
		string CompilerVersion { get; }

		/// <summary>
		/// Returns true if the codedom provider has warnAsError set to true
		/// </summary>
		bool WarnAsError { get; }

		/// <summary>
		/// Returns true if the codedom provider is requesting to use similar default
		/// compiler options as ASP.Net does with in-box .Net Framework compilers.
		/// These options are programatically enforced on top of parameters passed
		/// in to the codedom provider.
		/// </summary>
		bool UseAspNetSettings { get; }

		/// <summary>
		/// Returns the entire set of options - known or not - as configured in &lt;providerOptions&gt;
		/// </summary>
		IDictionary<string, string> AllOptions { get; }
	}
#pragma warning restore CS0618

}
