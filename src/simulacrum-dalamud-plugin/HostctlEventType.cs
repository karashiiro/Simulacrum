using Thinktecture;

namespace Simulacrum;

public sealed partial class HostctlEventType : IEnum<string>
{
    public static readonly HostctlEventType ScreenCreate = new("SCREEN_CREATE");
    public static readonly HostctlEventType MediaSourceListScreens = new("MEDIA_SOURCE_LIST_SCREENS");
    public static readonly HostctlEventType MediaSourceList = new("MEDIA_SOURCE_LIST");
    public static readonly HostctlEventType MediaSourceCreate = new("MEDIA_SOURCE_CREATE");
    public static readonly HostctlEventType VideoSourceSync = new("VIDEO_SOURCE_SYNC");
    public static readonly HostctlEventType VideoSourcePlay = new("VIDEO_SOURCE_PLAY");
    public static readonly HostctlEventType VideoSourcePause = new("VIDEO_SOURCE_PAUSE");
    public static readonly HostctlEventType VideoSourcePan = new("VIDEO_SOURCE_PAN");
}