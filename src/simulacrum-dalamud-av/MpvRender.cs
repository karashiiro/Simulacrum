using System.Runtime.InteropServices;

namespace Simulacrum.AV;

public static partial class MpvRender
{
    [LibraryImport("mpv.exe", EntryPoint = "mpv_render_context_create")]
    internal static partial int CreateContext(nint context, nint handle, ReadOnlySpan<MpvRenderParam> parameters);

    [LibraryImport("mpv.exe", EntryPoint = "mpv_render_context_get_info")]
    internal static partial int GetContextInfo(nint context, MpvRenderParam parameter);

    [LibraryImport("mpv.exe", EntryPoint = "mpv_render_context_set_update_callback")]
    internal static partial void SetContextUpdateCallback(nint context, MpvRenderUpdateCallback callback, nint ctx);

    [LibraryImport("mpv.exe", EntryPoint = "mpv_render_context_update")]
    internal static partial ulong UpdateContext(nint context);

    [LibraryImport("mpv.exe", EntryPoint = "mpv_render_context_render")]
    internal static partial int RenderContext(nint context, ReadOnlySpan<MpvRenderParam> parameters);

    [LibraryImport("mpv.exe", EntryPoint = "mpv_render_context_free")]
    internal static partial void FreeContext(nint context);

    public delegate void MpvRenderUpdateCallback(nint ctx);
}