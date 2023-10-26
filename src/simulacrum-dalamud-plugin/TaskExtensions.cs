using Dalamud.Plugin.Services;

namespace Simulacrum;

public static class TaskExtensions
{
    public static void FireAndForget(this Task task, IPluginLog log)
    {
        _ = FireAndForgetInternal(task, log);
    }

    private static async Task FireAndForgetInternal(Task task, IPluginLog log)
    {
        try
        {
            await task;
        }
        catch (Exception e)
        {
            log.Error(e, "Exception thrown in dispatched task");
        }
    }
}