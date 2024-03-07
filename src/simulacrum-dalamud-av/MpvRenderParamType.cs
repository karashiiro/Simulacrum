namespace Simulacrum.AV;

// https://github.com/mpv-player/mpv/blob/665a47209869d7a0c4ea860b28910fcd6ca874c8/libmpv/render.h#L171
public enum MpvRenderParamType
{
    Invalid,
    ApiType,
    OpenGLInitParams,
    OpenGLFrameBufferObject,
    FlipY,
    Depth,
    IccProfile,
    AmbientLight,
    X11Display,
    WaylandDisplay,
    AdvancedControl,
    NextFrameInfo,
    BlockForTargetTime,
    SkipRendering,
    DrmDisplay,
    DrmDrawSurfaceSize,
    DrmDisplayV2,
    SoftwareSize,
    SoftwareFormat,
    SoftwareStride,
    SoftwarePointer,
}