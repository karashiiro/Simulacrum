using System.Diagnostics.CodeAnalysis;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Serilog;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[GitHubActions("build", GitHubActionsImage.WindowsLatest,
    On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(EnableCorepack), nameof(YarnInstall), nameof(YarnBuild), nameof(YarnTest) }, // TODO: Enable plugin builds in CI
    CacheKeyFiles = new[] { "**/global.json", "**/*.csproj", "**/package.json", "**/yarn.lock" },
    CacheIncludePatterns = new[] { ".nuke/temp", "~/.nuget/packages", "**/node_modules" })]
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

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    [PathVariable] readonly Tool Corepack;

    [PathVariable] readonly Tool Yarn;

    [LocalPath("./src/simulacrum-depcheck/depcheck.cmd")]
    readonly Tool DepCheck;

    Target EnableCorepack => _ => _
        .Description("Enables corepack for the current Node.js installation.")
        .DependentFor(YarnInstall)
        .Executes(() => { Corepack("enable"); });

    Target CheckDeps => _ => _
        .Description("Checks for the required dependencies in the environment.")
        .DependentFor(YarnInstall, YarnDev, YarnBuild, CdkDiff)
        .Executes(() => { DepCheck(""); });

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
        .Executes(() => { Yarn("test", exitHandler: _ => { }); });

    Target YarnBuild => _ => _
        .Description("Builds the Node.js packages in the monorepo using yarn.")
        .Executes(() => { Yarn("build"); });

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
            MSBuildTasks.MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Build")
                .SetConfiguration(Configuration)
                .SetTargetPlatform(MSBuildTargetPlatform.x64)
                .EnableNodeReuse());
        });

    // TODO: Vendor Simulacrum.AV
}