﻿using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Simulacrum.Game.Structures;

namespace Simulacrum.Game;

public unsafe class TextureBootstrap : IDisposable
{
    private readonly SigScanner _sigScanner;
    private byte[]? _tex;

    private ApricotTexture* _apricotTexture;

    public Texture Texture => _apricotTexture != null
        ? Marshal.PtrToStructure<Texture>((nint)_apricotTexture->Texture)
        : default;

    public nint TexturePointer => (nint)_apricotTexture->Texture;

    public TextureBootstrap(SigScanner sigScanner)
    {
        _sigScanner = sigScanner;
    }

    public void Mutate(Action<MappedSubresource, Texture2DDesc> mutate)
    {
        var dxContext = (ID3D11DeviceContext*)Device.Instance()->D3D11DeviceContext;
        var dxTexture = (ID3D11Texture2D*)_apricotTexture->Texture->D3D11Texture2D;
        var dxTextureDesc = new Texture2DDesc();
        dxTexture->GetDesc(ref dxTextureDesc);
        if (dxTextureDesc.Usage != Usage.Dynamic && dxTextureDesc.Usage != Usage.Default)
        {
            throw new InvalidOperationException("Only default or dynamic textures may be mutated.");
        }
        
        var dxResource = (ID3D11Resource*)dxTexture;
        
        var dxMappedSubresource = new MappedSubresource();
        SilkMarshal.ThrowHResult(dxContext->Map(dxResource, 0, Map.WriteDiscard, 0, ref dxMappedSubresource));

        mutate(dxMappedSubresource, dxTextureDesc);

        dxContext->Unmap(dxResource, 0);
    }

    public void Initialize(int width, int height)
    {
        // TODO: Clean up this signature
        var addr = _sigScanner.ScanText(
            "48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 57 48 83 ec 40 48 8b f2 41 8b e8 45 33 c0 33 ff 44");
        var easyCreate = Marshal.GetDelegateForFunctionPointer<CreateApricotTextureFromTex>(addr);
        PluginLog.Log($"CreateApricotTextureFromTex: ffxiv_dx11.exe+{addr - _sigScanner.Module.BaseAddress:X}");

        using var texFile = Assembly.GetExecutingAssembly().GetManifestResourceStream("Simulacrum.test.tex") ??
                            throw new InvalidOperationException("Could not find embedded file.");
        _tex = GC.AllocateArray<byte>(Convert.ToInt32(texFile.Length), pinned: true);
        var read = texFile.Read(_tex);
        if (read != texFile.Length)
        {
            throw new InvalidOperationException("Failed to read stream data.");
        }
        
        fixed (byte* tex = _tex)
        {
            // TODO: This can return null if it's called early enough, defer it somehow
            _apricotTexture = (ApricotTexture*)easyCreate(nint.Zero, (nint)tex, _tex.Length);
            TextureUtils.DescribeTexture(_apricotTexture->Texture);

            // Swap the texture for a mutable one
            var dxDevice = (ID3D11Device*)Device.Instance()->D3D11Forwarder;
            var dxTexture = (ID3D11Texture2D*)_apricotTexture->Texture->D3D11Texture2D;
            var dxTextureDesc = new Texture2DDesc();
            dxTexture->GetDesc(ref dxTextureDesc);
            var dxShaderView = (ID3D11ShaderResourceView1*)_apricotTexture->Texture->D3D11ShaderResourceView;
            
            // Set the size as needed
            dxTextureDesc.Width = Convert.ToUInt32(width);
            dxTextureDesc.Height = Convert.ToUInt32(height);

            // Ensure the texture is writable
            dxTextureDesc.MipLevels = 1;
            dxTextureDesc.Usage = Usage.Dynamic;
            dxTextureDesc.CPUAccessFlags = 0x10000;

            // Create the new texture
            ID3D11Texture2D* dxNewTexture;
            SilkMarshal.ThrowHResult(dxDevice->CreateTexture2D(dxTextureDesc, null, &dxNewTexture));

            var dxResource = (ID3D11Resource*)dxNewTexture;

            // Bind the texture to the pipeline with a new resource view
            ID3D11ShaderResourceView* dxNewShaderView;
            SilkMarshal.ThrowHResult(dxDevice->CreateShaderResourceView(dxResource, null, &dxNewShaderView));

            // Swap out the final data
            _apricotTexture->Texture->Width = dxTextureDesc.Width;
            _apricotTexture->Texture->Height = dxTextureDesc.Height;
            _apricotTexture->Texture->MipLevel = Convert.ToByte(dxTextureDesc.MipLevels);
            _apricotTexture->Texture->D3D11Texture2D = dxNewTexture;
            _apricotTexture->Texture->D3D11ShaderResourceView = dxNewShaderView;
            TextureUtils.DescribeTexture(_apricotTexture->Texture);

            // Release the original resources
            dxTexture->Release();
            dxShaderView->Release();
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate nint CreateApricotTextureFromTex(nint thisPtr, nint unk1, long unk2);

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_apricotTexture != null)
        {
            _apricotTexture->Release();
            _apricotTexture = null;
        }
    }
}