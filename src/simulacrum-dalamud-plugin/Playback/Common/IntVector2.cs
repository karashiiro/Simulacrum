using Thinktecture;

namespace Simulacrum.Playback.Common;

[ValueObject]
public readonly partial struct IntVector2
{
    public readonly int X;
    public readonly int Y;

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
}