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

		internal static string RenderFieldTypeAsJsonValue(ServiceTypeInfo typeInfo)
		{
			switch (typeInfo.Kind)
			{
			case ServiceTypeKind.String:
				return "\"(string)\"";
			case ServiceTypeKind.Boolean:
				return "(true|false)";
			case ServiceTypeKind.Double:
			case ServiceTypeKind.Decimal:
				return "(number)";
			case ServiceTypeKind.Int32:
			case ServiceTypeKind.Int64:
				return "(integer)";
			case ServiceTypeKind.Bytes:
				return "\"(base64)\"";
			case ServiceTypeKind.Object:
				return "{ ... }";
			case ServiceTypeKind.Error:
				return "{ \"code\": ... }";
			case ServiceTypeKind.Dto:
				return RenderDtoAsJsonValue(typeInfo.Dto!);
			case ServiceTypeKind.Enum:
				return RenderEnumAsJsonValue(typeInfo.Enum!);
			case ServiceTypeKind.Result:
				return $"{{ \"value\": {RenderFieldTypeAsJsonValue(typeInfo.ValueType!)} | \"error\": {{ \"code\": ... }} }}";
			case ServiceTypeKind.Array:
				return $"[ {RenderFieldTypeAsJsonValue(typeInfo.ValueType!)}, ... ]";
			case ServiceTypeKind.Map:
				return $"{{ \"...\": {RenderFieldTypeAsJsonValue(typeInfo.ValueType!)}, ... }}";
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

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

		internal static string RenderFieldType(ServiceTypeInfo typeInfo)
		{
			switch (typeInfo.Kind)
			{
			case ServiceTypeKind.String:
				return "string";
			case ServiceTypeKind.Boolean:
				return "boolean";
			case ServiceTypeKind.Double:
				return "double";
			case ServiceTypeKind.Int32:
				return "int32";
			case ServiceTypeKind.Int64:
				return "int64";
			case ServiceTypeKind.Decimal:
				return "decimal";
			case ServiceTypeKind.Bytes:
				return "bytes";
			case ServiceTypeKind.Object:
				return "object";
			case ServiceTypeKind.Error:
				return "error";
			case ServiceTypeKind.Dto:
				return $"[{typeInfo.Dto!.Name}]({typeInfo.Dto.Name}.md)";
			case ServiceTypeKind.Enum:
				return $"[{typeInfo.Enum!.Name}]({typeInfo.Enum.Name}.md)";
			case ServiceTypeKind.Result:
				return $"result<{RenderFieldType(typeInfo.ValueType!)}>";
			case ServiceTypeKind.Array:
				return $"{RenderFieldType(typeInfo.ValueType!)}[]";
			case ServiceTypeKind.Map:
				return $"map<{RenderFieldType(typeInfo.ValueType!)}>";
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		private void WriteSummary(CodeWriter code, string summary)
		{
			if (!string.IsNullOrWhiteSpace(summary))
				code.WriteLine(summary);
		}

		private void WriteRemarks(CodeWriter code, IReadOnlyList<string> remarks)
		{
			if (remarks != null && remarks.Count != 0)
			{
				code.WriteLine();
				foreach (var remark in remarks)
					code.WriteLine(Regex.Replace(remark, @"\[([^\]]+)\]\(\)", "[$1]($1.md)"));
			}
		}

		private void WriteCodeGenComment(CodeWriter code)
		{
			code.WriteLine();
			code.WriteLine($"<!-- {CodeGenUtility.GetCodeGenComment(GeneratorName ?? "")} -->");
		}

		private static string GetEmbeddedResourceText(string name)
		{
			using var reader = new StreamReader(typeof(MarkdownGenerator).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException());
			return reader.ReadToEnd();
		}
	}
}
