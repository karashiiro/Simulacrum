using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Simulacrum.AV;
using Simulacrum.Drawing;
using Simulacrum.Drawing.Common;
using Simulacrum.Game;
using Simulacrum.Game.Structures;
using Material = Simulacrum.Game.Material;

namespace Simulacrum;

public class Simulacrum : IDalamudPlugin
{
    public string Name => "Simulacrum";

    private readonly ClientState _clientState;
    private readonly CommandManager _commandManager;
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Framework _framework;
    private readonly CustomizationWindow _customizationWindow;
    private readonly PluginConfiguration _config;
    private readonly PrimitiveDebug _primitive;
    private readonly VideoReader _videoReader;
    private readonly WindowSystem _windows;

    private readonly TextureBootstrap _textureBootstrap;

    private IDisposable? _unsubscribe;
    private IPlaybackTracker? _sync;
    private Material? _material;
    private TextureScreen? _screen;
    private VideoReaderMediaSource? _renderSource;
    private GCHandle? _logFunctionHandle;
    private HostctlClient? _hostctl;
    private IList<IDisposable> _hostctlBag;
    private IDictionary<string, HostctlEvent.MediaSourceDto> _mediaSources;

    private bool _initialized;

    public Simulacrum(
        [RequiredVersion("1.0")] ClientState clientState,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] Framework framework,
        [RequiredVersion("1.0")] SigScanner sigScanner)
    {
        InstallAVLogHandler();

        _clientState = clientState;
        _commandManager = commandManager;
        _framework = framework;
        _pluginInterface = pluginInterface;

        _config = (PluginConfiguration?)pluginInterface.GetPluginConfig() ?? new PluginConfiguration();
        _config.Initialize(pluginInterface);

        _primitive = new PrimitiveDebug(sigScanner);

        _textureBootstrap = new TextureBootstrap(sigScanner);

        _videoReader = new VideoReader();

        _windows = new WindowSystem("Simulacrum");
        _customizationWindow = new CustomizationWindow();
        _windows.AddWindow(_customizationWindow);
        _customizationWindow.IsOpen = true;

        _pluginInterface.UiBuilder.Draw += _windows.Draw;

        _framework.Update += OnFrameworkUpdate;

        _hostctlBag = new List<IDisposable>();

        _mediaSources = new Dictionary<string, HostctlEvent.MediaSourceDto>();

        _commandManager.AddHandler("/simplay", new CommandInfo((_, _) => _sync?.Play()));
        _commandManager.AddHandler("/simpause", new CommandInfo((_, _) => _sync?.Pause()));
        _commandManager.AddHandler("/simsync", new CommandInfo((_, _) => _sync?.Pan(0)));
        _commandManager.AddHandler("/simoff", new CommandInfo((_, _) => _screen?.Show(new BlankMediaSource())));
        _commandManager.AddHandler("/simon",
            new CommandInfo((_, _) => _screen?.Show((IMediaSource?)_renderSource ?? new BlankMediaSource())));
    }

    private const string VideoPath = @"https://dc6xbzf7ukys8.cloudfront.net/rider64_xKQhMNjffD.m3u8";

    public void OnFrameworkUpdate(Framework f)
    {
        if (_clientState.LocalPlayer is null)
        {
            return;
        }

        if (_initialized) return;
        _initialized = true;

        async Task Connect()
        {
            var hostctlUri = new Uri("ws://localhost:3000");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            _hostctl = await HostctlClient.FromUri(hostctlUri, cts.Token);
            _hostctlBag.Add(_hostctl.OnMediaSourceCreate().Subscribe(ev =>
            {
                if (ev.Data?.Id is null) return;
                _mediaSources[ev.Data.Id] = ev.Data;
            }));
            _hostctlBag.Add(_hostctl.OnMediaSourceList().Subscribe(ev =>
            {
                if (ev.Data is null) return;
                foreach (var mediaSource in ev.Data)
                {
                    if (mediaSource.Id is null) continue;
                    _mediaSources[mediaSource.Id] = mediaSource;
                }
            }));
            _hostctlBag.Add(_hostctl.OnVideoSourcePlay().Subscribe(_ => { _sync?.Play(); }));
            _hostctlBag.Add(_hostctl.OnVideoSourcePause().Subscribe(_ => { _sync?.Pause(); }));
            _hostctlBag.Add(_hostctl.OnVideoSourcePan().Subscribe(_ => { _sync?.Pan(0); }));
            _hostctlBag.Add(_hostctl.OnVideoSourceSync().Subscribe(ev =>
            {
                if (ev.Data?.Meta is not HostctlEvent.VideoMetadata videoMetadata) return;
                var diff = DateTimeOffset.UtcNow - videoMetadata.PlayheadUpdatedAt;
                var playheadCurrent = videoMetadata.PlayheadSeconds + diff.TotalSeconds;
                _sync?.Pan(playheadCurrent);
            }));
        }

        _ = Connect();

        if (!_videoReader.Open(VideoPath))
        {
            throw new InvalidOperationException("Failed to open video.");
        }

        var width = _videoReader.Width;
        var height = _videoReader.Height;
        PluginLog.Log($"Bootstrapping texture with dimensions ({width}, {height})");
        _textureBootstrap.Initialize(width, height);

        // Initialize the screen
        _sync = new TimePlaybackTracker();
        _renderSource = new VideoReaderMediaSource(_videoReader, _sync);
        _screen = new TextureScreen(_textureBootstrap, _pluginInterface.UiBuilder);
        _screen.Show(_renderSource);

        try
        {
            _material = Material.CreateFromTexture(_textureBootstrap.TexturePointer);

            PluginLog.Log("Initializing PrimitiveDebug");
            _primitive.Initialize();
            _unsubscribe = _primitive.Subscribe(() =>
            {
                if (_clientState.LocalPlayer is null)
                {
                    return;
                }

                var position = _clientState.LocalPlayer.Position;

                // TODO: There's a 1px texture wraparound on all sides of the primitive, possibly due to UV/command type
                var context = _primitive.GetContext();
                var vertexPtr = context.DrawCommand(0x21, 4, 5, _material.Pointer);
                if (vertexPtr == nint.Zero)
                {
                    return;
                }

                var aspectRatio = GetAspectRatio(_textureBootstrap.Texture);
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
            });
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, "Failed to initialize primitive");
        }
    }

    private static float GetAspectRatio(Texture texture)
    {
        return GetAspectRatio(Convert.ToInt32(texture.Width), Convert.ToInt32(texture.Height));
    }

    private static float GetAspectRatio(int width, int height)
    {
        return (float)height / width;
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

        _commandManager.RemoveHandler("/simon");
        _commandManager.RemoveHandler("/simoff");
        _commandManager.RemoveHandler("/simsync");
        _commandManager.RemoveHandler("/simpause");
        _commandManager.RemoveHandler("/simplay");

        _screen?.Dispose();
        _renderSource?.Dispose();

        _pluginInterface.UiBuilder.Draw -= _windows.Draw;
        _framework.Update -= OnFrameworkUpdate;

        _unsubscribe?.Dispose();
        _textureBootstrap.Dispose();
        _material?.Dispose();

        foreach (var subscription in _hostctlBag)
        {
            subscription.Dispose();
        }

        _hostctl?.Dispose();

        _videoReader.Close();
        _videoReader.Dispose();

        UninstallAVLogHandler();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}