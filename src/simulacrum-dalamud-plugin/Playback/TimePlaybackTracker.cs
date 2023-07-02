using System.Diagnostics;
using System.Reactive.Subjects;
using Simulacrum.Playback.Common;

namespace Simulacrum.Playback;

public class TimePlaybackTracker : IPlaybackTracker
{
    private readonly ISubject<TimeSpan> _play;
    private readonly ISubject<TimeSpan> _pause;
    private readonly ISubject<TimeSpan> _pan;
    private readonly Stopwatch _clock;
    private TimeSpan _baseTime;

    public TimePlaybackTracker()
    {
        _clock = new Stopwatch();
        _play = new Subject<TimeSpan>();
        _pause = new Subject<TimeSpan>();
        _pan = new Subject<TimeSpan>();
    }

    public TimeSpan GetTime()
    {
        return _baseTime + _clock.Elapsed;
    }

    public IObservable<TimeSpan> OnPan()
    {
        return _pan;
    }

    public IObservable<TimeSpan> OnPause()
    {
        return _pause;
    }

    public IObservable<TimeSpan> OnPlay()
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