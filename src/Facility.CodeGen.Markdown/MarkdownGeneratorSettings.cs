using Facility.Definition.CodeGen;

namespace Facility.CodeGen.Markdown;

/// <summary>
/// Settings for generating Markdown.
/// </summary>
public sealed class MarkdownGeneratorSettings : FileGeneratorSettings
{
	/// <summary>
	/// True if the HTTP documentation should be omitted.
	/// </summary>
	public bool NoHttp { get; set; }

	/// <summary>
	/// The path to the template to be used when generating output.
	/// </summary>
	public string? TemplatePath { get; set; }
}
