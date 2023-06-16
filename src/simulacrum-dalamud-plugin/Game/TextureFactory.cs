using Dalamud.Game;

namespace Simulacrum.Game;

public class TextureFactory : IDisposable
{
    private readonly IList<TextureBootstrap> _bootstraps;
    private readonly SigScanner _sigScanner;

    public TextureFactory(SigScanner sigScanner)
    {
        _bootstraps = new List<TextureBootstrap>();
        _sigScanner = sigScanner;
    }

    public async ValueTask<TextureBootstrap> Create(int width, int height, CancellationToken cancellationToken)
    {
        var bootstrap = new TextureBootstrap(_sigScanner);
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