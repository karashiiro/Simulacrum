namespace Simulacrum.AV.Buffers;

public class StreamingQueueNodeRef<T> : IDisposable
{
    private readonly StreamingQueueNode<T> _node;
    private int _disposed;

    public T Value => _node.Value;

    public int RefCount => _node.RefCount;

    public StreamingQueueNodeRef<T>? Next => _node.Next != null ? new(_node.Next) : null;

    public StreamingQueueNodeRef(StreamingQueueNode<T> node)
    {
        _node = node;
    }

    public void Dispose()
    {
        if (0 != Interlocked.CompareExchange(ref _disposed, 1, 0))
        {
            return;
        }

        _node.Release();
        GC.SuppressFinalize(this);
    }
}