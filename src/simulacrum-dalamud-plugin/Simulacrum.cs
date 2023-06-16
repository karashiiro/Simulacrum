﻿using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Simulacrum.AV;
using Simulacrum.Drawing;
using Simulacrum.Game;
using Simulacrum.Game.Structures;

namespace Simulacrum;

public class Simulacrum : IDalamudPlugin
{
    public string Name => "Simulacrum";

    private readonly ClientState _clientState;
    private readonly CommandManager _commandManager;
    private readonly CustomizationWindow _customizationWindow;
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly MediaSourceManager _mediaSources;
    private readonly PlaybackTrackerManager _playbackTrackers;
    private readonly PluginConfiguration _config;
    private readonly PrimitiveDebug _primitive;
    private readonly ScreenManager _screens;
    private readonly TextureFactory _textureFactory;
    private readonly WindowSystem _windows;

    private readonly CancellationTokenSource _cts;
    private readonly Task _task;

    private IDisposable? _unsubscribe;
    private GCHandle? _logFunctionHandle;
    private HostctlClient? _hostctl;
    private IList<IDisposable> _hostctlBag;

    public Simulacrum(
        [RequiredVersion("1.0")] ClientState clientState,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] SigScanner sigScanner)
    {
        InstallAVLogHandler();

        _clientState = clientState;
        _commandManager = commandManager;
        _pluginInterface = pluginInterface;

        _config = (PluginConfiguration?)pluginInterface.GetPluginConfig() ?? new PluginConfiguration();
        _config.Initialize(pluginInterface);

        _primitive = new PrimitiveDebug(sigScanner);

        _windows = new WindowSystem("Simulacrum");
        _customizationWindow = new CustomizationWindow();
        _windows.AddWindow(_customizationWindow);
        _customizationWindow.IsOpen = true;

        _pluginInterface.UiBuilder.Draw += _windows.Draw;

        _hostctlBag = new List<IDisposable>();

        _mediaSources = new MediaSourceManager();
        _playbackTrackers = new PlaybackTrackerManager();
        _screens = new ScreenManager();
        _textureFactory = new TextureFactory(sigScanner);

        // Continue initialization in a separate task which will be rejoined on dispose
        _cts = new CancellationTokenSource();
        _task = Initialize(_cts.Token);
    }

    private async Task Initialize(CancellationToken cancellationToken)
    {
        await Connect(cancellationToken);
        await _hostctl!.SendEvent(new HostctlEvent.MediaSourceListRequest(), cancellationToken);

        PluginLog.Log("Initializing PrimitiveDebug");
        _primitive.Initialize();
        _unsubscribe = _primitive.Subscribe(() =>
        {
            if (_clientState.LocalPlayer is null)
            {
                return;
            }

            var position = _clientState.LocalPlayer.Position;

            // TODO: Add safety checks
            foreach (var screen in _screens.Screens.OfType<MaterialScreen>().Where(s => s.MaterialPointer != nint.Zero))
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
        var hostctlUri = new Uri("ws://localhost:3000");

        _hostctl = await HostctlClient.FromUri(hostctlUri, cancellationToken);
        _hostctlBag.Add(_hostctl.OnScreenCreate().Subscribe(ev => { InitializeScreen(ev.Data); }));
        _hostctlBag.Add(_hostctl.OnMediaSourceListScreens().Subscribe(ev =>
        {
            if (ev.Data is null) return;
            foreach (var screen in ev.Data)
            {
                InitializeScreen(screen);
            }
        }));
        _hostctlBag.Add(_hostctl.OnMediaSourceList().Subscribe(ev =>
        {
            if (ev.Data is null) return;
            foreach (var mediaSource in ev.Data)
            {
                InitializeMediaSource(mediaSource);
                _ = _hostctl.SendEvent(new HostctlEvent.MediaSourceListScreensRequest
                {
                    MediaSourceId = mediaSource.Id,
                }, _cts.Token);
            }
        }));
        _hostctlBag.Add(_hostctl.OnMediaSourceCreate().Subscribe(ev => { InitializeMediaSource(ev.Data); }));
        _hostctlBag.Add(_hostctl.OnVideoSourcePlay().Subscribe(ev =>
        {
            PluginLog.Log($"Now playing media source \"{ev.Data?.Id}\"");
            _playbackTrackers.GetPlaybackTracker(ev.Data?.Id)?.Play();
        }));
        _hostctlBag.Add(_hostctl.OnVideoSourcePause().Subscribe(ev =>
        {
            PluginLog.Log($"Now pausing media source \"{ev.Data?.Id}\"");
            _playbackTrackers.GetPlaybackTracker(ev.Data?.Id)?.Pause();
        }));
        _hostctlBag.Add(_hostctl.OnVideoSourcePan().Subscribe(ev =>
        {
            _playbackTrackers.GetPlaybackTracker(ev.Data?.Id)?.Pan(0);
        }));
        _hostctlBag.Add(_hostctl.OnVideoSourceSync().Subscribe(ev =>
        {
            if (ev.Data?.Meta is not HostctlEvent.VideoMetadata videoMetadata) return;
            _playbackTrackers.GetPlaybackTracker(ev.Data?.Id)?.Pan(videoMetadata.PlayheadSecondsActual);
        }));
    }

    private void InitializeScreen(HostctlEvent.ScreenDto? dto)
    {
        if (dto?.Id is null) return;

        var materialScreen = new MaterialScreen(_textureFactory, _pluginInterface.UiBuilder);
        _screens.AddScreen(dto.Id, materialScreen);

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
                PluginLog.Log($"Got new video source: {video.Uri}");

                var videoSync = new TimePlaybackTracker();
                // TODO: This breaks for some reason
                // videoSync.Pan(video.PlayheadSecondsActual);
                if (video.State == "playing")
                {
                    videoSync.Play();
                }

                var videoMediaSource = new VideoReaderMediaSource(video.Uri, videoSync);
                _playbackTrackers.AddPlaybackTracker(dto.Id, videoSync);
                _mediaSources.AddMediaSource(dto.Id, videoMediaSource);
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dto));
        }
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
        _logFunctionHandle?.Free();
        AVLog.UseDefaultCallback();
    }

    private static void HandleAVLog(AVLogLevel level, string? message)
    {
        message = $"[libav] {message}";
        switch (level)
        {
            case AVLogLevel.Quiet:
                break;
            case AVLogLevel.Panic:
                PluginLog.LogFatal(message);
                break;
            case AVLogLevel.Fatal:
                PluginLog.LogFatal(message);
                break;
            case AVLogLevel.Error:
                PluginLog.LogError(message);
                break;
            case AVLogLevel.Warning:
                PluginLog.LogWarning(message);
                break;
            case AVLogLevel.Info:
                PluginLog.LogInformation(message);
                break;
            case AVLogLevel.Verbose:
                PluginLog.LogVerbose(message);
                break;
            case AVLogLevel.Debug:
                PluginLog.LogDebug(message);
                break;
            case AVLogLevel.Trace:
                PluginLog.LogVerbose(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        _cts.Cancel();
        try
        {
            _task.GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            PluginLog.LogWarning(e, "The main task completed with an exception.");
        }

        _cts.Dispose();

        _commandManager.RemoveHandler("/simon");
        _commandManager.RemoveHandler("/simoff");
        _commandManager.RemoveHandler("/simsync");
        _commandManager.RemoveHandler("/simpause");
        _commandManager.RemoveHandler("/simplay");

        _screens.Dispose();
        _playbackTrackers.Dispose();
        _mediaSources.Dispose();
        _textureFactory.Dispose();

        _pluginInterface.UiBuilder.Draw -= _windows.Draw;

        _unsubscribe?.Dispose();

        foreach (var subscription in _hostctlBag)
        {
            subscription.Dispose();
        }

        _hostctl?.Dispose();

        UninstallAVLogHandler();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}