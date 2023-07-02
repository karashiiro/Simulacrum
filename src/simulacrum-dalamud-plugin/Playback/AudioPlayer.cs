using NAudio.Wave;
using Simulacrum.Playback.Common;

namespace Simulacrum.Playback;

public class AudioPlayer : IDisposable
{
    private readonly IWavePlayer _wavePlayer;

    private readonly IDisposable _unsubscribePlay;
    private readonly IDisposable _unsubscribePause;
    private readonly IDisposable _unsubscribeAudioBuffered;

    public AudioPlayer(IMediaSource mediaSource)
    {
        _wavePlayer = new DirectSoundOut();
        _wavePlayer.Init(mediaSource.WaveProvider());

        _unsubscribePause = mediaSource.OnAudioPause().Subscribe(_ => _wavePlayer.Pause());
        _unsubscribePlay = mediaSource.OnAudioPlay().Subscribe(_ => _wavePlayer.Play());
        _unsubscribeAudioBuffered = mediaSource.OnAudioBuffered().Subscribe(_ =>
        {
            if (_wavePlayer.PlaybackState == PlaybackState.Stopped)
            {
                _wavePlayer.Play();
            }
        });
    }

    public void Dispose()
    {
        _unsubscribePlay.Dispose();
        _unsubscribePause.Dispose();
        _unsubscribeAudioBuffered.Dispose();
        _wavePlayer.Dispose();
        GC.SuppressFinalize(this);
    }
}