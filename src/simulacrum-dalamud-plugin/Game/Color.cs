using System.Runtime.InteropServices;

namespace Simulacrum.Game;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct Color
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;

    public static Color FromRGBA(byte r, byte g, byte b, byte a)
    {
        return new Color
        {
            R = r,
            G = g,
            B = b,
            A = a,
        };
    }
}