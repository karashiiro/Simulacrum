using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Simulacrum.Game.Structures;

namespace Simulacrum.Game;

public unsafe class Material
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly byte[] _material;

    public nint Pointer { get; }

    private PrimitiveMaterial* PrimitiveMaterial => (PrimitiveMaterial*)Pointer;

    private Material()
    {
        _material = GC.AllocateArray<byte>(Marshal.SizeOf<PrimitiveMaterial>(), pinned: true);
        Pointer = (nint)Unsafe.AsPointer(ref _material.AsSpan()[0]);
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
}