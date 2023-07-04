namespace Simulacrum.Monitoring;

#if DEBUG
public interface IHistogram : Prometheus.IHistogram
{
}
#else
public interface IHistogram
{
    void Observe(double val);
}
#endif