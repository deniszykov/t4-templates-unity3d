// 
// RecyclableAppDomain.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com_
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace GameDevWare.TextTransform.Editor.Processor
{
	public class TemplatingAppDomainRecycler
	{
		internal class RecyclableAppDomain
		{
			private DomainAssemblyLoader assemblyMap;

			//TODO: implement timeout based recycling
			//DateTime lastUsed;

			public int UnusedHandles { get; private set; } = DEFAULT_MAX_USES;

			public int LiveHandles { get; private set; }

			public AppDomain Domain { get; private set; }

			public RecyclableAppDomain(string name)
			{
				var info = new AppDomainSetup {
					//appbase needs to allow loading this assembly, for remoting
					ApplicationBase = Path.GetDirectoryName(typeof(TemplatingAppDomainRecycler).Assembly.Location),
					DisallowBindingRedirects = false,
					DisallowCodeDownload = true,
					DisallowApplicationBaseProbing = false,
					ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
				};
				this.Domain = AppDomain.CreateDomain(name, null, info);
				var t = typeof(DomainAssemblyLoader);
				AppDomain.CurrentDomain.AssemblyResolve += this.CurrentDomain_AssemblyResolve;
				this.assemblyMap = (DomainAssemblyLoader)this.Domain.CreateInstanceFromAndUnwrap(t.Assembly.Location, t.FullName);
				AppDomain.CurrentDomain.AssemblyResolve -= this.CurrentDomain_AssemblyResolve;
				this.Domain.AssemblyResolve += this.assemblyMap.Resolve; // new DomainAssemblyLoader(assemblyMap).Resolve;
			}

			private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
			{
				var a = typeof(RecyclableAppDomain).Assembly;
				if (args.Name == a.FullName)
					return a;

				return null;
			}

			public void AddAssembly(Assembly assembly)
			{
				this.assemblyMap.Add(assembly.FullName, assembly.Location);
			}

			public Handle GetHandle()
			{
				lock (this)
				{
					if (this.UnusedHandles <= 0) throw new InvalidOperationException("No handles left");

					this.UnusedHandles--;
					this.LiveHandles++;
				}

				return new Handle(this);
			}

			public void ReleaseHandle()
			{
				int lh;
				lock (this)
				{
					this.LiveHandles--;
					lh = this.LiveHandles;
				}

				//We must unload domain every time after using it for generation
				//Otherwise we could not load new version of the project-generated 
				//assemblies into it. So remove checking for unusedHandles == 0
				if (lh == 0) this.UnloadDomain();
			}

			private void UnloadDomain()
			{
				AppDomain.Unload(this.Domain);
				this.Domain = null;
				this.assemblyMap = null;
				GC.SuppressFinalize(this);
			}

			~RecyclableAppDomain()
			{
				if (this.LiveHandles != 0)
					Console.WriteLine("WARNING: recyclable AppDomain's handles were not all disposed");
			}
		}

		public class Handle : IDisposable
		{
			private RecyclableAppDomain parent;

			public AppDomain Domain => this.parent.Domain;

			internal Handle(RecyclableAppDomain parent)
			{
				this.parent = parent;
			}

			public void AddAssembly(Assembly assembly)
			{
				this.parent.AddAssembly(assembly);
			}

			public void Dispose()
			{
				if (this.parent == null)
					return;

				var p = this.parent;
				lock (this)
				{
					if (this.parent == null)
						return;

					this.parent = null;
				}

				p.ReleaseHandle();
			}
		}

		[Serializable]
		private class DomainAssemblyLoader : MarshalByRefObject
		{
			private readonly Dictionary<string, string> map = new();

			public Assembly Resolve(object sender, ResolveEventArgs args)
			{
				var assemblyFile = this.ResolveAssembly(args.Name);
				if (assemblyFile != null)
					return Assembly.LoadFrom(assemblyFile);

				return null;
			}

			public string ResolveAssembly(string name)
			{
				string result;
				if (this.map.TryGetValue(name, out result))
					return result;

				return null;
			}

			public void Add(string name, string location)
			{
				this.map[name] = location;
			}

			//keep this alive as long as the app domain is alive
			public override object InitializeLifetimeService()
			{
				return null;
			}
		}

		private const int DEFAULT_MAX_USES = 20;
		private const int DEFAULT_TIMEOUT_MS = 2 * 60 * 1000;
		private readonly object lockObj = new();

		private readonly string name;

		private RecyclableAppDomain domain;

		public TemplatingAppDomainRecycler(string name)
		{
			this.name = name;
		}

		public Handle GetHandle()
		{
			lock (this.lockObj)
			{
				if (this.domain == null || this.domain.Domain == null || this.domain.UnusedHandles == 0) this.domain = new RecyclableAppDomain(this.name);
				return this.domain.GetHandle();
			}
		}
	}
}
