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

    public static HostctlEventWrapper Wrap(HEvent @event)
    {
        var eventType = @event switch
        {
            HEvent.MediaSourceListRequest => HType.MediaSourceListRequest,
            HEvent.MediaSourceCreateRequest => HType.MediaSourceCreateRequest,
            HEvent.VideoSourceSyncRequest => HType.VideoSourceSyncRequest,
            HEvent.VideoSourcePlayRequest => HType.VideoSourcePlayRequest,
            HEvent.VideoSourcePauseRequest => HType.VideoSourcePauseRequest,
            HEvent.VideoSourcePanRequest => HType.VideoSourcePanRequest,
            HEvent.MediaSourceListResponse => HType.MediaSourceListResponse,
            HEvent.MediaSourceCreateBroadcast => HType.MediaSourceCreateBroadcast,
            HEvent.VideoSourceSyncResponse => HType.VideoSourceSyncResponse,
            HEvent.VideoSourcePlayBroadcast => HType.VideoSourcePlayBroadcast,
            HEvent.VideoSourcePauseBroadcast => HType.VideoSourcePauseBroadcast,
            HEvent.VideoSourcePanBroadcast => HType.VideoSourcePanBroadcast,
            _ => throw new ArgumentOutOfRangeException(nameof(@event)),
        };

        var element = JsonSerializer.SerializeToElement(@event);
        return new HostctlEventWrapper
        {
            Event = eventType,
            Data = element,
        };
    }

    public static HEvent? Unwrap(HostctlEventWrapper eventWrapper)
    {
        return eventWrapper.Event.Switch<HostctlEventWrapper, HEvent?>(eventWrapper,
            HType.MediaSourceListRequest, static w => w.Data.Deserialize<HEvent.MediaSourceListRequest>(),
            HType.MediaSourceCreateRequest, static w => w.Data.Deserialize<HEvent.MediaSourceCreateRequest>(),
            HType.VideoSourceSyncRequest, static w => w.Data.Deserialize<HEvent.VideoSourceSyncRequest>(),
            HType.VideoSourcePlayRequest, static w => w.Data.Deserialize<HEvent.VideoSourcePlayRequest>(),
            HType.VideoSourcePauseRequest, static w => w.Data.Deserialize<HEvent.VideoSourcePauseRequest>(),
            HType.VideoSourcePanRequest, static w => w.Data.Deserialize<HEvent.VideoSourcePanRequest>(),
            HType.MediaSourceListResponse, static w => w.Data.Deserialize<HEvent.MediaSourceListResponse>(),
            HType.MediaSourceCreateBroadcast, static w => w.Data.Deserialize<HEvent.MediaSourceCreateBroadcast>(),
            HType.VideoSourceSyncResponse, static w => w.Data.Deserialize<HEvent.VideoSourceSyncResponse>(),
            HType.VideoSourcePlayBroadcast, static w => w.Data.Deserialize<HEvent.VideoSourcePlayBroadcast>(),
            HType.VideoSourcePauseBroadcast, static w => w.Data.Deserialize<HEvent.VideoSourcePauseBroadcast>(),
            HType.VideoSourcePanBroadcast, static w => w.Data.Deserialize<HEvent.VideoSourcePanBroadcast>());
    }
}