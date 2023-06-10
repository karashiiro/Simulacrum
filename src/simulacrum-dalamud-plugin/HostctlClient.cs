using System.Net;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simulacrum;

public class HostctlClient : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly SocketsHttpHandler _handler;
    private readonly SemaphoreSlim _sendLock;
    private readonly Subject<EventWrapper<ScreenEvent>> _screenEvents;
    private readonly Uri _uri;

    private ClientWebSocket? _ws;

    private HostctlClient(Uri uri)
    {
        _cts = new CancellationTokenSource();
        _handler = new SocketsHttpHandler();
        _sendLock = new SemaphoreSlim(0, 1);
        _screenEvents = new Subject<EventWrapper<ScreenEvent>>();
        _uri = uri;

        RebuildClient();
    }

    public IObservable<ScreenEvent> OnScreenPlay()
    {
        return _screenEvents.Where(ev => ev is { Event: "play", Data: not null }).Select(ev => ev.Data!);
    }

    public IObservable<ScreenEvent> OnScreenPause()
    {
        return _screenEvents.Where(ev => ev is { Event: "pause", Data: not null }).Select(ev => ev.Data!);
    }

    public IObservable<ScreenEvent> OnScreenPan()
    {
        return _screenEvents.Where(ev => ev is { Event: "pan", Data: not null }).Select(ev => ev.Data!);
    }

    public async Task SendScreenEvent(ScreenEvent @event, CancellationToken cancellationToken = default)
    {
        if (_ws?.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("The WebSocket connection is in an invalid state.");
        }

        if (!await _sendLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
        {
            throw new InvalidOperationException("The operation timed out.");
        }

        try
        {
            var buffer = JsonSerializer.SerializeToUtf8Bytes(@event);
            await _ws.SendAsync(buffer, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage,
                cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private void RebuildClient()
    {
        _ws = new ClientWebSocket();
        _ws.Options.HttpVersion = HttpVersion.Version30;
        _ws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    }

    private async Task InboundLoop(CancellationToken cancellationToken)
    {
        // Limit inbound message size to 1KB
        var buffer = new byte[1024];

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_ws?.State != WebSocketState.Open)
            {
                // Attempt to reconnect to the server
                RebuildClient();
                await Connect(_cts.Token);
                return;
            }

            try
            {
                var result = await _ws.ReceiveAsync(buffer, cancellationToken);
                if (result.CloseStatus is not null)
                {
                    break;
                }

                ReceiveEvent(buffer.AsSpan()[..result.Count]);
            }
            catch (Exception)
            {
                // TODO: Route to logger
            }
        }
    }

    private void ReceiveEvent(Span<byte> buf)
    {
        var @event = JsonSerializer.Deserialize<EventWrapper<ScreenEvent>>(buf);
        if (@event is null)
        {
            throw new InvalidOperationException("The event was null.");
        }

        _screenEvents.OnNext(@event);
    }

    private async Task Connect(CancellationToken cancellationToken)
    {
        if (_ws is null)
        {
            RebuildClient();
        }

        // The parameter is used to control timeouts from the caller, and
        // the field is used to handle disposal.
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        await _ws!.ConnectAsync(_uri, new HttpMessageInvoker(_handler), cts.Token);
        cts.Token.ThrowIfCancellationRequested();
        _ = InboundLoop(_cts.Token);
    }

    private async Task Disconnect()
    {
        if (_ws is null)
        {
            return;
        }

        try
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", _cts.Token);
        }
        catch (Exception)
        {
            // TODO: Route exception to logger
        }
    }

    public static async Task<HostctlClient> FromUri(Uri uri, CancellationToken cancellationToken = default)
    {
        var client = new HostctlClient(uri);
        await client.Connect(cancellationToken);
        return client;
    }

    public void Dispose()
    {
        _cts.CancelAfter(TimeSpan.FromSeconds(5));
        Disconnect().GetAwaiter().GetResult();
        _cts.Dispose();

        _screenEvents.Dispose();
        _sendLock.Dispose();
        _ws?.Dispose();
        _handler.Dispose();

        GC.SuppressFinalize(this);
    }

    private class EventWrapper<T>
    {
        [JsonPropertyName("event")] public string? Event { get; init; }

        [JsonPropertyName("data")] public T? Data { get; init; }
    }

    public class ScreenEvent
    {
        [JsonPropertyName("screenId")] public string? ScreenId { get; init; }

        [JsonPropertyName("screenState")] public string? ScreenState { get; init; }
    }
}