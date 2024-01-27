using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Serilog;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[GitHubActionsWithExtraSteps("build-api", GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(YarnInstall), nameof(YarnBuild), nameof(YarnTest) },
    CacheKeyFiles = new[] { "**/global.json", "**/*.csproj", "**/package.json", "**/yarn.lock" },
    CacheIncludePatterns = new[] { ".nuke/temp", "~/.nuget/packages", "**/node_modules" },
    Setup = new[] { "uses(actions/setup-node@v4, node-version=18)", "run(corepack enable)" })]
[GitHubActions("test-api", GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(APIDockerBuild), nameof(APILambdaDockerBuild), nameof(TestHostctl) },
    CacheKeyFiles = new[] { "**/global.json", "**/*.csproj", "**/package.json", "**/yarn.lock" },
    CacheIncludePatterns = new[] { ".nuke/temp", "~/.nuget/packages", "**/node_modules" })]
[GitHubActionsWithExtraSteps("build-plugin", GitHubActionsImage.WindowsLatest,
    On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(Compile) },
    Setup = new[] { "run(wget https://goatcorp.github.io/dalamud-distrib/latest.zip -O /tmp/dalamud.zip && unzip /tmp/dalamud.zip -d /tmp/dalamud)" })]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath SolutionD17 => RootDirectory / "targets" / "simulacrum-d17.sln";
    AbsolutePath SolutionHostctl => RootDirectory / "targets" / "simulacrum-hostctl.sln";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    [PathVariable] readonly Tool Yarn;

    Target YarnAssertRoot => _ => _
        .Unlisted()
        .DependentFor(YarnInstall, YarnDev, YarnBuild, CdkDiff)
        .Executes(() => { Assert.FileExists(RootDirectory / "yarn.lock"); });

    Target YarnInstall => _ => _
        .Description("Installs the Node.js package dependencies using yarn.")
        .DependentFor(YarnDev, YarnBuild, CdkDiff)
        .Before(YarnDev, YarnBuild, CdkDiff)
        .Executes(() => { Yarn("install"); });

    Target YarnDev => _ => _
        .Description("Runs the development servers using yarn.")
        .Executes(() => { Yarn("dev", exitHandler: _ => { }); });

    Target YarnTest => _ => _
        .Description("Runs tests for workspace packages using yarn.")
        .Executes(() => { Yarn("test"); });

    Target YarnBuild => _ => _
        .Description("Builds the Node.js packages in the monorepo using yarn.")
        .Executes(() => { Yarn("build"); });

    Target APIDockerBuild => _ => _
        .Description("Builds the Docker container image for the API.")
        .Executes(() =>
        {
            DockerTasks.DockerBuild(s => s
                .SetPath(RootDirectory)
                .SetFile(SourceDirectory / "simulacrum-cloud-api" / "Dockerfile")
                .SetTag("simulacrum-cloud-api")
                .SetProcessLogger(DockerLogger));
        });

    Target APILambdaDockerBuild => _ => _
        .Description("Builds the Docker container image for the API on Lambda.")
        .Executes(() =>
        {
            DockerTasks.DockerBuild(s => s
                .SetPath(RootDirectory)
                .SetFile(SourceDirectory / "simulacrum-cloud-api" / "Dockerfile.lambda")
                .SetTag("simulacrum-cloud-api:lambda")
                .SetProcessLogger(DockerLogger));
        });

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    Target CdkDiff => _ => _
        .Description("Diffs the CDK stacks in the repo against those in your AWS account.")
        .Executes(() => { Yarn("workspace simulacrum-cloud-aws-cdk cdk diff", logger: (_, m) => Log.Debug(m)); });

    Target Clean => _ => _
        .Before(Restore, RestoreD17)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj", "**/x64").ForEach(dir => dir.DeleteDirectory());

            var cxxOutput = RootDirectory / "x64";
            cxxOutput.DeleteDirectory();
        });

    Target RestoreHostctl => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(SolutionHostctl));
        });

    Target RestoreD17 => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(SolutionD17));
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target CompileD17 => _ => _
        .Description("[TODO] Compiles the Dalamud plugin using pre-built native dependencies.")
        .DependsOn(RestoreD17)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(SolutionD17)
                .SetConfiguration(Configuration));
        });

    Target Compile => _ => _
        .Description("Compiles the Dalamud plugin.")
        .DependsOn(Restore)
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s =>
                    {
                        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI")))
                        {
                            s = s.SetProcessEnvironmentVariable("DALAMUD_HOME", "/tmp/dalamud");
                        }

                        return s.SetTargetPath(Solution)
                            .SetTargets("Build")
                            .SetConfiguration(Configuration)
                            .SetTargetPlatform(MSBuildTargetPlatform.x64)
                            .EnableNodeReuse();
                    });
        });

    Target TestHostctl => _ => _
        .Description("Tests Hostctl.")
        .DependsOn(RestoreHostctl)
        .After(APIDockerBuild, APILambdaDockerBuild)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(SolutionHostctl)
                .SetConfiguration(Configuration));
        });

    // TODO: Vendor Simulacrum.AV

    void DockerLogger(OutputType outputType, string output)
    {
        // Deal with Docker build output always being logged as errors
        // ReSharper disable TemplateIsNotCompileTimeConstantProblem
        if (outputType != OutputType.Std)
        {
            Log.Information(output);
        }
        else
        {
            Log.Error(output);
        }
        // ReSharper restore TemplateIsNotCompileTimeConstantProblem
    }
}