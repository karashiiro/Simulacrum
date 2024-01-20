namespace Simulacrum.Hostctl.Tests;

public class ServerIntegrationTests(ServerIntegrationFixture Server) : IClassFixture<ServerIntegrationFixture>
{
    [Fact]
    public async Task HostctlClient_CanCreateScreen()
    {
        // Arrange: Set up a timeout for connecting to the API
        var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Arrange: Create a client
        var uri = new Uri($"ws://{Server.Hostname}:{Server.Port}");
        using var client = await HostctlClient.FromUri(uri,
            (ex, message) => Assert.Fail($"{ex}, {message}"), cts1.Token);

        // Arrange: Set up an event handler
        HostctlEvent.ScreenCreateBroadcast? ev = null;
        using var _ = client.OnScreenCreate().Subscribe(res => { ev = res; });

        // Arrange: Set up a timeout for sending the request
        var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act: Send a SCREEN_CREATE event
        await client.SendEvent(new HostctlEvent.ScreenCreateRequest
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
            },
        }, cts2.Token);

        // Set up a timeout for receiving a response
        var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Wait a few seconds for a response
        await Task.Run(async () =>
        {
            do
            {
                await Task.Yield();
            } while (ev is null);
        }, cts3.Token);

        // Assert: The broadcast response matches what we sent
        Assert.NotNull(ev?.Data);
        Assert.Equal(7, ev.Data.Territory);
        Assert.Equal(74, ev.Data.World);

        Assert.NotNull(ev.Data.Position);
        Assert.Equal(23, ev.Data.Position.X);
        Assert.Equal(24, ev.Data.Position.Y);
        Assert.Equal(25, ev.Data.Position.Z);
    }
}