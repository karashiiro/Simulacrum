using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class PlaybackTrackerManager : IDisposable
{
    private readonly IDictionary<string, IPlaybackTracker> _playbackTrackers;

    public PlaybackTrackerManager()
    {
        _playbackTrackers = new Dictionary<string, IPlaybackTracker>();
    }

    public void AddPlaybackTracker(string id, IPlaybackTracker mediaSource)
    {
        _playbackTrackers[id] = mediaSource;
    }

    public IPlaybackTracker? GetPlaybackTracker(string? id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _playbackTrackers.TryGetValue(id, out var pt) ? pt : null;
    }

    public void Dispose()
    {
        foreach (var playbackTracker in _playbackTrackers
                     .Select(kvp => kvp.Value as IDisposable)
                     .Where(ms => ms is not null)
                     .Select(ms => ms!))
        {
            playbackTracker.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}