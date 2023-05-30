using System.Runtime.InteropServices;

namespace Simulacrum.Game.Structures;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct PrimitiveMaterialParams
{
    private int _data;

    public int TextureRemapColor
    {
        get => _data & 7;
        set => _data |= value;
    }

    public int TextureRemapAlpha
    {
        get => (_data >> 3) & 3;
        set => _data |= value << 3;
    }

    public bool DepthTestEnable
    {
        get => ((_data >> 5) & 1) != 0;
        set => _data |= (value ? 1 : 0) << 5;
    }

    public bool DepthWriteEnable
    {
        get => ((_data >> 6) & 1) != 0;
        set => _data |= (value ? 1 : 0) << 6;
    }

    public bool FaceCullEnable
    {
        get => ((_data >> 7) & 1) != 0;
        set => _data |= (value ? 1 : 0) << 7;
    }

    public int FaceCullMode
    {
        get => (_data >> 8) & 1;
        set => _data |= value << 8;
    }
}