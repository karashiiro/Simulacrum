using System.Globalization;
using R3;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class MpvMediaSource : IMediaSource, IDisposable
{
    private readonly MpvHandle _handle;

    private readonly IDisposable _unsubscribeAll;

    public MpvMediaSource(string? uri, IReadOnlyPlaybackTracker sync)
    {
        ArgumentNullException.ThrowIfNull(uri);

        _handle = new MpvHandle();
        _handle.Initialize();

        // https://github.com/mpv-player/mpv-examples/blob/master/libmpv/csharp/Form1.cs
        _handle.SetOptionString("keep-open"u8, "always"u8);

        _handle.Command(new[] { "loadfile", uri });

        var unsubscribePause = sync.OnPause()
            .Subscribe(_handle, static (_, mpv) => mpv.SetProperty("pause"u8, 1, "yes"u8));
        var unsubscribePlay =
            sync.OnPlay().Subscribe(_handle, static (_, mpv) => mpv.SetProperty("pause"u8, 1, "no"u8));
        var unsubscribePan = sync.OnPan().Subscribe(_handle,
            static (t, mpv) =>
            {
                mpv.Command(new[] { "seek", t.TotalSeconds.ToString(CultureInfo.InvariantCulture), "absolute" });
            });

        _unsubscribeAll = Disposable.Combine(unsubscribePause, unsubscribePlay, unsubscribePan);
    }

    public void RenderTo(Span<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public void RenderTo(Span<byte> buffer, out TimeSpan delay)
    {
        throw new NotImplementedException();
    }

    public int PixelSize()
    {
        throw new NotImplementedException();
    }

    public IntVector2 Size()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _unsubscribeAll.Dispose();
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}