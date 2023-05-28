using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace Simulacrum;

public class Simulacrum : IDalamudPlugin
{
    public string Name => "Simulacrum";

    private readonly Framework _framework;
    private readonly PluginConfiguration _config;
    private readonly PrimitiveDebug _primitive;

    private IDisposable? _unsubscribe;
    private bool _initialized;

    public Simulacrum(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] Framework framework,
        [RequiredVersion("1.0")] SigScanner sigScanner)
    {
        _framework = framework;

        _config = (PluginConfiguration?)pluginInterface.GetPluginConfig() ?? new PluginConfiguration();
        _config.Initialize(pluginInterface);

        _primitive = new PrimitiveDebug(sigScanner);

        _framework.Update += OnFrameworkUpdate;
    }

    public void OnFrameworkUpdate(Framework f)
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _primitive.Initialize();
            _unsubscribe = _primitive.Subscribe(() => { });
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, "Failed to initialize primitive");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        _framework.Update -= OnFrameworkUpdate;
        _unsubscribe?.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}