using System.Diagnostics;
using System.Reactive.Subjects;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class TimePlaybackTracker : IPlaybackTracker
{
    private readonly ISubject<double> _play;
    private readonly ISubject<double> _pause;
    private readonly ISubject<double> _pan;
    private readonly Stopwatch _clock;
    private double _baseSeconds;

    public TimePlaybackTracker()
    {
        _clock = new Stopwatch();
        _play = new Subject<double>();
        _pause = new Subject<double>();
        _pan = new Subject<double>();
    }

    public double GetTime()
    {
        return _baseSeconds + _clock.Elapsed.TotalSeconds;
    }

    public IObservable<double> OnPan()
    {
        return _pan;
    }

    public IObservable<double> OnPause()
    {
        return _pause;
    }

    public IObservable<double> OnPlay()
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

    public void Pan(double ts)
    {
        _baseSeconds += ts - GetTime();
        _pan.OnNext(GetTime());
    }
}