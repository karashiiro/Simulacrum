using System.Runtime.InteropServices;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility.Numerics;
using Simulacrum.Game;

namespace Simulacrum;

public class Simulacrum : IDalamudPlugin
{
    public string Name => "Simulacrum";

    private readonly ClientState _clientState;
    private readonly Framework _framework;
    private readonly PluginConfiguration _config;
    private readonly PrimitiveDebug _primitive;

    private readonly TextureHook _textureHook;

    private readonly byte[] _material;

    private IDisposable? _unsubscribe;
    private bool _initialized;

    public Simulacrum(
        [RequiredVersion("1.0")] ClientState clientState,
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] DataManager dataManager,
        [RequiredVersion("1.0")] Framework framework,
        [RequiredVersion("1.0")] SigScanner sigScanner)
    {
        _clientState = clientState;
        _framework = framework;

        _config = (PluginConfiguration?)pluginInterface.GetPluginConfig() ?? new PluginConfiguration();
        _config.Initialize(pluginInterface);

        _primitive = new PrimitiveDebug(sigScanner);

        _textureHook = new TextureHook(sigScanner, dataManager);

        _material = GC.AllocateArray<byte>(Marshal.SizeOf<PrimitiveMaterial>(), pinned: true);

        _framework.Update += OnFrameworkUpdate;
    }

    public void OnFrameworkUpdate(Framework f)
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _textureHook.Initialize();
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, "Failed to hook texture ctor");
        }

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
                Texture = _textureHook.TexturePointer,
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
                    FaceCullEnable = true,
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
                var color = Color.FromRGBA(0xFF, 0xFF, 0xFF, 0xFF);

                var context = _primitive.GetContext();
                var vertexPtr = context.DrawCommand(0x21, 4, 5, materialPtr);

                unsafe
                {
                    var vertices = new Span<Vertex>((void*)vertexPtr, 4)
                    {
                        [0] = new()
                        {
                            Position = position,
                            Color = color,
                            UV = UV.FromUV(0, 0),
                        },
                        [1] = new()
                        {
                            Position = position.WithZ(position.Z + 1).WithY(position.Y + 0.01f),
                            Color = color,
                            UV = UV.FromUV(1, 0),
                        },
                        [2] = new()
                        {
                            Position = position.WithX(position.X + 1).WithY(position.Y + 0.01f),
                            Color = color,
                            UV = UV.FromUV(0, 1),
                        },
                        [3] = new()
                        {
                            Position = position.WithX(position.X + 1).WithZ(position.Z + 1).WithY(position.Y + 0.01f),
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

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        _framework.Update -= OnFrameworkUpdate;
        _textureHook.Dispose();
        _unsubscribe?.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}