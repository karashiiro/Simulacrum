#if DEBUG
using Prometheus;

namespace Simulacrum.Monitoring;

public class Counter : ICounter
{
    public Prometheus.ICounter? Inner { get; init; }

    public double Value => Inner?.Value ?? 0;

    public void Inc(double increment = 1)
    {
        Inner?.Inc(increment);
    }

    public void Inc(Exemplar? exemplar)
    {
        Inner?.Inc(exemplar);
    }

    public void Inc(double increment, Exemplar? exemplar)
    {
        Inner?.Inc(increment, exemplar);
    }

    public void IncTo(double targetValue)
    {
        Inner?.IncTo(targetValue);
    }
}
#endif