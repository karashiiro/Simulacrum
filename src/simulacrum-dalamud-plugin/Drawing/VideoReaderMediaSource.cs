using System.Runtime.InteropServices;
using Dalamud.Logging;
using NAudio.Wave;
using Simulacrum.AV;
using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class VideoReaderMediaSource : IMediaSource, IDisposable
{
    private readonly VideoReader _reader;

    private readonly nint _cacheBufferPtr;
    private readonly int _cacheBufferRawSize;
    private readonly int _cacheBufferSize;
    private readonly int _audioBufferSize;
    private readonly BufferQueue _audioBufferQueue;
    private readonly BufferQueueWaveProvider _waveProvider;
    private readonly IWavePlayer _soundOut;

    private readonly IReadOnlyPlaybackTracker _sync;
    private readonly IDisposable _unsubscribe;
    private double _pts;

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

        _audioBufferSize = 262144;
        _audioBufferQueue = new BufferQueue();
        _waveProvider = new BufferQueueWaveProvider(_audioBufferQueue,
            new WaveFormat(_reader.SampleRate, _reader.BitsPerSample, _reader.AudioChannelCount));
        _soundOut = new DirectSoundOut();
        _soundOut.Init(_waveProvider);

        _unsubscribe = sync.OnPan().Subscribe(pts =>
        {
            _pts = pts;
            if (!_reader.SeekFrame(pts))
            {
                PluginLog.LogWarning("Failed to seek through video");
            }
        });
    }

    private void BufferAudio()
    {
        if (_audioBufferQueue.Count >= 4)
        {
            // Avoid having tons of large buffers in-flight all at once
            return;
        }

        // TODO: Why does this play stuttered audio when using ArrayPool (even an owned one)?
        var audioBuffer = new byte[_audioBufferSize];
        var audioBytesRead = _reader.ReadAudioStream(audioBuffer);
        _audioBufferQueue.Push(audioBuffer, audioBytesRead);

        if (_soundOut.PlaybackState == PlaybackState.Stopped)
        {
            _soundOut.Play();
        }
    }

    public unsafe void RenderTo(Span<byte> buffer)
    {
        BufferAudio();

        var cacheBuffer = new Span<byte>((byte*)_cacheBufferPtr, _cacheBufferRawSize);

        if (_sync.GetTime() < _pts)
        {
            cacheBuffer[.._cacheBufferSize].CopyTo(buffer);
            return;
        }

        if (!_reader.ReadFrame(cacheBuffer, out var pts))
        {
            return;
        }

        _pts = pts;
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
        _unsubscribe.Dispose();
        _reader.Close();
        _reader.Dispose();
        _soundOut.Dispose();
        _waveProvider.Dispose();
        _audioBufferQueue.Dispose();
        Marshal.FreeHGlobal(_cacheBufferPtr);
        GC.SuppressFinalize(this);
    }
}