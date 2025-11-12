// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform
{

	/// <summary>
	/// Provides settings for the C# and VB CodeProviders
	/// </summary>
	[Obsolete("ICompilerSettings is obsolete. Please update code to use IProviderOptions instead.", false)]
	public interface ICompilerSettings
	{

		/// <summary>
		/// The full path to csc.exe or vbc.exe
		/// </summary>
		string CompilerFullPath { get; }

		/// <summary>
		/// TTL in seconds
		/// </summary>
		int CompilerServerTimeToLive { get; }
	}
}
