using Dalamud.Logging;

namespace Simulacrum;

public static class TaskExtensions
{
    public static void FireAndForget(this Task task)
    {
        _ = FireAndForgetInternal(task);
    }

    private static async Task FireAndForgetInternal(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, "Exception thrown in dispatched task");
        }
    }
}