using UnityEditor.AssetImporters;

namespace GameDevWare.TextTransform.Editor
{
	[ScriptedImporter(1, "tt")]
	public class TtTemplateImporter : TextTemplateImporter
	{
		/// <inheritdoc />
		public override void OnImportAsset(AssetImportContext ctx)
		{
			T4TemplateImporter.ImportAsset(ctx);
		}
	}
}
