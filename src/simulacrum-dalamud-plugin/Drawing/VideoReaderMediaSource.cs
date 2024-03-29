﻿using System.Buffers;
using System.Runtime.InteropServices;
using Dalamud.Plugin.Services;
using NAudio.Wave;
using R3;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;
using Simulacrum.Monitoring;

namespace Simulacrum.Drawing;

public class VideoReaderMediaSource : IMediaSource, IDisposable
{
    private const int AudioBufferMinSize = 65536;
    private const int AudioBufferQueueMaxItems = 8;

    private static readonly IHistogram? VideoReaderRenderDuration =
        DebugMetrics.CreateHistogram("simulacrum_video_reader_render_duration",
            "The render duration of the video reader (s).");

    private static readonly IHistogram? VideoReaderAudioBufferDuration =
        DebugMetrics.CreateHistogram("simulacrum_video_reader_audio_buffer_duration",
            "The audio chunk buffering duration (ms).");

    private static readonly TimeSpan AudioSyncThreshold = TimeSpan.FromMilliseconds(100);

    private readonly VideoReader _reader;

    private readonly nint _videoBufferPtr;
    private readonly int _videoBufferRawSize;
    private readonly int _videoBufferSize;

    // This needs to be a dedicated thread or else playback can get choppy randomly
    private readonly Thread _videoThread;

    private readonly BufferQueueWaveProvider _waveProvider;
    private readonly IWavePlayer _wavePlayer; // TODO: Move this into the screen class for spatial audio
    private readonly Thread _audioThread;

    private readonly IReadOnlyPlaybackTracker _sync;
    private readonly IDisposable _unsubscribeAll;

    private readonly IPluginLog _log;

    private TimeSpan _nextPts;
    private bool _audioFlushRequested;
    private bool _done;

    private unsafe Span<byte> VideoBuffer => new((byte*)_videoBufferPtr, _videoBufferRawSize);

    public VideoReaderMediaSource(string? uri, IReadOnlyPlaybackTracker sync, IPluginLog log)
    {
        _log = log;

        ArgumentNullException.ThrowIfNull(uri);

        _reader = new VideoReader();
        if (!_reader.Open(uri))
        {
            throw new InvalidOperationException("Failed to open video.");
        }

        _sync = sync;
        _videoBufferSize = _reader.Width * _reader.Height * PixelSize();

        // For some reason, sws_scale writes 8 black pixels after the end of the buffer.
        // If video playback randomly crashes, it's probably because this needs to be
        // more specific.
        _videoBufferRawSize = _videoBufferSize + 32;
        _videoBufferPtr = Marshal.AllocHGlobal(_videoBufferRawSize);

        _videoThread = new Thread(VideoLoop);
        _videoThread.Start();

        _waveProvider =
            new BufferQueueWaveProvider(new WaveFormat(_reader.SampleRate, _reader.BitsPerSample,
                _reader.AudioChannelCount));
        _wavePlayer = new DirectSoundOut();
        _wavePlayer.Init(_waveProvider);
        _audioThread = new Thread(AudioLoop);
        _audioThread.Start();

        var unsubscribePause = _sync.OnPause().Subscribe(_wavePlayer, static (_, wp) => wp.Pause());
        var unsubscribePlay = _sync.OnPlay().Subscribe(_wavePlayer, static (_, wp) => wp.Play());
        var unsubscribePan = _sync.OnPan().Subscribe(this, static (targetPts, ms) =>
        {
            var audioDiff = targetPts - ms._waveProvider.PlaybackPosition;
            if (audioDiff < TimeSpan.Zero || audioDiff > TimeSpan.FromSeconds(5))
            {
                if (!ms._reader.SeekAudioStream(targetPts.TotalSeconds))
                {
                    ms._log.Warning("Failed to seek through audio stream");
                }

                ms._audioFlushRequested = true;
            }

            var videoDiff = targetPts - ms._nextPts;
            if (videoDiff < TimeSpan.Zero || videoDiff > TimeSpan.FromSeconds(5))
            {
                if (!ms._reader.SeekVideoFrame(targetPts.TotalSeconds))
                {
                    ms._log.Warning("Failed to seek through video stream");
                }

                ms._nextPts = targetPts + ms._reader.VideoFrameDelay;
            }
        });

        _unsubscribeAll = Disposable.Combine(unsubscribePause, unsubscribePlay, unsubscribePan);
    }

    public void RenderTo(Span<byte> buffer)
    {
        VideoBuffer[.._videoBufferSize].CopyTo(buffer);
    }

    public void RenderTo(Span<byte> buffer, out TimeSpan delay)
    {
        RenderTo(buffer);
        delay = _reader.VideoFrameDelay;
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
        var ts = _sync.GetTime();
        var pos = _waveProvider.PlaybackPosition;
        var audioDiff = pos - ts;
        if (audioDiff > AudioSyncThreshold)
        {
            // Pad samples with silence to delay playback slightly
            var nPadded = _waveProvider.PadSamples(audioDiff);
            if (nPadded > 0)
            {
                _log.Warning(
                    $"Audio stream was {audioDiff.ToString()} ahead of clock, padded {nPadded} bytes of data (wav={pos.ToString()}, sync={ts.ToString()})");
            }
        }
        else if (audioDiff < -AudioSyncThreshold)
        {
            /*
             Discarding samples from our audio buffer doesn't discard samples from the
             audio device interface responsible for actually playing sound. This means
             that if the time difference is large enough, we can get stuck attempting
             to discard samples from our buffer because all of that time difference is
             in the device buffer, which we can't directly manipulate.

             Instead, we stop the player to force it to flush samples, and then restart
             it after discarding the samples we need to get rid of.
            */
            _wavePlayer.Stop();

            // Discard samples to catch up
            var nDiscarded = _waveProvider.DiscardSamples(audioDiff);
            if (nDiscarded > 0)
            {
                _log.Warning(
                    $"Audio stream was {audioDiff.ToString()} behind clock, discarded {nDiscarded} bytes of data (wav={pos.ToString()}, sync={ts.ToString()})");
            }

            _wavePlayer.Play();
        }
    }

    private int BufferAudio()
    {
        if (_waveProvider.Count > AudioBufferQueueMaxItems)
        {
            // Ensure we don't have too many large buffers floating around at once
            return 0;
        }

        // Rent a buffer for the audio data; we want it to be larger when we have fewer buffers already
        var audioBufferScale = Convert.ToDouble(AudioBufferQueueMaxItems) / Math.Max(_waveProvider.Count, 1);
        var audioBufferSize = Convert.ToInt32(AudioBufferMinSize * audioBufferScale);
        var audioBuffer = ArrayPool<byte>.Shared.Rent(audioBufferSize);
        var audioSpan = audioBuffer.AsSpan(0, audioBufferSize);

        int audioBytesRead;
        double pts;
        var startTime = _sync.GetTime();
        try
        {
            audioBytesRead = _reader.ReadAudioStream(audioSpan, out pts);
        }
        finally
        {
            VideoReaderAudioBufferDuration?.Observe((_sync.GetTime() - startTime).TotalMilliseconds);
        }

        if (audioBytesRead > 0)
        {
            _waveProvider.Enqueue(new BufferQueueNode(audioBuffer, audioBytesRead, pts,
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
                _log.Error(e, "Error in audio loop");
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
        try
        {
            if (!_reader.ReadVideoFrame(VideoBuffer, t.TotalSeconds, out _))
            {
                // Don't trust the pts if we failed to read a frame.
                return;
            }
        }
        finally
        {
            VideoReaderRenderDuration?.Observe((_sync.GetTime() - t).TotalSeconds);
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
                _log.Error(e, "Error in video loop");
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

        _unsubscribeAll.Dispose();
        _reader.Close();
        _reader.Dispose();
        _wavePlayer.Dispose();
        _waveProvider.Dispose();

        Marshal.FreeHGlobal(_videoBufferPtr);
        GC.SuppressFinalize(this);
    }
}