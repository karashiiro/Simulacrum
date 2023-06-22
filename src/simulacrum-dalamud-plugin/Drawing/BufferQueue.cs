using Thinktecture;

namespace Simulacrum.Drawing;

/// <summary>
/// A FIFO queue of buffers, which are optionally disposable. This is intended to be
/// used with <see cref="System.Buffers.ArrayPool{T}"/>.
/// </summary>
public class BufferQueue : IDisposable
{
    private readonly Action<byte[]> _disposeBuffer;
    private readonly SemaphoreSlim _lock;

    private BufferListNode? _head;
    private BufferListNode? _tail;

    public int Count { get; private set; }

    public BufferQueue(Action<byte[]> disposeBuffer)
    {
        _disposeBuffer = disposeBuffer;
        _lock = new SemaphoreSlim(1, 1);
    }

    public BufferQueue() : this(Empty.Action)
    {
    }

    public void Push(byte[] buffer, int length)
    {
        _lock.Wait();
        try
        {
            var lastHead = _head;
            _head = new BufferListNode(buffer, length, _disposeBuffer);

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

    public BufferListNode? Pop()
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

            return lastTail;
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
        _head?.Dispose();
        GC.SuppressFinalize(this);
    }

    public class BufferListNode : IDisposable
    {
        private readonly Action<byte[]> _dispose;
        private readonly byte[] _buffer;
        private readonly int _length;

        public BufferListNode? Prev { get; set; }
        public BufferListNode? Next { get; set; }

        public ReadOnlySpan<byte> Span => _buffer.AsSpan()[.._length];

        public BufferListNode(byte[] buffer, int length, Action<byte[]> dispose)
        {
            _buffer = buffer;
            _length = length;
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose(_buffer);
            Next?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}