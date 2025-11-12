namespace GameDevWare.TextTransform.Editor
{
	/// <summary>
	/// Result of T4 template run.
	/// </summary>
	public enum TransformationResult
	{
		/// <summary>
		/// Template transformation is successful.
		/// </summary>
		Success,
		/// <summary>
		/// Template transformation is successful but output is same as existing.
		/// </summary>
		NoChanges,
		/// <summary>
		/// Template transformation is failed due invalid settings.
		/// </summary>
		UnknownOutputType,
		/// <summary>
		/// Template transformation is failed due error during transformation.
		/// </summary>
		TemplateProcessingError,
		/// <summary>
		/// Template transformation is failed due error during template compilation.
		/// </summary>
		TemplateCompilationError,
	}
}
