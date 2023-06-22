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
        var nRead = 0;
        while (nRead < count)
        {
            if (_currentNode == null && !ReadNextNode())
            {
                break;
            }

            var toRead = Math.Min(count - nRead, _currentNodeSize);
            _currentNode?.Span.Slice(_currentNodeIndex, toRead).CopyTo(buffer.AsSpan(offset + nRead));
            nRead += toRead;
            _currentNodeIndex += toRead;
            _currentNodeSize -= toRead;

            if (_currentNodeSize == 0)
            {
                _currentNode?.Dispose();
                _currentNode = null;
            }
        }

        _totalRead += nRead;

        PluginLog.Log($"req={count} recv={nRead} total={_totalRead} buffer={_currentNodeSize}");
        return nRead;
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