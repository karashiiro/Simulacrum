using System.Numerics;
using Dalamud.Logging;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class VideoReaderRenderSource : IRenderSource, IDisposable
{
    private readonly VideoReader _reader;
    private readonly byte[] _cacheBuffer;
    private readonly IReadOnlyPlaybackTracker _sync;
    private readonly IDisposable _unsubscribe;
    private double _ptsSeconds;

    public VideoReaderRenderSource(VideoReader reader, IReadOnlyPlaybackTracker sync)
    {
        _reader = reader;
        _sync = sync;
        _cacheBuffer = new byte[reader.Width * reader.Height * PixelSize()];

        _unsubscribe = sync.OnPan().Subscribe(ts =>
        {
            _ptsSeconds = ts;
            if (!_reader.SeekFrame(Convert.ToInt64(ts)))
            {
                PluginLog.LogWarning("Failed to seek through video");
            }
        });
    }

    public void RenderTo(Span<byte> buffer)
    {
        if (_sync.GetTime() < _ptsSeconds)
        {
            _cacheBuffer.CopyTo(buffer);
            return;
        }

        // TODO: Calling this eventually leads to a CTD, even without any additional calls at coreclr.dll+41fe1
        // TODO: Calling this causes reloads to CTD at coreclr.dll+323055
        if (!_reader.ReadFrame(_cacheBuffer, out var pts))
        {
            PluginLog.LogWarning("Failed to read frame from video reader");
            return;
        }

        var timeBase = _reader.TimeBase;
        var ptsSeconds = pts * timeBase.Numerator / (double)timeBase.Denominator;
        _ptsSeconds = ptsSeconds;
    }

    public int PixelSize()
    {
        // This is set in Simulacrum::AV::Core::VideoReader::ReadFrame
        return 4;
    }

    public Vector2 Size()
    {
        return new Vector2(_reader.Width, _reader.Height);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _unsubscribe.Dispose();
    }
}