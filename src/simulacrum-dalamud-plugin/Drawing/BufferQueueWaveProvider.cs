using NAudio.Wave;

namespace Simulacrum.Drawing;

/// <summary>
/// A wrapper around <see cref="BufferQueue"/> enabling wave playback through NAudio.
/// TODO: This may need to be moved into native code, since we have 3 buffer copies between the audio frame and NAudio.
/// </summary>
public class BufferQueueWaveProvider : IWaveProvider, IDisposable
{
    private readonly BufferQueue _bufferQueue;
    private BufferQueue.BufferListNode? _currentNode;
    private int _currentNodeIndex;
    private int _currentNodeSize;

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