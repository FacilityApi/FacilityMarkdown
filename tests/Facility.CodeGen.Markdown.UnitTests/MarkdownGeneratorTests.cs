using System.Reflection;
using Facility.Definition;
using Facility.Definition.Fsd;
using NUnit.Framework;

namespace Facility.CodeGen.Markdown.UnitTests;

public sealed class MarkdownGeneratorTests
{
	[Test]
	public void GenerateExampleApiSuccess()
	{
		ServiceInfo service;
		const string fileName = "Facility.CodeGen.Markdown.UnitTests.ConformanceApi.fsd";
		var parser = new FsdParser();
		var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(fileName);
		Assert.IsNotNull(stream);
		using (var reader = new StreamReader(stream!))
			service = parser.ParseDefinition(new ServiceDefinitionText(Path.GetFileName(fileName), reader.ReadToEnd()));

		var generator = new MarkdownGenerator
		{
			GeneratorName = "MarkdownGeneratorTests",
		};
		generator.GenerateOutput(service);

		generator.NoHttp = true;
		generator.GenerateOutput(service);
	}
}
