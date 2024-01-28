using System.Collections.Generic;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

class GitHubActionsUsesStep : GitHubActionsStep
{
    public required string Uses { get; init; }

    public required IDictionary<string, string> With { get; init; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine($"- name: {("Setup: " + Uses).SingleQuote()}");
        writer.WriteLine($"  uses: {Uses}");
        if (With.Count <= 0)
        {
            return;
        }

        using (writer.Indent())
        {
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                With.ForEach(kvp =>
                {
                    var (key, value) = kvp;
                    writer.WriteLine($"{key}: {value}");
                });
            }
        }
    }
}