using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Simulacrum.AV;

public class MpvHandle : IDisposable
{
    internal nint _handle;

    public MpvHandle()
    {
        _handle = MpvClient.Create();
        MpvException.ThrowMpvError(MpvClient.Initialize(_handle));
    }

    public void LoadFile(string uri)
    {
        Command("loadfile", uri);
    }

    public void Seek(TimeSpan ts)
    {
        Command("seek", ts.TotalSeconds.ToString(CultureInfo.InvariantCulture), "absolute");
    }

    public unsafe void Command(params string[] args)
    {
        if (_handle == nint.Zero) return;

        /*
         * It's like this https://github.com/mpv-player/mpv-examples/blob/57d26935c0525585482748b7236b60fe83d3f044/libmpv/csharp/Form1.cs#L122-L153
         * but without as many allocations. It's probably not necessary, but it was fun to write.
         */

        // Get the UTF8 lengths of each argument, plus null terminators
        var argSizes = args.Select(arg => Encoding.UTF8.GetByteCount(arg) + 1).ToArray();

        // Allocate a big chunk of stack memory to shove it all in
        var bufferSize = argSizes.Sum();
        Span<byte> buffer = stackalloc byte[bufferSize];

        // Allocate a pointer array of the addresses of each string within the buffer, plus a null terminator
        Span<nint> argPointers = stackalloc nint[args.Length + 1];

        var bufferIndex = 0;
        for (var i = 0; i < args.Length; i++)
        {
            // Get the subsection of the buffer that we want to copy the arg into
            var bufferArg = buffer.Slice(bufferIndex, argSizes[i]);

            // Convert the string to its UTF8 representation
            Encoding.UTF8.GetBytes(args[i], bufferArg);

            // Get an interior pointer to the string within the buffer and store it.
            // This is safe, because we're creating a pointer into a stack-allocated array.
            argPointers[i] = (nint)Unsafe.AsPointer(ref bufferArg[0]);

            bufferIndex += argSizes[i];
        }

        // Pass all of that into mpv_command, which is expecting a char** of args
        MpvException.ThrowMpvError(MpvClient.Command(_handle, argPointers));
    }

    public void SetWakeupCallback(MpvClient.MpvWakeupCallback callback)
    {
        if (_handle == nint.Zero) return;
        MpvClient.SetWakeupCallback(_handle, callback, nint.Zero);
    }

    public void SetOption(string name, long data)
    {
        Span<byte> rawData = stackalloc byte[sizeof(long)];
        Unsafe.As<byte, long>(ref rawData[0]) = data;
        SetOption(name, MpvFormat.Int64, rawData);
    }

    public void SetOption(string name, MpvFormat format, ReadOnlySpan<byte> data)
    {
        if (_handle == nint.Zero) return;
        MpvException.ThrowMpvError(MpvClient.SetOption(_handle, name, (int)format, data));
    }

    public void SetOptionString(string name, string data)
    {
        if (_handle == nint.Zero) return;
        MpvException.ThrowMpvError(MpvClient.SetOptionString(_handle, name, data));
    }

    public void Play()
    {
        SetPropertyString("pause", "no");
    }

    public void Pause()
    {
        SetPropertyString("pause", "yes");
    }

    public void GetProperty(string name, MpvFormat format, ref nint data)
    {
        if (_handle == nint.Zero) return;
        MpvException.ThrowMpvError(MpvClient.GetProperty(_handle, name, (int)format, ref data));
    }

    public void SetPropertyString(string name, string data)
    {
        if (_handle == nint.Zero) return;
        MpvException.ThrowMpvError(MpvClient.SetPropertyString(_handle, name, data));
    }

    private void ReleaseUnmanagedResources()
    {
        if (_handle == nint.Zero) return;
        MpvException.ThrowMpvError(MpvClient.TerminateDestroy(_handle));
        _handle = nint.Zero;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~MpvHandle()
    {
        ReleaseUnmanagedResources();
    }
}