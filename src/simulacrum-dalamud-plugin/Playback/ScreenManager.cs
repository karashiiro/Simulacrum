using Simulacrum.Playback.Common;

namespace Simulacrum.Playback;

public class ScreenManager : IDisposable
{
    private readonly IDictionary<string, IScreen> _screens;

    public IEnumerable<KeyValuePair<string, IScreen>> ScreenEntries =>
        new Dictionary<string, IScreen>(_screens); // Enumeration-safe

    public IEnumerable<IScreen> Screens => new List<IScreen>(_screens.Values); // Enumeration-safe

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
        return _screens.TryGetValue(id, out var s) ? s : null;
    }

    public void Dispose()
    {
        foreach (var screen in _screens
                     .Select(kvp => kvp.Value as IDisposable)
                     .Where(ms => ms is not null)
                     .Select(s => s!))
        {
            screen.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}