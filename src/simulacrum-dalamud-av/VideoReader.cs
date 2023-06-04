using System.Runtime.InteropServices;

namespace Simulacrum.AV;

public partial class VideoReader : IDisposable
{
    private readonly nint _ptr;

    public VideoReader()
    {
        _ptr = VideoReaderAlloc();
    }

    public bool Open(string filename)
    {
        return VideoReaderOpen(_ptr, filename);
    }

    public bool ReadFrame(Span<byte> frameBuffer, out long pts)
    {
        return VideoReaderReadFrame(_ptr, frameBuffer, out pts);
    }

    public bool SeekFrame(long ts)
    {
        return VideoReaderSeekFrame(_ptr, ts);
    }

    public void Close()
    {
        VideoReaderClose(_ptr);
    }

    private void ReleaseUnmanagedResources()
    {
        VideoReaderFree(_ptr);
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


    [LibraryImport("Simulacrum.AV.Core", EntryPoint = "VideoReaderAlloc")]
    internal static partial nint VideoReaderAlloc();

    [LibraryImport("Simulacrum.AV.Core", EntryPoint = "VideoReaderFree")]
    internal static partial void VideoReaderFree(nint reader);

    [LibraryImport("Simulacrum.AV.Core", EntryPoint = "VideoReaderOpen")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VideoReaderOpen(nint reader, [MarshalAs(UnmanagedType.LPStr)] string filename);

    [LibraryImport("Simulacrum.AV.Core", EntryPoint = "VideoReaderReadFrame")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VideoReaderReadFrame(nint reader, Span<byte> frameBuffer, out long pts);

    [LibraryImport("Simulacrum.AV.Core", EntryPoint = "VideoReaderSeekFrame")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VideoReaderSeekFrame(nint reader, long ts);

    [LibraryImport("Simulacrum.AV.Core", EntryPoint = "VideoReaderClose")]
    internal static partial void VideoReaderClose(nint reader);
}