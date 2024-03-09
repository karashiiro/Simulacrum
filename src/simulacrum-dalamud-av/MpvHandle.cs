namespace Simulacrum.AV;

public class MpvHandle : IDisposable
{
    internal nint _handle;

    public MpvHandle()
    {
        _handle = MpvClient.Create();
        MpvException.ThrowMpvError(MpvClient.Initialize(_handle));
    }

    public void Command(params string[][] args)
    {
        if (_handle == nint.Zero)
        {
            return;
        }

        MpvException.ThrowMpvError(MpvClient.Command(_handle, args));
    }

    public void SetOption(ReadOnlySpan<byte> name, int format, ReadOnlySpan<byte> data)
    {
        if (_handle == nint.Zero)
        {
            return;
        }

        MpvException.ThrowMpvError(MpvClient.SetOption(_handle, name, format, data));
    }

    public void SetOptionString(ReadOnlySpan<byte> name, ReadOnlySpan<byte> data)
    {
        if (_handle == nint.Zero)
        {
            return;
        }

        MpvException.ThrowMpvError(MpvClient.SetOptionString(_handle, name, data));
    }

    public void GetProperty(ReadOnlySpan<byte> name, int format, ref nint data)
    {
        if (_handle == nint.Zero)
        {
            return;
        }

        MpvException.ThrowMpvError(MpvClient.GetProperty(_handle, name, format, ref data));
    }

    public void SetProperty(ReadOnlySpan<byte> name, int format, ReadOnlySpan<byte> data)
    {
        if (_handle == nint.Zero)
        {
            return;
        }

        MpvException.ThrowMpvError(MpvClient.SetProperty(_handle, name, format, data));
    }

    private void ReleaseUnmanagedResources()
    {
        if (_handle == nint.Zero)
        {
            return;
        }

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