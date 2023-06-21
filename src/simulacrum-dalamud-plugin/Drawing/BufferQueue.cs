namespace Simulacrum.Drawing;

public class BufferQueue : IDisposable
{
    private readonly Action<byte[]> _disposeBuffer;
    private readonly SemaphoreSlim _lock;

    private BufferListNode? _head;
    private BufferListNode? _tail;

    public BufferQueue(Action<byte[]> disposeBuffer)
    {
        _disposeBuffer = disposeBuffer;
        _lock = new SemaphoreSlim(1, 1);
    }

    public void Push(byte[] buffer, int length)
    {
        _lock.Wait();
        try
        {
            var temp = _head;
            _head = new BufferListNode(buffer, length, _disposeBuffer);
            _head.Next = temp;

            _tail ??= _head;
        }
        finally
        {
            _lock.Release();
        }
    }

    public BufferListNode Pop()
    {
        _lock.Wait();
        try
        {
            var temp = _head;
            if (temp == null)
            {
                throw new InvalidOperationException("No elements are remaining in the list.");
            }

            _head = _head?.Next;

            if (_head == null)
            {
                _tail = null;
            }

            temp.Next = null;
            return temp;
        }
        finally
        {
            _lock.Release();
        }
    }

    public bool IsEmpty()
    {
        // This is safe to access outside of the semaphore because it doesn't mutate the list.
        return _head == null;
    }

    public void Dispose()
    {
        _head?.Dispose();
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }

    public class BufferListNode : IDisposable
    {
        private readonly Action<byte[]> _dispose;
        private readonly byte[] _buffer;
        private readonly int _length;

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