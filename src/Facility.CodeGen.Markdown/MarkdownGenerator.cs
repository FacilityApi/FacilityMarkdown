using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
			var outputFiles = new List<CodeGenFile>();

			var httpServiceInfo = NoHttp ? null : HttpServiceInfo.Create(serviceInfo);

			var templateText = GetEmbeddedResourceText("Facility.CodeGen.Markdown.template.scriban-txt");
			var outputText = CodeTemplateUtility.Render(templateText, new CodeTemplateGlobals(this, serviceInfo, httpServiceInfo));
			using var stringReader = new StringReader(outputText);

			var fileStart = "";

			string? line;
			while ((line = stringReader.ReadLine()) != null)
			{
				var match = Regex.Match(line, @"^==+>");
				if (match.Success)
				{
					fileStart = match.Value;
					break;
				}
			}

			while (line != null)
			{
				var fileName = line.Substring(fileStart.Length);

				var fileLines = new List<string>();
				while ((line = stringReader.ReadLine()) != null && !line.StartsWith(fileStart, StringComparison.Ordinal))
					fileLines.Add(line);

				// skip exactly one blank line to allow file start to stand out
				if (fileLines.Count != 0 && string.IsNullOrWhiteSpace(fileLines[0]))
					fileLines.RemoveAt(0);

				// remove all blank lines at the end
				while (fileLines.Count != 0 && string.IsNullOrWhiteSpace(fileLines[fileLines.Count - 1]))
					fileLines.RemoveAt(fileLines.Count - 1);

				outputFiles.Add(CreateFile(fileName.Trim(), code =>
				{
					foreach (var fileLine in fileLines)
						code.WriteLine(fileLine);
				}));
			}

			var codeGenComment = CodeGenUtility.GetCodeGenComment(GeneratorName ?? "");
			var patternsToClean = new[]
			{
				new CodeGenPattern("*.md", codeGenComment),
			};
			return new CodeGenOutput(outputFiles, patternsToClean);
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

		internal static string RenderFieldTypeAsJsonValue(ServiceTypeInfo typeInfo) =>
			typeInfo.Kind switch
			{
				ServiceTypeKind.String => "\"(string)\"",
				ServiceTypeKind.Boolean => "(true|false)",
				ServiceTypeKind.Double => "(number)",
				ServiceTypeKind.Decimal => "(number)",
				ServiceTypeKind.Int32 => "(integer)",
				ServiceTypeKind.Int64 => "(integer)",
				ServiceTypeKind.Bytes => "\"(base64)\"",
				ServiceTypeKind.Object => "{ ... }",
				ServiceTypeKind.Error => "{ \"code\": ... }",
				ServiceTypeKind.Dto => RenderDtoAsJsonValue(typeInfo.Dto!),
				ServiceTypeKind.Enum => RenderEnumAsJsonValue(typeInfo.Enum!),
				ServiceTypeKind.Result => $"{{ \"value\": {RenderFieldTypeAsJsonValue(typeInfo.ValueType!)} | \"error\": {{ \"code\": ... }} }}",
				ServiceTypeKind.Array => $"[ {RenderFieldTypeAsJsonValue(typeInfo.ValueType!)}, ... ]",
				ServiceTypeKind.Map => $"{{ \"...\": {RenderFieldTypeAsJsonValue(typeInfo.ValueType!)}, ... }}",
				_ => throw new ArgumentOutOfRangeException()
			};

		private static string RenderDtoAsJsonValue(ServiceDtoInfo dtoInfo)
		{
			var visibleFields = dtoInfo.Fields.Where(x => !x.IsObsolete).ToList();
			return visibleFields.Count == 0 ? "{}" : $"{{ \"{visibleFields[0].Name}\": ... }}";
		}

		private static string RenderEnumAsJsonValue(ServiceEnumInfo enumInfo)
		{
			const int maxValues = 3;
			var values = enumInfo.Values.Where(x => !x.IsObsolete).ToList();
			return values.Count == 1 ? $"\"{values[0].Name}\"" :
				"\"(" + string.Join("|", values.Select(x => x.Name).Take(maxValues)) + (values.Count > maxValues ? "|..." : "") + ")\"";
		}

		internal static string RenderFieldType(ServiceTypeInfo typeInfo) =>
			typeInfo.Kind switch
			{
				ServiceTypeKind.String => "string",
				ServiceTypeKind.Boolean => "boolean",
				ServiceTypeKind.Double => "double",
				ServiceTypeKind.Int32 => "int32",
				ServiceTypeKind.Int64 => "int64",
				ServiceTypeKind.Decimal => "decimal",
				ServiceTypeKind.Bytes => "bytes",
				ServiceTypeKind.Object => "object",
				ServiceTypeKind.Error => "error",
				ServiceTypeKind.Dto => $"[{typeInfo.Dto!.Name}]({typeInfo.Dto.Name}.md)",
				ServiceTypeKind.Enum => $"[{typeInfo.Enum!.Name}]({typeInfo.Enum.Name}.md)",
				ServiceTypeKind.Result => $"result<{RenderFieldType(typeInfo.ValueType!)}>",
				ServiceTypeKind.Array => $"{RenderFieldType(typeInfo.ValueType!)}[]",
				ServiceTypeKind.Map => $"map<{RenderFieldType(typeInfo.ValueType!)}>",
				_ => throw new ArgumentOutOfRangeException()
			};

		private static string GetEmbeddedResourceText(string name)
		{
			using var reader = new StreamReader(typeof(MarkdownGenerator).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException());
			return reader.ReadToEnd();
		}
	}
}
