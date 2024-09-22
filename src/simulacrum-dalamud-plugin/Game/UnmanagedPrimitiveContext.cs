using Dalamud.Plugin.Services;
using static Simulacrum.Game.GameFunctions;

namespace Simulacrum.Game;

public class UnmanagedPrimitiveContext(nint data, PrimitiveContextDrawCommand drawCommand, IPluginLog log) : IPrimitiveContext
{
    public nint DrawCommand(ulong commandType, uint vertices, uint priority, nint material)
    {
        log.Debug("Drawing primitive {primitiveType} with priority {priority}", commandType, priority);
        return drawCommand(data, commandType, vertices, priority, material);
    }
}