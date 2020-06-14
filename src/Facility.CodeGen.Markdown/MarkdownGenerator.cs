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

			outputFiles.Add(GenerateService(serviceInfo, httpServiceInfo));

			foreach (var methodInfo in serviceInfo.Methods.Where(x => !x.IsObsolete))
				outputFiles.Add(GenerateMethod(methodInfo, serviceInfo, httpServiceInfo));

			foreach (var dtoInfo in serviceInfo.Dtos.Where(x => !x.IsObsolete))
				outputFiles.Add(GenerateDto(dtoInfo, serviceInfo, httpServiceInfo));

			foreach (var enumInfo in serviceInfo.Enums.Where(x => !x.IsObsolete))
				outputFiles.Add(GenerateEnum(enumInfo, serviceInfo));

			foreach (var errorSetInfo in serviceInfo.ErrorSets.Where(x => !x.IsObsolete))
				outputFiles.Add(GenerateErrorSet(errorSetInfo, serviceInfo));

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

		private CodeGenFile GenerateService(ServiceInfo serviceInfo, HttpServiceInfo? httpServiceInfo)
		{
			var serviceName = serviceInfo.Name;

			return CreateFile($"{serviceName}.md", code =>
			{
				code.WriteLine($"# {serviceName}");

				code.WriteLine();
				WriteSummary(code, serviceInfo.Summary);

				if (httpServiceInfo?.Url != null)
				{
					code.WriteLine();
					code.WriteLine($"URL: [`{httpServiceInfo.Url}`]({httpServiceInfo.Url})");
				}

				if (serviceInfo.Methods.Count != 0)
				{
					if (httpServiceInfo != null)
					{
						code.WriteLine();
						code.WriteLine("| method | path | description |");
						code.WriteLine("| --- | --- | --- |");
						foreach (var methodInfo in httpServiceInfo.Methods.Where(x => !x.ServiceMethod.IsObsolete))
						{
							code.WriteLine($"| [{methodInfo.ServiceMethod.Name}]({serviceName}/{methodInfo.ServiceMethod.Name}.md) | " +
								$"`{methodInfo.Method.ToUpperInvariant()} {methodInfo.Path}` | {methodInfo.ServiceMethod.Summary} |");
						}
					}
					else
					{
						code.WriteLine();
						code.WriteLine("| method | description |");
						code.WriteLine("| --- | --- |");
						foreach (var methodInfo in serviceInfo.Methods.Where(x => !x.IsObsolete))
							code.WriteLine($"| [{methodInfo.Name}]({serviceName}/{methodInfo.Name}.md) | {methodInfo.Summary} |");
					}
				}

				if (serviceInfo.Dtos.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| data | description |");
					code.WriteLine("| --- | --- |");
					foreach (var dtoInfo in serviceInfo.Dtos.Where(x => !x.IsObsolete))
						code.WriteLine($"| [{dtoInfo.Name}]({serviceName}/{dtoInfo.Name}.md) | {dtoInfo.Summary} |");
				}

				if (serviceInfo.Enums.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| enum | description |");
					code.WriteLine("| --- | --- |");
					foreach (var enumInfo in serviceInfo.Enums.Where(x => !x.IsObsolete))
						code.WriteLine($"| [{enumInfo.Name}]({serviceName}/{enumInfo.Name}.md) | {enumInfo.Summary} |");
				}

				if (serviceInfo.ErrorSets.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| errors | description |");
					code.WriteLine("| --- | --- |");
					foreach (var errorSetInfo in serviceInfo.ErrorSets.Where(x => !x.IsObsolete))
						code.WriteLine($"| [{errorSetInfo.Name}]({serviceName}/{errorSetInfo.Name}.md) | {errorSetInfo.Summary} |");
				}

				WriteRemarks(code, serviceInfo.Remarks);

				WriteCodeGenComment(code);
			});
		}

		private CodeGenFile GenerateMethod(ServiceMethodInfo methodInfo, ServiceInfo serviceInfo, HttpServiceInfo? httpServiceInfo)
		{
			var serviceName = serviceInfo.Name;

			return CreateFile($"{serviceName}/{methodInfo.Name}.md", code =>
			{
				var templateText = GetEmbeddedResourceText("Facility.CodeGen.Markdown.template.scriban-txt");

				code.Write(CodeTemplateUtility.Render(templateText,
					new CodeTemplateGlobals(this, methodInfo, serviceInfo, httpServiceInfo)));
			});
		}

		private CodeGenFile GenerateDto(ServiceDtoInfo dtoInfo, ServiceInfo serviceInfo, HttpServiceInfo? httpServiceInfo)
		{
			var serviceName = serviceInfo.Name;

			return CreateFile($"{serviceName}/{dtoInfo.Name}.md", code =>
			{
				code.WriteLine($"# {dtoInfo.Name}");

				code.WriteLine();
				code.WriteLine(dtoInfo.Summary);

				var fields = dtoInfo.Fields.Where(x => !x.IsObsolete).ToList();

				if (httpServiceInfo != null)
				{
					code.WriteLine();
					code.WriteLine("```");
					code.WriteLine("{");
					for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
					{
						var fieldInfo = fields[fieldIndex];
						var jsonValue = RenderFieldTypeAsJsonValue(serviceInfo.GetFieldType(fieldInfo)!);
						var suffix = fieldIndex == fields.Count - 1 ? "" : ",";
						code.WriteLine($"  \"{fieldInfo.Name}\": {jsonValue}{suffix}");
					}
					code.WriteLine("}");
					code.WriteLine("```");
				}

				if (fields.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| field | type | description |");
					code.WriteLine("| --- | --- | --- |");
					foreach (var fieldInfo in fields)
						code.WriteLine($"| {fieldInfo.Name} | {RenderFieldType(serviceInfo.GetFieldType(fieldInfo)!)} | {fieldInfo.Summary} |");
				}

				WriteRemarks(code, dtoInfo.Remarks);

				WriteCodeGenComment(code);
			});
		}

		private CodeGenFile GenerateEnum(ServiceEnumInfo enumInfo, ServiceInfo serviceInfo)
		{
			var serviceName = serviceInfo.Name;

			return CreateFile($"{serviceName}/{enumInfo.Name}.md", code =>
			{
				code.WriteLine($"# {enumInfo.Name}");

				code.WriteLine();
				code.WriteLine(enumInfo.Summary);

				if (enumInfo.Values.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| name | description |");
					code.WriteLine("| --- | --- |");
					foreach (var enumValue in enumInfo.Values.Where(x => !x.IsObsolete))
						code.WriteLine($"| {enumValue.Name} | {enumValue.Summary} |");
				}

				WriteRemarks(code, enumInfo.Remarks);

				WriteCodeGenComment(code);
			});
		}

		private CodeGenFile GenerateErrorSet(ServiceErrorSetInfo errorSetInfo, ServiceInfo serviceInfo)
		{
			var serviceName = serviceInfo.Name;

			return CreateFile($"{serviceName}/{errorSetInfo.Name}.md", code =>
			{
				code.WriteLine($"# {errorSetInfo.Name}");

				code.WriteLine();
				code.WriteLine(errorSetInfo.Summary);

				if (errorSetInfo.Errors.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| error | description |");
					code.WriteLine("| --- | --- |");
					foreach (var errorInfo in errorSetInfo.Errors.Where(x => !x.IsObsolete))
						code.WriteLine($"| {errorInfo.Name} | {errorInfo.Summary} |");
				}

				WriteRemarks(code, errorSetInfo.Remarks);

				WriteCodeGenComment(code);
			});
		}

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
