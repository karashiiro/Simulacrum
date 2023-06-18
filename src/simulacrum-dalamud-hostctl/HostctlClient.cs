using System.Net;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;

namespace Simulacrum.Hostctl;

public class HostctlClient : IDisposable
{
    private readonly Action<Exception, string> _logError;
    private readonly CancellationTokenSource _cts;
    private readonly SocketsHttpHandler _handler;
    private readonly SemaphoreSlim _sendLock;
    private readonly Subject<HostctlEvent> _events;
    private readonly Uri _uri;

    private ClientWebSocket? _ws;
    private Task? _inboundLoop;

    private HostctlClient(Uri uri, Action<Exception, string> logError)
    {
        _logError = logError;
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
            _logError(e, "Failed to send event");
        }
        finally
        {
            _sendLock.Release();
        }
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
                await Connect(cancellationToken);
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
                _logError(e, "Failed to receive event");
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

    private void RebuildClient()
    {
        _ws?.Dispose();
        _ws = new ClientWebSocket();
        _ws.Options.HttpVersion = HttpVersion.Version30;
        _ws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    }

    private async Task Connect(CancellationToken cancellationToken)
    {
        // Retry until the connection succeeds, or a non-WebSocketException
        // (such as an OperationCancelledException) is thrown.
        while (true)
        {
            try
            {
                RebuildClient();
                await ConnectInternal(cancellationToken);
                return;
            }
            catch (WebSocketException e)
            {
                _logError(e, "Failed to connect to the server");
            }
        }
    }

    private async Task ConnectInternal(CancellationToken cancellationToken)
    {
        // The parameter is used to control timeouts from the caller, and
        // the field is used to handle disposal.
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        await _ws!.ConnectAsync(_uri, new HttpMessageInvoker(_handler), cts.Token);
        cts.Token.ThrowIfCancellationRequested();
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
            _logError(e, "Failed to close connection");
        }
    }

    public static async Task<HostctlClient> FromUri(Uri uri, Action<Exception, string> logError,
        CancellationToken cancellationToken = default)
    {
        var client = new HostctlClient(uri, logError);
        await client.Connect(cancellationToken);
        return client;
    }

    public void Dispose()
    {
        _cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            Disconnect().ContinueWith(_ => _inboundLoop).ContinueWith(_ => _cts.Dispose()).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            _logError(e, "The WebSocket loop completed with an exception");
        }

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
            .Where(ev => ev is not null)
            .Select(ev => ev!);
    }

    public IObservable<HostctlEvent.MediaSourceListScreensResponse> OnMediaSourceListScreens()
    {
        return _events
            .Select(ev => ev as HostctlEvent.MediaSourceListScreensResponse)
            .Where(ev => ev is not null)
            .Select(ev => ev!);
    }

    public IObservable<HostctlEvent.MediaSourceListResponse> OnMediaSourceList()
    {
        return _events
            .Select(ev => ev as HostctlEvent.MediaSourceListResponse)
            .Where(ev => ev is not null)
            .Select(ev => ev!);
    }

    public IObservable<HostctlEvent.MediaSourceCreateBroadcast> OnMediaSourceCreate()
    {
        return _events
            .Select(ev => ev as HostctlEvent.MediaSourceCreateBroadcast)
            .Where(ev => ev is not null)
            .Select(ev => ev!);
    }

    public IObservable<HostctlEvent.VideoSourceSyncResponse> OnVideoSourceSync()
    {
        return _events
            .Select(ev => ev as HostctlEvent.VideoSourceSyncResponse)
            .Where(ev => ev is not null)
            .Select(ev => ev!);
    }

    public IObservable<HostctlEvent.VideoSourcePlayBroadcast> OnVideoSourcePlay()
    {
        return _events
            .Select(ev => ev as HostctlEvent.VideoSourcePlayBroadcast)
            .Where(ev => ev is not null)
            .Select(ev => ev!);
    }

    public IObservable<HostctlEvent.VideoSourcePauseBroadcast> OnVideoSourcePause()
    {
        return _events
            .Select(ev => ev as HostctlEvent.VideoSourcePauseBroadcast)
            .Where(ev => ev is not null)
            .Select(ev => ev!);
    }

    public IObservable<HostctlEvent.VideoSourcePanBroadcast> OnVideoSourcePan()
    {
        return _events
            .Select(ev => ev as HostctlEvent.VideoSourcePanBroadcast)
            .Where(ev => ev is not null)
            .Select(ev => ev!);
    }
}