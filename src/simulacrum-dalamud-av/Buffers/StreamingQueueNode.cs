namespace Simulacrum.AV.Buffers;

public class StreamingQueueNode<T>
{
    private int _refCount;

    public int RefCount => _refCount;
    public StreamingQueueNode<T>? Next { get; set; }
    public StreamingQueueNode<T>? Prev { get; set; }
    public T Value { get; }

    public StreamingQueueNode(T value)
    {
        _refCount = 1;
        Value = value;
    }

    public void AddRef()
    {
        Interlocked.Increment(ref _refCount);
    }

    public void Release()
    {
        if (0 == Interlocked.Decrement(ref _refCount))
        {
            if (Next != null)
            {
                Next.Prev = null;
            }

            Next = null;
        }
    }
}