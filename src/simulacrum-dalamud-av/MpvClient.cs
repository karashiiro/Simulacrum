using System.Runtime.InteropServices;

namespace Simulacrum.AV;

public static partial class MpvClient
{
    [LibraryImport("libmpv-2", EntryPoint = "mpv_create")]
    internal static partial nint Create();

    [LibraryImport("libmpv-2", EntryPoint = "mpv_initialize")]
    internal static partial int Initialize(nint client);

    [LibraryImport("libmpv-2", EntryPoint = "mpv_terminate_destroy")]
    internal static partial int TerminateDestroy(nint client);

    [LibraryImport("libmpv-2", EntryPoint = "mpv_command")]
    internal static partial int Command(nint client, ReadOnlySpan<nint> args);

    [LibraryImport("libmpv-2", EntryPoint = "mpv_set_option")]
    internal static partial int SetOption(nint client, ReadOnlySpan<byte> name, int format, ReadOnlySpan<byte> data);

    [LibraryImport("libmpv-2", EntryPoint = "mpv_set_option_string")]
    internal static partial int SetOptionString(nint client, ReadOnlySpan<byte> name, ReadOnlySpan<byte> data);

    [LibraryImport("libmpv-2", EntryPoint = "mpv_get_property")]
    internal static partial int GetProperty(nint client, ReadOnlySpan<byte> name, int format, ref nint data);

    [LibraryImport("libmpv-2", EntryPoint = "mpv_set_property")]
    internal static partial int SetProperty(nint client, ReadOnlySpan<byte> name, int format, ReadOnlySpan<byte> data);

    [LibraryImport("libmpv-2", EntryPoint = "mpv_free")]
    internal static partial void Free(nint data);
}