using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Lumina.Data.Files;
using Silk.NET.Direct3D11;

namespace Simulacrum.Game;

public static class TextureUtils
{
    public static unsafe void DescribeTexture(Texture texture)
    {
        var dxTexture = (ID3D11Texture2D*)texture.D3D11Texture2D;
        var dxShaderView = (ID3D11ShaderResourceView1*)texture.D3D11ShaderResourceView;
        var dxDesc = new Texture2DDesc();
        var dxResource = new ResourceDimension();
        dxTexture->GetDesc(ref dxDesc);
        dxTexture->GetType(ref dxResource);
        var dxShader = new ShaderResourceViewDesc();
        dxShaderView->GetDesc(ref dxShader);
        PluginLog.LogDebug("Texture:\n" +
                           $"  vtbl: {(nint)texture.vtbl:X}\n" +
                           $"  Width: {texture.Width}\n" +
                           $"  Height: {texture.Height}\n" +
                           $"  Depth: {texture.Depth}\n" +
                           $"  MipLevel: {texture.MipLevel}\n" +
                           $"  TextureFormat: {(TexFile.TextureFormat)texture.TextureFormat}\n" +
                           "  D3D11Texture2D:\n" +
                           "    Texture2DDesc:\n" +
                           $"      Width: {dxDesc.Width}\n" +
                           $"      Height: {dxDesc.Height}\n" +
                           $"      MipLevels: {dxDesc.MipLevels}\n" +
                           $"      Format: {dxDesc.Format}\n" +
                           $"    ResourceDimension: {dxResource}\n" +
                           "  D3D11ShaderResourceView:\n" +
                           "    ShaderResourceViewDesc:\n" +
                           $"      Format: {dxShader.Format}\n" +
                           $"      ViewDimension: {dxShader.ViewDimension}");
    }
}