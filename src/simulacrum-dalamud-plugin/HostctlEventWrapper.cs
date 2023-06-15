using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simulacrum;

public class HostctlEventWrapper
{
    [JsonPropertyName("event")] private HostctlEventType? Event { get; init; }

    [JsonPropertyName("data")] public JsonElement Data { get; init; }

    public static HostctlEventWrapper WrapRequest(HostctlEvent @event)
    {
        var eventType = @event switch
        {
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
            HostctlEventType.MediaSourceList, static w => w.Deserialize<HostctlEvent.MediaSourceListResponse>(),
            HostctlEventType.MediaSourceCreate, static w => w.Deserialize<HostctlEvent.MediaSourceCreateBroadcast>(),
            HostctlEventType.VideoSourceSync, static w => w.Deserialize<HostctlEvent.VideoSourceSyncResponse>(),
            HostctlEventType.VideoSourcePlay, static w => w.Deserialize<HostctlEvent.VideoSourcePlayBroadcast>(),
            HostctlEventType.VideoSourcePause, static w => w.Deserialize<HostctlEvent.VideoSourcePauseBroadcast>(),
            HostctlEventType.VideoSourcePan, static w => w.Deserialize<HostctlEvent.VideoSourcePanBroadcast>());
    }
}