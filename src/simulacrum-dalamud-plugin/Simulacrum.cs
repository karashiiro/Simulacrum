using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using R3;
using Simulacrum.AV;
using Simulacrum.Drawing;
using Simulacrum.Game;
using Simulacrum.Game.Structures;
using Simulacrum.Hostctl;
using Simulacrum.Monitoring;

namespace Simulacrum;

public class Simulacrum : IDalamudPlugin
{
    private static readonly ICounter? PluginStart =
        DebugMetrics.CreateCounter("simulacrum_start", "The plugin start count.");

    private static readonly ICounter? PluginShutdown =
        DebugMetrics.CreateCounter("simulacrum_shutdown", "The plugin shutdown count.");

    public string Name => "Simulacrum";

    private readonly IClientState _clientState;
    private readonly ICommandManager _commandManager;
    private readonly IPluginLog _log;
    private readonly CustomizationWindow _customizationWindow;
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly MediaSourceManager _mediaSources;
    private readonly PlaybackTrackerManager _playbackTrackers;
    private readonly PluginConfiguration _config;
    private readonly PrimitiveDebug _primitive;
    private readonly MaterialScreenManager _materialScreens;
    private readonly TextureFactory _textureFactory;
    private readonly WindowSystem _windows;

    private readonly CancellationTokenSource _cts;
    private readonly Task _task;

    private IDisposable? _unsubscribe;
    private DebugMetrics? _debugMetrics;
    private GCHandle? _logFunctionHandle;
    private HostctlClient? _hostctl;
    private DisposableBag _hostctlBag;

    public Simulacrum(
        [RequiredVersion("1.0")] IClientState clientState,
        [RequiredVersion("1.0")] ICommandManager commandManager,
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] IFramework framework,
        [RequiredVersion("1.0")] ISigScanner sigScanner,
        [RequiredVersion("1.0")] IGameInteropProvider gameInteropProvider,
        [RequiredVersion("1.0")] IPluginLog log)
    {
        _log = log;

        InstallDebugMetricsServer();
        InstallAVLogHandler();

        _clientState = clientState;
        _commandManager = commandManager;
        _pluginInterface = pluginInterface;

        _config = (PluginConfiguration?)pluginInterface.GetPluginConfig() ?? new PluginConfiguration();
        _config.Initialize(pluginInterface);

        _primitive = new PrimitiveDebug(sigScanner, gameInteropProvider, log);

        _hostctlBag = new DisposableBag();

        _mediaSources = new MediaSourceManager();
        _playbackTrackers = new PlaybackTrackerManager();
        _materialScreens = new MaterialScreenManager();
        _textureFactory = new TextureFactory(sigScanner, framework, log);

        _windows = new WindowSystem("Simulacrum");
        _customizationWindow = new CustomizationWindow();
        _windows.AddWindow(_customizationWindow);
        _windows.AddWindow(new DebugWindow(_materialScreens, _mediaSources));
        _customizationWindow.IsOpen = true;

        _pluginInterface.UiBuilder.Draw += _windows.Draw;

        _commandManager.AddHandler("/simgc", new CommandInfo((_, _) => { GC.Collect(2); }));

        _commandManager.AddHandler("/simplay", new CommandInfo((_, mediaSourceId) =>
        {
            if (mediaSourceId.IsNullOrWhitespace())
            {
                return;
            }

            _hostctl?.SendEvent(new HostctlEvent.VideoSourcePlayRequest
            {
                Id = mediaSourceId,
            }).FireAndForget(_log);
        }));

        _commandManager.AddHandler("/simpause", new CommandInfo((_, mediaSourceId) =>
        {
            if (mediaSourceId.IsNullOrWhitespace())
            {
                return;
            }

            _hostctl?.SendEvent(new HostctlEvent.VideoSourcePauseRequest
            {
                Id = mediaSourceId,
            }).FireAndForget(_log);
        }));

        _commandManager.AddHandler("/simpan", new CommandInfo((_, arguments) =>
        {
            var argsSplit = arguments.Split(' ');
            var mediaSourceId = argsSplit[0];
            var ts = TimeSpan.FromSeconds(double.Parse(argsSplit[1]));

            if (mediaSourceId.IsNullOrWhitespace())
            {
                return;
            }

            _hostctl?.SendEvent(new HostctlEvent.VideoSourcePanRequest
            {
                Id = mediaSourceId,
                PlayheadSeconds = ts.TotalSeconds,
            }).FireAndForget(_log);
        }));

        _commandManager.AddHandler("/simskip3", new CommandInfo((_, mediaSourceId) =>
        {
            if (mediaSourceId.IsNullOrWhitespace())
            {
                return;
            }

            var tracker = _playbackTrackers.GetPlaybackTracker(mediaSourceId);
            if (tracker is null)
            {
                return;
            }

            var ts = tracker.GetTime() + TimeSpan.FromSeconds(3);
            _hostctl?.SendEvent(new HostctlEvent.VideoSourcePanRequest
            {
                Id = mediaSourceId,
                PlayheadSeconds = ts.TotalSeconds,
            }).FireAndForget(_log);
        }));

        _commandManager.AddHandler("/simback3", new CommandInfo((_, mediaSourceId) =>
        {
            if (mediaSourceId.IsNullOrWhitespace())
            {
                return;
            }

            var tracker = _playbackTrackers.GetPlaybackTracker(mediaSourceId);
            if (tracker is null)
            {
                return;
            }

            var ts = tracker.GetTime() - TimeSpan.FromSeconds(3);
            _hostctl?.SendEvent(new HostctlEvent.VideoSourcePanRequest
            {
                Id = mediaSourceId,
                PlayheadSeconds = ts.TotalSeconds,
            }).FireAndForget(_log);
        }));

        _commandManager.AddHandler("/simskip10", new CommandInfo((_, mediaSourceId) =>
        {
            if (mediaSourceId.IsNullOrWhitespace())
            {
                return;
            }

            var tracker = _playbackTrackers.GetPlaybackTracker(mediaSourceId);
            if (tracker is null)
            {
                return;
            }

            var ts = tracker.GetTime() + TimeSpan.FromSeconds(10);
            _hostctl?.SendEvent(new HostctlEvent.VideoSourcePanRequest
            {
                Id = mediaSourceId,
                PlayheadSeconds = ts.TotalSeconds,
            }).FireAndForget(_log);
        }));

        _commandManager.AddHandler("/simback10", new CommandInfo((_, mediaSourceId) =>
        {
            if (mediaSourceId.IsNullOrWhitespace())
            {
                return;
            }

            var tracker = _playbackTrackers.GetPlaybackTracker(mediaSourceId);
            if (tracker is null)
            {
                return;
            }

            var ts = tracker.GetTime() - TimeSpan.FromSeconds(10);
            _hostctl?.SendEvent(new HostctlEvent.VideoSourcePanRequest
            {
                Id = mediaSourceId,
                PlayheadSeconds = ts.TotalSeconds,
            }).FireAndForget(_log);
        }));

        _commandManager.AddHandler("/simcreate", new CommandInfo((_, _) =>
        {
            _hostctl?.SendEvent(new HostctlEvent.MediaSourceCreateRequest
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
            }).FireAndForget(_log);
        }));

        _commandManager.AddHandler("/simplace", new CommandInfo((_, arguments) =>
        {
            if (_clientState.LocalPlayer is null || arguments.IsNullOrEmpty())
            {
                return;
            }

            var position = _clientState.LocalPlayer.Position;
            _hostctl?.SendEvent(new HostctlEvent.ScreenCreateRequest
            {
                Data = new HostctlEvent.ScreenDto
                {
                    Territory = _clientState.TerritoryType,
                    World = Convert.ToInt32(_clientState.LocalPlayer.CurrentWorld.Id),
                    Position = new HostctlEvent.PositionDto
                    {
                        X = position.X,
                        Y = position.Y,
                        Z = position.Z,
                    },
                    MediaSourceId = arguments,
                },
            }).FireAndForget(_log);
        }));

        // Continue initialization in a separate task which will be rejoined on dispose
        _cts = new CancellationTokenSource();
        _task = Initialize(_cts.Token);
    }

    private async Task Initialize(CancellationToken cancellationToken)
    {
        await Connect(cancellationToken);
        await _hostctl!.SendEvent(new HostctlEvent.MediaSourceListRequest(), cancellationToken);

        _log.Info("Initializing PrimitiveDebug");
        _primitive.Initialize();
        _unsubscribe = _primitive.Subscribe(() =>
        {
            if (_clientState.LocalPlayer is null)
            {
                return;
            }

            _customizationWindow.Territory = _clientState.TerritoryType;
            _customizationWindow.WorldPosition = _clientState.LocalPlayer.Position;

            // TODO: Add safety checks
            foreach (var screen in _materialScreens.Screens
                         .Where(s => s.MaterialPointer != nint.Zero)
                         .Where(s => s.GetLocation().World == _clientState.LocalPlayer.CurrentWorld.Id)
                         .Where(s => s.GetLocation().Territory == _clientState.TerritoryType))
            {
                // TODO: There's a 1px texture wraparound on all sides of the primitive, possibly due to UV/command type
                var context = _primitive.GetContext();
                var vertexPtr = context.DrawCommand(0x21, 4, 5, screen.MaterialPointer);
                if (vertexPtr == nint.Zero)
                {
                    return;
                }

                var aspectRatio = screen.GetAspectRatio();
                var dimensions = new Vector3(1, aspectRatio, 0);
                var translation = _customizationWindow.Translation;
                var scale = _customizationWindow.Scale;
                var color = _customizationWindow.Color;
                var location = screen.GetLocation();
                var position = location.Position;

                unsafe
                {
                    _ = new Span<Vertex>((void*)vertexPtr, 4)
                    {
                        [0] = new()
                        {
                            Position = position + translation + Vector3.UnitY * dimensions * scale,
                            Color = color,
                            UV = UV.FromUV(0, 0),
                        },
                        [1] = new()
                        {
                            Position = position + translation,
                            Color = color,
                            UV = UV.FromUV(0, 1),
                        },
                        [2] = new()
                        {
                            Position = position + translation + dimensions * scale,
                            Color = color,
                            UV = UV.FromUV(1, 0),
                        },
                        [3] = new()
                        {
                            Position = position + translation + Vector3.UnitX * dimensions * scale,
                            Color = color,
                            UV = UV.FromUV(1, 1),
                        },
                    };
                }
            }
        });
    }

    private async Task Connect(CancellationToken cancellationToken)
    {
        // TODO: Make this configurable
        var hostctlUri = new Uri("wss://1z4s5nrge4.execute-api.us-west-2.amazonaws.com/prod");

        _hostctl = await HostctlClient.FromUri(hostctlUri, (e, m) => _log.Error(e, m), cancellationToken);
        var unsubscribeScreenCreate = _hostctl.OnScreenCreate().DoCancelOnCompleted(_cts)
            .Subscribe(this, static (ev, p) => p.InitializeScreen(ev.Data));
        var unsubscribeMediaSourceListScreens = _hostctl.OnMediaSourceListScreens().DoCancelOnCompleted(_cts).Subscribe(
            this, static (ev, p) =>
            {
                if (ev.Data is null) return;
                foreach (var screen in ev.Data)
                {
                    p.InitializeScreen(screen);
                }
            });
        var unsubscribeMediaSourceList = _hostctl.OnMediaSourceList().DoCancelOnCompleted(_cts).Subscribe(this,
            static (ev, p) =>
            {
                if (ev.Data is null) return;
                foreach (var mediaSource in ev.Data)
                {
                    p.InitializeMediaSource(mediaSource);
                    p._hostctl?.SendEvent(new HostctlEvent.MediaSourceListScreensRequest
                    {
                        MediaSourceId = mediaSource.Id,
                    }, p._cts.Token).FireAndForget(p._log);
                }
            });
        var unsubscribeMediaSourceCreate = _hostctl.OnMediaSourceCreate().DoCancelOnCompleted(_cts)
            .Subscribe(this, static (ev, p) => p.InitializeMediaSource(ev.Data));
        var unsubscribeVideoSourcePlay = _hostctl.OnVideoSourcePlay().DoCancelOnCompleted(_cts).Subscribe(this,
            static (ev, p) =>
            {
                p._log.Info($"Now playing media source \"{ev.Data?.Id}\"");
                p._playbackTrackers.GetPlaybackTracker(ev.Data?.Id)?.Play();
            });
        var unsubscribeVideoSourcePause = _hostctl.OnVideoSourcePause().DoCancelOnCompleted(_cts).Subscribe(this,
            static (ev, p) =>
            {
                p._log.Info($"Now pausing media source \"{ev.Data?.Id}\"");
                p._playbackTrackers.GetPlaybackTracker(ev.Data?.Id)?.Pause();
            });
        var unsubscribeVideoSourcePan = _hostctl.OnVideoSourcePan().DoCancelOnCompleted(_cts).Subscribe(this,
            static (ev, p) =>
            {
                if (ev.Data?.Meta is not HostctlEvent.VideoMetadata videoMetadata) return;
                p._playbackTrackers.GetPlaybackTracker(ev.Data?.Id)?.Pan(videoMetadata.PlayheadActual);
            });
        var unsubscribeVideoSourceSync = _hostctl.OnVideoSourceSync().DoCancelOnCompleted(_cts).Subscribe(this,
            static (ev, p) =>
            {
                if (ev.Data?.Meta is not HostctlEvent.VideoMetadata videoMetadata) return;
                p._playbackTrackers.GetPlaybackTracker(ev.Data?.Id)?.Pan(videoMetadata.PlayheadActual);
            });

        _hostctlBag.Add(Disposable.Combine(unsubscribeScreenCreate, unsubscribeMediaSourceCreate,
            unsubscribeMediaSourceList, unsubscribeVideoSourcePan, unsubscribeVideoSourcePause,
            unsubscribeVideoSourcePlay, unsubscribeVideoSourceSync, unsubscribeMediaSourceListScreens));
    }

    private void InitializeScreen(HostctlEvent.ScreenDto? dto)
    {
        if (dto?.Id is null || dto.Position is null) return;

        var materialScreen = new MaterialScreen(_textureFactory, _pluginInterface.UiBuilder, new Location
        {
            Territory = dto.Territory,
            World = dto.World,
            Position = Position.FromCoordinates(dto.Position.X, dto.Position.Y, dto.Position.Z),
        }, _log);

        _materialScreens.AddScreen(dto.Id, materialScreen);

        if (dto.MediaSourceId is null) return;

        var mediaSource = _mediaSources.GetMediaSource(dto.MediaSourceId);
        if (mediaSource is not null)
        {
            materialScreen.Show(mediaSource);
        }
    }

    private void InitializeMediaSource(HostctlEvent.MediaSourceDto? dto)
    {
        if (dto?.Id is null) return;
        switch (dto.Meta)
        {
            case HostctlEvent.BlankMetadata:
                _mediaSources.AddMediaSource(dto.Id, new BlankMediaSource());
                break;
            case HostctlEvent.ImageMetadata:
                // TODO
                break;
            case HostctlEvent.VideoMetadata video:
            {
                _log.Info($"Got new video source: {video.Uri}");

                var videoSync = new TimePlaybackTracker();
                var videoMediaSource = new VideoReaderMediaSource(video.Uri, videoSync, _log);

                videoSync.Pan(video.PlayheadActual);
                if (video.State == "playing")
                {
                    videoSync.Play();
                }
                else
                {
                    videoSync.Pause();
                }

                _playbackTrackers.AddPlaybackTracker(dto.Id, videoSync);
                _mediaSources.AddMediaSource(dto.Id, videoMediaSource);
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dto));
        }
    }

    [Conditional("DEBUG")]
    private void InstallDebugMetricsServer()
    {
        _debugMetrics = new DebugMetrics(_log);
        _debugMetrics.Start();
        PluginStart?.Inc();
    }

    [Conditional("DEBUG")]
    private void UninstallDebugMetricsServer()
    {
        // TODO: This can be missed if Prometheus doesn't catch it soon enough, fix this
        PluginShutdown?.Inc();
        _debugMetrics?.Dispose();
    }

    [Conditional("DEBUG")]
    private void InstallAVLogHandler()
    {
        var logFunction = new AVLog.AVLogCallback(HandleAVLog);
        _logFunctionHandle = GCHandle.Alloc(logFunction);
        AVLog.SetCallback(logFunction);
    }

    [Conditional("DEBUG")]
    private void UninstallAVLogHandler()
    {
        AVLog.UseDefaultCallback();
        _logFunctionHandle?.Free();
    }

    private void HandleAVLog(AVLogLevel level, string? message)
    {
        message = $"[libav] {message}";
        switch (level)
        {
            case AVLogLevel.Quiet:
                break;
            case AVLogLevel.Panic:
                _log.Fatal(message);
                break;
            case AVLogLevel.Fatal:
                _log.Fatal(message);
                break;
            case AVLogLevel.Error:
                _log.Error(message);
                break;
            case AVLogLevel.Warning:
                _log.Warning(message);
                break;
            case AVLogLevel.Info:
                _log.Information(message);
                break;
            case AVLogLevel.Verbose:
                _log.Verbose(message);
                break;
            case AVLogLevel.Debug:
                _log.Debug(message);
                break;
            case AVLogLevel.Trace:
                _log.Verbose(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        _commandManager.RemoveHandler("/simplay");
        _commandManager.RemoveHandler("/simpause");
        _commandManager.RemoveHandler("/simskip3");
        _commandManager.RemoveHandler("/simback3");
        _commandManager.RemoveHandler("/simskip10");
        _commandManager.RemoveHandler("/simback10");
        _commandManager.RemoveHandler("/simplace");
        _commandManager.RemoveHandler("/simcreate");
        _commandManager.RemoveHandler("/simgc");

        _cts.Cancel();
        try
        {
            _task.GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            _log.Warning(e, "The main task completed with an exception");
        }

        _cts.Dispose();

        _materialScreens.Dispose();
        _playbackTrackers.Dispose();
        _mediaSources.Dispose();
        _textureFactory.Dispose();

        _pluginInterface.UiBuilder.Draw -= _windows.Draw;

        _unsubscribe?.Dispose();

        _hostctlBag.Dispose();
        _hostctl?.Dispose();

        UninstallDebugMetricsServer();
        UninstallAVLogHandler();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}