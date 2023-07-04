namespace Simulacrum.Monitoring;

#if DEBUG
public interface ICounter : Prometheus.ICounter
{
}
#else
public interface ICounter
{
    void Inc(double increment = 1.0);
}
#endif