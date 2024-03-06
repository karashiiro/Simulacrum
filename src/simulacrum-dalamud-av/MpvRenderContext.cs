namespace Simulacrum.AV;

public class MpvRenderContext : IDisposable
{
    private nint _context;

    public MpvRenderContext(MpvHandle handle)
    {
        var result = MpvRender.CreateContext(_context, handle._handle, ReadOnlySpan<MpvRender.MpvRenderParam>.Empty);
        MpvException.ThrowMpvError(result);
    }

    private void ReleaseUnmanagedResources()
    {
        if (_context == nint.Zero)
        {
            return;
        }

        MpvRender.FreeContext(_context);
        _context = nint.Zero;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~MpvRenderContext()
    {
        ReleaseUnmanagedResources();
    }
}