using System.Collections.Generic;
using Facility.Console;
using Facility.Markdown;
using Facility.Definition.CodeGen;

namespace fsdgenmd
{
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
			"   --nohttp",
			"      Omit HTTP documentation.",
		};

		protected override CodeGenerator CreateGenerator(ArgsReader args)
		{
			return new MarkdownGenerator
			{
				NoHttp = args.ReadFlag("nohttp"),
			};
		}

		protected override bool SupportsClean => true;
	}
}
