using CodeGenCore;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Fsd;
using Facility.Definition.Http;

namespace Facility.CodeGen.Markdown;

/// <summary>
/// Generates Markdown.
/// </summary>
public sealed class MarkdownGenerator : CodeGenerator
{
	/// <summary>
	/// Generates Markdown.
	/// </summary>
	/// <param name="parser">The parser.</param>
	/// <param name="settings">The settings.</param>
	/// <returns>The number of updated files.</returns>
	public static int GenerateMarkdown(ServiceParser parser, MarkdownGeneratorSettings settings) =>
		FileGenerator.GenerateFiles(parser, new MarkdownGenerator { GeneratorName = nameof(MarkdownGenerator) }, settings);

	/// <summary>
	/// Generates Markdown.
	/// </summary>
	/// <param name="settings">The settings.</param>
	/// <returns>The number of updated files.</returns>
	[Obsolete("Use the overload that takes a parser.")]
	public static int GenerateMarkdown(MarkdownGeneratorSettings settings) =>
		FileGenerator.GenerateFiles(new MarkdownGenerator { GeneratorName = nameof(MarkdownGenerator) }, settings);

	/// <summary>
	/// True if the HTTP documentation should be omitted.
	/// </summary>
	public bool NoHttp { get; set; }

	/// <summary>
	/// The text of the template to be used when generating output.
	/// </summary>
	public string? TemplateText { get; set; }

	/// <summary>
	/// Generates the Markdown.
	/// </summary>
	public override CodeGenOutput GenerateOutput(ServiceInfo service)
	{
		var httpServiceInfo = NoHttp ? null : HttpServiceInfo.Create(service);

		var template = CodeGenTemplate.Parse(TemplateText ?? GetEmbeddedResourceText("Facility.CodeGen.Markdown.template.scriban-txt"));
		var globals = CodeGenGlobals.Create(new MarkdownGeneratorGlobals(this, service, httpServiceInfo));
		var files = template.Generate(globals, new CodeGenSettings { NewLine = NewLine, IndentText = IndentText });

		return new CodeGenOutput(
			files: files.Select(x => new CodeGenFile(x.Name, x.Text)).ToList(),
			patternsToClean: [new CodeGenPattern($"{service.Name}/*.md", CodeGenUtility.GetCodeGenComment(GeneratorName ?? ""))]);
	}

	/// <summary>
	/// Applies generator-specific settings.
	/// </summary>
	public override void ApplySettings(FileGeneratorSettings settings)
	{
		var ourSettings = (MarkdownGeneratorSettings) settings;

		NoHttp = ourSettings.NoHttp;
		TemplateText = ourSettings.TemplatePath is null ? null : File.ReadAllText(ourSettings.TemplatePath);
	}

	/// <summary>
	/// Patterns to clean are returned with the output.
	/// </summary>
	public override bool HasPatternsToClean => true;

	private static string GetEmbeddedResourceText(string name)
	{
		using var reader = new StreamReader(typeof(MarkdownGenerator).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException());
		return reader.ReadToEnd();
	}
}
