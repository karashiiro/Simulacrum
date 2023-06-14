using System.Text.Json;
using System.Text.Json.Serialization;
using HEvent = Simulacrum.HostctlEvent;
using HType = Simulacrum.HostctlEventType;

namespace Simulacrum;

public class HostctlEventWrapper
{
    [JsonPropertyName("event")] private string? _event;

    public HType Event
    {
        get => HType.Get(_event ?? throw new ArgumentNullException(nameof(_event)));
        set => _event = value;
    }

    [JsonPropertyName("data")] public JsonElement Data { get; init; }

    public static HostctlEventWrapper WrapRequest(HEvent @event)
    {
        var eventType = @event switch
        {
            HEvent.MediaSourceListRequest => HType.MediaSourceList,
            HEvent.MediaSourceCreateRequest => HType.MediaSourceCreate,
            HEvent.VideoSourceSyncRequest => HType.VideoSourceSync,
            HEvent.VideoSourcePlayRequest => HType.VideoSourcePlay,
            HEvent.VideoSourcePauseRequest => HType.VideoSourcePause,
            HEvent.VideoSourcePanRequest => HType.VideoSourcePan,
            _ => throw new ArgumentOutOfRangeException(nameof(@event)),
        };

        var element = JsonSerializer.SerializeToElement(@event);
        return new HostctlEventWrapper
        {
            Event = eventType,
            Data = element,
        };
    }

    public static HEvent? UnwrapResponse(HostctlEventWrapper eventWrapper)
    {
        return eventWrapper.Event.Switch<HostctlEventWrapper, HEvent?>(eventWrapper,
            HType.MediaSourceList, static w => w.Data.Deserialize<HEvent.MediaSourceListResponse>(),
            HType.MediaSourceCreate, static w => w.Data.Deserialize<HEvent.MediaSourceCreateBroadcast>(),
            HType.VideoSourceSync, static w => w.Data.Deserialize<HEvent.VideoSourceSyncResponse>(),
            HType.VideoSourcePlay, static w => w.Data.Deserialize<HEvent.VideoSourcePlayBroadcast>(),
            HType.VideoSourcePause, static w => w.Data.Deserialize<HEvent.VideoSourcePauseBroadcast>(),
            HType.VideoSourcePan, static w => w.Data.Deserialize<HEvent.VideoSourcePanBroadcast>());
    }
}