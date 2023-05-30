using System.Runtime.InteropServices;

namespace Simulacrum.Game.Structures;

[StructLayout(LayoutKind.Sequential, Size = 24)]
public struct PrimitiveMaterial : INativeObject
{
    public BlendState BlendState;
    public int Unknown;
    public nint Texture;
    public SamplerState SamplerState;
    public PrimitiveMaterialParams Params;
}