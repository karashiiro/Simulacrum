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
    public static unsafe void WriteStructure<T>(this T nativeObject, void* destination) where T : INativeObject
    {
        Marshal.StructureToPtr(nativeObject, (nint)destination, false);
    }

    /// <summary>
    /// Writes this array of <see cref="INativeObject"/> to the provided block of unmanaged memory.
    /// This should only be used with structures; reference types require a more complicated
    /// finalization process, which this function does not implement.
    /// </summary>
    /// <param name="array">The array of structures to write.</param>
    /// <param name="destination">The unmanaged memory to write to.</param>
    /// <typeparam name="T">The underlying type of the elements of this instance.</typeparam>
    public static unsafe void WriteArray<T>(this T[] array, void* destination) where T : INativeObject
    {
        for (var i = 0; i < array.Length; i++)
        {
            var element = array[i];
            element.WriteStructure((void*)((nint)destination + Marshal.SizeOf<T>() * i));
        }
    }
}