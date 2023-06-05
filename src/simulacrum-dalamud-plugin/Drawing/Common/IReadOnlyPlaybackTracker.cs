namespace Simulacrum.Drawing.Common;

public interface IReadOnlyPlaybackTracker
{
    double GetTime();

    IObservable<double> OnPan();
}