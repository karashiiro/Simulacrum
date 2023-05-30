using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Simulacrum.Game.Structures;

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
public unsafe struct ApricotTexture
{
    public void* Vtbl;
    public void* Vtbl2;
    public Texture* Texture;
    public uint RefCount;
}