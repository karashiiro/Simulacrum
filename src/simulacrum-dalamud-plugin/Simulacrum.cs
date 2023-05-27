using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace Simulacrum;

public class Simulacrum : IDalamudPlugin
{
    public string Name => "Simulacrum";

    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Framework _framework;
    private readonly PluginConfiguration _config;

    public Simulacrum(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] Framework framework)
    {
        _pluginInterface = pluginInterface;
        _framework = framework;

        _config = (PluginConfiguration?)_pluginInterface.GetPluginConfig() ?? new PluginConfiguration();
        _config.Initialize(_pluginInterface);

        _framework.Update += OnFrameworkUpdate;
    }

    public void OnFrameworkUpdate(Framework f)
    {
        //
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        _framework.Update -= OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}