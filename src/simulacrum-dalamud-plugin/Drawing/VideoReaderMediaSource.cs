using System.Buffers;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using NAudio.Wave;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class VideoReaderMediaSource : IMediaSource, IDisposable
{
    private static readonly TimeSpan AudioSyncThreshold = TimeSpan.FromMilliseconds(100);

    private readonly VideoReader _reader;

    private readonly nint _cacheBufferPtr;
    private readonly int _cacheBufferRawSize;
    private readonly int _cacheBufferSize;

    private readonly int _audioBufferSize;
    private readonly BufferQueue _audioBufferQueue;
    private readonly BufferQueueWaveProvider _waveProvider;
    private readonly IWavePlayer _wavePlayer;
    private readonly Thread _audioThread;

    private readonly IReadOnlyPlaybackTracker _sync;
    private readonly IDisposable _unsubscribe;

    private double _nextPts;
    private bool _done;

    public VideoReaderMediaSource(string? uri, IReadOnlyPlaybackTracker sync)
    {
        ArgumentNullException.ThrowIfNull(uri);

        _reader = new VideoReader();
        if (!_reader.Open(uri))
        {
            throw new InvalidOperationException("Failed to open video.");
        }

        _sync = sync;
        _cacheBufferSize = _reader.Width * _reader.Height * PixelSize();

        // For some reason, sws_scale writes 8 black pixels after the end of the buffer.
        // If video playback randomly crashes, it's probably because this needs to be
        // more specific.
        _cacheBufferRawSize = _cacheBufferSize + 32;
        _cacheBufferPtr = Marshal.AllocHGlobal(_cacheBufferRawSize);

        _audioBufferSize = 65536;
        _audioBufferQueue = new BufferQueue(buffer => ArrayPool<byte>.Shared.Return(buffer));
        _waveProvider = new BufferQueueWaveProvider(_audioBufferQueue,
            new WaveFormat(_reader.SampleRate, _reader.BitsPerSample, _reader.AudioChannelCount));
        _wavePlayer = new DirectSoundOut();
        _wavePlayer.Init(_waveProvider);
        _audioThread = new Thread(TickAudio);
        _audioThread.Start();

        _unsubscribe = sync.OnPan().Subscribe(pts =>
        {
            _nextPts = pts;
            if (!_reader.SeekFrame(pts))
            {
                PluginLog.LogWarning("Failed to seek through video");
            }
        });
    }

    private void TickAudio()
    {
        while (!_done)
        {
            BufferAudio();

            // Discard audio samples if the audio pts is ahead of the clock, and pad
            // silence if the audio pts is behind the clock.
            var audioDiff = TimeSpan.FromSeconds(_waveProvider.PlaybackPosition.TotalSeconds - _sync.GetTime());
            if (audioDiff > AudioSyncThreshold)
            {
                var nPadded = _waveProvider.PadSamples(audioDiff);
                if (nPadded > 0)
                {
                    PluginLog.LogWarning($"Audio stream was ahead of clock, padded {nPadded} bytes of data");
                }
            }
            else if (audioDiff < -AudioSyncThreshold)
            {
                var nDiscarded = _waveProvider.DiscardSamples(audioDiff);
                if (nDiscarded > 0)
                {
                    PluginLog.LogWarning($"Audio stream was behind clock, discarded {nDiscarded} bytes of data");
                }
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(10));
        }
    }

    private void BufferAudio()
    {
        var audioBuffer = ArrayPool<byte>.Shared.Rent(_audioBufferSize);
        var audioSpan = audioBuffer.AsSpan(0, _audioBufferSize);

        var audioBytesRead = _reader.ReadAudioStream(audioSpan);
        if (audioBytesRead > 0)
        {
            _audioBufferQueue.Push(audioBuffer, audioBytesRead);

            if (_wavePlayer.PlaybackState == PlaybackState.Stopped)
            {
                _wavePlayer.Play();
            }
        }
        else
        {
            ArrayPool<byte>.Shared.Return(audioBuffer);
        }
    }

    public unsafe void RenderTo(Span<byte> buffer)
    {
        var t = _sync.GetTime();

        var cacheBuffer = new Span<byte>((byte*)_cacheBufferPtr, _cacheBufferRawSize);
        if (t < _nextPts)
        {
            cacheBuffer[.._cacheBufferSize].CopyTo(buffer);
            return;
        }

        var audioPts = _waveProvider.PlaybackPosition.TotalSeconds;
        PluginLog.Log($"t={t} dv={Math.Round(_nextPts - t, 3)} da={Math.Round(audioPts - t, 3)}");

        // Read frames until the pts matches the external clock, or until there are
        // no frames left to read.
        do
        {
            if (!_reader.ReadFrame(cacheBuffer, out var pts))
            {
                // Don't trust the pts if we failed to read a frame.
                return;
            }

            // TODO: Why does this need to be 2s ahead?
            _nextPts = pts - 2;
        } while (_nextPts < t);
    }

    public int PixelSize()
    {
        // This is set in Simulacrum::AV::Core::VideoReader::ReadFrame
        return 4;
    }

    public IntVector2 Size()
    {
        return IntVector2.Create(_reader.Width, _reader.Height);
    }

    public void Dispose()
    {
        _done = true;
        _audioThread.Join();
        _unsubscribe.Dispose();
        _reader.Close();
        _reader.Dispose();
        _wavePlayer.Dispose();
        _waveProvider.Dispose();
        _audioBufferQueue.Dispose();
        Marshal.FreeHGlobal(_cacheBufferPtr);
        GC.SuppressFinalize(this);
    }
}