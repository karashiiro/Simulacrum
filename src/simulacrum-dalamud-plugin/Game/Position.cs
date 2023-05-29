using System.Numerics;
using System.Runtime.InteropServices;

namespace Simulacrum.Game;

[StructLayout(LayoutKind.Sequential, Size = 12)]
public struct Position
{
    public float X;
    public float Y;
    public float Z;

    public static Position FromCoordinates(float x, float y, float z)
    {
        return new Position
        {
            X = x,
            Y = y,
            Z = z,
        };
    }

    public static Position FromVector3(Vector3 vector)
    {
        return new Position
        {
            X = vector.X,
            Y = vector.Y,
            Z = vector.Z,
        };
    }

    public static implicit operator Position(Vector3 vector) => FromVector3(vector);
}