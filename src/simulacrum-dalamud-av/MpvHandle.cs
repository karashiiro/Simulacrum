namespace Simulacrum.AV;

public class MpvHandle : IDisposable
{
    private nint _handle = Mpv.Create();

    public int Initialize()
    {
        return _handle != nint.Zero ? Mpv.Initialize(_handle) : -1;
    }

    public int Command(string[,] args)
    {
        return _handle != nint.Zero ? Mpv.Command(_handle, args) : -1;
    }

    public int SetOption(ReadOnlySpan<byte> name, int format, ReadOnlySpan<byte> data)
    {
        return _handle != nint.Zero ? Mpv.SetOption(_handle, name, format, data) : -1;
    }

    public int SetOptionString(ReadOnlySpan<byte> name, ReadOnlySpan<byte> data)
    {
        return _handle != nint.Zero ? Mpv.SetOptionString(_handle, name, data) : -1;
    }

    public int GetProperty(ReadOnlySpan<byte> name, int format, ref nint data)
    {
        return _handle != nint.Zero ? Mpv.GetProperty(_handle, name, format, ref data) : -1;
    }

    public int SetProperty(ReadOnlySpan<byte> name, int format, ReadOnlySpan<byte> data)
    {
        return _handle != nint.Zero ? Mpv.SetProperty(_handle, name, format, data) : -1;
    }

    private void ReleaseUnmanagedResources()
    {
        if (_handle == nint.Zero)
        {
            return;
        }

        Mpv.TerminateDestroy(_handle);
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