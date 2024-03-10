namespace Simulacrum.AV;

public class MpvRenderContext : IDisposable
{
    private nint _context;

    private readonly int _width;
    private readonly int _height;

    public unsafe MpvRenderContext(MpvHandle handle, int width, int height)
    {
        _width = width;
        _height = height;

        Span<MpvRenderParam> contextParams = stackalloc MpvRenderParam[2];

        var apiType = "sw\0"u8.ToArray().AsSpan();
        fixed (byte* apiTypePtr = apiType)
        {
            contextParams[0] = new MpvRenderParam { Type = MpvRenderParamType.ApiType, Data = (nint)apiTypePtr };

            var result = MpvRender.CreateContext(ref _context, handle._handle, contextParams);
            MpvException.ThrowMpvError(result);
        }
    }

    public (int, int) GetSize()
    {
        return (_width, _height);
    }

    public unsafe void ReadVideoFrame(Span<byte> buffer)
    {
        if (_context == nint.Zero) return;

        Span<MpvRenderParam> contextParams = stackalloc MpvRenderParam[5];

        // Configure surface size
        Span<int> size = stackalloc int[2];
        size[0] = _width;
        size[1] = _height;

        // Configure surface format
        var format = "bgr0\0"u8.ToArray().AsSpan();

        // Configure surface stride
        Span<int> stride = stackalloc int[1];
        stride[0] = _width *
                    4; // inefficient if this isn't a multiple of 64 but this is only a temporary implementation

        // Set everything and call the render function
        fixed (int* sizePtr = size)
        fixed (byte* formatPtr = format)
        fixed (int* stridePtr = stride)
        fixed (byte* bufferPtr = buffer)
        {
            contextParams[0] = new MpvRenderParam { Type = MpvRenderParamType.SoftwareSize, Data = (nint)sizePtr };
            contextParams[1] = new MpvRenderParam { Type = MpvRenderParamType.SoftwareFormat, Data = (nint)formatPtr };
            contextParams[2] = new MpvRenderParam { Type = MpvRenderParamType.SoftwareStride, Data = (nint)stridePtr };
            contextParams[3] = new MpvRenderParam { Type = MpvRenderParamType.SoftwarePointer, Data = (nint)bufferPtr };

            var result = MpvRender.RenderContext(_context, contextParams);
            MpvException.ThrowMpvError(result);
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (_context == nint.Zero) return;
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