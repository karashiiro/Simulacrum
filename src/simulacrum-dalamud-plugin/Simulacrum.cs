using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Simulacrum.Game;

namespace Simulacrum;

public class Simulacrum : IDalamudPlugin
{
    public string Name => "Simulacrum";

    private readonly Framework _framework;
    private readonly PluginConfiguration _config;
    private readonly PrimitiveDebug _primitive;

    private readonly byte[] _material;

    private IDisposable? _unsubscribe;
    private bool _initialized;

    public Simulacrum(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] Framework framework,
        [RequiredVersion("1.0")] SigScanner sigScanner)
    {
        _framework = framework;

        _config = (PluginConfiguration?)pluginInterface.GetPluginConfig() ?? new PluginConfiguration();
        _config.Initialize(pluginInterface);

        _primitive = new PrimitiveDebug(sigScanner);

        _material = GC.AllocateArray<byte>(Marshal.SizeOf<PrimitiveMaterial>(), pinned: true);

        _framework.Update += OnFrameworkUpdate;
    }

    public void OnFrameworkUpdate(Framework f)
    {
        if (_initialized) return;
        _initialized = true;

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
                Texture = (nint)0x2f0bc662d40,
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
                unsafe
                {
                    var context = _primitive.GetContext();
                    var vertexPtr = context.DrawCommand(0x21, 4, 5, materialPtr);
                    var vertices = new Span<Vertex>((void*)vertexPtr, 4)
                    {
                        [0] = new()
                        {
                            Position = Position.FromCoordinates(-248.0723f, 40.1f, 202.5298f),
                            Color = Color.FromRGBA(0xFF, 0xFF, 0xFF, 0xFF),
                            UV = UV.FromUV(0, 0),
                        },
                        [1] = new()
                        {
                            Position = Position.FromCoordinates(-248.0400f, 40.1f, 210.9190f),
                            Color = Color.FromRGBA(0xFF, 0xFF, 0xFF, 0xFF),
                            UV = UV.FromUV(1, 0),
                        },
                        [2] = new()
                        {
                            Position = Position.FromCoordinates(-240.6584f, 40.1f, 204.1926f),
                            Color = Color.FromRGBA(0xFF, 0xFF, 0xFF, 0xFF),
                            UV = UV.FromUV(0, 1),
                        },
                        [3] = new()
                        {
                            Position = Position.FromCoordinates(-240.8050f, 40.1f, 211.2095f),
                            Color = Color.FromRGBA(0xFF, 0xFF, 0xFF, 0xFF),
                            UV = UV.FromUV(1, 1),
                        },
                    };

                    PluginLog.Log($"Wrote {vertices.Length} vertices");
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
        _unsubscribe?.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}