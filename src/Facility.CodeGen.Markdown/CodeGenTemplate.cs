using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Scriban;

namespace CodeGenCore
{
	public sealed class CodeGenTemplate
	{
		public static CodeGenTemplate Parse(string text) => new(Template.Parse(text));

		public IReadOnlyList<CodeGenOutputFile> Generate(CodeGenGlobals globals, CodeGenSettings settings)
		{
			if (globals is null)
				throw new ArgumentNullException(nameof(globals));
			if (settings is null)
				throw new ArgumentNullException(nameof(settings));

			var newLine = settings.NewLine ?? Environment.NewLine;
			var indentText = settings.IndentText;
			string? templateIndentText = null;

			var context = new TemplateContext
			{
				StrictVariables = true,
				MemberRenamer = x => x.Name,
			};
			context.PushCulture(new CultureInfo("en-US"));
			context.PushGlobal(globals.ScriptObject);

			var text = Template.Render(context);
			using var reader = new StringReader(text);

			// find first file
			var fileStart = "";
			string? line;
			while ((line = reader.ReadLine()) != null)
			{
				var match = Regex.Match(line, @"^==+>");
				if (match.Success)
				{
					fileStart = match.Value;
					break;
				}
			}

			var files = new List<CodeGenOutputFile>();

			while (line != null)
			{
				var fileName = line.Substring(fileStart.Length);

				var fileLines = new List<string>();
				while ((line = reader.ReadLine()) != null && !line.StartsWith(fileStart, StringComparison.Ordinal))
				{
					line = line.TrimEnd();

					if (indentText != null)
					{
						var indentMatch = s_indentRegex.Match(line);
						if (indentMatch.Success)
						{
							templateIndentText ??= indentMatch.Value;
							var indent = indentMatch.Length / templateIndentText.Length;
							var lineBuilder = new StringBuilder();
							for (var i = 0; i < indent; i++)
								lineBuilder.Append(indentText);
							lineBuilder.Append(line.Substring(templateIndentText.Length * indent));
							line = lineBuilder.ToString();
						}
					}

					fileLines.Add(line);
				}

				// skip exactly one blank line to allow file start to stand out
				if (fileLines.Count != 0 && string.IsNullOrWhiteSpace(fileLines[0]))
					fileLines.RemoveAt(0);

				// remove all blank lines at the end
				while (fileLines.Count != 0 && string.IsNullOrWhiteSpace(fileLines[fileLines.Count - 1]))
					fileLines.RemoveAt(fileLines.Count - 1);

				// build text from lines
				using var stringWriter = new StringWriter { NewLine = newLine };
				foreach (var fileLine in fileLines)
					stringWriter.WriteLine(fileLine);
				files.Add(new CodeGenOutputFile(name: fileName.Trim(), text: stringWriter.ToString()));
			}

			return files;
		}

		private Template Template { get; }

		private CodeGenTemplate(Template template) => Template = template;

		private static readonly Regex s_indentRegex = new(@"^[ \t]+", RegexOptions.Compiled);
	}
}
