using System;
using System.IO;
using System.Linq;
using CodeGenCore;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Http;

namespace Facility.CodeGen.Markdown
{
	/// <summary>
	/// Generates Markdown.
	/// </summary>
	public sealed class MarkdownGenerator : CodeGenerator
	{
		/// <summary>
		/// Generates Markdown.
		/// </summary>
		/// <param name="settings">The settings.</param>
		/// <returns>The number of updated files.</returns>
		public static int GenerateMarkdown(MarkdownGeneratorSettings settings) =>
			FileGenerator.GenerateFiles(new MarkdownGenerator { GeneratorName = nameof(MarkdownGenerator) }, settings);

		/// <summary>
		/// True if the HTTP documentation should be omitted.
		/// </summary>
		public bool NoHttp { get; set; }

		/// <summary>
		/// Generates the Markdown.
		/// </summary>
		public override CodeGenOutput GenerateOutput(ServiceInfo serviceInfo)
		{
			var httpServiceInfo = NoHttp ? null : HttpServiceInfo.Create(serviceInfo);

			var template = CodeGenTemplate.Parse(GetEmbeddedResourceText("Facility.CodeGen.Markdown.template.scriban-txt"));
			var globals = CodeGenGlobals.Create(new MarkdownGeneratorGlobals(this, serviceInfo, httpServiceInfo));
			var files = template.Generate(globals, new CodeGenSettings { NewLine = NewLine, IndentText = IndentText });

			return new CodeGenOutput(
				files: files.Select(x => new CodeGenFile(x.Name, x.Text)).ToList(),
				patternsToClean: new[] { new CodeGenPattern("*.md", CodeGenUtility.GetCodeGenComment(GeneratorName ?? "")) });
		}

		/// <summary>
		/// Applies generator-specific settings.
		/// </summary>
		public override void ApplySettings(FileGeneratorSettings settings)
		{
			NoHttp = ((MarkdownGeneratorSettings) settings).NoHttp;
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
}
