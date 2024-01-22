namespace Simulacrum.Hostctl.Tests;

public class ServerIntegrationTests : IClassFixture<ServerIntegrationFixture>
{
    private ServerIntegrationFixture Server { get; }

    public ServerIntegrationTests(ServerIntegrationFixture server)
    {
        Server = server;
    }

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
        await client.SendEvent(ServerIntegrationUtils.BuildVideoSourceCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => ev is not null, TimeSpan.FromSeconds(5));

        // Assert: The broadcast response matches what we expect
        Assert.NotNull(ev?.Data);

        Assert.NotNull(ev.Data.Id);
        Assert.NotEmpty(ev.Data.Id);

        var videoMeta = ev.Data.Meta as HostctlEvent.VideoMetadata;
        Assert.NotNull(videoMeta);
        Assert.Equal("video", videoMeta.Type);
        Assert.Equal("https://dc6xbzf7ukys8.cloudfront.net/chugjug.m3u8", videoMeta.Uri);
        Assert.Equal("paused", videoMeta.State);
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
        await client.SendEvent(ServerIntegrationUtils.BuildScreenCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => ev is not null, TimeSpan.FromSeconds(5));

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
        await client.SendEvent(ServerIntegrationUtils.BuildVideoSourceCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => mediaSourceCreate is not null, TimeSpan.FromSeconds(5));

        // Arrange: Set up an event handler
        HostctlEvent.ScreenCreateBroadcast? screenCreate = null;
        using var onScreenCreate = client.OnScreenCreate().Subscribe(res => { screenCreate = res; });

        // Act: Send a SCREEN_CREATE event
        var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(ServerIntegrationUtils.BuildScreenCreateRequest(mediaSourceCreate?.Data?.Id),
            cts3.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => screenCreate is not null, TimeSpan.FromSeconds(5));

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
        await client.SendEvent(ServerIntegrationUtils.BuildVideoSourceCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => mediaSourceCreate is not null, TimeSpan.FromSeconds(5));

        var screens = new List<HostctlEvent.ScreenDto>();

        // Arrange: Set up an event handler
        using var onScreenCreate = client.OnScreenCreate().Subscribe(res =>
        {
            Assert.NotNull(res.Data);
            screens.Add(res.Data);
        });

        // Arrange: Send some SCREEN_CREATE events
        const int screensTarget = 22;
        var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Task.WhenAll(Enumerable.Range(0, screensTarget)
            // ReSharper disable once AccessToDisposedClosure
            .Select(_ => client.SendEvent(ServerIntegrationUtils.BuildScreenCreateRequest(mediaSourceCreate?.Data?.Id),
                cts3.Token)));

        // Wait a few seconds for responses
        await ServerIntegrationUtils.WaitUntil(() => screens.Count >= screensTarget, TimeSpan.FromSeconds(5));

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
        await client.SendEvent(ServerIntegrationUtils.BuildMediaSourceListScreensRequest(mediaSourceCreate.Data.Id),
            cts4.Token);

        // Wait a few seconds for responses
        await ServerIntegrationUtils.WaitUntil(() => responseScreens.Count >= screens.Count, TimeSpan.FromSeconds(5));

        // Assert: The screens received are the same as what we created
        var responseScreensDict = responseScreens.ToDictionary(ms =>
        {
            Assert.NotNull(ms.Id);
            return ms.Id;
        }, ms => ms);
        Assert.Equal(screens.Count, responseScreens.Count);
        foreach (var screen in screens)
        {
            Assert.NotNull(screen.Id);

            var responseScreen = responseScreensDict[screen.Id];
            Assert.Equivalent(screen, responseScreen);
        }

        // Assert: No exceptions were thrown during the test
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task HostctlClient_CanSyncVideoSource()
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
        await client.SendEvent(ServerIntegrationUtils.BuildVideoSourceCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => mediaSourceCreate is not null, TimeSpan.FromSeconds(5));

        // Arrange: Set up an event handler
        HostctlEvent.VideoSourceSyncResponse? videoSourceSync = null;
        using var onScreenCreate = client.OnVideoSourceSync().Subscribe(res => { videoSourceSync = res; });

        // Arrange: Send a VIDEO_SOURCE_SYNC event
        Assert.NotNull(mediaSourceCreate?.Data?.Id);
        var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(new HostctlEvent.VideoSourceSyncRequest { Id = mediaSourceCreate.Data.Id }, cts3.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => videoSourceSync is not null, TimeSpan.FromSeconds(5));

        // Assert: The data received is the same as what we created
        Assert.NotNull(videoSourceSync?.Data?.Id);
        Assert.Equal(mediaSourceCreate.Data.Id, videoSourceSync.Data.Id);
        Assert.Equal(mediaSourceCreate.Data.UpdatedAt, videoSourceSync.Data.UpdatedAt);
        Assert.Equivalent(mediaSourceCreate.Data.MetaRaw, videoSourceSync.Data.MetaRaw);

        // Assert: No exceptions were thrown during the test
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task HostctlClient_CanPlayVideoSource()
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
        await client.SendEvent(ServerIntegrationUtils.BuildVideoSourceCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => mediaSourceCreate is not null, TimeSpan.FromSeconds(5));

        // Validate that the video is paused
        var videoSourceInitial = mediaSourceCreate?.Data?.Meta as HostctlEvent.VideoMetadata;
        Assert.NotNull(videoSourceInitial);
        Assert.Equal("paused", videoSourceInitial.State);

        // Arrange: Set up an event handler
        HostctlEvent.VideoSourcePlayBroadcast? videoSourcePlay = null;
        using var onScreenCreate = client.OnVideoSourcePlay().Subscribe(res => { videoSourcePlay = res; });

        // Arrange: Send a VIDEO_SOURCE_PLAY event
        Assert.NotNull(mediaSourceCreate?.Data?.Id);
        var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(new HostctlEvent.VideoSourcePlayRequest { Id = mediaSourceCreate.Data.Id }, cts3.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => videoSourcePlay is not null, TimeSpan.FromSeconds(5));

        var videoSourceUpdated = videoSourcePlay?.Data?.Meta as HostctlEvent.VideoMetadata;
        Assert.NotNull(videoSourceUpdated);

        // Assert: The video is now playing
        Assert.Equal("playing", videoSourceUpdated.State);

        // Assert: No exceptions were thrown during the test
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task HostctlClient_CanPauseVideoSource()
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
        await client.SendEvent(ServerIntegrationUtils.BuildVideoSourceCreateRequest(true), cts2.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => mediaSourceCreate is not null, TimeSpan.FromSeconds(5));

        // Validate that the video is playing
        var videoSourceInitial = mediaSourceCreate?.Data?.Meta as HostctlEvent.VideoMetadata;
        Assert.NotNull(videoSourceInitial);
        Assert.Equal("playing", videoSourceInitial.State);

        // Arrange: Set up an event handler
        HostctlEvent.VideoSourcePauseBroadcast? videoSourcePaused = null;
        using var onScreenCreate = client.OnVideoSourcePause().Subscribe(res => { videoSourcePaused = res; });

        // Arrange: Send a VIDEO_SOURCE_PAUSE event
        Assert.NotNull(mediaSourceCreate?.Data?.Id);
        var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(new HostctlEvent.VideoSourcePauseRequest { Id = mediaSourceCreate.Data.Id }, cts3.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => videoSourcePaused is not null, TimeSpan.FromSeconds(5));

        var videoSourceUpdated = videoSourcePaused?.Data?.Meta as HostctlEvent.VideoMetadata;
        Assert.NotNull(videoSourceUpdated);

        // Assert: The video is now paused
        Assert.Equal("paused", videoSourceUpdated.State);

        // Assert: No exceptions were thrown during the test
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task HostctlClient_CanPanVideoSource()
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
        await client.SendEvent(ServerIntegrationUtils.BuildVideoSourceCreateRequest(), cts2.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => mediaSourceCreate is not null, TimeSpan.FromSeconds(5));

        // Validate that the video is paused
        var videoSourceInitial = mediaSourceCreate?.Data?.Meta as HostctlEvent.VideoMetadata;
        Assert.NotNull(videoSourceInitial);
        Assert.Equal("paused", videoSourceInitial.State);

        // Arrange: Set up an event handler
        HostctlEvent.VideoSourcePanBroadcast? videoSourcePan = null;
        using var onScreenCreate = client.OnVideoSourcePan().Subscribe(res => { videoSourcePan = res; });

        // Arrange: Send a VIDEO_SOURCE_PAN event
        Assert.NotNull(mediaSourceCreate?.Data?.Id);
        var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(
            new HostctlEvent.VideoSourcePanRequest
            {
                Id = mediaSourceCreate.Data.Id,
                PlayheadSeconds = 20.88,
            },
            cts3.Token);

        // Wait a few seconds for a response
        await ServerIntegrationUtils.WaitUntil(() => videoSourcePan is not null, TimeSpan.FromSeconds(5));

        var videoSourceUpdated = videoSourcePan?.Data?.Meta as HostctlEvent.VideoMetadata;
        Assert.NotNull(videoSourceUpdated);

        // Assert: The video timestamp has changed
        Assert.Equal(20.88, videoSourceUpdated.PlayheadSeconds);

        // Assert: The video timestamp update timestamp has changed
        Assert.NotEqual(videoSourceInitial.PlayheadUpdatedAt, videoSourceUpdated.PlayheadUpdatedAt);

        // Assert: The video is still paused
        Assert.Equal("paused", videoSourceUpdated.State);

        // Assert: No exceptions were thrown during the test
        Assert.Empty(exceptions);
    }
}