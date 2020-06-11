using System;
using System.Linq;
using Faithlife.Build;
using static Faithlife.Build.BuildUtility;
using static Faithlife.Build.DotNetRunner;

internal static class Build
{
	public static int Main(string[] args) => BuildRunner.Execute(args, build =>
	{
		var codegen = "fsdgenmd";

		var dotNetBuildSettings = new DotNetBuildSettings
		{
			NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
			DocsSettings = new DotNetDocsSettings
			{
				GitLogin = new GitLoginInfo("FacilityApiBot", Environment.GetEnvironmentVariable("BUILD_BOT_PASSWORD") ?? ""),
				GitAuthor = new GitAuthorInfo("FacilityApiBot", "facilityapi@gmail.com"),
				GitBranchName = Environment.GetEnvironmentVariable("APPVEYOR_REPO_BRANCH"),
				SourceCodeUrl = "https://github.com/FacilityApi/FacilityMarkdown/tree/master/src",
				ProjectHasDocs = name => !name.StartsWith("fsdgen", StringComparison.Ordinal),
			},
		};

		build.AddDotNetTargets(dotNetBuildSettings);

		build.Target("codegen")
			.DependsOn("build")
			.Describe("Generates code from the FSD")
			.Does(() => codeGen(verify: false));

		build.Target("verify-codegen")
			.DependsOn("build")
			.Describe("Ensures the generated code is up-to-date")
			.Does(() => codeGen(verify: true));

		build.Target("test")
			.DependsOn("verify-codegen");

		void codeGen(bool verify)
		{
			var configuration = dotNetBuildSettings!.BuildOptions!.ConfigurationOption!.Value;
			var toolPath = FindFiles($"src/{codegen}/bin/{configuration}/netcoreapp*/{codegen}.dll").FirstOrDefault();

			var verifyOption = verify ? "--verify" : null;

			RunDotNet("tool", "restore");
			RunDotNet("tool", "run", "FacilityConformance", "fsd", "--output", "conformance/ConformanceApi.fsd", verifyOption);

			RunDotNet(toolPath, "conformance/ConformanceApi.fsd", "conformance/http/", "--newline", "lf", verifyOption);
			RunDotNet(toolPath, "conformance/ConformanceApi.fsd", "conformance/no-http/", "--no-http", "--newline", "lf", verifyOption);
		}
	});
}
