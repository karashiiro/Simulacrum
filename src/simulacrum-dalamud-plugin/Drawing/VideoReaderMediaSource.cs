﻿using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using NAudio.Wave;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class VideoReaderMediaSource : IMediaSource, IDisposable
{
    private const int AudioBufferMinSize = 65536;
    private const int AudioBufferQueueMaxItems = 8;

    private static readonly TimeSpan AudioSyncThreshold = TimeSpan.FromMilliseconds(100);

    private readonly VideoReader _reader;

    private readonly nint _cacheBufferPtr;
    private readonly int _cacheBufferRawSize;
    private readonly int _cacheBufferSize;
    private readonly Thread _videoThread;

    private readonly ConcurrentQueue<BufferQueueNode> _audioBufferQueue;
    private readonly BufferQueueWaveProvider _waveProvider;
    private readonly IWavePlayer _wavePlayer;
    private readonly Thread _audioThread;

    private readonly IReadOnlyPlaybackTracker _sync;
    private readonly IDisposable _unsubscribePlay;
    private readonly IDisposable _unsubscribePause;
    private readonly IDisposable _unsubscribePan;

    private TimeSpan _nextPts;
    private bool _audioFlushRequested;
    private bool _done;

    private unsafe Span<byte> CacheBuffer => new((byte*)_cacheBufferPtr, _cacheBufferRawSize);

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

        _videoThread = new Thread(VideoLoop);
        _videoThread.Start();

        _audioBufferQueue = new ConcurrentQueue<BufferQueueNode>();
        _waveProvider = new BufferQueueWaveProvider(_audioBufferQueue,
            new WaveFormat(_reader.SampleRate, _reader.BitsPerSample, _reader.AudioChannelCount));
        _wavePlayer = new DirectSoundOut();
        _wavePlayer.Init(_waveProvider);
        _audioThread = new Thread(AudioLoop);
        _audioThread.Start();

        _unsubscribePause = _sync.OnPause().Subscribe(_ => _wavePlayer.Pause());
        _unsubscribePlay = _sync.OnPlay().Subscribe(_ => _wavePlayer.Play());
        _unsubscribePan = _sync.OnPan().Subscribe(targetPts =>
        {
            if (!_reader.SeekAudioStream(targetPts.TotalSeconds))
            {
                PluginLog.LogWarning("Failed to seek through audio stream");
            }

            _audioFlushRequested = true;

            if (!_reader.SeekVideoFrame(targetPts.TotalSeconds))
            {
                PluginLog.LogWarning("Failed to seek through video stream");
            }

            _nextPts = targetPts + _reader.VideoFrameDelay;
        });
    }

    public void RenderTo(Span<byte> buffer)
    {
        if (_sync.GetTime() < _nextPts)
        {
            CacheBuffer[.._cacheBufferSize].CopyTo(buffer);
        }
    }

    private void HandleAudioTick()
    {
        if (_audioFlushRequested)
        {
            _waveProvider.Flush();
            _audioFlushRequested = false;
        }

        if (BufferAudio() > 0)
        {
            if (_wavePlayer.PlaybackState == PlaybackState.Stopped)
            {
                _wavePlayer.Play();
            }
        }

        if (_wavePlayer.PlaybackState == PlaybackState.Stopped)
        {
            return;
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
    }

    private int BufferAudio()
    {
        if (_audioBufferQueue.Count > AudioBufferQueueMaxItems)
        {
            // Ensure we don't have too many large buffers floating around at once
            return 0;
        }

        // Rent a buffer for the audio data; we want it to be larger when we have fewer buffers already
        var audioBufferScale = Convert.ToDouble(AudioBufferQueueMaxItems) / Math.Max(_audioBufferQueue.Count, 1);
        var audioBufferSize = Convert.ToInt32(AudioBufferMinSize * audioBufferScale);
        var audioBuffer = ArrayPool<byte>.Shared.Rent(audioBufferSize);
        var audioSpan = audioBuffer.AsSpan(0, audioBufferSize);

        var audioBytesRead = _reader.ReadAudioStream(audioSpan, out var pts);
        if (audioBytesRead > 0)
        {
            _audioBufferQueue.Enqueue(new BufferQueueNode(audioBuffer, audioBytesRead, pts,
                buffer => ArrayPool<byte>.Shared.Return(buffer)));
        }
        else
        {
            ArrayPool<byte>.Shared.Return(audioBuffer);
        }

        return audioBytesRead;
    }

    private void AudioLoop()
    {
        while (!_done)
        {
            try
            {
                HandleAudioTick();

                var bytesPerSecond = Convert.ToDouble(_waveProvider.WaveFormat.AverageBytesPerSecond);
                var delay = TimeSpan.FromSeconds(AudioBufferMinSize / bytesPerSecond);
                Thread.Sleep(delay / 2);
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, "Error in audio loop");
            }
        }
    }

    private void HandleVideoTick()
    {
        var t = _sync.GetTime();
        if (t < _nextPts)
        {
            return;
        }

        // Read frames until the pts matches the external clock, or until there are
        // no frames left to read.
        if (!_reader.ReadVideoFrame(CacheBuffer, t.TotalSeconds, out _))
        {
            // Don't trust the pts if we failed to read a frame.
            return;
        }

        _nextPts = t + _reader.VideoFrameDelay;
    }

    private void VideoLoop()
    {
        while (!_done)
        {
            try
            {
                HandleVideoTick();
                Thread.Sleep(_reader.VideoFrameDelay / 2);
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, "Error in video loop");
            }
        }
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
        _videoThread.Join();

        _unsubscribePlay.Dispose();
        _unsubscribePause.Dispose();
        _unsubscribePan.Dispose();
        _reader.Close();
        _reader.Dispose();
        _wavePlayer.Dispose();
        _waveProvider.Dispose();

        foreach (var bufferNode in _audioBufferQueue)
        {
            bufferNode.Dispose();
        }

        _audioBufferQueue.Clear();

        Marshal.FreeHGlobal(_cacheBufferPtr);
        GC.SuppressFinalize(this);
    }
}