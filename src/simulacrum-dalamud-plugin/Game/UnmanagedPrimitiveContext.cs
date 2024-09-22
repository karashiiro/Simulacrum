using Dalamud.Plugin.Services;
using static Simulacrum.Game.GameFunctions;

namespace Simulacrum.Game;

public class UnmanagedPrimitiveContext(nint data, PrimitiveContextDrawCommand drawCommand) : IPrimitiveContext
{
    public nint DrawCommand(ulong commandType, uint vertices, uint priority, nint material)
    {
        return drawCommand(data, commandType, vertices, priority, material);
    }
}