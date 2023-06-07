using System.Runtime.InteropServices;
using Dalamud.Logging;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class VideoReaderRenderSource : IRenderSource, IDisposable
{
    private readonly VideoReader _reader;

    private readonly nint _cacheBufferPtr;
    private readonly int _cacheBufferRawSize;
    private readonly int _cacheBufferSize;

    private readonly IReadOnlyPlaybackTracker _sync;
    private readonly IDisposable _unsubscribe;
    private double _ptsSeconds;

    public VideoReaderRenderSource(VideoReader reader, IReadOnlyPlaybackTracker sync)
    {
        _reader = reader;
        _sync = sync;
        _cacheBufferSize = reader.Width * reader.Height * PixelSize();

        // For some reason, sws_scale writes 8 black pixels after the end of the buffer
        _cacheBufferRawSize = _cacheBufferSize + 32;
        _cacheBufferPtr = Marshal.AllocHGlobal(_cacheBufferRawSize);

        _unsubscribe = sync.OnPan().Subscribe(ts =>
        {
            _ptsSeconds = ts;
            if (!_reader.SeekFrame(Convert.ToInt64(ts)))
            {
                PluginLog.LogWarning("Failed to seek through video");
            }
        });
    }

    public unsafe void RenderTo(Span<byte> buffer)
    {
        var cacheBuffer = new Span<byte>((byte*)_cacheBufferPtr, _cacheBufferRawSize);
        if (_sync.GetTime() < _ptsSeconds)
        {
            cacheBuffer[.._cacheBufferSize].CopyTo(buffer);
            return;
        }

        if (!_reader.ReadFrame(cacheBuffer, out var pts))
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

    public IntVector2 Size()
    {
        return new IntVector2(_reader.Width, _reader.Height);
    }

    public void Dispose()
    {
        _unsubscribe.Dispose();
        Marshal.FreeHGlobal(_cacheBufferPtr);
        GC.SuppressFinalize(this);
    }
}