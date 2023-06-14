using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simulacrum;

public abstract class HostctlEvent
{
    public class MediaSourceListRequest : HostctlEvent
    {
    }

    public class MediaSourceListResponse : HostctlEvent
    {
        [JsonPropertyName("mediaSources")] public MediaSourceDto[]? Data { get; init; }
    }

    public class MediaSourceCreateRequest : HostctlEvent
    {
        [JsonPropertyName("mediaSource")] public MediaSourceDto? Data { get; init; }
    }

    public class MediaSourceCreateBroadcast : HostctlEvent
    {
        [JsonPropertyName("mediaSource")] public MediaSourceDto? Data { get; init; }
    }

    public class VideoSourceSyncRequest : HostctlEvent
    {
    }

    public class VideoSourceSyncResponse : HostctlEvent
    {
        [JsonPropertyName("mediaSource")] public MediaSourceDto? Data { get; init; }
    }

    public class VideoSourcePlayRequest : HostctlEvent
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
    }

    public class VideoSourcePlayBroadcast : HostctlEvent
    {
        [JsonPropertyName("mediaSource")] public MediaSourceDto? Data { get; init; }
    }

    public class VideoSourcePauseRequest : HostctlEvent
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
    }

    public class VideoSourcePauseBroadcast : HostctlEvent
    {
        [JsonPropertyName("mediaSource")] public MediaSourceDto? Data { get; init; }
    }

    public class VideoSourcePanRequest : HostctlEvent
    {
        [JsonPropertyName("id")] public string? Id { get; init; }

        [JsonPropertyName("playheadSeconds")] public long PlayheadSeconds { get; init; }
    }

    public class VideoSourcePanBroadcast : HostctlEvent
    {
        [JsonPropertyName("mediaSource")] public MediaSourceDto? Data { get; init; }
    }

    public abstract class MediaMetadata
    {
        [JsonPropertyName("type")] public string? Type { get; init; }
    }

    public class BlankMetadata : MediaMetadata
    {
    }

    public class ImageMetadata : MediaMetadata
    {
        [JsonPropertyName("uri")] public string? Uri { get; init; }
    }

    public class VideoMetadata : MediaMetadata
    {
        [JsonPropertyName("uri")] public string? Uri { get; init; }

        [JsonPropertyName("playheadSeconds")] public long PlayheadSeconds { get; init; }

        [JsonPropertyName("state")] public string? State { get; init; }
    }

    public class MediaSourceDto
    {
#pragma warning disable CS0649
        [JsonPropertyName("meta")] private JsonElement _meta;
#pragma warning restore CS0649

        [JsonPropertyName("id")] public string? Id { get; init; }

        [JsonIgnore] public MediaMetadata? Meta => DeserializeMeta();

        [JsonPropertyName("updatedAt")] public long UpdatedAt { get; init; }

        private MediaMetadata? DeserializeMeta()
        {
            return _meta.GetProperty("type").GetString() switch
            {
                "blank" => _meta.Deserialize<BlankMetadata>(),
                "image" => _meta.Deserialize<ImageMetadata>(),
                "video" => _meta.Deserialize<VideoMetadata>(),
                _ => throw new ArgumentOutOfRangeException(nameof(_meta)),
            };
        }
    }
}