using static Simulacrum.Game.GameFunctions;

namespace Simulacrum.Game;

public class UnmanagedPrimitiveContext : IPrimitiveContext
{
    private readonly nint _data;
    private readonly PrimitiveContextDrawCommand _drawCommand;

    public UnmanagedPrimitiveContext(nint data, PrimitiveContextDrawCommand drawCommand)
    {
        _data = data;
        _drawCommand = drawCommand;
    }

    public IntPtr DrawCommand(ulong commandType, uint vertices, uint priority, IntPtr material)
    {
        return _drawCommand(_data, commandType, vertices, priority, material);
    }
}