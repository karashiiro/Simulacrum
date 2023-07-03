namespace Simulacrum.Drawing;

public class BufferQueue<T> : IDisposable where T : class
{
    private readonly SemaphoreSlim _lock;

    private NodeWrapper? _head;
    private NodeWrapper? _tail;

    public int Count { get; private set; }

    public BufferQueue()
    {
        _lock = new SemaphoreSlim(1, 1);
    }

    public void Push(T node)
    {
        _lock.Wait();
        try
        {
            var lastHead = _head;
            _head = new NodeWrapper(node);

            _head.Next = lastHead;
            if (lastHead != null)
            {
                lastHead.Prev = _head;
            }

            Count++; // Only mutated under a lock, no need to use atomics
            _tail ??= _head;
        }
        finally
        {
            _lock.Release();
        }
    }

    public T? Pop()
    {
        _lock.Wait();
        try
        {
            var lastTail = _tail;
            if (lastTail == null)
            {
                return null;
            }

            _tail = lastTail.Prev;
            if (_tail == null)
            {
                _head = null;
            }

            lastTail.Prev = null;
            Count--;

            return lastTail.Value;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Clear()
    {
        _lock.Wait();
        try
        {
            _head = null;
            _tail = null;
            Count = 0;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Check if the queue is empty. This is faster than attempting to pop and checking the result.
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty()
    {
        // This is safe to access outside of the semaphore because it doesn't mutate the list.
        return _head == null;
    }

    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }

    private class NodeWrapper
    {
        public NodeWrapper? Next { get; set; }
        public NodeWrapper? Prev { get; set; }
        public T Value { get; }

        public NodeWrapper(T value)
        {
            Value = value;
        }
    }
}