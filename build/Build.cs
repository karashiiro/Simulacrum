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
    InvokedTargets = new[] { nameof(YarnBuild) })] // TODO: Enable plugin builds in CI
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

    [PathVariable] readonly Tool Yarn;

    Target YarnAssertRoot => _ => _
        .Executes(() => { Assert.FileExists(RootDirectory / "yarn.lock"); });

    Target YarnInstall => _ => _
        .DependsOn(YarnAssertRoot)
        .Before(YarnBuild, CdkDiff)
        .Executes(() => { Yarn("install"); });

    Target YarnBuild => _ => _
        .DependsOn(YarnAssertRoot)
        .Executes(() => { Yarn("build"); });

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    Target CdkDiff => _ => _
        .DependsOn(YarnAssertRoot)
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
        .DependsOn(RestoreD17)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(SolutionD17)
                .SetConfiguration(Configuration));
        });

    Target Compile => _ => _
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