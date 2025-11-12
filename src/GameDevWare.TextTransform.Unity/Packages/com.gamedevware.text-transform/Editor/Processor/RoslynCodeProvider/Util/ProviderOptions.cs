// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GameDevWare.TextTransform.Editor.Processor.RoslynCodeProvider.Util {

    /// <summary>
    /// A set of options for the C# and VB CodeProviders.
    /// </summary>
    public sealed class ProviderOptions : IProviderOptions {

        private IDictionary<string, string> _allOptions;

        /// <summary>
        /// Create a default set of options for the C# and VB CodeProviders.
        /// </summary>
        public ProviderOptions()
        {
            this.CompilerFullPath = null;
            this.CompilerVersion = null;
            this.WarnAsError = false;

            // To be consistent, make sure there is always a dictionary here. It is less than ideal
            // for some parts of code to be checking AllOptions.count and some part checking
            // for AllOptions == null.
            this.AllOptions = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

            // This results in no keep-alive for the compiler. This will likely result in
            // slower performance for any program that calls out the the compiler any
            // significant number of times. This is why the CompilerUtil.GetProviderOptionsFor
            // does not leave this as 0.
            this.CompilerServerTimeToLive = 0;

            // This is different from the default that the CompilerUtil.GetProviderOptionsFor
            // factory method uses. The primary known user of the factory method is us, and
            // this package is first intended to support ASP.Net. However, if somebody is
            // creating an instance of this directly, they are probably not an ASP.Net
            // project. Thus the different default here.
            this.UseAspNetSettings = false;
        }

        /// <summary>
        /// Create a set of options for the C# or VB CodeProviders using the specified inputs.
        /// </summary>
        public ProviderOptions(IProviderOptions opts)
        {
            this.CompilerFullPath = opts.CompilerFullPath;
            this.CompilerServerTimeToLive = opts.CompilerServerTimeToLive;
            this.CompilerVersion = opts.CompilerVersion;
            this.WarnAsError = opts.WarnAsError;
            this.UseAspNetSettings = opts.UseAspNetSettings;
            this.AllOptions = new ReadOnlyDictionary<string, string>(opts.AllOptions);
        }

        /// <summary>
        /// Create a set of options for the C# or VB CodeProviders using some specified inputs.
        /// </summary>
        public ProviderOptions(string compilerFullPath, int compilerServerTimeToLive) : this()
        {
            this.CompilerFullPath = compilerFullPath;
            this.CompilerServerTimeToLive = compilerServerTimeToLive;
        }

#pragma warning disable CS0618
        internal ProviderOptions(ICompilerSettings settings) : this(settings.CompilerFullPath, settings.CompilerServerTimeToLive) { }
#pragma warning restore CS0618

        /// <summary>
        /// The full path to csc.exe or vbc.exe
        /// </summary>
        public string CompilerFullPath { get; internal set; }

        /// <summary>
        /// TTL in seconds
        /// </summary>
        public int CompilerServerTimeToLive { get; internal set; }

        /// <summary>
        /// Used by in-box framework code providers to determine which compat version of the compiler to use.
        /// </summary>
        public string CompilerVersion { get; internal set; }

        // smolloy todo debug degub - Does it really override everything? Is that the right thing to do?
        /// <summary>
        /// Treat all warnings as errors. Will override defaults and command-line options given for a compiler.
        /// </summary>
        public bool WarnAsError { get; internal set; }

        /// <summary>
        /// Use the set of compiler options that was traditionally added programatically for ASP.Net.
        /// </summary>
        public bool UseAspNetSettings { get; internal set; }

        /// <summary>
        /// A collection of all &lt;providerOptions&gt; specified in config for the given CodeDomProvider.
        /// </summary>
        public IDictionary<string, string> AllOptions {
            get {
                return this._allOptions;
            }
            internal set {
                this._allOptions = (value != null) ? new ReadOnlyDictionary<string, string>(value) : null;
            }
        }
    }
}
