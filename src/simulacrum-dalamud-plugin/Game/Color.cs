using System.Numerics;
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

    public static implicit operator Color(Vector4 color)
    {
        var r = (byte)Math.Floor(color.X * 255);
        var g = (byte)Math.Floor(color.Y * 255);
        var b = (byte)Math.Floor(color.Z * 255);
        var a = (byte)Math.Floor(color.W * 255);
        return FromRGBA(r, g, b, a);
    }
}