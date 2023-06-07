using System.Runtime.InteropServices;
using Dalamud.Logging;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class VideoReaderRenderSource : IRenderSource, IDisposable
{
    private const int Alignment = 128;

    private readonly VideoReader _reader;
    private readonly nint _cacheBufferPtr;
    private readonly Memory<byte> _cacheBuffer;
    private readonly IReadOnlyPlaybackTracker _sync;
    private readonly IDisposable _unsubscribe;
    private double _ptsSeconds;

    public VideoReaderRenderSource(VideoReader reader, IReadOnlyPlaybackTracker sync)
    {
        _reader = reader;
        _sync = sync;
        var cacheBufferSize = reader.Width * reader.Height * PixelSize();

        /*
         * This video reader requires that frame data is 128-byte aligned. Windows
         * always allocates on an 8-byte (or word-sized?) alignment boundary, so
         * this needs to over-allocate memory and adjust the bounds accordingly.
         * https://github.com/bmewj/video-app/blob/efda3fbd11133842e6154a62f853a6066ccc190c/src/main.cpp#L40
         * https://stackoverflow.com/a/13416185
         */
        var cacheBufferRawSize = cacheBufferSize + Alignment - 8;
        _cacheBufferPtr = Marshal.AllocHGlobal(cacheBufferRawSize);
        unsafe
        {
            var alignedPtr = (nint)(Alignment * (((long)_cacheBufferPtr + (Alignment - 1)) / Alignment));
            var manager = new UnmanagedMemoryManager<byte>((byte*)alignedPtr, cacheBufferSize);
            _cacheBuffer = manager.Memory;
        }

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
            _cacheBuffer.Span.CopyTo(buffer);
            return;
        }

        if (!_reader.ReadFrame(_cacheBuffer.Span, out var pts))
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