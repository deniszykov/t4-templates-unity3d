// 
// RequiresProvidesDirectiveProcessor.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace GameDevWare.TextTransform.Editor.Processor
{
	public abstract class RequiresProvidesDirectiveProcessor : DirectiveProcessor
	{
		private readonly StringBuilder codeBuffer = new();
		private readonly StringBuilder postInitBuffer = new();
		private readonly StringBuilder preInitBuffer = new();
		private bool isInProcessingRun;
		private CodeDomProvider languageProvider;
		protected abstract string FriendlyName { get; }

		protected ITextTemplatingEngineHost Host { get; private set; }

		public override void Initialize(ITextTemplatingEngineHost host)
		{
			base.Initialize(host);
			this.Host = host;
		}

		protected abstract void InitializeProvidesDictionary(string directiveName, IDictionary<string, string> providesDictionary);
		protected abstract void InitializeRequiresDictionary(string directiveName, IDictionary<string, string> requiresDictionary);

		protected abstract void GeneratePostInitializationCode
		(
			string directiveName,
			StringBuilder codeBuffer,
			CodeDomProvider languageProvider,
			IDictionary<string, string> requiresArguments,
			IDictionary<string, string> providesArguments);

		protected abstract void GeneratePreInitializationCode
		(
			string directiveName,
			StringBuilder codeBuffer,
			CodeDomProvider languageProvider,
			IDictionary<string, string> requiresArguments,
			IDictionary<string, string> providesArguments);

		protected abstract void GenerateTransformCode
		(
			string directiveName,
			StringBuilder codeBuffer,
			CodeDomProvider languageProvider,
			IDictionary<string, string> requiresArguments,
			IDictionary<string, string> providesArguments);

		protected virtual void PostProcessArguments
		(
			string directiveName,
			IDictionary<string, string> requiresArguments,
			IDictionary<string, string> providesArguments)
		{
		}

		public override string GetClassCodeForProcessingRun()
		{
			this.AssertNotProcessing();
			return this.codeBuffer.ToString();
		}

		public override string[] GetImportsForProcessingRun()
		{
			this.AssertNotProcessing();
			return null;
		}

		public override string[] GetReferencesForProcessingRun()
		{
			this.AssertNotProcessing();
			return null;
		}

		public override string GetPostInitializationCodeForProcessingRun()
		{
			this.AssertNotProcessing();
			return this.postInitBuffer.ToString();
		}

		public override string GetPreInitializationCodeForProcessingRun()
		{
			this.AssertNotProcessing();
			return this.preInitBuffer.ToString();
		}

		public override void StartProcessingRun(CodeDomProvider languageProvider, string templateContents, CompilerErrorCollection errors)
		{
			this.AssertNotProcessing();
			this.isInProcessingRun = true;
			base.StartProcessingRun(languageProvider, templateContents, errors);

			this.languageProvider = languageProvider;
			this.codeBuffer.Length = 0;
			this.preInitBuffer.Length = 0;
			this.postInitBuffer.Length = 0;
		}

		public override void FinishProcessingRun()
		{
			this.isInProcessingRun = false;
		}

		private void AssertNotProcessing()
		{
			if (this.isInProcessingRun)
				throw new InvalidOperationException();
		}

		//FIXME: handle escaping
		private IEnumerable<KeyValuePair<string, string>> ParseArgs(string args)
		{
			var pairs = args.Split(';');
			foreach (var p in pairs)
			{
				var eq = p.IndexOf('=');
				var k = p.Substring(0, eq);
				var v = p.Substring(eq);
				yield return new KeyValuePair<string, string>(k, v);
			}
		}

		public override void ProcessDirective(string directiveName, IDictionary<string, string> arguments)
		{
			if (directiveName == null)
				throw new ArgumentNullException("directiveName");
			if (arguments == null)
				throw new ArgumentNullException("arguments");

			var providesDictionary = new Dictionary<string, string>();
			var requiresDictionary = new Dictionary<string, string>();

			string provides;
			if (arguments.TryGetValue("provides", out provides))
			{
				foreach (var arg in this.ParseArgs(provides))
				{
					providesDictionary.Add(arg.Key, arg.Value);
				}
			}

			string requires;
			if (arguments.TryGetValue("requires", out requires))
			{
				foreach (var arg in this.ParseArgs(requires))
				{
					requiresDictionary.Add(arg.Key, arg.Value);
				}
			}

			this.InitializeRequiresDictionary(directiveName, requiresDictionary);
			this.InitializeProvidesDictionary(directiveName, providesDictionary);

			var id = this.ProvideUniqueId(directiveName, arguments, requiresDictionary, providesDictionary);

			foreach (var req in requiresDictionary)
			{
				var val = this.Host.ResolveParameterValue(id, this.FriendlyName, req.Key);
				if (val != null)
					requiresDictionary[req.Key] = val;
				else if (req.Value == null)
					throw new DirectiveProcessorException("Could not resolve required value '" + req.Key + "'");
			}

			foreach (var req in providesDictionary)
			{
				var val = this.Host.ResolveParameterValue(id, this.FriendlyName, req.Key);
				if (val != null)
					providesDictionary[req.Key] = val;
			}

			this.PostProcessArguments(directiveName, requiresDictionary, providesDictionary);

			this.GeneratePreInitializationCode(directiveName, this.preInitBuffer, this.languageProvider, requiresDictionary, providesDictionary);
			this.GeneratePostInitializationCode(directiveName, this.postInitBuffer, this.languageProvider, requiresDictionary, providesDictionary);
			this.GenerateTransformCode(directiveName, this.codeBuffer, this.languageProvider, requiresDictionary, providesDictionary);
		}

		protected virtual string ProvideUniqueId
		(
			string directiveName,
			IDictionary<string, string> arguments,
			IDictionary<string, string> requiresArguments,
			IDictionary<string, string> providesArguments)
		{
			return directiveName;
		}
	}
}
