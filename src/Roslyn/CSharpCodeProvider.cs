// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.ComponentModel;

namespace Microsoft.CodeDom.Providers.DotNetCompilerPlatform
{
	/// <summary>
	/// Provides access to instances of the .NET Compiler Platform C# code generator and code compiler.
	/// </summary>
	[DesignerCategory("code")]
	public sealed class CSharpCodeProvider : Microsoft.CSharp.CSharpCodeProvider
	{
		private IProviderOptions _providerOptions;

		/// <summary>
		/// Default Constructor
		/// </summary>
		public CSharpCodeProvider()
			: this((IProviderOptions)null)
		{
		}

		/// <summary>
		/// Creates an instance using the given ICompilerSettings
		/// </summary>
		/// <param name="compilerSettings"></param>
		[Obsolete("ICompilerSettings is obsolete. Please update code to use IProviderOptions instead.", false)]
		public CSharpCodeProvider(ICompilerSettings compilerSettings = null)
		{
			_providerOptions = compilerSettings == null ? CompilationUtil.CSC2 : new ProviderOptions(compilerSettings);
		}

		/// <summary>
		/// Creates an instance using the given IProviderOptions
		/// </summary>
		/// <param name="providerOptions"></param>
		public CSharpCodeProvider(IProviderOptions providerOptions = null)
		{
			_providerOptions = providerOptions == null ? CompilationUtil.CSC2 : providerOptions;
		}

		/// <summary>
		/// Gets an instance of the .NET Compiler Platform C# code compiler.
		/// </summary>
		/// <returns>An instance of the .NET Compiler Platform C# code compiler</returns>
		[Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class.")]
		public override ICodeCompiler CreateCompiler()
		{
			return new CSharpCompiler(this, _providerOptions);
		}
	}
}
