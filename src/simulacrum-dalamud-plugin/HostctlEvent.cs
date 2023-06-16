using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Logging;

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

        [JsonPropertyName("playheadSeconds")] public double PlayheadSeconds { get; init; }

        [JsonPropertyName("playheadUpdatedAt")]
        private long _playheadUpdatedAt;

        [JsonIgnore]
        public DateTimeOffset PlayheadUpdatedAt => DateTimeOffset.FromUnixTimeMilliseconds(_playheadUpdatedAt);

        [JsonIgnore]
        public double PlayheadSecondsActual
        {
            get
            {
                var diff = DateTimeOffset.UtcNow - PlayheadUpdatedAt;
                return PlayheadSeconds + diff.TotalSeconds;
            }
        }

        [JsonPropertyName("state")] public string? State { get; init; }
    }

    public class MediaSourceDto
    {
        [JsonPropertyName("meta")] public JsonElement MetaRaw { get; init; }

        [JsonPropertyName("id")] public string? Id { get; init; }

        [JsonIgnore] public MediaMetadata? Meta => DeserializeMeta();

        [JsonPropertyName("updatedAt")] public long UpdatedAt { get; init; }

        private MediaMetadata? DeserializeMeta()
        {
            return MetaRaw.GetProperty("type").GetString() switch
            {
                "blank" => MetaRaw.Deserialize<BlankMetadata>(),
                "image" => MetaRaw.Deserialize<ImageMetadata>(),
                "video" => MetaRaw.Deserialize<VideoMetadata>(),
                _ => throw new ArgumentOutOfRangeException(nameof(MetaRaw)),
            };
        }
    }
}