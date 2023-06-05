using System.Runtime.CompilerServices;
using Dalamud.Interface;
using Simulacrum.Drawing.Common;
using Simulacrum.Game;

namespace Simulacrum.Drawing;

public class TextureScreen : IScreen, IDisposable
{
    private readonly TextureBootstrap _texture;
    private readonly UiBuilder _ui;
    private byte[]? _buffer;
    private IRenderSource? _source;

    public TextureScreen(TextureBootstrap texture, UiBuilder ui)
    {
        _texture = texture;
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
            var bufferSize = Convert.ToInt32(sourceSize.X * sourceSize.Y * sourcePixelSize);
            _buffer = GC.AllocateArray<byte>(bufferSize, pinned: true);
        }

        _source.RenderTo(_buffer);

        _texture.Mutate((sub, desc) =>
        {
            unsafe
            {
                var src = (byte*)Unsafe.AsPointer(ref _buffer.AsSpan()[0]);
                var dst = (byte*)sub.PData;
                var pitch = sub.RowPitch;
                const int pixelSize = 4;
                TextureUtils.CopyTexture2D(src, dst, desc.Width, desc.Height, pixelSize, pitch);
            }
        });
    }

    public void Show(IRenderSource source)
    {
        _source = source;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _ui.Draw -= Draw;
    }
}