using System.Text.Json;

namespace Simulacrum.Hostctl.Tests;

public class ServerIntegrationTests(ServerIntegrationFixture Server) : IClassFixture<ServerIntegrationFixture>
{
    [Fact]
    public async Task HostctlClient_CanCreateMediaSource()
    {
        var exceptions = new List<Exception>();

        // Arrange: Create a client
        var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = await Server.CreateClient((ex, _) => exceptions.Add(ex), cts1.Token);

        // Arrange: Set up an event handler
        HostctlEvent.MediaSourceCreateBroadcast? ev = null;
        using var _ = client.OnMediaSourceCreate().Subscribe(res => { ev = res; });

        // Act: Send a MEDIA_SOURCE_CREATE event
        var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(BuildMediaSourceCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await WaitUntil(() => ev is not null, TimeSpan.FromSeconds(5));

        // Assert: The broadcast response matches what we expect
        Assert.NotNull(ev?.Data);

        Assert.NotNull(ev.Data.Id);
        Assert.NotEmpty(ev.Data.Id);

        var videoMeta = ev.Data.Meta as HostctlEvent.VideoMetadata;
        Assert.NotNull(videoMeta);
        Assert.Equal("video", videoMeta.Type);
        Assert.Equal("https://dc6xbzf7ukys8.cloudfront.net/chugjug.m3u8", videoMeta.Uri);
        Assert.Equal("playing", videoMeta.State);
        Assert.Equal(0, videoMeta.PlayheadSeconds);
        Assert.NotEqual(0, videoMeta.PlayheadUpdatedAt);

        Assert.NotNull(ev.Data.UpdatedAt);
        Assert.NotEqual(0, ev.Data.UpdatedAt);

        // Assert: No exceptions were thrown during the test
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task HostctlClient_CanCreateScreen()
    {
        var exceptions = new List<Exception>();

        // Arrange: Create a client
        var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = await Server.CreateClient((ex, _) => exceptions.Add(ex), cts1.Token);

        // Arrange: Set up an event handler
        HostctlEvent.ScreenCreateBroadcast? ev = null;
        using var _ = client.OnScreenCreate().Subscribe(res => { ev = res; });

        // Act: Send a SCREEN_CREATE event
        var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(BuildScreenCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await WaitUntil(() => ev is not null, TimeSpan.FromSeconds(5));

        // Assert: The broadcast response matches what we expect
        Assert.NotNull(ev?.Data);

        Assert.NotNull(ev.Data.Id);
        Assert.NotEmpty(ev.Data.Id);

        Assert.Null(ev.Data.MediaSourceId);

        Assert.Equal(7, ev.Data.Territory);
        Assert.Equal(74, ev.Data.World);

        Assert.NotNull(ev.Data.Position);
        Assert.Equal(23, ev.Data.Position.X);
        Assert.Equal(24, ev.Data.Position.Y);
        Assert.Equal(25, ev.Data.Position.Z);

        Assert.NotNull(ev.Data.UpdatedAt);
        Assert.NotEqual(0, ev.Data.UpdatedAt);

        // Assert: No exceptions were thrown during the test
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task HostctlClient_CanCreateLinkedScreen()
    {
        var exceptions = new List<Exception>();

        // Arrange: Create a client
        var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = await Server.CreateClient((ex, _) => exceptions.Add(ex), cts1.Token);

        // Arrange: Set up an event handler
        HostctlEvent.MediaSourceCreateBroadcast? mediaSourceCreate = null;
        using var onMediaSourceCreate = client.OnMediaSourceCreate().Subscribe(res => { mediaSourceCreate = res; });

        // Arrange: Send a MEDIA_SOURCE_CREATE event
        var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(BuildMediaSourceCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await WaitUntil(() => mediaSourceCreate is not null, TimeSpan.FromSeconds(5));

        // Arrange: Set up an event handler
        HostctlEvent.ScreenCreateBroadcast? screenCreate = null;
        using var onScreenCreate = client.OnScreenCreate().Subscribe(res => { screenCreate = res; });

        // Act: Send a SCREEN_CREATE event
        var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(BuildScreenCreateRequest(mediaSourceCreate?.Data?.Id), cts3.Token);

        // Wait a few seconds for a response
        await WaitUntil(() => screenCreate is not null, TimeSpan.FromSeconds(5));

        // Assert: The broadcast response matches what we expect
        Assert.NotNull(screenCreate?.Data);

        Assert.NotNull(screenCreate.Data.Id);
        Assert.NotEmpty(screenCreate.Data.Id);

        Assert.NotNull(mediaSourceCreate?.Data?.Id);
        Assert.NotEqual("", mediaSourceCreate.Data.Id);
        Assert.Equal(mediaSourceCreate.Data.Id, screenCreate.Data.MediaSourceId);

        Assert.Equal(7, screenCreate.Data.Territory);
        Assert.Equal(74, screenCreate.Data.World);

        Assert.NotNull(screenCreate.Data.Position);
        Assert.Equal(23, screenCreate.Data.Position.X);
        Assert.Equal(24, screenCreate.Data.Position.Y);
        Assert.Equal(25, screenCreate.Data.Position.Z);

        Assert.NotNull(screenCreate.Data.UpdatedAt);
        Assert.NotEqual(0, screenCreate.Data.UpdatedAt);

        // Assert: No exceptions were thrown during the test
        Assert.Empty(exceptions);
    }


    [Fact]
    public async Task HostctlClient_CanListScreensForMediaSource()
    {
        var exceptions = new List<Exception>();

        // Arrange: Create a client
        var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = await Server.CreateClient((ex, _) => exceptions.Add(ex), cts1.Token);

        // Arrange: Set up an event handler
        HostctlEvent.MediaSourceCreateBroadcast? mediaSourceCreate = null;
        using var onMediaSourceCreate = client.OnMediaSourceCreate().Subscribe(res => { mediaSourceCreate = res; });

        // Arrange: Send a MEDIA_SOURCE_CREATE event
        var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(BuildMediaSourceCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await WaitUntil(() => mediaSourceCreate is not null, TimeSpan.FromSeconds(5));

        var screens = new List<HostctlEvent.ScreenDto>();

        // Arrange: Set up an event handler
        using var onScreenCreate = client.OnScreenCreate().Subscribe(res =>
        {
            Assert.NotNull(res?.Data);
            screens.Add(res.Data);
        });

        // Arrange: Send some SCREEN_CREATE events
        const int screensTarget = 22;
        var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Task.WhenAll(Enumerable.Range(0, screensTarget)
            // ReSharper disable once AccessToDisposedClosure
            .Select(_ => client.SendEvent(BuildScreenCreateRequest(mediaSourceCreate?.Data?.Id), cts3.Token)));

        // Wait a few seconds for responses
        await WaitUntil(() => screens.Count >= screensTarget, TimeSpan.FromSeconds(5));

        // Arrange: Set up an event handler
        var responseScreens = new List<HostctlEvent.ScreenDto>();
        using var onMediaSourceListScreens =
            client.OnMediaSourceListScreens().Subscribe(res =>
            {
                Assert.NotNull(res.Data);
                responseScreens.AddRange(res.Data);
            });

        // Act: Send a MEDIA_SOURCE_LIST_SCREENS event
        Assert.NotNull(mediaSourceCreate?.Data?.Id);
        var cts4 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(BuildMediaSourceListScreensRequest(mediaSourceCreate.Data.Id), cts4.Token);

        // Wait a few seconds for responses
        await WaitUntil(() => responseScreens.Count >= screens.Count, TimeSpan.FromSeconds(5));

        // Assert: The screens received are the same as what we created
        Assert.Equal(screens.Count, responseScreens.Count);

        // Assert: No exceptions were thrown during the test
        Assert.Empty(exceptions);
    }

    private static async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        await Task.Run(async () =>
        {
            do
            {
                await Task.Yield();
            } while (!condition());
        }, cts.Token);
    }

    private static HostctlEvent.MediaSourceListScreensRequest BuildMediaSourceListScreensRequest(string mediaSourceId)
    {
        return new HostctlEvent.MediaSourceListScreensRequest
        {
            MediaSourceId = mediaSourceId,
        };
    }

    private static HostctlEvent.MediaSourceCreateRequest BuildMediaSourceCreateRequest()
    {
        return new HostctlEvent.MediaSourceCreateRequest
        {
            Data = new HostctlEvent.MediaSourceDto
            {
                MetaRaw = JsonSerializer.SerializeToElement(new HostctlEvent.VideoMetadata
                {
                    Type = "video",
                    Uri = "https://dc6xbzf7ukys8.cloudfront.net/chugjug.m3u8",
                    State = "playing",
                }),
            },
        };
    }

    private static HostctlEvent.ScreenCreateRequest BuildScreenCreateRequest(string? mediaSourceId = null)
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