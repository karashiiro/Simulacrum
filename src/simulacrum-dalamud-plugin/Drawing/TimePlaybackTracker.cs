using System.Diagnostics;
using R3;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class TimePlaybackTracker : IPlaybackTracker
{
    private readonly Subject<TimeSpan> _play = new();
    private readonly Subject<TimeSpan> _pause = new();
    private readonly Subject<TimeSpan> _pan = new();
    private readonly Stopwatch _clock = new();
    private TimeSpan _baseTime;

    public TimeSpan GetTime()
    {
        return _baseTime + _clock.Elapsed;
    }

    public Observable<TimeSpan> OnPan()
    {
        return _pan;
    }

    public Observable<TimeSpan> OnPause()
    {
        return _pause;
    }

    public Observable<TimeSpan> OnPlay()
    {
        return _play;
    }

    public void Play()
    {
        _clock.Start();
        _play.OnNext(GetTime());
    }

    public void Pause()
    {
        _clock.Stop();
        _pause.OnNext(GetTime());
    }

    public void Pan(TimeSpan ts)
    {
        _baseTime += ts - GetTime();
        _pan.OnNext(GetTime());
    }
}