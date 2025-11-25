namespace GameDevWare.TextTransform.Editor
{
	/// <summary>
	///     Run mode. Determine result of transformation.
	/// </summary>
	public enum GenerationOutput
	{
		/// <summary>
		///     Result is generated code/markup.
		/// </summary>
		Text,
		/// <summary>
		///     Result is C# code of template which could output <see cref="Text" /> if ran.
		/// </summary>
		TextGenerator
	}
}
