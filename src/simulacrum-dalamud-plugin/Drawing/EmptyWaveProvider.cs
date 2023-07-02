using NAudio.Wave;

namespace Simulacrum.Drawing;

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