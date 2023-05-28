using System.Runtime.InteropServices;

namespace Simulacrum.Game;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct SamplerState
{
    private int _data;

    public int AddressU
    {
        get => _data & 3;
        set => _data |= value;
    }

    public int AddressV
    {
        get => (_data >> 2) & 3;
        set => _data |= value << 2;
    }

    public int AddressW
    {
        get => (_data >> 4) & 3;
        set => _data |= value << 4;
    }

    public int Filter
    {
        get => (_data >> 6) & 0xF;
        set => _data |= value << 6;
    }

    public int MipLODBias
    {
        get => (_data >> 10) & 0x3FF;
        set => _data |= value << 10;
    }

    public int MinLOD
    {
        get => (_data >> 20) & 0xF;
        set => _data |= value << 20;
    }

    public int MaxAnisotropy
    {
        get => (_data >> 24) & 0x7;
        set => _data |= value << 24;
    }

    public bool GammaEnable
    {
        get => ((_data >> 27) & 1) != 0;
        set => _data |= (value ? 1 : 0) << 27;
    }
}