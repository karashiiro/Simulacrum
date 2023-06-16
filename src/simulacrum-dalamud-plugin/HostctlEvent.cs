﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simulacrum;

public abstract class HostctlEvent
{
    public class ScreenCreateRequest : HostctlEvent
    {
        [JsonPropertyName("screen")] public ScreenDto? Data { get; init; }
    }

    public class ScreenCreateBroadcast : HostctlEvent
    {
        [JsonPropertyName("screen")] public ScreenDto? Data { get; init; }
    }

    public class MediaSourceListScreensRequest : HostctlEvent
    {
        [JsonPropertyName("mediaSourceId")] public string? MediaSourceId { get; init; }
    }

    public class MediaSourceListScreensResponse : HostctlEvent
    {
        [JsonPropertyName("screens")] public ScreenDto[]? Data { get; init; }
    }

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

        [JsonPropertyName("playheadSeconds")] public double PlayheadSeconds { get; init; }
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
        public double PlayheadUpdatedAt { get; init; }

        [JsonIgnore]
        public double PlayheadSecondsActual
        {
            get
            {
                var diff = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - PlayheadUpdatedAt;
                return PlayheadSeconds + diff;
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

    public class ScreenDto
    {
        [JsonPropertyName("id")] public string? Id { get; init; }

        [JsonPropertyName("mediaSourceId")] public string? MediaSourceId { get; init; }

        [JsonPropertyName("updatedAt")] public long UpdatedAt { get; init; }
    }
}