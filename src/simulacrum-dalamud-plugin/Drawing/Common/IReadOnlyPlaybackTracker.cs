using R3;

namespace Simulacrum.Drawing.Common;

public interface IReadOnlyPlaybackTracker
{
    /// <summary>
    /// Pulls the current timestamp from the tracker.
    /// </summary>
    /// <returns></returns>
    TimeSpan GetTime();

    /// <summary>
    /// Creates an observable over pans through playback. This is used to
    /// enable render sources to perform more complex processing when
    /// manual time changes occur.
    /// </summary>
    /// <returns></returns>
    Observable<TimeSpan> OnPan();

    /// <summary>
    /// Creates an observable over plays.
    /// </summary>
    /// <returns></returns>
    Observable<TimeSpan> OnPlay();

    /// <summary>
    /// Creates an observable over pauses.
    /// </summary>
    /// <returns></returns>
    Observable<TimeSpan> OnPause();
}