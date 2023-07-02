using NAudio.Wave;

namespace Simulacrum.Playback;

public class EmptyWaveProvider : IWaveProvider
{
    public WaveFormat WaveFormat { get; }

    public EmptyWaveProvider()
    {
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        return 0;
    }
}