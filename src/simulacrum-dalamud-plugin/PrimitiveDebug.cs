using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

namespace Simulacrum;

public class PrimitiveDebug : Primitive
{
    private const int VertexDeclarationBufferElements = 3;

    private readonly byte[] _vertexDeclarationBuffer;
    private readonly byte[] _initializeSettingsBuffer;
    private readonly byte[] _singletonBuffer;

    public PrimitiveDebug(SigScanner sigScanner) : base(sigScanner)
    {
        _vertexDeclarationBuffer = GC.AllocateArray<byte>(12 * VertexDeclarationBufferElements, true);
        _initializeSettingsBuffer = GC.AllocateArray<byte>(24, true);
        _singletonBuffer = GC.AllocateArray<byte>(200, true);
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
            PrimitiveContext = (nint)singleton;
        }

        fixed (byte* buf = _vertexDeclarationBuffer)
        {
            WriteInputElement((nint)buf, 0, 0x00, 0x23, 0x00);
            WriteInputElement((nint)buf + 12, 0, 0x0C, 0x44, 0x03);
            WriteInputElement((nint)buf + 24, 0, 0x10, 0x22, 0x08);

            PluginLog.Log("Executing CallKernelDeviceCreateVertexDeclaration");
            var vertexDecl = CallKernelDeviceCreateVertexDeclaration((nint)Device.Instance(), (nint)buf,
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

        PrimitiveContext = Marshal.ReadIntPtr(PrimitiveContext + 0xB8);

        PluginLog.Log("Initialized PrimitiveContext");
    }

    private static void WriteInputElement(nint pointer, byte slot, byte offset, byte format, byte semantic)
    {
        Marshal.WriteInt32(pointer, slot);
        Marshal.WriteInt32(pointer + 1, offset);
        Marshal.WriteInt32(pointer + 2, format);
        Marshal.WriteInt32(pointer + 3, semantic);
    }
}