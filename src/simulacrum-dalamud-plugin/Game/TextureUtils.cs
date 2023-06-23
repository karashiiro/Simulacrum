using System.Diagnostics;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Lumina.Data.Files;
using Silk.NET.Direct3D11;

namespace Simulacrum.Game;

public static class TextureUtils
{
    [Conditional("DEBUG")]
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

    /// <summary>
    /// Copies 2D texture data from a source to a destination. The source data is assumed to be
    /// contiguous and have the same pixel width as the destination.
    /// </summary>
    /// <param name="src">The source buffer.</param>
    /// <param name="dst">The destination buffer.</param>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <param name="pixelWidth">The pixel width of the texture.</param>
    /// <param name="rowPitch">The row pitch of the destination texture.</param>
    public static unsafe void CopyTexture2D(
        byte[] src,
        byte* dst,
        uint width,
        uint height,
        int pixelWidth,
        uint rowPitch)
    {
        if (src.Length < width * height * pixelWidth)
        {
            throw new InvalidOperationException("The source buffer is too short to copy.");
        }

        fixed (byte* s1 = src)
        {
            var s2 = s1;

            // Perform a row-by-row copy of the source image to the destination texture
            var rowSize = width * pixelWidth;
            for (var i = 0; i < height; i++)
            {
                Buffer.MemoryCopy(s2, dst, rowSize, rowSize);
                dst += rowPitch;
                s2 += rowSize;
            }
        }
    }
}