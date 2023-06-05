using System.Numerics;
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
    private TextureScreen? _screen;
    private VideoReaderRenderer? _renderSource;
    private bool _initialized;

    public Simulacrum(
        [RequiredVersion("1.0")] ClientState clientState,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] Framework framework,
        [RequiredVersion("1.0")] SigScanner sigScanner)
    {
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

        _commandManager.AddHandler("/simplay", new CommandInfo((_, _) => _sync?.Play()));
        _commandManager.AddHandler("/simpause", new CommandInfo((_, _) => _sync?.Pause()));
        _commandManager.AddHandler("/simsync", new CommandInfo((_, _) => _sync?.Pan(0)));
    }

    private const string VideoPath = @"D:\rider64_xKQhMNjffD.mp4";

    public void OnFrameworkUpdate(Framework f)
    {
        // TODO: Something here randomly causes a CTD, fix it
        if (_clientState.LocalPlayer is null)
        {
            return;
        }

        if (_initialized) return;
        _initialized = true;

        if (!_videoReader.Open(VideoPath))
        {
            throw new InvalidOperationException("Failed to open video.");
        }

        var width = _videoReader.Width;
        var height = _videoReader.Height;
        _textureBootstrap.Initialize(width, height);

        // Initialize the screen
        _sync = new TimePlaybackTracker();
        _renderSource = new VideoReaderRenderer(_videoReader, _sync);
        _screen = new TextureScreen(_textureBootstrap, _pluginInterface.UiBuilder);
        _screen.Show(_renderSource);

        try
        {
            var material = Material.CreateFromTexture(_textureBootstrap.TexturePointer);

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
                var vertexPtr = context.DrawCommand(0x21, 4, 5, material.Pointer);

                var aspectRatio = GetAspectRatio(_textureBootstrap.Texture);
                var dimensions = new Vector3(1, aspectRatio, 0);
                var translation = _customizationWindow.Translation;
                var scale = _customizationWindow.Scale;
                var color = _customizationWindow.Color;
                unsafe
                {
                    var vertices = new Span<Vertex>((void*)vertexPtr, 4)
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

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        _commandManager.RemoveHandler("/simsync");
        _commandManager.RemoveHandler("/simpause");
        _commandManager.RemoveHandler("/simplay");
        _screen?.Dispose();
        _renderSource?.Dispose();
        _pluginInterface.UiBuilder.Draw -= _windows.Draw;
        _framework.Update -= OnFrameworkUpdate;
        _unsubscribe?.Dispose();
        _textureBootstrap.Dispose();
        _videoReader.Close();
        _videoReader.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}