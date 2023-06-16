﻿using Dalamud.Interface;
using Simulacrum.Drawing.Common;
using Simulacrum.Game;

namespace Simulacrum.Drawing;

public class MaterialScreen : IScreen, IDisposable
{
    private readonly TextureFactory _textureFactory;
    private readonly UiBuilder _ui;

    private byte[] _buffer;
    private Material? _material;
    private TextureBootstrap? _texture;
    private IMediaSource? _source;
    private IntVector2 _size;

    public nint MaterialPointer => _material?.Pointer ?? nint.Zero;

    public MaterialScreen(TextureFactory textureFactory, UiBuilder ui)
    {
        _buffer = Array.Empty<byte>();

        _textureFactory = textureFactory;
        _ui = ui;

        // TODO: This works because it's called on IDXGISwapChain::Present, that should be hooked instead of rendering mid-imgui
        _ui.Draw += Draw;
    }

    private async Task RebuildMaterial(int width, int height)
    {
        // You're not supposed to call GetResult() on a ValueTask, so this is just a regular
        // task instead.
        _material?.Dispose();
        _texture = await _textureFactory.Create(width, height, default);
        _material = Material.CreateFromTexture(_texture.TexturePointer);
    }

    private void Draw()
    {
        if (_source == null || _texture?.TexturePointer == nint.Zero) return;

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

        _source.RenderTo(_buffer);

        _texture?.Mutate((sub, desc) =>
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
        _material?.Dispose();
        GC.SuppressFinalize(this);
    }
}