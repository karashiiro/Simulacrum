namespace Simulacrum.Drawing.Common;

public interface IPlaybackTracker : IReadOnlyPlaybackTracker
{
    /// <summary>
    /// Signal to begin content playback.
    /// </summary>
    void Play();

    /// <summary>
    /// Signal to pause content playback.
    /// </summary>
    void Pause();

    /// <summary>
    /// Pan to the specified timestamp.
    /// </summary>
    /// <param name="ts">The time to pan to.</param>
    void Pan(double ts);
}