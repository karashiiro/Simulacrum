﻿using System.Buffers;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using NAudio.Wave;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class VideoReaderMediaSource : IMediaSource, IDisposable
{
    private const int AudioBufferSize = 65536;

    private static readonly TimeSpan AudioSyncThreshold = TimeSpan.FromMilliseconds(100);

    private readonly VideoReader _reader;

    private readonly nint _cacheBufferPtr;
    private readonly int _cacheBufferRawSize;
    private readonly int _cacheBufferSize;

    private readonly BufferQueue _audioBufferQueue;
    private readonly BufferQueueWaveProvider _waveProvider;
    private readonly IWavePlayer _wavePlayer;
    private readonly Thread _audioThread;

    private readonly IReadOnlyPlaybackTracker _sync;
    private readonly IDisposable _unsubscribePlay;
    private readonly IDisposable _unsubscribePause;

    private TimeSpan _nextPts;
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

        _audioBufferQueue = new BufferQueue(buffer => ArrayPool<byte>.Shared.Return(buffer));
        _waveProvider = new BufferQueueWaveProvider(_audioBufferQueue,
            new WaveFormat(_reader.SampleRate, _reader.BitsPerSample, _reader.AudioChannelCount));
        _wavePlayer = new DirectSoundOut();
        _wavePlayer.Init(_waveProvider);
        _audioThread = new Thread(TickAudio);
        _audioThread.Start();

        _unsubscribePause = _sync.OnPause().Subscribe(_ => _wavePlayer.Pause());
        _unsubscribePlay = _sync.OnPlay().Subscribe(_ => _wavePlayer.Play());
    }

    private void TickAudio()
    {
        while (!_done)
        {
            if (BufferAudio() > 0)
            {
                if (_wavePlayer.PlaybackState == PlaybackState.Stopped)
                {
                    _wavePlayer.Play();
                }
            }

            if (_wavePlayer.PlaybackState == PlaybackState.Stopped)
            {
                continue;
            }

            // Discard audio samples if the audio pts is ahead of the clock, and pad
            // silence if the audio pts is behind the clock.
            var audioDiff = _waveProvider.PlaybackPosition - _sync.GetTime();
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

    private int BufferAudio()
    {
        var audioBuffer = ArrayPool<byte>.Shared.Rent(AudioBufferSize);
        var audioSpan = audioBuffer.AsSpan(0, AudioBufferSize);

        var audioBytesRead = _reader.ReadAudioStream(audioSpan);
        if (audioBytesRead > 0)
        {
            _audioBufferQueue.Push(audioBuffer, audioBytesRead);
        }
        else
        {
            ArrayPool<byte>.Shared.Return(audioBuffer);
        }

        return audioBytesRead;
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

        var audioPts = _waveProvider.PlaybackPosition;
        PluginLog.Log($"t={t} dv={_nextPts - t} da={audioPts - t}");

        // Read frames until the pts matches the external clock, or until there are
        // no frames left to read.
        // TODO: Why does this need to be 2s ahead?
        if (!_reader.ReadFrame(cacheBuffer, t.TotalSeconds + 2, out var pts))
        {
            // Don't trust the pts if we failed to read a frame.
            return;
        }

        _nextPts = TimeSpan.FromSeconds(pts - 2);
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
        _unsubscribePlay.Dispose();
        _unsubscribePause.Dispose();
        _reader.Close();
        _reader.Dispose();
        _wavePlayer.Dispose();
        _waveProvider.Dispose();
        _audioBufferQueue.Dispose();
        Marshal.FreeHGlobal(_cacheBufferPtr);
        GC.SuppressFinalize(this);
    }
}