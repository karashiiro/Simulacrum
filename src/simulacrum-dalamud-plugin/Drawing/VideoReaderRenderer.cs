using System.Numerics;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class VideoReaderRenderer : IRenderSource
{
    private readonly VideoReader _reader;
    private readonly byte[] _cacheBuffer;
    private readonly PlaybackSynchronizer _sync;
    private double _ptsSeconds;
    private long _lastPts;

    public VideoReaderRenderer(VideoReader reader, PlaybackSynchronizer sync)
    {
        _reader = reader;
        _sync = sync;
        // TODO: Detect this somehow
        _cacheBuffer = GC.AllocateArray<byte>(reader.Width * reader.Height * 4, pinned: true);
    }

    public void RenderTo(Span<byte> buffer)
    {
        if (_sync.GetTime() < _ptsSeconds)
        {
            _cacheBuffer.CopyTo(buffer);
            return;
        }

        if (!_reader.ReadFrame(_cacheBuffer, out var pts))
        {
            throw new InvalidOperationException("Failed to read frame from video reader");
        }

        var timeBase = _reader.TimeBase;
        var ptsSeconds = pts * timeBase.Numerator / (double)timeBase.Denominator;
        _ptsSeconds = ptsSeconds;
        _lastPts = pts;
    }

    public int PixelSize()
    {
        // TODO: Detect this somehow
        return 4;
    }

    public Vector2 Size()
    {
        return new Vector2(_reader.Width, _reader.Height);
    }

    public void Sync()
    {
        if (!_reader.SeekFrame(0))
        {
            throw new InvalidOperationException("Failed to seek stream.");
        }

        _ptsSeconds = 0;
        _sync.SetTime(0);
    }
}