namespace Simulacrum.Drawing;

public class MaterialScreenManager : IDisposable
{
    private readonly IDictionary<string, MaterialScreen> _screens;

    public IEnumerable<KeyValuePair<string, MaterialScreen>> ScreenEntries =>
        new Dictionary<string, MaterialScreen>(_screens); // Enumeration-safe

    public IEnumerable<MaterialScreen> Screens => new List<MaterialScreen>(_screens.Values); // Enumeration-safe

    public MaterialScreenManager()
    {
        _screens = new Dictionary<string, MaterialScreen>();
    }

    public void AddScreen(string id, MaterialScreen mediaSource)
    {
        _screens[id] = mediaSource;
    }

    public MaterialScreen? GetScreen(string id)
    {
        return _screens.TryGetValue(id, out var ms) ? ms : null;
    }

    public void Dispose()
    {
        foreach (var screen in _screens
                     .Select(kvp => kvp.Value as IDisposable)
                     .Select(s => s!))
        {
            screen.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}