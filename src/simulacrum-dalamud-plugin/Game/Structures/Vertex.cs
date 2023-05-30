using System.Runtime.InteropServices;

namespace Simulacrum.Game.Structures;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Position Position;
    public Color Color;
    public UV UV;
}