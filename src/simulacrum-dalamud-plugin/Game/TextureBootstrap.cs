﻿using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Simulacrum.Game.Structures;
using Simulacrum.Monitoring;

namespace Simulacrum.Game;

public class TextureBootstrap : IDisposable
{
    private static readonly IHistogram? TextureMutateDuration =
        DebugMetrics.CreateHistogram("simulacrum_texture_mutate_duration", "The DX11 texture mutation duration (ms).");

    private readonly ISigScanner _sigScanner;
    private readonly IFramework _framework;
    private readonly IPluginLog _log;

    private unsafe ApricotTexture* _apricotTexture;

    public unsafe Texture Texture => _apricotTexture != null
        ? Marshal.PtrToStructure<Texture>((nint)_apricotTexture->Texture)
        : default;

    public unsafe nint TexturePointer => _apricotTexture != null
        ? (nint)_apricotTexture->Texture
        : nint.Zero;

    public TextureBootstrap(ISigScanner sigScanner, IFramework framework, IPluginLog log)
    {
        _sigScanner = sigScanner;
        _framework = framework;
        _log = log;
    }

    /// <summary>
    /// Maps the texture for editing in the provided callback. Handles safely unmapping the
    /// texture on completion.
    /// </summary>
    /// <param name="mutate">A callback in which the texture can be safely mutated.</param>
    /// <exception cref="InvalidOperationException">
    /// If the texture was not created with the default or dynamic usage flags.
    /// </exception>
    public unsafe void Mutate(Action<MappedSubresource, Texture2DDesc> mutate)
    {
        var stopwatch = Stopwatch.StartNew();

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

        try
        {
            mutate(dxMappedSubresource, dxTextureDesc);
        }
        finally
        {
            dxContext->Unmap(dxResource, 0);
        }

        TextureMutateDuration?.Observe(stopwatch.Elapsed.TotalMilliseconds);
    }

    public async Task Initialize(int width, int height, CancellationToken cancellationToken)
    {
        // TODO: Clean up this signature
        var addr = _sigScanner.ScanText(
            "48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 57 48 83 ec 40 48 8b f2 41 8b e8 45 33 c0 33 ff 44");
        var easyCreate = Marshal.GetDelegateForFunctionPointer<CreateApricotTextureFromTex>(addr);
        _log.Info($"CreateApricotTextureFromTex: ffxiv_dx11.exe+{addr - _sigScanner.Module.BaseAddress:X}");

        await using var texFile =
            Assembly.GetExecutingAssembly().GetManifestResourceStream("Simulacrum.bootstrap_rgba.tex") ??
            throw new InvalidOperationException("Could not find embedded file.");

        // Allocate a pinned array and get a stable pointer to it in a safe context so
        // we can await the graphics subsystem initialization later (can't use async in
        // an unsafe context).
        var tex = GC.AllocateArray<byte>(Convert.ToInt32(texFile.Length), pinned: true);
        nint texPtr;
        unsafe
        {
            fixed (byte* texFixed = tex)
            {
                texPtr = (nint)texFixed;
            }
        }

        var read = texFile.Read(tex);
        if (read != texFile.Length)
        {
            throw new InvalidOperationException("Failed to read stream data.");
        }

        var apricotTexture = await CreateTexture(easyCreate, texPtr, tex.Length, cancellationToken);
        unsafe
        {
            _apricotTexture = (ApricotTexture*)apricotTexture;

            TextureUtils.DescribeTexture(_log, _apricotTexture->Texture);

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
            TextureUtils.DescribeTexture(_log, _apricotTexture->Texture);

            // Release the original resources
            dxTexture->Release();
            dxShaderView->Release();
        }
    }

    private async Task<nint> CreateTexture(
        CreateApricotTextureFromTex easyCreate,
        nint texPtr,
        int texLength,
        CancellationToken cancellationToken)
    {
        var apricotTexture = await _framework.RunOnFrameworkThread(() => easyCreate(nint.Zero, texPtr, texLength));

        // If the graphics subsystem hasn't been initialized yet, easyCreate will return a null pointer.
        while (apricotTexture == nint.Zero)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            apricotTexture = await _framework.RunOnFrameworkThread(() => easyCreate(nint.Zero, texPtr, texLength));
        }

        return apricotTexture;
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate nint CreateApricotTextureFromTex(nint thisPtr, nint unk1, long unk2);

    unsafe void IDisposable.Dispose()
    {
        if (_apricotTexture != null)
        {
            _apricotTexture->Release();
            _apricotTexture = null;
        }

        GC.SuppressFinalize(this);
    }
}