namespace Simulacrum.Drawing;

public class PlaybackSynchronizer
{
    private double _ts;

    public double GetTime()
    {
        return _ts;
    }

    public void SetTime(double ts)
    {
        _ts = ts;
    }

    public void AddTime(double dt)
    {
        _ts += dt;
    }
}