using NAudio.Wave;

namespace Simulacrum.Drawing;

/// <summary>
/// A wrapper around <see cref="BufferQueue"/> enabling wave playback through NAudio.
/// TODO: This may need to be moved into native code, since we have 3 buffer copies between the audio frame and NAudio.
/// </summary>
public class BufferQueueWaveProvider : IWaveProvider, IDisposable
{
    private readonly SemaphoreSlim _lock;
    private readonly BufferQueue _bufferQueue;
    private BufferQueue.BufferListNode? _currentNode;
    private int _currentNodeIndex;
    private int _currentNodeSize;
    private int _totalRead;
    private int _silentBytes;

    public WaveFormat WaveFormat { get; }

    public TimeSpan PlaybackPosition => GetDurationForByteCount(_totalRead);

    public BufferQueueWaveProvider(BufferQueue bufferQueue, WaveFormat waveFormat)
    {
        _lock = new SemaphoreSlim(1, 1);
        _bufferQueue = bufferQueue;
        WaveFormat = waveFormat;
    }

    public int PadSamples(TimeSpan duration)
    {
        if (_silentBytes > 0 || duration < TimeSpan.Zero)
        {
            return 0;
        }

        var seconds = duration.TotalSeconds;
        var toPad = Convert.ToInt32(WaveFormat.AverageBytesPerSecond * seconds);
        var toPadPadded = toPad + toPad % WaveFormat.BlockAlign;

        _silentBytes += toPadPadded;

        return toPadPadded;
    }

    public int DiscardSamples(TimeSpan duration)
    {
        if (duration > TimeSpan.Zero)
        {
            return 0;
        }

        var seconds = -duration.TotalSeconds;
        var toDiscard = Convert.ToInt32(WaveFormat.AverageBytesPerSecond * seconds);
        var toDiscardPadded = toDiscard + toDiscard % WaveFormat.BlockAlign;

        _lock.Wait();
        try
        {
            return DiscardInternal(toDiscardPadded);
        }
        finally
        {
            _lock.Release();
        }
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        _lock.Wait();
        try
        {
            return Read(buffer.AsSpan(offset, count));
        }
        finally
        {
            _lock.Release();
        }
    }

    public int Read(Span<byte> buffer)
    {
        var nSkipped = Math.Min(buffer.Length, _silentBytes);
        if (nSkipped > 0)
        {
            _silentBytes -= nSkipped;
            buffer[..nSkipped].Clear();
        }

        return ReadInternal(buffer[nSkipped..]) + nSkipped;
    }

    private int DiscardInternal(int n)
    {
        var nSkipped = 0;
        while (nSkipped < n)
        {
            if (_currentNode == null && !ReadNextNode())
            {
                break;
            }

            var toRead = Math.Min(n - nSkipped, _currentNodeSize);
            nSkipped += toRead;
            _currentNodeIndex += toRead;
            _currentNodeSize -= toRead;

            if (_currentNodeSize == 0)
            {
                _currentNode?.Dispose();
                _currentNode = null;
            }
        }

        _totalRead += nSkipped;

        return nSkipped;
    }

    private int ReadInternal(Span<byte> buffer)
    {
        var nRead = 0;
        while (nRead < buffer.Length)
        {
            if (_currentNode == null && !ReadNextNode())
            {
                break;
            }

            var toRead = Math.Min(buffer.Length - nRead, _currentNodeSize);
            _currentNode?.Span.Slice(_currentNodeIndex, toRead).CopyTo(buffer.Slice(nRead, toRead));
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

        return nRead;
    }

    private bool ReadNextNode()
    {
        if (_bufferQueue.IsEmpty())
        {
            return false;
        }

        _currentNode = _bufferQueue.Pop();
        if (_currentNode == null)
        {
            return false;
        }

        _currentNodeIndex = 0;
        _currentNodeSize = _currentNode.Span.Length;

        return true;
    }

    private TimeSpan GetDurationForByteCount(int bytes)
    {
        return TimeSpan.FromSeconds(bytes / Convert.ToDouble(WaveFormat.AverageBytesPerSecond));
    }

    public void Dispose()
    {
        _currentNode?.Dispose();
        GC.SuppressFinalize(this);
    }
}