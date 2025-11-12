// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using GameDevWare.TextTransform.Editor.Processor.RoslynCodeProvider.Util;

namespace GameDevWare.TextTransform.Editor.Processor.RoslynCodeProvider {
    internal class CSharpCompiler : Compiler {
        private static volatile Regex outputRegWithFileAndLine;
        private static volatile Regex outputRegSimple;

        public CSharpCompiler(CodeDomProvider codeDomProvider, IProviderOptions providerOptions = null)
            : base(codeDomProvider, providerOptions) {
        }

        protected override string FileExtension {
            get {
                return ".cs";
            }
        }

        protected override void ProcessCompilerOutputLine(CompilerResults results, string line) {
            if (outputRegSimple == null) {
                outputRegWithFileAndLine =
                    new Regex(@"(^(.*)(\(([0-9]+),([0-9]+)\)): )(error|warning) ([A-Z]+[0-9]+) ?: (.*)");
                outputRegSimple =
                    new Regex(@"(error|warning) ([A-Z]+[0-9]+) ?: (.*)");
            }

            //First look for full file info
            Match m = outputRegWithFileAndLine.Match(line);
            bool full;
            if (m.Success) {
                full = true;
            }
            else {
                m = outputRegSimple.Match(line);
                full = false;
            }

            if (m.Success) {
                var ce = new CompilerError();
                if (full) {
                    ce.FileName = m.Groups[2].Value;
                    ce.Line = int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
                    ce.Column = int.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture);
                }

                if (string.Compare(m.Groups[full ? 6 : 1].Value, "warning", StringComparison.OrdinalIgnoreCase) == 0) {
                    ce.IsWarning = true;
                }

                ce.ErrorNumber = m.Groups[full ? 7 : 2].Value;
                ce.ErrorText = m.Groups[full ? 8 : 3].Value;
                results.Errors.Add(ce);
            }
        }

        protected override string FullPathsOption {
            get {
                return " /fullpaths ";
            }
        }

        protected override void FixUpCompilerParameters(CompilerParameters options) {
            base.FixUpCompilerParameters(options);

            // We used to magically add some ASP.net-centric options here. For compatibilities sake
            // we will continue to do so in ASP.Net mode. If these are getting in the way for people
            // though, disable ASP.Net mode and they will go away. (Sort of. These are the defaults
            // in the XDT config transform, so they will already be here anyway for most folks.)
            if (this.ProviderOptions.UseAspNetSettings)
            {
                List<string> noWarnStrings = new List<string>(5);
                noWarnStrings.AddRange(new string[] { "1659", "1699", "1701" });

                // disableObsoleteWarnings
                noWarnStrings.Add("612"); // [Obsolete] without message
                noWarnStrings.Add("618"); // [Obsolete("with message")]

                CompilationUtil.PrependCompilerOption(options, "/nowarn:" + String.Join(";", noWarnStrings));
            }
        }

        protected override string CmdArgsFromParameters(CompilerParameters parameters) {
            var allArgsBuilder = new StringBuilder();

            if (parameters.GenerateExecutable) {
                allArgsBuilder.Append("/t:exe ");
                if (parameters.MainClass != null && parameters.MainClass.Length > 0) {
                    allArgsBuilder.Append("/main:");
                    allArgsBuilder.Append(parameters.MainClass);
                    allArgsBuilder.Append(" ");
                }
            }
            else {
                allArgsBuilder.Append("/t:library ");
            }

            // Get UTF8 output from the compiler
            allArgsBuilder.Append("/utf8output ");

            var coreAssembly = typeof(object).Assembly.Location;
            string coreAssemblyFileName = parameters.CoreAssemblyFileName;
            if (string.IsNullOrWhiteSpace(coreAssemblyFileName)) {
                coreAssemblyFileName = coreAssembly;
            }

            if (!string.IsNullOrWhiteSpace(coreAssemblyFileName)) {
                allArgsBuilder.Append("/nostdlib+ ");
                allArgsBuilder.Append("/R:\"").Append(coreAssemblyFileName.Trim()).Append("\" ");
            }

            // Bug 913691: Explicitly add System.Runtime as a reference.
            string systemRuntimeAssemblyPath = null;
            try {
                var systemRuntimeAssembly = Assembly.Load("System.Runtime");
                systemRuntimeAssemblyPath = systemRuntimeAssembly.Location;
            }
            catch {
                // swallow any exceptions if we cannot find the assembly
            }

            if (systemRuntimeAssemblyPath != null) {
                allArgsBuilder.Append($"/R:\"{systemRuntimeAssemblyPath}\" ");
            }

            foreach (string s in parameters.ReferencedAssemblies) {
                allArgsBuilder.Append("/R:");
                allArgsBuilder.Append("\"");
                allArgsBuilder.Append(s);
                allArgsBuilder.Append("\"");
                allArgsBuilder.Append(" ");
            }

            allArgsBuilder.Append("/out:");
            allArgsBuilder.Append("\"");
            allArgsBuilder.Append(parameters.OutputAssembly);
            allArgsBuilder.Append("\"");
            allArgsBuilder.Append(" ");

            if (parameters.IncludeDebugInformation) {
                allArgsBuilder.Append("/D:DEBUG ");
                allArgsBuilder.Append("/debug+ ");
                allArgsBuilder.Append("/optimize- ");
            }
            else {
                allArgsBuilder.Append("/debug- ");
                allArgsBuilder.Append("/optimize+ ");
            }

#if !FEATURE_PAL
            if (parameters.Win32Resource != null) {
                allArgsBuilder.Append("/win32res:\"" + parameters.Win32Resource + "\" ");
            }
#endif // !FEATURE_PAL

            foreach (string s in parameters.EmbeddedResources) {
                allArgsBuilder.Append("/res:\"");
                allArgsBuilder.Append(s);
                allArgsBuilder.Append("\" ");
            }

            foreach (string s in parameters.LinkedResources) {
                allArgsBuilder.Append("/linkres:\"");
                allArgsBuilder.Append(s);
                allArgsBuilder.Append("\" ");
            }

			allArgsBuilder.Append("-langversion:latest ");

            if (parameters.TreatWarningsAsErrors) {
                allArgsBuilder.Append("/warnaserror+ ");
            }
            else {
                allArgsBuilder.Append("/warnaserror- ");
            }

            if (parameters.WarningLevel >= 0) {
                allArgsBuilder.Append("/w:" + parameters.WarningLevel + " ");
            }

            if (parameters.CompilerOptions != null) {
                allArgsBuilder.Append(parameters.CompilerOptions + " ");
            }

            string allArgs = allArgsBuilder.ToString();
            return allArgs;
        }
    }
}
