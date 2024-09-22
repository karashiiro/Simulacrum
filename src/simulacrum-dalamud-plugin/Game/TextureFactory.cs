using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace Simulacrum.Game;

public class TextureFactory(ISigScanner sigScanner, IFramework framework, IPluginLog log) : IDisposable
{
    private readonly List<TextureBootstrap> _bootstraps = [];

    public async ValueTask<TextureBootstrap> Create(int width, int height, CancellationToken cancellationToken)
    {
        var bootstrap = new TextureBootstrap(sigScanner, framework, log);
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