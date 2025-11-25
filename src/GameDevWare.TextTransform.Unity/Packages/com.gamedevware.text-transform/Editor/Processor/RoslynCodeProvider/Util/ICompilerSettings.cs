// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace GameDevWare.TextTransform.Editor.Processor.RoslynCodeProvider.Util
{
	/// <summary>
	///     Provides settings for the C# and VB CodeProviders
	/// </summary>
	[Obsolete("ICompilerSettings is obsolete. Please update code to use IProviderOptions instead.", false)]
	public interface ICompilerSettings
	{
		/// <summary>
		///     The full path to csc.exe or vbc.exe
		/// </summary>
		string CompilerFullPath { get; }

		/// <summary>
		///     TTL in seconds
		/// </summary>
		int CompilerServerTimeToLive { get; }
	}
}
