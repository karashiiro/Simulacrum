using Dalamud.Hooking;

namespace Simulacrum.Game;

public class HookSubscription<T> : IDisposable where T : Delegate
{
    private readonly Hook<T> _hook;

    public HookSubscription(Hook<T> hook)
    {
        _hook = hook;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _hook.Disable();
        _hook.Dispose();
    }
}