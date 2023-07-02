namespace Simulacrum.Drawing;

public class AudioBufferNode
{
    private readonly Action<byte[]> _cleanup;
    private readonly byte[] _buffer;
    private readonly int _length;

    public double Pts { get; }

    public Span<byte> Span => _buffer.AsSpan(0, _length);

    public AudioBufferNode(byte[] buffer, int length, double pts, Action<byte[]> cleanup)
    {
        _cleanup = cleanup;
        _buffer = buffer;
        _length = length;

        Pts = pts;
    }

    ~AudioBufferNode()
    {
        _cleanup(_buffer);
    }
}