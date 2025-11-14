# Introduction
This Unity [editor extension](https://assetstore.unity.com/packages/tools/utilities/t4-code-generation-63294) provides [T4 text templates](https://msdn.microsoft.com/en-US/library/bb126445.aspx) processor. 
T4 template is a mixture of text blocks and control logic that can generate a text file. The control logic is written as fragments of program code in C#.
The generated file can be text of any kind, such as resource file, source code or web page.

T4 template example:
```csharp
<html><body>
 The date and time now is: <#= DateTime.Now #>
</body></html>
```

## Use cases
* Source code generation
* [Project resources catalogization](GameDevWare.TextTransform.Unity/Assets/Editor/GameDevWare.TextTransform/Examples/FileList_Example.tt)
* [Resource loading/reading code generation](GameDevWare.TextTransform.Unity/Assets/Editor/GameDevWare.TextTransform/Examples/ResourceAsyncLoad_Example.tt)
* [Code generation by DSL](GameDevWare.TextTransform.Unity/Assets/Editor/GameDevWare.TextTransform/Examples/DSL_Example.tt)
* Code generation for ORM (BLToolkit for example)
* [Embedding environment information into project's build](GameDevWare.TextTransform.Unity/Assets/Editor/GameDevWare.TextTransform/Examples/EnvironmentInfo_Example.tt)
	
## How to use
Create or copy file with **.tt** extension. Select this file in Project window (Window -> Project), then in Inspector window (Window -> Inspector) setup T4 template's parameters. Click "Generate" button.
Inspector window for T4 template contains following parameters:
* **Output Type** - type of generated file
  * **Text** - normal template's output. It corresponds to "Design-time T4 text templates" in Microsoft's terminology.
  * **Text Generator** - generator-class which can generate content when TransformText() is called. It corresponds to "Run time T4 text templates" in Microsoft's terminology.
* **Output Path** - path to generated file. If not specified, generated file will have file name of template and file extension from *output* directive.
* **Auto-Gen Triggers** - list of events which trigger auto-generation.
  * **Code Compilation** - after each code compilation
  * **Asset Changes** - after watched assets are changed, look for **Assets to Watch**
* **Auto-Gen Delay (Ms)** - delay before triggered auto-generation starts
* **Assets to Watch** - list of assets and folders which trigger auto-generation

## Details
T4 template can use *hostspecific=true* [property](https://msdn.microsoft.com/en-us/library/bb126478.aspx#Anchor_4) to access *Host.ResolvePath* method, which maps path relatively to template's location.

By default *UnityEngine.dll* and *UnityEditor.dll* assemblies are referenced in all templates. 
You can reference project's assemblies *Assembly-CSharp* and *Assembly-CSharp-firstpass* by adding **assembly** [directive](https://msdn.microsoft.com/en-us/library/bb126478.aspx#Anchor_3):
```xml
<#@ assembly name="Assembly-CSharp" #>
<#@ assembly name="Assembly-CSharp-firstpass" #>
```
### Target Runtime
The template always uses the current runtime and core libraries of Unity Editor. 

### Language Version
You could specify C# language version with `language=` directive.
List of available language versions:
* language="C#v3.5" - C# version 3.5.
* language="C#" - C# up to version 7, depending on Unity Editor's version.
* unspecified - It is currently the equivalent of "C#".

[MSBuild Macros](https://msdn.microsoft.com/en-US/library/c02as0cs.aspx) are not available.

You can run template generation from your code with **UnityTemplateGenerator.RunForTemplate(templatePath)** call.

## Contacts
Please send any questions at support@gamedevware.com

## License
[Asset Store Terms of Service and EULA](LICENSE.md)
