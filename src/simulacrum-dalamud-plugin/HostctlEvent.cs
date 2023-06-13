using System.Text.Json.Serialization;

namespace Simulacrum;

public abstract class HostctlEvent
{
    public class ImageSourceList : HostctlEvent
    {
        [JsonPropertyName("event")] public static string Event => "IMAGE_SOURCE_LIST";
    }

    public class ImageSourceCreate : HostctlEvent
    {
        [JsonPropertyName("event")] public static string Event => "IMAGE_SOURCE_CREATE";
    }

    public class VideoSourceSync : HostctlEvent
    {
        [JsonPropertyName("event")] public static string Event => "VIDEO_SOURCE_SYNC";
    }

    public class VideoSourceList : HostctlEvent
    {
        [JsonPropertyName("event")] public static string Event => "VIDEO_SOURCE_LIST";
    }

    public class VideoSourceCreate : HostctlEvent
    {
        [JsonPropertyName("event")] public static string Event => "VIDEO_SOURCE_CREATE";
    }

    public class VideoSourcePlay : HostctlEvent
    {
        [JsonPropertyName("event")] public static string Event => "VIDEO_SOURCE_PLAY";

        [JsonPropertyName("id")] public string? Id { get; init; }
    }

    public class VideoSourcePause : HostctlEvent
    {
        [JsonPropertyName("event")] public static string Event => "VIDEO_SOURCE_PAUSE";

        [JsonPropertyName("id")] public string? Id { get; init; }
    }

    public class VideoSourcePan : HostctlEvent
    {
        [JsonPropertyName("event")] public static string Event => "VIDEO_SOURCE_PAN";

        [JsonPropertyName("id")] public string? Id { get; init; }

        [JsonPropertyName("playheadSeconds")] public long PlayheadSeconds { get; init; }
    }
}