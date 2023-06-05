namespace Simulacrum.Drawing.Common;

public interface IPlaybackTracker : IReadOnlyPlaybackTracker
{
    void Play();

    void Pause();

    void Pan(double ts);
}