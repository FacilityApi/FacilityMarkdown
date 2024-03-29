return BuildRunner.Execute(args, build =>
{
	var codegen = "fsdgenmd";

	var gitLogin = new GitLoginInfo("FacilityApiBot", Environment.GetEnvironmentVariable("BUILD_BOT_PASSWORD") ?? "");

	var dotNetBuildSettings = new DotNetBuildSettings
	{
		NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
		DocsSettings = new DotNetDocsSettings
		{
			GitLogin = gitLogin,
			GitAuthor = new GitAuthorInfo("FacilityApiBot", "facilityapi@gmail.com"),
			SourceCodeUrl = "https://github.com/FacilityApi/FacilityMarkdown/tree/master/src",
			ProjectHasDocs = name => !name.StartsWith("fsdgen", StringComparison.Ordinal),
		},
		PackageSettings = new DotNetPackageSettings
		{
			GitLogin = gitLogin,
			PushTagOnPublish = x => $"nuget.{x.Version}",
		},
	};

	build.AddDotNetTargets(dotNetBuildSettings);

	build.Target("codegen")
		.DependsOn("build")
		.Describe("Generates code from the FSD")
		.Does(() => CodeGen(verify: false));

	build.Target("verify-codegen")
		.DependsOn("build")
		.Describe("Ensures the generated code is up-to-date")
		.Does(() => CodeGen(verify: true));

	build.Target("test")
		.DependsOn("verify-codegen");

	void CodeGen(bool verify)
	{
		var configuration = dotNetBuildSettings.GetConfiguration();
		var verifyOption = verify ? "--verify" : null;

		RunDotNet("tool", "run", "FacilityConformance", "fsd", "--output", "conformance/ConformanceApi.fsd", verify ? "--verify" : null);

		RunCodeGen("conformance/ConformanceApi.fsd", "conformance/http/");
		RunCodeGen("conformance/ConformanceApi.fsd", "conformance/no-http/", "--no-http");

		void RunCodeGen(params string?[] args) =>
			RunDotNet(new[] { "run", "--no-build", "--project", $"src/{codegen}", "-f", "net6.0", "-c", configuration, "--", "--newline", "lf", verifyOption }.Concat(args));
	}
});
