using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Utilities;

class GitHubActionsSimpleRunStep : GitHubActionsStep
{
    public string Command { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine($"- name: {("Run: " + Command).SingleQuote()}");
        writer.WriteLine($"  run: {Command}");
    }
}