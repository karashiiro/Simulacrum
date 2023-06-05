using System.Runtime.InteropServices;
using Simulacrum.AV.Structures;

namespace Simulacrum.AV;

public partial class VideoReader : IDisposable
{
    private nint _ptr;

    public int Width => VideoReaderGetWidth(_ptr);
    public int Height => VideoReaderGetHeight(_ptr);
    public AVRational TimeBase => VideoReaderGetTimeBase(_ptr);

    public VideoReader()
    {
        _ptr = VideoReaderAlloc();
    }

    public bool Open(string filename)
    {
        return _ptr != nint.Zero && VideoReaderOpen(_ptr, filename);
    }

    public bool ReadFrame(Span<byte> frameBuffer, out long pts)
    {
        pts = 0;
        return _ptr != nint.Zero && VideoReaderReadFrame(_ptr, frameBuffer, out pts);
    }

    public bool SeekFrame(long ts)
    {
        return _ptr != nint.Zero && VideoReaderSeekFrame(_ptr, ts);
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
    internal static partial bool VideoReaderOpen(nint reader, [MarshalAs(UnmanagedType.LPStr)] string filename);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderReadFrame")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VideoReaderReadFrame(nint reader, Span<byte> frameBuffer, out long pts);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderSeekFrame")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VideoReaderSeekFrame(nint reader, long ts);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderClose")]
    internal static partial void VideoReaderClose(nint reader);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderGetWidth")]
    internal static partial int VideoReaderGetWidth(nint reader);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderGetHeight")]
    internal static partial int VideoReaderGetHeight(nint reader);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "VideoReaderGetTimeBase")]
    internal static partial AVRational VideoReaderGetTimeBase(nint reader);
}