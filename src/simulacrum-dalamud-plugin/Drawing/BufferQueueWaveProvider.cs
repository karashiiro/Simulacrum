using Dalamud.Logging;
using NAudio.Wave;

namespace Simulacrum.Drawing;

public class BufferQueueWaveProvider : IWaveProvider, IDisposable
{
    private readonly BufferQueue _bufferQueue;
    private BufferQueue.BufferListNode? _currentNode;
    private int _currentNodeIndex;
    private int _currentNodeSize;
    private int _totalRead;

    public WaveFormat WaveFormat { get; }

    public BufferQueueWaveProvider(BufferQueue bufferQueue, WaveFormat waveFormat)
    {
        _bufferQueue = bufferQueue;
        WaveFormat = waveFormat;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        if (_currentNode == null && !ReadNextNode())
        {
            return 0;
        }

        var toRead = Math.Min(count, _currentNodeSize);
        _currentNode?.Span.Slice(_currentNodeIndex, toRead).CopyTo(buffer.AsSpan(offset));
        _currentNodeIndex += toRead;
        _currentNodeSize -= toRead;

        if (_currentNodeSize == 0)
        {
            _currentNode?.Dispose();
            _currentNode = null;
        }

        _totalRead += toRead;

        PluginLog.Log($"req={count} recv={toRead} total={_totalRead} buffer={_currentNodeSize}");
        return toRead;
    }

    private bool ReadNextNode()
    {
        if (_bufferQueue.IsEmpty())
        {
            return false;
        }

        _currentNode = _bufferQueue.Pop();
        _currentNodeIndex = 0;
        _currentNodeSize = _currentNode.Span.Length;

        return true;
    }

    public void Dispose()
    {
        _currentNode?.Dispose();
        GC.SuppressFinalize(this);
    }
}