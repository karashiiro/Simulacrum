using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class ScreenManager : IDisposable
{
    private readonly IDictionary<string, IScreen> _screens;

    public ScreenManager()
    {
        _screens = new Dictionary<string, IScreen>();
    }

    public void AddScreen(string id, IScreen mediaSource)
    {
        _screens[id] = mediaSource;
    }

    public IScreen? GetScreen(string id)
    {
        return _screens.TryGetValue(id, out var ms) ? ms : null;
    }

    public void Dispose()
    {
        foreach (var screen in _screens
                     .Select(kvp => kvp.Value as IDisposable)
                     .Where(s => s is not null)
                     .Select(s => s!))
        {
            screen.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}