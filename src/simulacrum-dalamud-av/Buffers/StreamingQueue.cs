using System.Collections;
using System.Reactive.Subjects;

namespace Simulacrum.AV.Buffers;

/// <summary>
/// A producer-consumer queue. Nodes cannot be directly popped; they can only be accessed via the
/// enumerator, which mutates a node's reference counter and marks it for release once all consumers
/// have consumed it.
/// </summary>
/// <typeparam name="T">The inner node type.</typeparam>
public class StreamingQueue<T> : IDisposable, IEnumerable<StreamingQueueNodeRef<T>?> where T : class
{
    private readonly SemaphoreSlim _lock;
    private readonly Subject<bool> _clear;

    private StreamingQueueNode<T>? _head;
    private StreamingQueueNode<T>? _tail;

    public StreamingQueue()
    {
        _lock = new SemaphoreSlim(1, 1);
        _clear = new Subject<bool>();
    }

    public void Push(T node)
    {
        _lock.Wait();
        try
        {
            var lastTail = _tail;
            _tail = new StreamingQueueNode<T>(node)
            {
                Prev = lastTail,
            };

            if (lastTail != null)
            {
                lastTail.Next = _tail;
            }

            _head ??= _tail;
        }
        finally
        {
            _lock.Release();
        }
    }

    public StreamingQueueNode<T>? Peek()
    {
        _lock.Wait();
        try
        {
            while (_head?.RefCount == 0)
            {
                _head = _head.Next;
            }

            return _head;
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
            _clear.OnNext(true);
        }
        finally
        {
            _lock.Release();
        }
    }

    public IObservable<bool> OnClear()
    {
        return _clear;
    }

    public IEnumerator<StreamingQueueNodeRef<T>?> GetEnumerator()
    {
        return new StreamingQueueEnumerator<T>(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Dispose()
    {
        _lock.Dispose();
        _clear.Dispose();
        GC.SuppressFinalize(this);
    }
}