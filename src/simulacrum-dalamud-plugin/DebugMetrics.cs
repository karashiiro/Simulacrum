using Dalamud.Logging;
using Prometheus;

namespace Simulacrum;

// TODO: Exclude this from release builds entirely somehow
public class DebugMetrics : IDisposable
{
    private const int Port = 7231;

    private readonly IMetricServer _server;

    public DebugMetrics()
    {
        _server = new MetricServer(port: Port);
    }

    public void Start()
    {
        try
        {
            _server.Start();
            PluginLog.Log($"Debug metrics server started on port {Port}");
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, "Failed to start debug metrics server.\n" +
                                  "You may need to grant permissions to your user account " +
                                  "if not running as Administrator:\n" +
                                  $"netsh http add urlacl url=http://+:{Port}/metrics user=[DOMAIN\\]<user>");
        }
    }

    public static IHistogram CreateHistogram(string name, string help)
    {
        return Metrics.CreateHistogram(name, help);
    }

    public void Dispose()
    {
        _server.Dispose();
        GC.SuppressFinalize(this);
    }
}