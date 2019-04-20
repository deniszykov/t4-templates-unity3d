// 
// CompiledTemplate.cs
//  
// Author:
//       Nathan Baulch <nathan.baulch@gmail.com>
// 
// Copyright (c) 2009 Nathan Baulch
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
using System.Globalization;
using System.Reflection;

namespace GameDevWare.TextTransform.Processor
{
	public sealed class CompiledTemplate : MarshalByRefObject, IDisposable
	{
		private ITextTemplatingEngineHost host;
		private object textTransformation;
		private readonly CultureInfo culture;
		private readonly string[] assemblyFiles;

		public CompiledTemplate(ITextTemplatingEngineHost host, CompilerResults results, string fullName, CultureInfo culture,
			string[] assemblyFiles)
		{
			AppDomain.CurrentDomain.AssemblyResolve += this.ResolveReferencedAssemblies;
			this.host = host;
			this.culture = culture;
			this.assemblyFiles = assemblyFiles;
			this.Load(results, fullName);
		}

		private void Load(CompilerResults results, string fullName)
		{
			var assembly = results.CompiledAssembly;
			var transformType = assembly.GetType(fullName);
			//MS Templating Engine does not look on the type itself, 
			//it checks only that required methods are exists in the compiled type 
			this.textTransformation = Activator.CreateInstance(transformType);

			//set the host property if it exists
			Type hostType = null;
			var gen = this.host as TemplateGenerator;
			if (gen != null)
			{
				hostType = gen.SpecificHostType;
			}
			var hostProp = transformType.GetProperty("Host", hostType ?? typeof(ITextTemplatingEngineHost));
			if (hostProp != null && hostProp.CanWrite)
				hostProp.SetValue(this.textTransformation, this.host, null);

			var sessionHost = this.host as ITextTemplatingSessionHost;
			if (sessionHost != null)
			{
				//FIXME: should we create a session if it's null?
				var sessionProp = transformType.GetProperty("Session", typeof(IDictionary<string, object>));
				sessionProp.SetValue(this.textTransformation, sessionHost.Session, null);
			}
		}

		public string Process()
		{
			var ttType = this.textTransformation.GetType();

			var errorProp = ttType.GetProperty("Errors", BindingFlags.Instance | BindingFlags.NonPublic);
			if (errorProp == null)
				throw new ArgumentException("Template must have 'Errors' property");
			var errorMethod = ttType.GetMethod("Error", new Type[] { typeof(string) });
			if (errorMethod == null)
			{
				throw new ArgumentException("Template must have 'Error(string message)' method");
			}

			var errors = (CompilerErrorCollection)errorProp.GetValue(this.textTransformation, null);
			errors.Clear();

			//set the culture
			if (this.culture != null)
				ToStringHelper.FormatProvider = this.culture;
			else
				ToStringHelper.FormatProvider = CultureInfo.InvariantCulture;

			string output = null;

			var initMethod = ttType.GetMethod("Initialize");
			var transformMethod = ttType.GetMethod("TransformText");

			if (initMethod == null)
			{
				errorMethod.Invoke(this.textTransformation, new object[] { "Error running transform: no method Initialize()" });
			}
			else if (transformMethod == null)
			{
				errorMethod.Invoke(this.textTransformation, new object[] { "Error running transform: no method TransformText()" });
			}
			else
				try
				{
					initMethod.Invoke(this.textTransformation, null);
					output = (string)transformMethod.Invoke(this.textTransformation, null);
				}
				catch (Exception ex)
				{
					errorMethod.Invoke(this.textTransformation, new object[] { "Error running transform: " + ex });
				}

			this.host.LogErrors(errors);

			ToStringHelper.FormatProvider = CultureInfo.InvariantCulture;
			return output;
		}

		private Assembly ResolveReferencedAssemblies(object sender, ResolveEventArgs args)
		{
			var asmName = new AssemblyName(args.Name);
			foreach (var asmFile in this.assemblyFiles)
			{
				if (asmName.Name == System.IO.Path.GetFileNameWithoutExtension(asmFile))
					return Assembly.LoadFrom(asmFile);
			}

			var path = this.host.ResolveAssemblyReference(asmName.Name + ".dll");
			if (System.IO.File.Exists(path))
				return Assembly.LoadFrom(path);

			return null;
		}

		public void Dispose()
		{
			if (this.host != null)
			{
				this.host = null;
				AppDomain.CurrentDomain.AssemblyResolve -= this.ResolveReferencedAssemblies;
			}
		}
	}
}
