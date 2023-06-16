using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simulacrum;

public class HostctlEventWrapper
{
    [JsonPropertyName("event")] public HostctlEventType? Event { get; set; }

    [JsonPropertyName("data")] public JsonElement Data { get; set; }

    public static HostctlEventWrapper WrapRequest<TEvent>(TEvent @event) where TEvent : HostctlEvent
    {
        var eventType = @event switch
        {
            HostctlEvent.ScreenCreateRequest => HostctlEventType.ScreenCreate,
            HostctlEvent.MediaSourceListScreensRequest => HostctlEventType.MediaSourceListScreens,
            HostctlEvent.MediaSourceListRequest => HostctlEventType.MediaSourceList,
            HostctlEvent.MediaSourceCreateRequest => HostctlEventType.MediaSourceCreate,
            HostctlEvent.VideoSourceSyncRequest => HostctlEventType.VideoSourceSync,
            HostctlEvent.VideoSourcePlayRequest => HostctlEventType.VideoSourcePlay,
            HostctlEvent.VideoSourcePauseRequest => HostctlEventType.VideoSourcePause,
            HostctlEvent.VideoSourcePanRequest => HostctlEventType.VideoSourcePan,
            _ => throw new ArgumentOutOfRangeException(nameof(@event)),
        };

        var element = JsonSerializer.SerializeToElement(@event);
        return new HostctlEventWrapper
        {
            Event = eventType,
            Data = element,
        };
    }

    public static HostctlEvent? UnwrapResponse(HostctlEventWrapper eventWrapper)
    {
        return eventWrapper.Event?.Switch<JsonElement, HostctlEvent?>(eventWrapper.Data,
            HostctlEventType.ScreenCreate, static w => w.Deserialize<HostctlEvent.ScreenCreateBroadcast>(),
            HostctlEventType.MediaSourceListScreens, static w => w.Deserialize<HostctlEvent.MediaSourceListScreensResponse>(),
            HostctlEventType.MediaSourceList, static w => w.Deserialize<HostctlEvent.MediaSourceListResponse>(),
            HostctlEventType.MediaSourceCreate, static w => w.Deserialize<HostctlEvent.MediaSourceCreateBroadcast>(),
            HostctlEventType.VideoSourceSync, static w => w.Deserialize<HostctlEvent.VideoSourceSyncResponse>(),
            HostctlEventType.VideoSourcePlay, static w => w.Deserialize<HostctlEvent.VideoSourcePlayBroadcast>(),
            HostctlEventType.VideoSourcePause, static w => w.Deserialize<HostctlEvent.VideoSourcePauseBroadcast>(),
            HostctlEventType.VideoSourcePan, static w => w.Deserialize<HostctlEvent.VideoSourcePanBroadcast>());
    }
}