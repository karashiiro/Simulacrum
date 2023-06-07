using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

namespace Simulacrum.Game;

public class PrimitiveDebug : Primitive, IDisposable
{
    private const int VertexDeclarationBufferElements = 3;

    private readonly nint _vertexDeclarationBuffer;
    private readonly nint _initializeSettingsBuffer;

    public PrimitiveDebug(SigScanner sigScanner) : base(sigScanner)
    {
        _vertexDeclarationBuffer =
            Marshal.AllocHGlobal(VertexDeclarationBufferElements * Marshal.SizeOf<InputElement>());
        _initializeSettingsBuffer = Marshal.AllocHGlobal(24);
        PrimitiveServer = Marshal.AllocHGlobal(200);
    }

    public override unsafe void Initialize()
    {
        base.Initialize();

        ArgumentNullException.ThrowIfNull(CallKernelDeviceCreateVertexDeclaration);
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerCtor);
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerInitialize);
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerLoadResource);

        SetVertexDeclarationOptions(_vertexDeclarationBuffer);

        PluginLog.Log("Executing CallKernelDeviceCreateVertexDeclaration");
        var vertexDecl = CallKernelDeviceCreateVertexDeclaration(
            (nint)Device.Instance(),
            _vertexDeclarationBuffer,
            VertexDeclarationBufferElements);

        PluginLog.Log("Executing CallPrimitiveServer");
        CallPrimitiveServerCtor(PrimitiveServer);

        Marshal.WriteInt64(_initializeSettingsBuffer, 0x00000000_000A0000);
        Marshal.WriteInt64(_initializeSettingsBuffer + 8, 0x00000000_00280000);
        Marshal.WriteInt64(_initializeSettingsBuffer + 16, 0x00000000_000A0000);

        PluginLog.Log("Executing CallPrimitiveServerInitialize");
        CallPrimitiveServerInitialize(PrimitiveServer, 0x01, 0x1E, 0x0C, 0x0F, 0, 24, vertexDecl,
            _initializeSettingsBuffer);

        PluginLog.Log("Executing CallPrimitiveServerLoadResource");
        CallPrimitiveServerLoadResource(PrimitiveServer);

        PrimitiveContext = Marshal.ReadIntPtr(PrimitiveServer + 0xB8);

        PluginLog.Log("Initialized PrimitiveContext");
    }

    private static void SetVertexDeclarationOptions(nint elements)
    {
        Marshal.StructureToPtr(new InputElement
        {
            Slot = 0,
            Offset = 0x00,
            Format = 0x23,
            Semantic = 0x00,
        }, elements, false);

        Marshal.StructureToPtr(new InputElement
        {
            Slot = 0,
            Offset = 0x0C,
            Format = 0x44,
            Semantic = 0x03,
        }, elements + Marshal.SizeOf<InputElement>(), false);

        Marshal.StructureToPtr(new InputElement
        {
            Slot = 0,
            Offset = 0x10,
            Format = 0x22,
            Semantic = 0x08,
        }, elements + Marshal.SizeOf<InputElement>() * 2, false);
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(PrimitiveServer);
        Marshal.FreeHGlobal(_initializeSettingsBuffer);
        Marshal.FreeHGlobal(_vertexDeclarationBuffer);
        GC.SuppressFinalize(this);
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private struct InputElement
    {
        public byte Slot;
        public byte Offset;
        public byte Format;
        public byte Semantic;
    }
}