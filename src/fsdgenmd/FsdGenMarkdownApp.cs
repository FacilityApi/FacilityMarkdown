using ArgsReading;
using Facility.CodeGen.Console;
using Facility.CodeGen.Markdown;
using Facility.Definition.CodeGen;

namespace fsdgenmd;

public sealed class FsdGenMarkdownApp : CodeGeneratorApp
{
	public static int Main(string[] args)
	{
		return new FsdGenMarkdownApp().Run(args);
	}

	protected override IReadOnlyList<string> Description => new[]
	{
		"Generates Markdown for a Facility Service Definition.",
	};

	protected override IReadOnlyList<string> ExtraUsage => new[]
	{
		"   --no-http",
		"      Omit HTTP documentation.",
	};

	protected override CodeGenerator CreateGenerator() => new MarkdownGenerator();

	protected override FileGeneratorSettings CreateSettings(ArgsReader args) =>
		new MarkdownGeneratorSettings { NoHttp = args.ReadFlag("no-http") };
}
