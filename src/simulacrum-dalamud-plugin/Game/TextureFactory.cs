using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace Simulacrum.Game;

public class TextureFactory : IDisposable
{
    private readonly IList<TextureBootstrap> _bootstraps;
    private readonly ISigScanner _sigScanner;
    private readonly IFramework _framework;
    private readonly IPluginLog _log;

    public TextureFactory(ISigScanner sigScanner, IFramework framework, IPluginLog log)
    {
        _bootstraps = new List<TextureBootstrap>();
        _sigScanner = sigScanner;
        _framework = framework;
        _log = log;
    }

    public async ValueTask<TextureBootstrap> Create(int width, int height, CancellationToken cancellationToken)
    {
        var bootstrap = new TextureBootstrap(_sigScanner, _framework, _log);
        await bootstrap.Initialize(width, height, cancellationToken);
        _bootstraps.Add(bootstrap);
        return bootstrap;
    }

    public void Dispose()
    {
        foreach (var bootstrap in _bootstraps)
        {
            (bootstrap as IDisposable).Dispose();
        }

        GC.SuppressFinalize(this);
    }
}