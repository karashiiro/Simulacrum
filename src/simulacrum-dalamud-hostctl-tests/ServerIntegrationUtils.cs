using System.Text.Json;

namespace Simulacrum.Hostctl.Tests;

public static class ServerIntegrationUtils
{
    public static async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        await Task.Run(async () =>
        {
            do
            {
                if (cts.IsCancellationRequested)
                {
                    throw new TimeoutException("The test condition has timed out.");
                }

                await Task.Yield();
            } while (!condition());
        }, cts.Token);
    }

    public static HostctlEvent.MediaSourceListScreensRequest BuildMediaSourceListScreensRequest(string mediaSourceId)
    {
        return new HostctlEvent.MediaSourceListScreensRequest
        {
            MediaSourceId = mediaSourceId,
        };
    }

    public static HostctlEvent.MediaSourceCreateRequest BuildVideoSourceCreateRequest(bool playing = false)
    {
        return new HostctlEvent.MediaSourceCreateRequest
        {
            Data = new HostctlEvent.MediaSourceDto
            {
                MetaRaw = JsonSerializer.SerializeToElement(new HostctlEvent.VideoMetadata
                {
                    Type = "video",
                    Uri = "https://dc6xbzf7ukys8.cloudfront.net/chugjug.m3u8",
                    State = playing ? "playing" : null,
                }),
            },
        };
    }

    public static HostctlEvent.ScreenCreateRequest BuildScreenCreateRequest(string? mediaSourceId = null)
    {
        return new HostctlEvent.ScreenCreateRequest
        {
            Data = new HostctlEvent.ScreenDto
            {
                Territory = 7,
                World = 74,
                Position = new HostctlEvent.PositionDto
                {
                    X = 23,
                    Y = 24,
                    Z = 25,
                },
                MediaSourceId = mediaSourceId,
            },
        };
    }
}