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

		var generator = new MarkdownGenerator { GeneratorName = nameof(MarkdownGeneratorTests) };
		generator.GenerateOutput(service);

		generator.NoHttp = true;
		generator.GenerateOutput(service);
	}

	[Test]
	public void DtoWithExternDataType()
	{
		const string definition = @"service TestApi { extern data Thing; data Test {
/// This is a description.
thing: Thing; } }";
		var parser = new FsdParser();
		var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
		var generator = new MarkdownGenerator { GeneratorName = nameof(MarkdownGeneratorTests) };

		var output = generator.GenerateOutput(service);

		var file = output.Files.First(x => x.Name == "TestApi/Test.md");
		StringAssert.Contains("\"thing\": (Thing)", file.Text);
		StringAssert.Contains("| thing | Thing | This is a description. |", file.Text);
	}

	[Test]
	public void DtoWithExternEnumType()
	{
		const string definition = @"service TestApi { extern enum Kind; data Test {
/// This is a description.
kind: Kind; } }";
		var parser = new FsdParser();
		var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
		var generator = new MarkdownGenerator { GeneratorName = nameof(MarkdownGeneratorTests) };

		var output = generator.GenerateOutput(service);

		var file = output.Files.First(x => x.Name == "TestApi/Test.md");
		StringAssert.Contains("\"kind\": (Kind)", file.Text);
		StringAssert.Contains("| kind | Kind | This is a description. |", file.Text);
	}
}
