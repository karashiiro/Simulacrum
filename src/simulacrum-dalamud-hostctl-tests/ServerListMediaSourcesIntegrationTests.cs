namespace Simulacrum.Hostctl.Tests;

public class ServerListMediaSourcesIntegrationTests(ServerIntegrationFixture Server)
    : IClassFixture<ServerIntegrationFixture>
{
    [Fact]
    public async Task HostctlClient_CanListAllMediaSources()
    {
        var exceptions = new List<Exception>();

        // Arrange: Create a client
        var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = await Server.CreateClient((ex, _) => exceptions.Add(ex), cts1.Token);

        // Arrange: Set up an event handler
        var mediaSources = new List<HostctlEvent.MediaSourceDto>();
        using var _ = client.OnMediaSourceCreate().Subscribe(res =>
        {
            Assert.NotNull(res.Data);
            mediaSources.Add(res.Data);
        });

        // Arrange: Send some MEDIA_SOURCE_CREATE events
        const int mediaSourcesTarget = 31;
        var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Task.WhenAll(Enumerable.Range(0, mediaSourcesTarget)
            // ReSharper disable once AccessToDisposedClosure
            .Select(_ => client.SendEvent(ServerIntegrationUtils.BuildVideoSourceCreateRequest(), cts2.Token)));

        // Wait a few seconds for responses
        await ServerIntegrationUtils.WaitUntil(() => mediaSources.Count >= mediaSourcesTarget, TimeSpan.FromSeconds(5));

        // Arrange: Set up an event handler
        var responseMediaSources = new List<HostctlEvent.MediaSourceDto>();
        using var onMediaSourceList = client.OnMediaSourceList().Subscribe(res =>
        {
            Assert.NotNull(res.Data);
            responseMediaSources.AddRange(res.Data);
        });

        // Act: Send a MEDIA_SOURCE_LIST event
        var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendEvent(new HostctlEvent.MediaSourceListRequest(), cts3.Token);

        // Wait a few seconds for responses
        await ServerIntegrationUtils.WaitUntil(() => responseMediaSources.Count >= mediaSources.Count,
            TimeSpan.FromSeconds(5));

        // Assert: We got back all of the media sources we created
        var responseMediaSourcesDict = responseMediaSources.ToDictionary(ms =>
        {
            Assert.NotNull(ms.Id);
            return ms.Id;
        }, ms => ms);
        Assert.Equal(mediaSources.Count, responseMediaSources.Count);
        foreach (var ms in mediaSources)
        {
            Assert.NotNull(ms.Id);

            var rms = responseMediaSourcesDict[ms.Id];
            Assert.Equal(ms.Id, rms.Id);
            Assert.Equal(ms.UpdatedAt, rms.UpdatedAt);
            Assert.Equivalent(ms.MetaRaw, rms.MetaRaw);
        }

        // Assert: No exceptions were thrown during the test
        Assert.Empty(exceptions);
    }
}