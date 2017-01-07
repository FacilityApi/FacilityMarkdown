using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Http;

namespace Facility.Markdown
{
	/// <summary>
	/// Generates Markdown.
	/// </summary>
	public sealed class MarkdownGenerator : CodeGenerator
	{
		/// <summary>
		/// True if HTTP documentation should be omitted.
		/// </summary>
		public bool NoHttp { get; set; }

		/// <summary>
		/// Generates the Markdown.
		/// </summary>
		protected override CodeGenOutput GenerateOutputCore(ServiceInfo serviceInfo)
		{
			var outputFiles = new List<NamedText>();

			var httpServiceInfo = new HttpServiceInfo(serviceInfo);

			outputFiles.Add(GenerateService(serviceInfo, httpServiceInfo));

			foreach (ServiceMethodInfo methodInfo in serviceInfo.Methods.Where(x => !x.IsObsolete()))
				outputFiles.Add(GenerateMethod(methodInfo, serviceInfo, httpServiceInfo));

			foreach (ServiceDtoInfo dtoInfo in serviceInfo.Dtos.Where(x => !x.IsObsolete()))
				outputFiles.Add(GenerateDto(dtoInfo, serviceInfo, httpServiceInfo));

			foreach (ServiceEnumInfo enumInfo in serviceInfo.Enums.Where(x => !x.IsObsolete()))
				outputFiles.Add(GenerateEnum(enumInfo));

			foreach (ServiceErrorSetInfo errorSetInfo in serviceInfo.ErrorSets.Where(x => !x.IsObsolete()))
				outputFiles.Add(GenerateErrorSet(errorSetInfo));

			string codeGenComment = CodeGenUtility.GetCodeGenComment(GeneratorName);
			var patternsToClean = new[]
			{
				new CodeGenPattern("*.md", codeGenComment),
			};
			return new CodeGenOutput(outputFiles, patternsToClean);
		}

		private NamedText GenerateService(ServiceInfo serviceInfo, HttpServiceInfo httpServiceInfo)
		{
			string serviceName = serviceInfo.Name;

			return CreateNamedText("README.md", code =>
			{
				code.WriteLine($"## {serviceName}");

				code.WriteLine();
				WriteSummary(code, serviceInfo.Summary);

				WriteRemarks(code, serviceInfo.Remarks);

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
						foreach (var methodInfo in httpServiceInfo.Methods.Where(x => !x.ServiceMethod.IsObsolete()))
						{
							code.WriteLine($"| [{methodInfo.ServiceMethod.Name}]({methodInfo.ServiceMethod.Name}.md) | " +
								$"`{methodInfo.Method.ToString().ToUpperInvariant()} {methodInfo.Path}` | {methodInfo.ServiceMethod.Summary} |");
						}
					}
					else
					{
						code.WriteLine();
						code.WriteLine("| method | description |");
						code.WriteLine("| --- | --- |");
						foreach (var methodInfo in serviceInfo.Methods.Where(x => !x.IsObsolete()))
							code.WriteLine($"| [{methodInfo.Name}]({methodInfo.Name}.md) | {methodInfo.Summary} |");
					}
				}

				if (serviceInfo.Dtos.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| data | description |");
					code.WriteLine("| --- | --- |");
					foreach (var dtoInfo in serviceInfo.Dtos.Where(x => !x.IsObsolete()))
						code.WriteLine($"| [{dtoInfo.Name}]({dtoInfo.Name}.md) | {dtoInfo.Summary} |");
				}

				if (serviceInfo.Enums.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| enum | description |");
					code.WriteLine("| --- | --- |");
					foreach (var enumInfo in serviceInfo.Enums.Where(x => !x.IsObsolete()))
						code.WriteLine($"| [{enumInfo.Name}]({enumInfo.Name}.md) | {enumInfo.Summary} |");
				}

				if (serviceInfo.ErrorSets.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| errors | description |");
					code.WriteLine("| --- | --- |");
					foreach (var errorSetInfo in serviceInfo.ErrorSets.Where(x => !x.IsObsolete()))
						code.WriteLine($"| [{errorSetInfo.Name}]({errorSetInfo.Name}.md) | {errorSetInfo.Summary} |");
				}

				WriteCodeGenComment(code);
			});
		}

		private NamedText GenerateMethod(ServiceMethodInfo methodInfo, ServiceInfo serviceInfo, HttpServiceInfo httpServiceInfo)
		{
			return CreateNamedText(methodInfo.Name + ".md", code =>
			{
				code.WriteLine($"## {methodInfo.Name}");

				code.WriteLine();
				WriteSummary(code, methodInfo.Summary);

				var httpMethodInfo = httpServiceInfo?.Methods.FirstOrDefault(x => x.ServiceMethod == methodInfo);
				if (httpMethodInfo != null)
				{
					code.WriteLine();
					code.WriteLine("```");

					code.WriteLine($"{httpMethodInfo.Method} {httpMethodInfo.Path}");
					var queryFields = httpMethodInfo.QueryFields.Where(x => !x.ServiceField.IsObsolete()).ToList();
					for (int queryIndex = 0; queryIndex < queryFields.Count; queryIndex++)
					{
						var queryInfo = queryFields[queryIndex];
						string prefix = queryIndex == 0 ? "?" : "&";
						code.WriteLine($"  {prefix}{queryInfo.Name}={{{queryInfo.ServiceField.Name}}}");
					}

					foreach (var headerField in httpMethodInfo.RequestHeaderFields)
						code.WriteLine($"{headerField.Name}: ({headerField.ServiceField.Name})");

					if (httpMethodInfo.RequestBodyField != null)
					{
						code.WriteLine($"({httpMethodInfo.RequestBodyField.ServiceField.Name})");
					}
					else if (httpMethodInfo.RequestNormalFields.Count != 0)
					{
						code.WriteLine("{");
						var fields = httpMethodInfo.RequestNormalFields.Where(x => !x.ServiceField.IsObsolete()).ToList();
						for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
						{
							var fieldInfo = fields[fieldIndex].ServiceField;
							string jsonValue = RenderFieldTypeAsJsonValue(serviceInfo.GetFieldType(fieldInfo));
							string suffix = fieldIndex == fields.Count - 1 ? "" : ",";
							code.WriteLine($"  \"{fieldInfo.Name}\": {jsonValue}{suffix}");
						}
						code.WriteLine("}");
					}

					if (httpMethodInfo.ResponseHeaderFields.Count != 0)
					{
						code.WriteLine("--- response");
						foreach (var headerField in httpMethodInfo.ResponseHeaderFields)
							code.WriteLine($"{headerField.Name}: ({headerField.ServiceField.Name})");
					}

					foreach (var validResponse in httpMethodInfo.ValidResponses)
					{
						HttpStatusCode statusCode = validResponse.StatusCode;
						string statusCodeString = ((int) statusCode).ToString(CultureInfo.InvariantCulture);
						string reasonPhrase = new HttpResponseMessage(statusCode).ReasonPhrase;

						code.WriteLine($"--- {statusCodeString} {reasonPhrase}");

						if (validResponse.BodyField != null)
						{
							string prefix = serviceInfo.GetFieldType(validResponse.BodyField.ServiceField).Kind == ServiceTypeKind.Boolean ? "if " : "";
							code.WriteLine($"({prefix}{validResponse.BodyField.ServiceField.Name})");
						}
						else if (validResponse.NormalFields.Count != 0)
						{
							code.WriteLine("{");
							var fields = validResponse.NormalFields.Where(x => !x.ServiceField.IsObsolete()).ToList();
							for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
							{
								var fieldInfo = fields[fieldIndex].ServiceField;
								string jsonValue = RenderFieldTypeAsJsonValue(serviceInfo.GetFieldType(fieldInfo));
								string suffix = fieldIndex == fields.Count - 1 ? "" : ",";
								code.WriteLine($"  \"{fieldInfo.Name}\": {jsonValue}{suffix}");
							}
							code.WriteLine("}");
						}
					}

					code.WriteLine("```");
				}

				var requestFields = methodInfo.RequestFields.Where(x => !x.IsObsolete()).ToList();
				if (requestFields.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| request | type | description |");
					code.WriteLine("| --- | --- | --- |");
					foreach (var fieldInfo in requestFields)
						code.WriteLine($"| {fieldInfo.Name} | {RenderFieldType(serviceInfo.GetFieldType(fieldInfo))} | {fieldInfo.Summary} |");
				}

				var responseFields = methodInfo.ResponseFields.Where(x => !x.IsObsolete()).ToList();
				if (responseFields.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| response | type | description |");
					code.WriteLine("| --- | --- | --- |");
					foreach (var fieldInfo in responseFields.Where(x => !x.IsObsolete()))
						code.WriteLine($"| {fieldInfo.Name} | {RenderFieldType(serviceInfo.GetFieldType(fieldInfo))} | {fieldInfo.Summary} |");
				}

				WriteRemarks(code, methodInfo.Remarks);

				WriteCodeGenComment(code);
			});
		}

		private NamedText GenerateDto(ServiceDtoInfo dtoInfo, ServiceInfo serviceInfo, HttpServiceInfo httpServiceInfo)
		{
			return CreateNamedText(dtoInfo.Name + ".md", code =>
			{
				code.WriteLine($"## {dtoInfo.Name}");

				code.WriteLine();
				code.WriteLine(dtoInfo.Summary);

				var fields = dtoInfo.Fields.Where(x => !x.IsObsolete()).ToList();

				if (httpServiceInfo != null)
				{
					code.WriteLine();
					code.WriteLine("```");
					code.WriteLine("{");
					for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
					{
						var fieldInfo = fields[fieldIndex];
						string jsonValue = RenderFieldTypeAsJsonValue(serviceInfo.GetFieldType(fieldInfo));
						string suffix = fieldIndex == fields.Count - 1 ? "" : ",";
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
						code.WriteLine($"| {fieldInfo.Name} | {RenderFieldType(serviceInfo.GetFieldType(fieldInfo))} | {fieldInfo.Summary} |");
				}

				WriteRemarks(code, dtoInfo.Remarks);

				WriteCodeGenComment(code);
			});
		}

		private NamedText GenerateEnum(ServiceEnumInfo enumInfo)
		{
			return CreateNamedText(enumInfo.Name + ".md", code =>
			{
				code.WriteLine($"## {enumInfo.Name}");

				code.WriteLine();
				code.WriteLine(enumInfo.Summary);

				if (enumInfo.Values.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| name | description |");
					code.WriteLine("| --- | --- |");
					foreach (var enumValue in enumInfo.Values.Where(x => !x.IsObsolete()))
						code.WriteLine($"| {enumValue.Name} | {enumValue.Summary} |");
				}

				WriteRemarks(code, enumInfo.Remarks);

				WriteCodeGenComment(code);
			});
		}

		private NamedText GenerateErrorSet(ServiceErrorSetInfo errorSetInfo)
		{
			return CreateNamedText(errorSetInfo.Name + ".md", code =>
			{
				code.WriteLine($"## {errorSetInfo.Name}");

				code.WriteLine();
				code.WriteLine(errorSetInfo.Summary);

				if (errorSetInfo.Errors.Count != 0)
				{
					code.WriteLine();
					code.WriteLine("| error | description |");
					code.WriteLine("| --- | --- |");
					foreach (var errorInfo in errorSetInfo.Errors.Where(x => !x.IsObsolete()))
						code.WriteLine($"| {errorInfo.Name} | {errorInfo.Summary} |");
				}

				WriteRemarks(code, errorSetInfo.Remarks);

				WriteCodeGenComment(code);
			});
		}

		private string RenderFieldTypeAsJsonValue(ServiceTypeInfo typeInfo)
		{
			switch (typeInfo.Kind)
			{
			case ServiceTypeKind.String:
				return "\"(string)\"";
			case ServiceTypeKind.Boolean:
				return "(true|false)";
			case ServiceTypeKind.Double:
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
				return RenderDtoAsJsonValue(typeInfo.Dto);
			case ServiceTypeKind.Enum:
				return RenderEnumAsJsonValue(typeInfo.Enum);
			case ServiceTypeKind.Result:
				return $"{{ \"value\": {RenderFieldTypeAsJsonValue(typeInfo.ValueType)} | \"error\": {{ \"code\": ... }} }}";
			case ServiceTypeKind.Array:
				return $"[ {RenderFieldTypeAsJsonValue(typeInfo.ValueType)}, ... ]";
			case ServiceTypeKind.Map:
				return $"{{ \"...\": {RenderFieldTypeAsJsonValue(typeInfo.ValueType)}, ... }}";
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		private string RenderDtoAsJsonValue(ServiceDtoInfo dtoInfo)
		{
			var visibleFields = dtoInfo.Fields.Where(x => !x.IsObsolete()).ToList();
			return visibleFields.Count == 0 ? "{}" : $"{{ \"{visibleFields[0].Name}\": ... }}";
		}

		private string RenderEnumAsJsonValue(ServiceEnumInfo enumInfo)
		{
			const int maxValues = 3;
			var values = enumInfo.Values.Where(x => !x.IsObsolete()).ToList();
			return "\"(" + string.Join("|", values.Select(x => x.Name).Take(maxValues)) + (values.Count > maxValues ? "|..." : "") + ")\"";
		}

		private string RenderFieldType(ServiceTypeInfo typeInfo)
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
			case ServiceTypeKind.Bytes:
				return "bytes";
			case ServiceTypeKind.Object:
				return "object";
			case ServiceTypeKind.Error:
				return "error";
			case ServiceTypeKind.Dto:
				return $"[{typeInfo.Dto.Name}]({typeInfo.Dto.Name}.md)";
			case ServiceTypeKind.Enum:
				return $"[{typeInfo.Enum.Name}]({typeInfo.Enum.Name}.md)";
			case ServiceTypeKind.Result:
				return $"result<{RenderFieldType(typeInfo.ValueType)}>";
			case ServiceTypeKind.Array:
				return $"{RenderFieldType(typeInfo.ValueType)}[]";
			case ServiceTypeKind.Map:
				return $"map<{RenderFieldType(typeInfo.ValueType)}>";
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
			code.WriteLine($"<!-- {CodeGenUtility.GetCodeGenComment(GeneratorName)} -->");
		}
	}
}
