using System.Diagnostics;
using System.Reactive.Subjects;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class TimePlaybackTracker : IPlaybackTracker
{
    private readonly ISubject<double> _subject;
    private readonly Stopwatch _clock;
    private double _baseSeconds;

    public TimePlaybackTracker()
    {
        _clock = new Stopwatch();
        _subject = new Subject<double>();
    }

    public double GetTime()
    {
        return _baseSeconds + _clock.Elapsed.TotalSeconds;
    }

    public IObservable<double> OnPan()
    {
        return _subject;
    }

    public void Play()
    {
        _clock.Start();
    }

    public void Pause()
    {
        _clock.Stop();
    }

    public void Pan(double ts)
    {
        _baseSeconds = ts;
        _clock.Restart();
        _subject.OnNext(GetTime());
    }
}