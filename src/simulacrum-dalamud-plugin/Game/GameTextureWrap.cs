using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using ImGuiScene;

namespace Simulacrum.Game;

public unsafe class GameTextureWrap : TextureWrap
{
    private readonly Texture* _texture;

    public nint ImGuiHandle => (nint)_texture->D3D11ShaderResourceView;

    public int Width => Convert.ToInt32(_texture->Width);

    public int Height => Convert.ToInt32(_texture->Height);

    public GameTextureWrap(nint texture)
    {
        _texture = (Texture*)texture;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}