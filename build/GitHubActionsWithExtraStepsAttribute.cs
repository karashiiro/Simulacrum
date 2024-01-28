using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities.Collections;

/// <summary>
/// A custom GHA workflow generator based on NUKE's stock generator API. NUKE does not
/// allow including arbitrary "uses" steps in generated workflows, but we need them to
/// set up the correct versions of dependencies like Node.js.
/// 
/// The <c>Setup</c> property takes an array of declarations for steps to be inserted
/// before the first <c>run</c> step.
/// 
/// The syntax for <c>uses</c> is <c>uses(action-name[@version], attribute=value[, ...])</c>.
/// The syntax for <c>run</c> is <c>run(command)</c>.
/// </summary>
partial class GitHubActionsWithExtraStepsAttribute : GitHubActionsAttribute
{
    public required string[] Setup { get; init; }

    [GeneratedRegex(@"uses\((?<Uses>[^,]+),\s*(?<With>.+)\s*\)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex UsesRegex();

    [GeneratedRegex(@"run\((?<Run>.+)\)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex RunRegex();

    public GitHubActionsWithExtraStepsAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images) : base(name, image, images)
    {
    }

    protected override GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var jobs = base.GetJobs(image, relevantTargets);
        var steps = jobs.Steps.ToList();

        // Splice our setup steps before the first run step
        var firstRunStepIdx = steps.FindIndex((step) => step is GitHubActionsRunStep);
        if (firstRunStepIdx == -1)
        {
            firstRunStepIdx = 0;
        }

        Setup.Reverse().ForEach((setupClause, i) =>
        {
            steps.Insert(firstRunStepIdx, ParseStep(setupClause));
        });

        jobs.Steps = steps.ToArray();

        return jobs;
    }

    private static GitHubActionsStep ParseStep(string enc)
    {
        var usesMatch = UsesRegex().Match(enc);
        if (usesMatch.Success)
        {
            return new GitHubActionsUsesStep { Uses = usesMatch.Groups["Uses"].Value, With = ParseWith(usesMatch.Groups["With"].Value) };
        }

        var runMatch = RunRegex().Match(enc);
        if (runMatch.Success)
        {
            return new GitHubActionsSimpleRunStep { Command = runMatch.Groups["Run"].Value };
        }

        throw new InvalidOperationException($"Unknown step syntax: {enc}");
    }

    private static IDictionary<string, string> ParseWith(string withList)
    {
        return withList.Split(',').Select(clause => clause.Trim().Split('=')).ToDictionary(kvp => kvp[0], kvp => kvp[1]);
    }
}