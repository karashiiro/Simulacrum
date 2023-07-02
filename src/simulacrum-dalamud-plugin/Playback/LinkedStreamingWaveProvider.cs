using NAudio.Wave;

namespace Simulacrum.Playback;

public class LinkedStreamingWaveProvider : IWaveProvider
{
    private readonly StreamingWaveProvider _parent;

    public WaveFormat WaveFormat => _parent.WaveFormat;

    public LinkedStreamingWaveProvider(StreamingWaveProvider parent)
    {
        _parent = parent;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        return _parent.Read(buffer, offset, count);
    }
}