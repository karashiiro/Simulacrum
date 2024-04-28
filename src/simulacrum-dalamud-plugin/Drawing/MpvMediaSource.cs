using System.Runtime.InteropServices;
using R3;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class MpvMediaSource : IMediaSource, IDisposable
{
    private readonly MpvHandle _handle;
    private readonly MpvRenderContext _renderContext;
    private readonly nint _videoBufferPtr;
    private readonly int _videoBufferSize;

    private readonly IDisposable _unsubscribeAll;

    private unsafe Span<byte> VideoBuffer => new((byte*)_videoBufferPtr, _videoBufferSize);

    public MpvMediaSource(string? uri, IReadOnlyPlaybackTracker sync)
    {
        ArgumentNullException.ThrowIfNull(uri);

        var (width, height) = (300, 300);
        _videoBufferSize = width * height * PixelSize();
        _videoBufferPtr = Marshal.AllocHGlobal(_videoBufferSize);

        _handle = new MpvHandle();
        _renderContext = new MpvRenderContext(_handle, width, height);

        // https://github.com/mpv-player/mpv-examples/blob/master/libmpv/csharp/Form1.cs
        _handle.SetPropertyString("keep-open", "always");
        _handle.SetPropertyString("video-sync", "audio");

        _handle.LoadFile(uri);

        var unsubscribePause = sync.OnPause().Subscribe(_handle, static (_, mpv) => mpv.Pause());
        var unsubscribePlay = sync.OnPlay().Subscribe(_handle, static (_, mpv) => mpv.Play());
        var unsubscribePan = sync.OnPan().Subscribe(_handle, static (t, mpv) => mpv.Seek(t));

        _unsubscribeAll = Disposable.Combine(unsubscribePause, unsubscribePlay, unsubscribePan);
    }

    public void RenderTo(Span<byte> buffer)
    {
        VideoBuffer.CopyTo(buffer);
    }

    public void RenderTo(Span<byte> buffer, out TimeSpan delay)
    {
        throw new NotImplementedException();
    }

    public int PixelSize()
    {
        return 4;
    }

    public IntVector2 Size()
    {
        var (w, h) = _renderContext.GetSize();
        return IntVector2.Create(w, h);
    }

    public void Dispose()
    {
        _unsubscribeAll.Dispose();
        _renderContext.Dispose();
        _handle.Dispose();
        Marshal.FreeHGlobal(_videoBufferPtr);
        GC.SuppressFinalize(this);
    }
}