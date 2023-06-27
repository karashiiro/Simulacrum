using System.Diagnostics;
using Dalamud.Interface;
using Simulacrum.Drawing.Common;
using Simulacrum.Game;

namespace Simulacrum.Drawing;

public class MaterialScreen : IScreen, IDisposable
{
    private readonly TextureFactory _textureFactory;
    private readonly UiBuilder _ui;
    private readonly Location _location;
    private readonly Stopwatch _stopwatch;

    private byte[] _buffer;
    private Material? _material;
    private TextureBootstrap? _texture;
    private IMediaSource? _source;
    private IntVector2 _size;
    private TimeSpan _delay;

    public nint MaterialPointer => _material?.Pointer ?? nint.Zero;

    public GameTextureWrap? ImGuiTextureWrap => _texture != null ? new(_texture.TexturePointer) : null;

    public MaterialScreen(TextureFactory textureFactory, UiBuilder ui, Location location)
    {
        _buffer = Array.Empty<byte>();

        _textureFactory = textureFactory;
        _ui = ui;
        _location = location;
        _stopwatch = new Stopwatch();
        _stopwatch.Start();

        // TODO: This works because it's called on IDXGISwapChain::Present, that should be hooked instead of rendering mid-imgui
        _ui.Draw += Draw;
    }

    private async Task RebuildMaterial(int width, int height)
    {
        // Detaching ValueTasks is bad, so this is a regular Task instead
        // https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2012
        _material?.Dispose();
        _texture = await _textureFactory.Create(width, height, default);
        _material = Material.CreateFromTexture(_texture.TexturePointer);
    }

    private void Draw()
    {
        if (_source == null || _texture?.TexturePointer == nint.Zero || _stopwatch.Elapsed < _delay) return;

        var sourceSize = _source.Size();
        if (_size != sourceSize)
        {
            _size = sourceSize;

            var (sourceWidth, sourceHeight) = sourceSize;
            var sourcePixelSize = _source.PixelSize(); // Not currently checking if this has changed
            var bufferSize = sourceWidth * sourceHeight * sourcePixelSize;

            _buffer = new byte[bufferSize];

            // Initialize the screen with white (default is transparent) so we know it exists
            for (var i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = 0xFF;
            }

            // Rebuild the render surface; this doesn't need to happen immediately
            _ = RebuildMaterial(sourceWidth, sourceHeight);
        }

        _source.RenderTo(_buffer, out _delay);
        _stopwatch.Restart();

        _texture?.Mutate((sub, desc) =>
        {
            unsafe
            {
                var dst = (byte*)sub.PData;
                var pitch = sub.RowPitch;
                const int pixelSize = 4;
                TextureUtils.CopyTexture2D(_buffer, dst, desc.Width, desc.Height, pixelSize, pitch);
            }
        });
    }

    public void Show(IMediaSource source)
    {
        _source = source;
    }

    public float GetAspectRatio()
    {
        if (_source is null)
        {
            return 0;
        }

        var (width, height) = _source.Size();
        return (float)height / width;
    }

    public Location GetLocation()
    {
        return _location;
    }

    public void Dispose()
    {
        _ui.Draw -= Draw;
        _material?.Dispose();
        GC.SuppressFinalize(this);
    }
}