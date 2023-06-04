﻿using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Simulacrum.AV;
using Simulacrum.Game;
using Simulacrum.Game.Structures;

namespace Simulacrum;

public class Simulacrum : IDalamudPlugin
{
    public string Name => "Simulacrum";

    private readonly ClientState _clientState;
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Framework _framework;
    private readonly CustomizationWindow _customizationWindow;
    private readonly PluginConfiguration _config;
    private readonly PrimitiveDebug _primitive;
    private readonly VideoReader _videoReader;
    private readonly WindowSystem _windows;

    private readonly TextureBootstrap _textureBootstrap;

    private readonly byte[] _material;

    private IDisposable? _unsubscribe;
    private bool _initialized;

    public Simulacrum(
        [RequiredVersion("1.0")] ClientState clientState,
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] Framework framework,
        [RequiredVersion("1.0")] SigScanner sigScanner)
    {
        _clientState = clientState;
        _framework = framework;
        _pluginInterface = pluginInterface;

        _config = (PluginConfiguration?)pluginInterface.GetPluginConfig() ?? new PluginConfiguration();
        _config.Initialize(pluginInterface);

        _primitive = new PrimitiveDebug(sigScanner);

        _textureBootstrap = new TextureBootstrap(sigScanner);

        _videoReader = new VideoReader();

        _material = GC.AllocateArray<byte>(Marshal.SizeOf<PrimitiveMaterial>(), pinned: true);

        _windows = new WindowSystem("Simulacrum");
        _customizationWindow = new CustomizationWindow();
        _windows.AddWindow(_customizationWindow);
        _customizationWindow.IsOpen = true;

        _pluginInterface.UiBuilder.Draw += _windows.Draw;

        _framework.Update += OnFrameworkUpdate;
    }

    private const string VideoPath = @"D:\rider64_xKQhMNjffD.mp4";

    public void OnFrameworkUpdate(Framework f)
    {
        if (_initialized) return;
        _initialized = true;

        _videoReader.Open(VideoPath);

        const int width = 424;
        const int height = 310;
        const int pixelSize = 4;

        var frameBuffer = new byte[width * height * pixelSize];
        _videoReader.ReadFrame(frameBuffer.AsSpan(), out _);

        _textureBootstrap.Initialize(width, height);

        _textureBootstrap.Mutate((sub, desc) =>
        {
            unsafe
            {
                // Copy the replacement image to the new texture
                var src = (byte*)Unsafe.AsPointer(ref frameBuffer.AsSpan()[0]);
                var dst = (byte*)sub.PData;
                var pitch = sub.RowPitch;
                TextureUtils.CopyTexture2D(src, dst, desc.Width, desc.Height, sizeof(Bgra32), pitch);
            }
        });

        try
        {
            nint materialPtr;
            unsafe
            {
                fixed (byte* material = _material)
                {
                    materialPtr = (nint)material;
                }
            }

            PluginLog.Log("Serializing material");
            new PrimitiveMaterial
            {
                BlendState = new BlendState
                {
                    ColorWriteEnable = ColorMask.RGBA,
                    AlphaBlendFactorDst = 0x5,
                    AlphaBlendFactorSrc = 0x0,
                    AlphaBlendOperation = 0,
                    ColorBlendFactorDst = 0x5,
                    ColorBlendFactorSrc = 0x4,
                    ColorBlendOperation = 0,
                    Enable = true,
                },
                Texture = _textureBootstrap.TexturePointer,
                SamplerState = new SamplerState
                {
                    GammaEnable = false,
                    MaxAnisotropy = 0,
                    MinLOD = 0x0,
                    MipLODBias = 0,
                    Filter = 9,
                    AddressW = 0,
                    AddressV = 0,
                    AddressU = 0,
                },
                Params = new PrimitiveMaterialParams
                {
                    FaceCullMode = 0,
                    FaceCullEnable = false,
                    DepthWriteEnable = true,
                    DepthTestEnable = true,
                    TextureRemapAlpha = 0x2,
                    TextureRemapColor = 0x2,
                },
            }.WriteStructure(materialPtr);

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
                var vertexPtr = context.DrawCommand(0x21, 4, 5, materialPtr);

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

        _pluginInterface.UiBuilder.Draw -= _windows.Draw;
        _framework.Update -= OnFrameworkUpdate;
        _textureBootstrap.Dispose();
        _unsubscribe?.Dispose();
        _videoReader.Close();
        _videoReader.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}