using System.Runtime.InteropServices;

namespace Simulacrum.Game;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct BlendState
{
    private int _data;

    public bool Enable
    {
        get => (_data & 1) != 0;
        set => _data |= value ? 1 : 0;
    }

    public int ColorBlendOperation
    {
        get => (_data >> 1) & 0x7;
        set => _data |= value << 1;
    }

    public int ColorBlendFactorSrc
    {
        get => (_data >> 4) & 0xF;
        set => _data |= value << 4;
    }

    public int ColorBlendFactorDst
    {
        get => (_data >> 8) & 0xF;
        set => _data |= value << 8;
    }

    public int AlphaBlendOperation
    {
        get => (_data >> 12) & 0x7;
        set => _data |= value << 12;
    }

    public int AlphaBlendFactorSrc
    {
        get => (_data >> 15) & 0xF;
        set => _data |= value << 15;
    }

    public int AlphaBlendFactorDst
    {
        get => (_data >> 19) & 0xF;
        set => _data |= value << 19;
    }

    public ColorMask ColorWriteEnable
    {
        get => (ColorMask)((_data >> 23) & 0x1FF);
        set => _data |= (int)value << 23;
    }
}