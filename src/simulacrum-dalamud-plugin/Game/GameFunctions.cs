using System.Runtime.InteropServices;

namespace Simulacrum.Game;

public class GameFunctions
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate nint PrimitiveServerCtor(nint thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate byte PrimitiveServerInitialize(nint thisPtr, uint unk1, ulong unk2, ulong unk3, uint unk4,
        uint unk5, ulong unk6, nint unk7, nint unk8);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate void PrimitiveServerLoadResource(nint thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate void PrimitiveServerBegin(nint thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate void PrimitiveServerSpursSortUnencumbered(nint thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate void PrimitiveServerRender(nint thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate nint PrimitiveContextDrawCommand(nint thisPtr, ulong unk1, uint unk2, uint unk3, nint unk4);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate nint KernelDeviceCreateVertexDeclaration(nint thisPtr, nint unk1, uint unk2);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate nint KernelEnd(nint thisPtr, nint unk1);
}