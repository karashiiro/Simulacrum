namespace Simulacrum.Drawing;

public class BufferQueueNode : IDisposable
{
    private readonly Action<byte[]> _onDispose;
    private readonly byte[] _buffer;
    private readonly int _length;

    private bool _disposed;

    public double Pts { get; }

    public Span<byte> Span => _buffer.AsSpan(0, _length);

    public BufferQueueNode(byte[] buffer, int length, double pts, Action<byte[]> onDispose)
    {
        _onDispose = onDispose;
        _buffer = buffer;
        _length = length;

        Pts = pts;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _onDispose(_buffer);
        GC.SuppressFinalize(this);
    }
}