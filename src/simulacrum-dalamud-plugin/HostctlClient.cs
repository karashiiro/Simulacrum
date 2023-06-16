using System.Net;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Dalamud.Logging;

namespace Simulacrum;

public class HostctlClient : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly SocketsHttpHandler _handler;
    private readonly SemaphoreSlim _sendLock;
    private readonly Subject<HostctlEvent> _events;
    private readonly Uri _uri;

    private ClientWebSocket? _ws;
    private Task? _inboundLoop;

    private HostctlClient(Uri uri)
    {
        _cts = new CancellationTokenSource();
        _handler = new SocketsHttpHandler();
        _sendLock = new SemaphoreSlim(1, 1);
        _events = new Subject<HostctlEvent>();
        _uri = uri;

        RebuildClient();
    }

    public async Task SendEvent<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : HostctlEvent
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
            var eventWrapper = HostctlEventWrapper.WrapRequest(@event);
            var buffer = JsonSerializer.SerializeToUtf8Bytes(eventWrapper);
            await _ws.SendAsync(buffer, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage,
                cancellationToken);
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Failed to send event");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private void RebuildClient()
    {
        _ws?.Dispose();
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
                try
                {
                    RebuildClient();
                    await Connect(cancellationToken);
                }
                catch (WebSocketException e)
                {
                    PluginLog.LogError(e, "Failed to reconnect to the server");
                    continue;
                }
            }

            try
            {
                var result = await _ws!.ReceiveAsync(buffer, cancellationToken);
                if (result.CloseStatus is not null)
                {
                    break;
                }

                ReceiveEvent(buffer.AsSpan()[..result.Count]);
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, "Failed to receive event");
            }
        }
    }

    private void ReceiveEvent(Span<byte> buf)
    {
        var eventWrapper = JsonSerializer.Deserialize<HostctlEventWrapper>(buf);
        if (eventWrapper is null)
        {
            throw new InvalidOperationException("The event was null.");
        }

        var @event = HostctlEventWrapper.UnwrapResponse(eventWrapper);
        if (@event is null)
        {
            throw new InvalidOperationException("The event was null.");
        }

        _events.OnNext(@event);
    }

    private async Task Connect(CancellationToken cancellationToken)
    {
        if (_ws is null)
        {
            RebuildClient();
        }

        // The parameter is used to control timeouts from the caller, and
        // the field is used to handle disposal.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        await _ws!.ConnectAsync(_uri, new HttpMessageInvoker(_handler), cts.Token);
        cts.Token.ThrowIfCancellationRequested();
        PluginLog.Log($"Now connected to {_uri}");
        _inboundLoop = InboundLoop(_cts.Token);
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
        catch (WebSocketException e)
        {
            PluginLog.LogError(e, "Failed to close connection");
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
        Disconnect().ContinueWith(_ => _inboundLoop).ContinueWith(_ => _cts.Dispose());

        _events.Dispose();
        _sendLock.Dispose();
        _ws?.Dispose();
        _handler.Dispose();

        GC.SuppressFinalize(this);
    }

    public IObservable<HostctlEvent.ScreenCreateBroadcast> OnScreenCreate()
    {
        return _events
            .Select(ev => ev as HostctlEvent.ScreenCreateBroadcast)
            .Where(dto => dto is not null)
            .Select(dto => dto!);
    }

    public IObservable<HostctlEvent.MediaSourceListScreensResponse> OnMediaSourceListScreens()
    {
        return _events
            .Select(ev => ev as HostctlEvent.MediaSourceListScreensResponse)
            .Where(dto => dto is not null)
            .Select(dto => dto!);
    }

    public IObservable<HostctlEvent.MediaSourceListResponse> OnMediaSourceList()
    {
        return _events
            .Select(ev => ev as HostctlEvent.MediaSourceListResponse)
            .Where(dto => dto is not null)
            .Select(dto => dto!);
    }

    public IObservable<HostctlEvent.MediaSourceCreateBroadcast> OnMediaSourceCreate()
    {
        return _events
            .Select(ev => ev as HostctlEvent.MediaSourceCreateBroadcast)
            .Where(dto => dto is not null)
            .Select(dto => dto!);
    }

    public IObservable<HostctlEvent.VideoSourceSyncResponse> OnVideoSourceSync()
    {
        return _events
            .Select(ev => ev as HostctlEvent.VideoSourceSyncResponse)
            .Where(dto => dto is not null)
            .Select(dto => dto!);
    }

    public IObservable<HostctlEvent.VideoSourcePlayBroadcast> OnVideoSourcePlay()
    {
        return _events
            .Select(ev => ev as HostctlEvent.VideoSourcePlayBroadcast)
            .Where(dto => dto is not null)
            .Select(dto => dto!);
    }

    public IObservable<HostctlEvent.VideoSourcePauseBroadcast> OnVideoSourcePause()
    {
        return _events
            .Select(ev => ev as HostctlEvent.VideoSourcePauseBroadcast)
            .Where(dto => dto is not null)
            .Select(dto => dto!);
    }

    public IObservable<HostctlEvent.VideoSourcePanBroadcast> OnVideoSourcePan()
    {
        return _events
            .Select(ev => ev as HostctlEvent.VideoSourcePanBroadcast)
            .Where(dto => dto is not null)
            .Select(dto => dto!);
    }
}