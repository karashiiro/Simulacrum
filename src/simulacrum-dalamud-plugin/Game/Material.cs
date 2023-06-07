using System.Runtime.InteropServices;
using Simulacrum.Game.Structures;

namespace Simulacrum.Game;

public unsafe class Material : IDisposable
{
    public nint Pointer { get; }

    private PrimitiveMaterial* PrimitiveMaterial => (PrimitiveMaterial*)Pointer;

    private Material()
    {
        Pointer = Marshal.AllocHGlobal(Marshal.SizeOf<PrimitiveMaterial>());
    }

    public static Material CreateFromTexture(nint texture)
    {
        var material = new Material();

        material.PrimitiveMaterial->BlendState = new BlendState
        {
            ColorWriteEnable = ColorMask.RGBA,
            AlphaBlendFactorDst = 0x5,
            AlphaBlendFactorSrc = 0x0,
            AlphaBlendOperation = 0,
            ColorBlendFactorDst = 0x5,
            ColorBlendFactorSrc = 0x4,
            ColorBlendOperation = 0,
            Enable = true,
        };

        material.PrimitiveMaterial->Texture = texture;

        material.PrimitiveMaterial->SamplerState = new SamplerState
        {
            GammaEnable = false,
            MaxAnisotropy = 0,
            MinLOD = 0x0,
            MipLODBias = 0,
            Filter = 9,
            AddressW = 0,
            AddressV = 0,
            AddressU = 0,
        };

        material.PrimitiveMaterial->Params = new PrimitiveMaterialParams
        {
            FaceCullMode = 0,
            FaceCullEnable = false,
            DepthWriteEnable = true,
            DepthTestEnable = true,
            TextureRemapAlpha = 0x2,
            TextureRemapColor = 0x2,
        };

        return material;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Marshal.FreeHGlobal(Pointer);
    }
}