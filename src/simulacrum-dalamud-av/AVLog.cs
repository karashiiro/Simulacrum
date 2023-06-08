using System.Runtime.InteropServices;

namespace Simulacrum.AV;

public static partial class AVLog
{
    /// <summary>
    /// Sets the logging callback for libav. The provided callback must not move.
    /// </summary>
    /// <param name="callback">The logging callback.</param>
    public static void SetCallback(AVLogCallback callback)
    {
        AVLogSetCallback(callback);
    }

    public static void UseDefaultCallback()
    {
        AVLogUseDefaultCallback();
    }

    public delegate void AVLogCallback(AVLogLevel level, [MarshalAs(UnmanagedType.LPStr)] string? message);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "AVLogSetCallback")]
    internal static partial void AVLogSetCallback(AVLogCallback callback);

    [LibraryImport("Simulacrum.AV.Core.dll", EntryPoint = "AVLogUseDefaultCallback")]
    internal static partial void AVLogUseDefaultCallback();
}