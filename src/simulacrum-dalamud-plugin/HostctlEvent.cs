using System.Text.Json.Serialization;

namespace Simulacrum;

public class HostctlEvent
{
    public class VideoPlay : HostctlEvent
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
    }

    public class VideoPause : HostctlEvent
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
    }

    public class VideoPan : HostctlEvent
    {
        [JsonPropertyName("id")] public string? Id { get; init; }

        [JsonPropertyName("playheadSeconds")] public long PlayheadSeconds { get; init; }
    }
}