using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GameDevWare.TextTransform.Editor
{
	[ScriptedImporter(1, "t4")]
	public class T4TemplateImporter : TextTemplateImporter
	{
		/// <inheritdoc />
		public override void OnImportAsset(AssetImportContext ctx) => ImportAsset(ctx);

		public static void ImportAsset(AssetImportContext ctx)
		{
			if (ctx.mainObject is TextAsset)
			{
				return;
			}

			var gameDataPath = ctx.assetPath ?? "";
			if (string.IsNullOrEmpty(gameDataPath) || !File.Exists(gameDataPath))
			{
				return;
			}

			var text = File.ReadAllText(gameDataPath);
			var textAsset = new TextAsset(text);

			// Add the TextAsset to the import context
			ctx.AddObjectToAsset("main", textAsset);
			ctx.SetMainObject(textAsset);
		}
	}
}
