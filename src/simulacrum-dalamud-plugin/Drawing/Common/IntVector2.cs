namespace Simulacrum.Drawing.Common;

public readonly struct IntVector2
{
    public readonly int X;
    public readonly int Y;

    public IntVector2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
}