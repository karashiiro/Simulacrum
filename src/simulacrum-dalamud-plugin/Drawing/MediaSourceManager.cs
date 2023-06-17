using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class MediaSourceManager : IDisposable
{
    private readonly IDictionary<string, IMediaSource> _mediaSources;

    public IEnumerable<KeyValuePair<string, IMediaSource>> MediaSourceEntries =>
        new Dictionary<string, IMediaSource>(_mediaSources); // Enumeration-safe

    public MediaSourceManager()
    {
        _mediaSources = new Dictionary<string, IMediaSource>();
    }

    public void AddMediaSource(string id, IMediaSource mediaSource)
    {
        _mediaSources[id] = mediaSource;
    }

    public IMediaSource? GetMediaSource(string id)
    {
        return _mediaSources.TryGetValue(id, out var ms) ? ms : null;
    }

    public void Dispose()
    {
        foreach (var mediaSource in _mediaSources
                     .Select(kvp => kvp.Value as IDisposable)
                     .Where(ms => ms is not null)
                     .Select(ms => ms!))
        {
            mediaSource.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}