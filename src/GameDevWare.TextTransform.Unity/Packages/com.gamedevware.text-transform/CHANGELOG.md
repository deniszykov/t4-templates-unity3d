# Release Notes

## [2.1.4]

- Made .t4 files considered as Text Template assets along with .tt files

## [2.1.3]

- The `Asset Changed` generation trigger now always includes the template `.tt` file to track as an asset.
- Fixed a bug where generation triggers would not work due to incorrect file path comparisons.

## [2.1.2]
### Major Changes
- **Refactored** to use `ScriptImporter` and modern Unity API
- **Packaging**: Created Unity package format instead of DLL and published to OpenUPM
### Breaking Changes
- `namespace GameDevWare.TextTransform` → `namespace GameDevWare.TextTransform.Editor`
- `TemplateSettings` → `TextTemplateImporter`

## [2.1.1]
### Fixes
- **Fixed** asset watching trigger error in slash treatment
- **Normalized** paths to OS default representation

### Changes
- **Changed** default extension for generated files to `.txt`

## [2.1.0]
### New Features
- **Added** 'Preferences' button to all template inspector windows
- **Added** path hint for assemblies in 'T4 Settings' window
- **Added** Roslyn compiler support with new language target "C#/Unity" (default if none set)
- **Added** ability to redefine Roslyn compiler location

### Improvements
- **Enforced** using latest C# version known to Roslyn compiler
- **Changed** default target runtime and libraries to Unity Editor's current ones

## [2.0.1]
### Fixes
- **Fixed** NullReferenceException in `TemplateSettings`
- **Added** additional checks for `userData`
- **Improved** error message "Failed to load settings..." to be less confusing

## [2.0.0]
### Breaking Changes
- `TemplateSettings.OutputTypes.Code` → `TemplateSettings.OutputTypes.Text`
- `TemplateSettings.OutputTypes.CodeGenerator` → `TemplateSettings.OutputTypes.TextGenerator`
- `GenerationResult` → `TransformationResult`

### New Features
- **Added** `UnityTemplateGenerator.RunForTemplate` with additional parameters
- **Added** XML documentation to most public members
- **Added** parameters passing example
