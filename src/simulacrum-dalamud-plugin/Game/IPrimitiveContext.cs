namespace Simulacrum.Game;

public interface IPrimitiveContext
{
    nint DrawCommand(ulong commandType, uint vertices, uint priority, nint material);
}