using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Http;

namespace Facility.CodeGen.Markdown
{
	public sealed class CodeTemplateGlobals
	{
		public CodeTemplateGlobals(MarkdownGenerator generator, ServiceInfo serviceInfo, HttpServiceInfo? httpServiceInfo)
		{
			Service = serviceInfo;
			HttpService = httpServiceInfo;
			CodeGenCommentText = CodeGenUtility.GetCodeGenComment(generator.GeneratorName ?? "");
		}

		public ServiceInfo Service { get; }

		public HttpServiceInfo? HttpService { get; }

		public string CodeGenCommentText { get; }

		public HttpElementInfo? GetHttp(ServiceElementInfo methodInfo) =>
			HttpService?.Methods.FirstOrDefault(x => x.ServiceMethod == methodInfo);

		public ServiceTypeInfo? GetFieldType(ServiceFieldInfo field) => Service.GetFieldType(field);

		public static string RenderFieldType(ServiceTypeInfo typeInfo) => MarkdownGenerator.RenderFieldType(typeInfo);

		public static string RenderFieldTypeAsJsonValue(ServiceTypeInfo typeInfo) => MarkdownGenerator.RenderFieldTypeAsJsonValue(typeInfo);

		public IEnumerable WhereNotObsolete(IEnumerable items)
		{
			foreach (var item in items)
			{
				if (item is ServiceElementWithAttributesInfo withAttributes)
				{
					if (!withAttributes.IsObsolete)
						yield return item;
				}
				else if (item is HttpMethodInfo httpMethod)
				{
					if (!httpMethod.ServiceMethod.IsObsolete)
						yield return item;
				}
				else if (item is HttpFieldInfo httpField)
				{
					if (!httpField.ServiceField.IsObsolete)
						yield return item;
				}
				else
				{
					throw new InvalidOperationException("WhereNotObsolete: Unsupported type " + item.GetType().Name);
				}
			}
		}

		public static string StatusCodePhrase(HttpStatusCode statusCode)
		{
			s_reasonPhrases.TryGetValue((int) statusCode, out var reasonPhrase);
			return reasonPhrase;
		}

		private static readonly Dictionary<int, string> s_reasonPhrases = new Dictionary<int, string>
		{
			{ 100, "Continue" },
			{ 101, "Switching Protocols" },
			{ 200, "OK" },
			{ 201, "Created" },
			{ 202, "Accepted" },
			{ 203, "Non-Authoritative Information" },
			{ 204, "No Content" },
			{ 205, "Reset Content" },
			{ 206, "Partial Content" },
			{ 300, "Multiple Choices" },
			{ 301, "Moved Permanently" },
			{ 302, "Found" },
			{ 303, "See Other" },
			{ 304, "Not Modified" },
			{ 305, "Use Proxy" },
			{ 307, "Temporary Redirect" },
			{ 400, "Bad Request" },
			{ 401, "Unauthorized" },
			{ 402, "Payment Required" },
			{ 403, "Forbidden" },
			{ 404, "Not Found" },
			{ 405, "Method Not Allowed" },
			{ 406, "Not Acceptable" },
			{ 407, "Proxy Authentication Required" },
			{ 408, "Request Timeout" },
			{ 409, "Conflict" },
			{ 410, "Gone" },
			{ 411, "Length Required" },
			{ 412, "Precondition Failed" },
			{ 413, "Request Entity Too Large" },
			{ 414, "Request-Uri Too Long" },
			{ 415, "Unsupported Media Type" },
			{ 416, "Requested Range Not Satisfiable" },
			{ 417, "Expectation Failed" },
			{ 426, "Upgrade Required" },
			{ 500, "Internal Server Error" },
			{ 501, "Not Implemented" },
			{ 502, "Bad Gateway" },
			{ 503, "Service Unavailable" },
			{ 504, "Gateway Timeout" },
			{ 505, "Http Version Not Supported" },
		};
	}
}
