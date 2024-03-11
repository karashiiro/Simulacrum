using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Simulacrum.AV;

public class MpvRenderContext : IDisposable
{
    private nint _context;
    private bool _done;
    private bool _redraw;

    private readonly MpvHandle _handle;
    private readonly Thread _thread;
    private readonly ConcurrentQueue<MpvRenderEvent> _events;

    private readonly int _width;
    private readonly int _height;

    public unsafe MpvRenderContext(MpvHandle handle, int width, int height)
    {
        _width = width;
        _height = height;

        _handle = handle;
        _events = new ConcurrentQueue<MpvRenderEvent>();

        Span<MpvRenderParam> contextParams = stackalloc MpvRenderParam[2];

        var apiType = "sw\0"u8.ToArray().AsSpan();
        fixed (byte* apiTypePtr = apiType)
        {
            contextParams[0] = new MpvRenderParam { Type = MpvRenderParamType.ApiType, Data = (nint)apiTypePtr };

            var result = MpvRender.CreateContext(ref _context, handle._handle, contextParams);
            MpvException.ThrowMpvError(result);
        }

        handle.SetWakeupCallback(OnMpvEvent);
        SetContextUpdateCallback(OnMpvRenderUpdate);

        _thread = new Thread(RenderLoop);
        _thread.Start();
    }

    public void SetContextUpdateCallback(MpvRender.MpvRenderUpdateCallback callback)
    {
        MpvRender.SetContextUpdateCallback(_context, OnMpvRenderUpdate, nint.Zero);
    }

    private void RenderLoop()
    {
        // TODO: Make the media source responsible for this
        while (!_done)
        {
            if (!_events.TryDequeue(out var @event))
            {
                Thread.Sleep(1);
                continue;
            }

            switch (@event)
            {
                case MpvRenderEvent.RenderUpdate:
                {
                    var flags = MpvRender.UpdateContext(_context);
                    if ((flags & 1) == 1)
                    {
                        _redraw = true;
                    }

                    break;
                }
                case MpvRenderEvent.ClientWakeup:
                {
                    MpvEvent eventPayload;
                    do
                    {
                        unsafe
                        {
                            eventPayload = *MpvClient.WaitEvent(_handle._handle, 0);
                        }
                    } while (eventPayload.EventId != MpvEventId.None);

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void OnMpvEvent(nint ctx)
    {
        _events.Enqueue(MpvRenderEvent.ClientWakeup);
    }

    private void OnMpvRenderUpdate(nint ctx)
    {
        _events.Enqueue(MpvRenderEvent.RenderUpdate);
    }

    public (int, int) GetSize()
    {
        return (_width, _height);
    }

    public unsafe void ReadVideoFrame(Span<byte> buffer)
    {
        if (_context == nint.Zero) return;

        while (!_redraw)
        {
            Thread.Sleep(1);
        }

        Span<MpvRenderParam> contextParams = stackalloc MpvRenderParam[5];

        // Configure surface size
        Span<int> size = stackalloc int[2];
        size[0] = _width;
        size[1] = _height;

        // Configure surface format
        var format = "bgr0\0"u8.ToArray().AsSpan();

        // Configure surface stride
        // This is inefficient if this isn't a multiple of 64 but it's only a temporary implementation
        var stride = _width * 4;
        var stridePtr = Unsafe.AsPointer(ref stride);

        // Set everything and call the render function
        fixed (int* sizePtr = size)
        fixed (byte* formatPtr = format)
        fixed (byte* bufferPtr = buffer)
        {
            contextParams[0] = new MpvRenderParam { Type = MpvRenderParamType.SoftwareSize, Data = (nint)sizePtr };
            contextParams[1] = new MpvRenderParam { Type = MpvRenderParamType.SoftwareFormat, Data = (nint)formatPtr };
            contextParams[2] = new MpvRenderParam { Type = MpvRenderParamType.SoftwareStride, Data = (nint)stridePtr };
            contextParams[3] = new MpvRenderParam { Type = MpvRenderParamType.SoftwarePointer, Data = (nint)bufferPtr };

            MpvException.ThrowMpvError(MpvRender.RenderContext(_context, contextParams));
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
        _done = true;
        _thread.Join();
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~MpvRenderContext()
    {
        ReleaseUnmanagedResources();
    }

    private enum MpvRenderEvent
    {
        ClientWakeup,
        RenderUpdate,
    }
}