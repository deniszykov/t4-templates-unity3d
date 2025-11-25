using System;

namespace GameDevWare.TextTransform.Editor
{
	/// <summary>
	///     Transformation triggers.
	/// </summary>
	[Flags]
	public enum GenerationTriggers
	{
		None = 0,
		/// <summary>
		///     Each Unity's editor code compilation will trigger transformation.
		/// </summary>
		CodeCompilation = 1 << 0,
		/// <summary>
		///     Each change in watched assets will trigger transformation.
		/// </summary>
		AssetChanges = 1 << 1,

		Any = ~0
	}
}
