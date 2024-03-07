using System.Runtime.CompilerServices;

namespace Simulacrum.AV;

public class MpvRenderContext : IDisposable
{
    private nint _context;

    public unsafe MpvRenderContext(MpvHandle handle)
    {
        Span<MpvRenderParam> contextParams = stackalloc MpvRenderParam[2];

        var apiType = "sw"u8.ToArray().AsSpan();
        fixed (byte* apiTypePtr = apiType)
        {
            contextParams[0] = new MpvRenderParam { Type = MpvRenderParamType.ApiType, Data = (nint)apiTypePtr };

            var result = MpvRender.CreateContext(_context, handle._handle, contextParams);
            MpvException.ThrowMpvError(result);
        }
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