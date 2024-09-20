using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

namespace Simulacrum.Game;

public class PrimitiveDebug : Primitive, IDisposable
{
    private const int VertexDeclarationBufferElements = 3;

    private readonly nint _vertexDeclarationBuffer;

    public PrimitiveDebug(ISigScanner sigScanner, IGameInteropProvider gameInteropProvider, IPluginLog log) : base(
        sigScanner, gameInteropProvider, log)
    {
        _vertexDeclarationBuffer =
            Marshal.AllocHGlobal(VertexDeclarationBufferElements * Marshal.SizeOf<InputElement>());
        PrimitiveServer = Marshal.AllocHGlobal(200);
    }

    public override unsafe void Initialize()
    {
        base.Initialize();

        ArgumentNullException.ThrowIfNull(CallKernelDeviceCreateVertexDeclaration);
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerCtor);
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerInitialize);
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerLoadResource);

        var vertexDeclarationElements =
            new Span<InputElement>((InputElement*)_vertexDeclarationBuffer, VertexDeclarationBufferElements);
        SetVertexDeclarationOptions(vertexDeclarationElements);

        Log.Info("Executing CallKernelDeviceCreateVertexDeclaration");
        var vertexDecl = CallKernelDeviceCreateVertexDeclaration(
            (nint)Device.Instance(),
            _vertexDeclarationBuffer,
            VertexDeclarationBufferElements);

        Log.Info("Executing CallPrimitiveServer");
        CallPrimitiveServerCtor(PrimitiveServer);

        var initializeSettings = new byte[24];
        fixed (byte* initializeSettingsPtr = initializeSettings)
        {
            Marshal.WriteInt64((nint)initializeSettingsPtr, 0x00000000_000A0000);
            Marshal.WriteInt64((nint)initializeSettingsPtr + 8, 0x00000000_00280000);
            Marshal.WriteInt64((nint)initializeSettingsPtr + 16, 0x00000000_000A0000);

            Log.Info("Executing CallPrimitiveServerInitialize");
            CallPrimitiveServerInitialize(PrimitiveServer, 0x01, 0x1E, 0x0C, 0x0F, 0, 24, vertexDecl,
                (nint)initializeSettingsPtr);
        }

        Log.Info("Executing CallPrimitiveServerLoadResource");
        CallPrimitiveServerLoadResource(PrimitiveServer);

        PrimitiveContext = Marshal.ReadIntPtr(PrimitiveServer + 0xB8);

        Log.Info("Initialized PrimitiveContext");
    }

    private static void SetVertexDeclarationOptions(Span<InputElement> elements)
    {
        if (elements.Length != VertexDeclarationBufferElements)
        {
            throw new InvalidOperationException("Buffer size mismatch.");
        }

        elements[0] = new InputElement
        {
            Slot = 0,
            Offset = 0x00,
            Format = 0x13,
            Semantic = 0x00,
        };

        elements[1] = new InputElement
        {
            Slot = 0,
            Offset = 0x0C,
            Format = 0x24,
            Semantic = 0x03,
        };

        elements[2] = new InputElement
        {
            Slot = 0,
            Offset = 0x10,
            Format = 0x12,
            Semantic = 0x08,
        };
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(PrimitiveServer);
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