using Facility.Definition.CodeGen;

namespace Facility.CodeGen.Markdown
{
	/// <summary>
	/// Settings for generating Markdown.
	/// </summary>
	public sealed class MarkdownGeneratorSettings : FileGeneratorSettings
	{
		/// <summary>
		/// True if the HTTP documentation should be omitted.
		/// </summary>
		public bool NoHttp { get; set; }
	}
}
