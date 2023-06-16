using Dalamud.Interface;
using Dalamud.Logging;
using Simulacrum.Drawing.Common;
using Simulacrum.Game;

namespace Simulacrum.Drawing;

public class MaterialScreen : IScreen, IDisposable
{
    private readonly TextureBootstrap _texture;
    private readonly Material _material;
    private readonly UiBuilder _ui;
    private byte[]? _buffer;
    private IMediaSource? _source;

    public nint MaterialPointer => _material.Pointer;

    public MaterialScreen(TextureBootstrap texture, UiBuilder ui)
    {
        _texture = texture;
        _material = Material.CreateFromTexture(_texture.TexturePointer);
        _ui = ui;

        // TODO: This works because it's called on IDXGISwapChain::Present, that should be hooked instead of rendering mid-imgui
        _ui.Draw += Draw;
    }

    private void Draw()
    {
        if (_source == null || _texture.TexturePointer == nint.Zero) return;

        if (_buffer == null)
        {
            var sourceSize = _source.Size();
            var sourcePixelSize = _source.PixelSize();
            var bufferSize = sourceSize.X * sourceSize.Y * sourcePixelSize;
            _buffer = new byte[bufferSize];
            for (var i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = 0xFF;
            }
        }

        _source.RenderTo(_buffer);

        _texture.Mutate((sub, desc) =>
        {
            unsafe
            {
                fixed (byte* src = _buffer)
                {
                    var dst = (byte*)sub.PData;
                    var pitch = sub.RowPitch;
                    const int pixelSize = 4;
                    TextureUtils.CopyTexture2D(src, dst, desc.Width, desc.Height, pixelSize, pitch);
                }
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

    public void Dispose()
    {
        _ui.Draw -= Draw;
        _material.Dispose();
        GC.SuppressFinalize(this);
    }
}