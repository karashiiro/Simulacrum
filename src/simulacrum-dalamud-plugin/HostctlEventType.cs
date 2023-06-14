using Thinktecture;

namespace Simulacrum;

public sealed partial class HostctlEventType : IEnum<string>
{
    public static readonly HostctlEventType MediaSourceListRequest = new("REQ_MEDIA_SOURCE_LIST");
    public static readonly HostctlEventType MediaSourceCreateRequest = new("REQ_MEDIA_SOURCE_CREATE");
    public static readonly HostctlEventType VideoSourceSyncRequest = new("REQ_VIDEO_SOURCE_SYNC");
    public static readonly HostctlEventType VideoSourcePlayRequest = new("REQ_VIDEO_SOURCE_PLAY");
    public static readonly HostctlEventType VideoSourcePauseRequest = new("REQ_VIDEO_SOURCE_PAUSE");
    public static readonly HostctlEventType VideoSourcePanRequest = new("REQ_VIDEO_SOURCE_PAN");

    public static readonly HostctlEventType MediaSourceListResponse = new("RES_MEDIA_SOURCE_LIST");
    public static readonly HostctlEventType MediaSourceCreateBroadcast = new("BROADCAST_MEDIA_SOURCE_CREATE");
    public static readonly HostctlEventType VideoSourceSyncResponse = new("RES_VIDEO_SOURCE_SYNC");
    public static readonly HostctlEventType VideoSourcePlayBroadcast = new("BROADCAST_VIDEO_SOURCE_PLAY");
    public static readonly HostctlEventType VideoSourcePauseBroadcast = new("BROADCAST_VIDEO_SOURCE_PAUSE");
    public static readonly HostctlEventType VideoSourcePanBroadcast = new("BROADCAST_VIDEO_SOURCE_PAN");
}