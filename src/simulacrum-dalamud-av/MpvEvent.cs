using System.Runtime.InteropServices;

namespace Simulacrum.AV;

[StructLayout(LayoutKind.Sequential)]
public struct MpvEvent
{
    public MpvEventId EventId;
    public ulong ReplyUserData;
    public nint Data;
}