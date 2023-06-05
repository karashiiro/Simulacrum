using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Simulacrum.Game.Structures;

[StructLayout(LayoutKind.Sequential, Size = 24)]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
public struct PrimitiveMaterial
{
    public BlendState BlendState;
    public int Unknown;
    public nint Texture;
    public SamplerState SamplerState;
    public PrimitiveMaterialParams Params;
}