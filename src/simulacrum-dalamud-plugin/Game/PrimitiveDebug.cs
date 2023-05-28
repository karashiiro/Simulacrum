using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

namespace Simulacrum.Game;

public class PrimitiveDebug : Primitive
{
    private const int VertexDeclarationBufferElements = 3;

    private readonly byte[] _vertexDeclarationBuffer;
    private readonly byte[] _initializeSettingsBuffer;
    private readonly byte[] _singletonBuffer;

    public PrimitiveDebug(SigScanner sigScanner) : base(sigScanner)
    {
        _vertexDeclarationBuffer = GC.AllocateArray<byte>(
            VertexDeclarationBufferElements * Marshal.SizeOf<InputElement>(),
            pinned: true);
        _initializeSettingsBuffer = GC.AllocateArray<byte>(24, pinned: true);
        _singletonBuffer = GC.AllocateArray<byte>(200, pinned: true);
    }

    public IPrimitiveContext GetContext()
    {
        ArgumentNullException.ThrowIfNull(CallPrimitiveContextDrawCommand);
        return new UnmanagedPrimitiveContext(PrimitiveContext, CallPrimitiveContextDrawCommand);
    }

    public override unsafe void Initialize()
    {
        base.Initialize();

        ArgumentNullException.ThrowIfNull(CallKernelDeviceCreateVertexDeclaration);
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerCtor);
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerInitialize);
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerLoadResource);

        fixed (byte* singleton = _singletonBuffer)
        {
            PrimitiveServer = (nint)singleton;
        }

        fixed (byte* vertexDeclarationBuffer = _vertexDeclarationBuffer)
        {
            var vertexDeclarationElements =
                new Span<InputElement>(vertexDeclarationBuffer, VertexDeclarationBufferElements);
            SetVertexDeclarationOptions(vertexDeclarationElements);

            PluginLog.Log("Executing CallKernelDeviceCreateVertexDeclaration");
            var vertexDecl = CallKernelDeviceCreateVertexDeclaration(
                (nint)Device.Instance(),
                (nint)vertexDeclarationBuffer,
                VertexDeclarationBufferElements);

            PluginLog.Log("Executing CallPrimitiveServer");
            CallPrimitiveServerCtor(PrimitiveServer);

            fixed (byte* initializeSettings = _initializeSettingsBuffer)
            {
                Marshal.WriteInt64((nint)initializeSettings, 0x00000000_000A0000);
                Marshal.WriteInt64((nint)initializeSettings + 8, 0x00000000_00280000);
                Marshal.WriteInt64((nint)initializeSettings + 16, 0x00000000_000A0000);

                PluginLog.Log("Executing CallPrimitiveServerInitialize");
                CallPrimitiveServerInitialize(PrimitiveServer, 0x01, 0x1E, 0x0C, 0x0F, 0, 24, vertexDecl,
                    (nint)initializeSettings);
            }

            PluginLog.Log("Executing CallPrimitiveServerLoadResource");
            CallPrimitiveServerLoadResource(PrimitiveServer);
        }

        PrimitiveContext = Marshal.ReadIntPtr(PrimitiveServer + 0xB8);

        PluginLog.Log("Initialized PrimitiveContext");
    }

    private static void SetVertexDeclarationOptions(Span<InputElement> elements)
    {
        if (elements.Length != VertexDeclarationBufferElements)
        {
            throw new InvalidOperationException("Buffer size mismatch.");
        }

        elements[0] = new()
        {
            Slot = 0,
            Offset = 0x00,
            Format = 0x23,
            Semantic = 0x00,
        };

        elements[1] = new()
        {
            Slot = 0,
            Offset = 0x0C,
            Format = 0x44,
            Semantic = 0x03,
        };

        elements[2] = new()
        {
            Slot = 0,
            Offset = 0x10,
            Format = 0x22,
            Semantic = 0x08,
        };
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private unsafe struct InputElement
    {
        public byte Slot;
        public byte Offset;
        public byte Format;
        public byte Semantic;
        private fixed byte Unknown[8];
    }
}