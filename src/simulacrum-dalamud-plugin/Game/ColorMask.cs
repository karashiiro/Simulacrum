namespace Simulacrum.Game;

[Flags]
public enum ColorMask
{
    None = 0,
    Alpha = 1 << 0,
    Red = 1 << 1,
    Green = 1 << 2,
    Blue = 1 << 3,

    RGB = Red | Green | Blue,
    RGBA = Red | Green | Blue | Alpha,
}