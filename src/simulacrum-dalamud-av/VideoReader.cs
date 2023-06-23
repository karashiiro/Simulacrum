using System.Runtime.InteropServices;

namespace Simulacrum.AV;

public partial class VideoReader : IDisposable
{
    private nint _ptr;

    public int Width => _ptr != nint.Zero ? VideoReaderGetWidth(_ptr) : 0;
    public int Height => _ptr != nint.Zero ? VideoReaderGetHeight(_ptr) : 0;
    public bool SupportsAudio => _ptr != nint.Zero && VideoReaderSupportsAudio(_ptr);
    public int SampleRate => _ptr != nint.Zero ? VideoReaderGetSampleRate(_ptr) : 0;
    public int BitsPerSample => _ptr != nint.Zero ? VideoReaderGetBitsPerSample(_ptr) : 0;
    public int AudioChannelCount => _ptr != nint.Zero ? VideoReaderGetAudioChannelCount(_ptr) : 0;

    public VideoReader()
    {
        _ptr = VideoReaderAlloc();
    }

    public bool Open(string? filename)
    {
        ArgumentNullException.ThrowIfNull(filename);
        return _ptr != nint.Zero && VideoReaderOpen(_ptr, filename);
    }

    public int ReadAudioStream(Span<byte> audioBuffer)
    {
        return _ptr != nint.Zero ? VideoReaderReadAudioStream(_ptr, audioBuffer, audioBuffer.Length) : 0;
    }

    public bool ReadFrame(Span<byte> frameBuffer, double targetPts, out double pts)
    {
        pts = 0;
        return _ptr != nint.Zero && VideoReaderReadFrame(_ptr, frameBuffer, targetPts, out pts);
    }

    public bool SeekFrame(double pts)
    {
        return _ptr != nint.Zero && VideoReaderSeekFrame(_ptr, pts);
    }

    public void Close()
    {
        if (_ptr == nint.Zero)
        {
            return;
        }

        VideoReaderClose(_ptr);
    }

    private void ReleaseUnmanagedResources()
    {
        if (_ptr == nint.Zero)
        {
            return;
        }

        VideoReaderFree(_ptr);
        _ptr = nint.Zero;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VideoReader()
    {
        ReleaseUnmanagedResources();
    }

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderAlloc")]
    internal static partial nint VideoReaderAlloc();

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderFree")]
    internal static partial void VideoReaderFree(nint reader);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderOpen")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VideoReaderOpen(nint reader, [MarshalAs(UnmanagedType.LPStr)] string uri);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderReadAudioStream")]
    internal static partial int VideoReaderReadAudioStream(nint reader, Span<byte> audioBuffer, int len);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderReadFrame")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VideoReaderReadFrame(nint reader, Span<byte> frameBuffer, in double targetPts,
        out double pts);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderSeekFrame")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VideoReaderSeekFrame(nint reader, double pts);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderClose")]
    internal static partial void VideoReaderClose(nint reader);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderGetWidth")]
    internal static partial int VideoReaderGetWidth(nint reader);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderGetHeight")]
    internal static partial int VideoReaderGetHeight(nint reader);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderGetSampleRate")]
    internal static partial int VideoReaderGetSampleRate(nint reader);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderGetBitsPerSample")]
    internal static partial int VideoReaderGetBitsPerSample(nint reader);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderGetAudioChannelCount")]
    internal static partial int VideoReaderGetAudioChannelCount(nint reader);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderSupportsAudio")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VideoReaderSupportsAudio(nint reader);
}