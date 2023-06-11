using System.Text.Json.Serialization;

namespace Simulacrum;

public class HostctlEvent
{
    public class Play : HostctlEvent
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
    }

    public class Pause : HostctlEvent
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
    }

    public class Pan : HostctlEvent
    {
        [JsonPropertyName("id")] public string? Id { get; init; }

        [JsonPropertyName("playheadSeconds")] public long PlayheadSeconds { get; init; }
    }
}