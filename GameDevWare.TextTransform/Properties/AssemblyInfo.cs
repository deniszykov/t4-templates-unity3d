/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "T4 Transform" Unity Asset - https://www.assetstore.unity3d.com/#!/content/63294
	
	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND 
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE 
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY, 
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE 
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.
	
	This source code is distributed via Unity Asset Store, 
	to use it in your project you should accept Terms of Service and EULA 
	https://unity3d.com/ru/legal/as_terms
*/
#if !(UNITY_5 || UNITY_4 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5)
using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("T4 CLI")]
[assembly: AssemblyDescription("Command line utility for text transformation with using T4 templates.\r\n" +
								"usage: GameDevWare.TextTranform.exe Transform --templatePath <path-to-t4-template> [--outputPath <path-to-output-file>] [--references <additional-refs>] " +
								"[--namespaces <additional_namespaces>] [--includes <additional-includes>] [--referencePaths <reference-lookup-paths>] [--createGenerator] [--verbose]")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("GameDevWare")]
[assembly: AssemblyProduct("GameDevWare.TextTransform")]
[assembly: AssemblyCopyright("Copyright © 2016 GameDevWare, Denis Zykov")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("7D96B049-0A37-4C61-91E1-1F122607664B")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion("1.0.6.0")]
[assembly: AssemblyFileVersion("1.0.6.0")]
[assembly: AssemblyInformationalVersion("1.0.6")]
#endif
