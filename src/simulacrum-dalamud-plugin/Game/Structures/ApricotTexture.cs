using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Simulacrum.Game.Structures;

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
public unsafe struct ApricotTexture
{
    public void** Vtbl;
    public void** Vtbl2;
    public Texture* Texture;
    public uint RefCount;

    public void Release()
    {
        var thisPtr = Unsafe.AsPointer(ref Unsafe.AsRef(in this));
        var release = (nint)Vtbl[1];
        var fn = Marshal.GetDelegateForFunctionPointer<ApricotTextureRelease>(release);
        fn((nint)thisPtr);
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void ApricotTextureRelease(nint thisPtr);
}