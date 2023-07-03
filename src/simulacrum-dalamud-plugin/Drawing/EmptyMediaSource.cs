using System.Reactive.Subjects;
using NAudio.Wave;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class EmptyMediaSource : IMediaSource, IDisposable
{
    private readonly Subject<bool> _empty;

    public EmptyMediaSource()
    {
        _empty = new Subject<bool>();
    }

    public void RenderTo(Span<byte> buffer, out TimeSpan delay)
    {
        buffer.Clear();
        delay = TimeSpan.Zero;
    }

    public IWaveProvider WaveProvider()
    {
        return new EmptyWaveProvider();
    }

    public IObservable<bool> OnAudioBuffered()
    {
        return _empty;
    }

    public IObservable<bool> OnAudioPlay()
    {
        return _empty;
    }

    public IObservable<bool> OnAudioPause()
    {
        return _empty;
    }

    public int PixelSize()
    {
        return 0;
    }

    public IntVector2 Size()
    {
        return IntVector2.Empty;
    }

    public void Dispose()
    {
        _empty.Dispose();
        GC.SuppressFinalize(this);
    }
}