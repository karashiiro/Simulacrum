using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Lumina.Data.Files;
using Silk.NET.Direct3D11;

namespace Simulacrum.Game;

public static class TextureUtils
{
    public static unsafe void DescribeTexture(Texture* texture)
    {
        var dxTexture = (ID3D11Texture2D*)texture->D3D11Texture2D;
        var dxShaderView = (ID3D11ShaderResourceView1*)texture->D3D11ShaderResourceView;
        var dxTextureDesc = new Texture2DDesc();
        var dxResource = new ResourceDimension();
        dxTexture->GetDesc(ref dxTextureDesc);
        dxTexture->GetType(ref dxResource);
        var dxShader = new ShaderResourceViewDesc();
        dxShaderView->GetDesc(ref dxShader);
        PluginLog.LogDebug($"Texture ({(nint)texture:X}):\n" +
                           $"  vtbl: {(nint)texture->vtbl:X}\n" +
                           $"  Width: {texture->Width}\n" +
                           $"  Height: {texture->Height}\n" +
                           $"  Depth: {texture->Depth}\n" +
                           $"  MipLevel: {texture->MipLevel}\n" +
                           $"  TextureFormat: {(TexFile.TextureFormat)texture->TextureFormat}\n" +
                           $"  D3D11Texture2D ({(nint)dxTexture:X}):\n" +
                           "    Texture2DDesc:\n" +
                           $"      Width: {dxTextureDesc.Width}\n" +
                           $"      Height: {dxTextureDesc.Height}\n" +
                           $"      MipLevels: {dxTextureDesc.MipLevels}\n" +
                           $"      ArraySize: {dxTextureDesc.ArraySize}\n" +
                           $"      Format: {dxTextureDesc.Format}\n" +
                           $"      Usage: {dxTextureDesc.Usage}\n" +
                           "      SampleDesc:\n" +
                           $"        Count: {dxTextureDesc.SampleDesc.Count}\n" +
                           $"        Quality: {dxTextureDesc.SampleDesc.Quality}\n" +
                           $"      BindFlags: {dxTextureDesc.BindFlags}\n" +
                           $"      CPUAccessFlags: {dxTextureDesc.CPUAccessFlags}\n" +
                           $"      MiscFlags: {dxTextureDesc.MiscFlags}\n" +
                           $"    ResourceDimension: {dxResource}\n" +
                           $"  D3D11ShaderResourceView ({(nint)dxShaderView:X}):\n" +
                           "    ShaderResourceViewDesc:\n" +
                           $"      Format: {dxShader.Format}\n" +
                           $"      ViewDimension: {dxShader.ViewDimension}");
    }
}