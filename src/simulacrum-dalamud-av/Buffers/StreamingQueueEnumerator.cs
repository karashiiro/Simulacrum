using System.Collections;

namespace Simulacrum.AV.Buffers;

public class StreamingQueueEnumerator<T> : IEnumerator<StreamingQueueNodeRef<T>?> where T : class
{
    private readonly StreamingQueue<T> _bufferQueue;
    private readonly IDisposable _unsubscribeClear;

    private bool _cleared;

    public StreamingQueueNodeRef<T>? Current { get; private set; }

    object? IEnumerator.Current => Current;

    public StreamingQueueEnumerator(StreamingQueue<T> bufferQueue)
    {
        _bufferQueue = bufferQueue;
        _unsubscribeClear = bufferQueue.OnClear().Subscribe(_ =>
        {
            // Set a flag that a clear happened rather than setting Current to null,
            // since that would mean Current could change in-between MoveNext calls.
            _cleared = true;
        });
    }

    public bool MoveNext()
    {
        if (_cleared)
        {
            MoveNextCleared();
        }
        else if (Current != null)
        {
            MoveNextActive();
        }
        else
        {
            MoveNextWaiting();
        }

        return true;
    }

    public void Reset()
    {
        throw new InvalidOperationException(
            "The collection may have been modified after the enumerator was created.");
    }

    private void MoveNextCleared()
    {
        Current = null;
        _cleared = false;
    }

    private void MoveNextWaiting()
    {
        var nextNode = _bufferQueue.Peek();
        if (nextNode != null)
        {
            Current = new StreamingQueueNodeRef<T>(nextNode);
        }
    }

    private void MoveNextActive()
    {
        var nextNode = Current?.Next;
        if (nextNode != null)
        {
            Current?.Dispose();
            Current = nextNode;
        }
        else
        {
            Current = null;
        }
    }

    public void Dispose()
    {
        _unsubscribeClear.Dispose();
        Current?.Dispose();
        GC.SuppressFinalize(this);
    }
}