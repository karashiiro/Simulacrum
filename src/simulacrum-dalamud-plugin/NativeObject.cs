using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Simulacrum;

public interface INativeObject
{
}

public static class NativeObjectExtensions
{
    /// <summary>
    /// Writes this <see cref="INativeObject"/> to the provided block of unmanaged memory.
    /// This should only be used with structures; reference types require a more complicated
    /// finalization process, which this function does not implement.
    /// </summary>
    /// <param name="nativeObject">The structure to write.</param>
    /// <param name="destination">The unmanaged memory to write to.</param>
    /// <typeparam name="T">The underlying type of this instance.</typeparam>
    public static void WriteStructure<T>(this T nativeObject, nint destination) where T : INativeObject
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new InvalidOperationException("The provided type is a reference type or contains reference types.");
        Marshal.StructureToPtr(nativeObject, destination, false);
    }
}