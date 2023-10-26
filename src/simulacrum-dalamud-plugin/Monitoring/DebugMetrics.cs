using System.Diagnostics.CodeAnalysis;

#if DEBUG
using Dalamud.Plugin.Services;
using Prometheus;
#endif

namespace Simulacrum.Monitoring;

[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public class DebugMetrics : IDisposable
{
#if DEBUG
    private const int Port = 7231;

    private readonly IMetricServer _server;
    private readonly IPluginLog _log;

    public DebugMetrics(IPluginLog log)
    {
        _server = new MetricServer(port: Port);
        _log = log;
    }
#endif

    public void Start()
    {
#if DEBUG
        try
        {
            _server.Start();
            _log.Info($"Debug metrics server started on port {Port}");
        }
        catch (Exception e)
        {
            _log.Warning(e, "Failed to start debug metrics server.\n" +
                            "You may need to grant permissions to your user account " +
                            "if not running as Administrator:\n" +
                            $"netsh http add urlacl url=http://+:{Port}/metrics user=[DOMAIN\\]<user>");
        }
#endif
    }

    [SuppressMessage("ReSharper", "ReturnTypeCanBeNotNullable")]
    public static IHistogram? CreateHistogram(string name, string help)
    {
#if DEBUG
        return new Histogram
        {
            Inner = Metrics.CreateHistogram(name, help),
        };
#else
        return null;
#endif
    }

    [SuppressMessage("ReSharper", "ReturnTypeCanBeNotNullable")]
    public static ICounter? CreateCounter(string name, string help)
    {
#if DEBUG
        return new Counter
        {
            Inner = Metrics.CreateCounter(name, help),
        };
#else
        return null;
#endif
    }

    public void Dispose()
    {
#if DEBUG
        _server.Stop();
        _server.Dispose();
#endif
        GC.SuppressFinalize(this);
    }
}