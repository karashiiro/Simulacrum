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
/// before the first <c>run</c> step. Currently, only <c>uses</c> is supported. The
/// syntax is <c>uses(action-name[@version], attribute=value, ...)</c>.
/// </summary>
partial class GithubActionsWithExtraStepsAttribute : GitHubActionsAttribute
{
    public string[] Setup { get; init; }

    [GeneratedRegex(@"uses\((?<Uses>[^,]+),\s*(?<With>.+)\s*\)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex UsesRegex();

    public GithubActionsWithExtraStepsAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images) : base(name, image, images)
    {
    }

    protected override GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var jobs = base.GetJobs(image, relevantTargets);
        var steps = jobs.Steps.ToList();

        // Splice our setup steps before the first run step
        var firstRunStepIdx = steps.FindIndex((step) => step is GitHubActionsRunStep);
        Setup.ForEach(setupClause =>
        {
            steps.Insert(firstRunStepIdx, ParseStep(setupClause));
        });

        jobs.Steps = steps.ToArray();

        return jobs;
    }

    private static GitHubActionsStep ParseStep(string enc)
    {
        var usesMatch = UsesRegex().Match(enc);
        if (usesMatch != null)
        {
            return new GitHubActionsUsesStep { Uses = usesMatch.Groups["Uses"].Value, With = ParseWith(usesMatch.Groups["With"].Value) };
        }

        throw new InvalidOperationException($"Unknown step syntax: {enc}");
    }

    private static IDictionary<string, string> ParseWith(string withList)
    {
        return withList.Split(',').Select(clause => clause.Trim().Split('=')).ToDictionary(kvp => kvp[0], kvp => kvp[1]);
    }
}