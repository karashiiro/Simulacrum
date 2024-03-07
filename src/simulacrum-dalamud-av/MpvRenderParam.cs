using System.Runtime.InteropServices;

namespace Simulacrum.AV;

[StructLayout(LayoutKind.Sequential)]
public struct MpvRenderParam
{
    public MpvRenderParamType Type;
    public nint Data;
}