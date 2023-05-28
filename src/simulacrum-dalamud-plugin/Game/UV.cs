using System.Runtime.InteropServices;

namespace Simulacrum.Game;

[StructLayout(LayoutKind.Sequential, Size = 8)]
public struct UV
{
    public float U;
    public float V;

    public static UV FromUV(float u, float v)
    {
        return new UV
        {
            U = u,
            V = v,
        };
    }
}