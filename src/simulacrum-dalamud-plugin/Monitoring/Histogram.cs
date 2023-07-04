#if DEBUG
using Prometheus;

namespace Simulacrum.Monitoring;

public class Histogram : IHistogram
{
    public Prometheus.IHistogram? Inner { get; init; }

    public double Sum => Inner?.Sum ?? 0;

    public long Count => Inner?.Count ?? 0;

    public void Observe(double val)
    {
        Inner?.Observe(val);
    }

    public void Observe(double val, long count)
    {
        Inner?.Observe(val, count);
    }

    public void Observe(double val, Exemplar? exemplar)
    {
        Inner?.Observe(val, exemplar);
    }
}
#endif