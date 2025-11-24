// 
// TemplatingHost.cs
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
using System.IO;
using System.Reflection;
using System.Text;

namespace GameDevWare.TextTransform.Editor.Processor
{
	public class TemplateGenerator : MarshalByRefObject, ITextTemplatingEngineHost
	{
		//re-usable
		private TemplatingEngine engine;

		//per-run variables
		private string inputFile;
		private string outputFile;
		private Encoding encoding;

		//host fields
		private readonly CompilerErrorCollection errors = new CompilerErrorCollection();
		private readonly List<string> refs = new List<string>();
		private readonly List<string> imports = new List<string>();
		private readonly List<string> includePaths = new List<string>();
		private readonly List<string> referencePaths = new List<string>();

		//host properties for consumers to access
		public CompilerErrorCollection Errors
		{
			get { return this.errors; }
		}

		public List<string> Refs
		{
			get { return this.refs; }
		}

		public List<string> Imports
		{
			get { return this.imports; }
		}

		public List<string> IncludePaths
		{
			get { return this.includePaths; }
		}

		public List<string> ReferencePaths
		{
			get { return this.referencePaths; }
		}

		public string OutputFile
		{
			get { return this.outputFile; }
		}

		public bool UseRelativeLinePragmas { get; set; }

		public TemplateGenerator()
		{
			this.Refs.Add(typeof(TextTransformation).Assembly.Location);
			this.Refs.Add(typeof(Uri).Assembly.Location);
			this.Imports.Add("System");
		}

		public CompiledTemplate CompileTemplate(string content)
		{
			if (String.IsNullOrEmpty(content))
				throw new ArgumentNullException(nameof(content));

			this.errors.Clear();
			this.encoding = Encoding.UTF8;

			return this.Engine.CompileTemplate(content, this);
		}

		protected TemplatingEngine Engine
		{
			get
			{
				if (this.engine == null)
					this.engine = new TemplatingEngine();
				return this.engine;
			}
		}

		public bool ProcessTemplate(string inputFile, ref string outputFile)
		{
			if (String.IsNullOrEmpty(inputFile))
				throw new ArgumentNullException(nameof(inputFile));
			if (String.IsNullOrEmpty(outputFile))
				throw new ArgumentNullException(nameof(outputFile));

			string content;
			try
			{
				content = File.ReadAllText(inputFile);
			}
			catch (IOException ex)
			{
				this.errors.Clear();
				this.AddError("Could not read input file '" + inputFile + "':\n" + ex);
				return false;
			}

			string output;
			this.ProcessTemplate(inputFile, content, ref outputFile, out output);

			try
			{
				if (!this.errors.HasErrors)
					File.WriteAllText(outputFile, output, this.encoding);
			}
			catch (IOException ex)
			{
				this.AddError("Could not write output file '" + outputFile + "':\n" + ex);
			}

			return !this.errors.HasErrors;
		}

		public bool ProcessTemplate(string inputFileName, string inputContent, ref string outputFileName, out string outputContent)
		{
			this.errors.Clear();
			this.encoding = Encoding.UTF8;

			this.outputFile = outputFileName;
			this.inputFile = inputFileName;
			outputContent = this.Engine.ProcessTemplate(inputContent, this);
			outputFileName = this.outputFile;

			return !this.errors.HasErrors;
		}

		public bool PreprocessTemplate(string inputFile, string className, string classNamespace,
			string outputFile, Encoding encoding, out string language, out string[] references)
		{
			language = null;
			references = null;

			if (string.IsNullOrEmpty(inputFile))
				throw new ArgumentNullException(nameof(inputFile));
			if (string.IsNullOrEmpty(outputFile))
				throw new ArgumentNullException(nameof(outputFile));

			string content;
			try
			{
				content = File.ReadAllText(inputFile);
			}
			catch (IOException ex)
			{
				this.errors.Clear();
				this.AddError("Could not read input file '" + inputFile + "':\n" + ex);
				return false;
			}

			string output;
			this.PreprocessTemplate(inputFile, className, classNamespace, content, out language, out references, out output);

			try
			{
				if (!this.errors.HasErrors)
					File.WriteAllText(outputFile, output, encoding);
			}
			catch (IOException ex)
			{
				this.AddError("Could not write output file '" + outputFile + "':\n" + ex);
			}

			return !this.errors.HasErrors;
		}

		public bool PreprocessTemplate(string inputFileName, string className, string classNamespace, string inputContent,
			out string language, out string[] references, out string outputContent)
		{
			this.errors.Clear();
			this.encoding = Encoding.UTF8;

			this.inputFile = inputFileName;
			outputContent = this.Engine.PreprocessTemplate(inputContent, this, className, classNamespace, out language, out references);

			return !this.errors.HasErrors;
		}

		private CompilerError AddError(string error)
		{
			var err = new CompilerError();
			err.ErrorText = error;
			this.Errors.Add(err);
			return err;
		}

		#region Virtual members

		public virtual object GetHostOption(string optionName)
		{
			switch (optionName)
			{
				case "UseRelativeLinePragmas":
					return this.UseRelativeLinePragmas;
			}
			return null;
		}

		public virtual AppDomain ProvideTemplatingAppDomain(string content)
		{
			return null;
		}

		protected virtual string ResolveAssemblyReference(string assemblyReference)
		{
			if (System.IO.Path.IsPathRooted(assemblyReference))
				return assemblyReference;
			foreach (var referencePath in this.ReferencePaths)
			{
				var path = System.IO.Path.Combine(referencePath, assemblyReference);
				if (System.IO.File.Exists(path))
					return path;

				path = System.IO.Path.Combine(referencePath, assemblyReference) + ".dll";
				if (System.IO.File.Exists(path))
					return path;

				path = System.IO.Path.Combine(referencePath, assemblyReference) + ".exe";
				if (System.IO.File.Exists(path))
					return path;
			}

			var assemblyName = new AssemblyName(assemblyReference);
			if (assemblyName.Version != null)
				return assemblyReference;

			if (!assemblyReference.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && !assemblyReference.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				return assemblyReference + ".dll";
			return assemblyReference;
		}

		protected virtual string ResolveParameterValue(string directiveId, string processorName, string parameterName)
		{
			var key = new ParameterKey(processorName, directiveId, parameterName);
			string value;
			if (this.parameters.TryGetValue(key, out value))
				return value;
			if (processorName != null || directiveId != null)
				return this.ResolveParameterValue(null, null, parameterName);
			return null;
		}

		protected virtual Type ResolveDirectiveProcessor(string processorName)
		{
			KeyValuePair<string, string> value;
			if (!this.directiveProcessors.TryGetValue(processorName, out value))
				throw new Exception(string.Format("No directive processor registered as '{0}'", processorName));
			var asmPath = this.ResolveAssemblyReference(value.Value);
			if (asmPath == null)
				throw new Exception(string.Format("Could not resolve assembly '{0}' for directive processor '{1}'", value.Value, processorName));
			var asm = Assembly.LoadFrom(asmPath);
			return asm.GetType(value.Key, true);
		}

		protected virtual string ResolvePath(string path)
		{
			path = Environment.ExpandEnvironmentVariables(path);
			if (Path.IsPathRooted(path))
				return path;
			var dir = Path.GetDirectoryName(this.inputFile);
			var test = Path.Combine(dir, path);
			if (File.Exists(test) || Directory.Exists(test))
				return test;
			return path;
		}

		#endregion

		private readonly Dictionary<ParameterKey, string> parameters = new Dictionary<ParameterKey, string>();
		private readonly Dictionary<string, KeyValuePair<string, string>> directiveProcessors = new Dictionary<string, KeyValuePair<string, string>>();

		public void AddDirectiveProcessor(string name, string klass, string assembly)
		{
			this.directiveProcessors.Add(name, new KeyValuePair<string, string>(klass, assembly));
		}

		public void AddParameter(string processorName, string directiveName, string parameterName, string value)
		{
			this.parameters.Add(new ParameterKey(processorName, directiveName, parameterName), value);
		}

		protected virtual bool LoadIncludeText(string requestFileName, out string content, out string location)
		{
			content = "";
			location = this.ResolvePath(requestFileName);

			if (location == null || !File.Exists(location))
			{
				foreach (var path in this.includePaths)
				{
					var f = Path.Combine(path, requestFileName);
					if (File.Exists(f))
					{
						location = f;
						break;
					}
				}
			}

			if (location == null)
				return false;

			try
			{
				content = File.ReadAllText(location);
				return true;
			}
			catch (IOException ex)
			{
				this.AddError("Could not read included file '" + location + "':\n" + ex);
			}
			return false;
		}

		#region Explicit ITextTemplatingEngineHost implementation

		bool ITextTemplatingEngineHost.LoadIncludeText(string requestFileName, out string content, out string location)
		{
			return this.LoadIncludeText(requestFileName, out content, out location);
		}

		void ITextTemplatingEngineHost.LogErrors(CompilerErrorCollection errors)
		{
			this.errors.AddRange(errors);
		}

		string ITextTemplatingEngineHost.ResolveAssemblyReference(string assemblyReference)
		{
			return this.ResolveAssemblyReference(assemblyReference);
		}

		string ITextTemplatingEngineHost.ResolveParameterValue(string directiveId, string processorName, string parameterName)
		{
			return this.ResolveParameterValue(directiveId, processorName, parameterName);
		}

		Type ITextTemplatingEngineHost.ResolveDirectiveProcessor(string processorName)
		{
			return this.ResolveDirectiveProcessor(processorName);
		}

		string ITextTemplatingEngineHost.ResolvePath(string path)
		{
			return this.ResolvePath(path);
		}

		void ITextTemplatingEngineHost.SetFileExtension(string extension)
		{
			extension = extension.TrimStart('.');
			if (Path.HasExtension(this.outputFile))
			{
				this.outputFile = Path.ChangeExtension(this.outputFile, extension);
			}
			else
			{
				this.outputFile = this.outputFile + "." + extension;
			}
		}

		void ITextTemplatingEngineHost.SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
		{
			this.encoding = encoding;
		}

		IList<string> ITextTemplatingEngineHost.StandardAssemblyReferences
		{
			get { return this.refs; }
		}

		IList<string> ITextTemplatingEngineHost.StandardImports
		{
			get { return this.imports; }
		}

		string ITextTemplatingEngineHost.TemplateFile
		{
			get { return this.inputFile; }
		}

		#endregion

		private struct ParameterKey : IEquatable<ParameterKey>
		{
			public ParameterKey(string processorName, string directiveName, string parameterName)
			{
				this.processorName = processorName ?? "";
				this.directiveName = directiveName ?? "";
				this.parameterName = parameterName ?? "";
				unchecked
				{
					this.hashCode = this.processorName.GetHashCode()
							   ^ this.directiveName.GetHashCode()
							   ^ this.parameterName.GetHashCode();
				}
			}

			private readonly string processorName;
			private readonly string directiveName;
			private readonly string parameterName;
			private readonly int hashCode;

			public override bool Equals(object obj)
			{
				return obj is ParameterKey && this.Equals((ParameterKey)obj);
			}

			public bool Equals(ParameterKey other)
			{
				return this.processorName == other.processorName && this.directiveName == other.directiveName && this.parameterName == other.parameterName;
			}

			public override int GetHashCode()
			{
				return this.hashCode;
			}
		}

		/// <summary>
		///     If non-null, the template's Host property will be the full type of this host.
		/// </summary>
		public virtual Type SpecificHostType
		{
			get { return null; }
		}

		/// <summary>
		///     Gets any additional directive processors to be included in the processing run.
		/// </summary>
		public virtual IEnumerable<IDirectiveProcessor> GetAdditionalDirectiveProcessors()
		{
			yield break;
		}
	}
}
