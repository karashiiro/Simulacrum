using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Simulacrum.AV.Structures;

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
public struct AVRational
{
    public int Numerator;
    public int Denominator;
}