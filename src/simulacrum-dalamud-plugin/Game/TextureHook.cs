using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Simulacrum.Game.Structures;

namespace Simulacrum.Game;

public unsafe class TextureHook : IDisposable
{
    private Hook<CreateApricotTextureFromTex>? _hook;
    private readonly SigScanner _sigScanner;
    private byte[]? _tex;

    // TODO: Call Release on this
    private ApricotTexture* _apricotTexture;

    public Texture Texture => _apricotTexture != null
        ? Marshal.PtrToStructure<Texture>((nint)_apricotTexture->Texture)
        : default;

    public nint TexturePointer => (nint)_apricotTexture->Texture;

    public TextureHook(SigScanner sigScanner)
    {
        _sigScanner = sigScanner;
    }

    public void Initialize()
    {
        // TODO: Clean up this signature
        var addr2 = _sigScanner.ScanText(
            "48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 57 48 83 ec 40 48 8b f2 41 8b e8 45 33 c0 33 ff 44");
        _hook = Hook<CreateApricotTextureFromTex>.FromAddress(addr2, (thisPtr, unk1, unk2) =>
        {
            var ret = _hook!.Original(thisPtr, unk1, unk2);
            try
            {
                PluginLog.Log($"CreateApricotTextureFromTex: {ret:X} - {thisPtr:X}, {unk1:X}, {unk2:X}");
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, "Failed to log CreateApricotTextureFromTex");
            }

            return ret;
        });

        _hook.Enable();

        using var texFile = Assembly.GetExecutingAssembly().GetManifestResourceStream("Simulacrum.test.tex") ??
                            throw new InvalidOperationException("Could not find embedded file.");
        _tex = GC.AllocateArray<byte>(Convert.ToInt32(texFile.Length), pinned: true);
        var read = texFile.Read(_tex);
        if (read != texFile.Length)
        {
            throw new InvalidOperationException("Failed to read stream data.");
        }

        using var pngFile = Assembly.GetExecutingAssembly().GetManifestResourceStream("Simulacrum.test.png") ??
                            throw new InvalidOperationException("Could not find embedded file.");
        using var pngImage = Image.Load(pngFile);

        var easyCreate = Marshal.GetDelegateForFunctionPointer<CreateApricotTextureFromTex>(addr2);
        PluginLog.Log($"CreateApricotTextureFromTex: ffxiv_dx11.exe+{addr2 - _sigScanner.Module.BaseAddress:X}");

        fixed (byte* tex = _tex)
        {
            // TODO: This can return null if it's called early enough, defer it somehow
            _apricotTexture = (ApricotTexture*)easyCreate(nint.Zero, (nint)tex, _tex.Length);
            TextureUtils.DescribeTexture(_apricotTexture->Texture);

            // Swap the texture
            var dxContext = (ID3D11DeviceContext*)Device.Instance()->D3D11DeviceContext;
            var dxDevice = (ID3D11Device*)Device.Instance()->D3D11Forwarder;
            var dxTexture = (ID3D11Texture2D*)_apricotTexture->Texture->D3D11Texture2D;
            var dxTextureDesc = new Texture2DDesc();
            dxTexture->GetDesc(ref dxTextureDesc);
            var dxShaderView = (ID3D11ShaderResourceView1*)_apricotTexture->Texture->D3D11ShaderResourceView;

            // Ensure the texture is writable
            dxTextureDesc.MipLevels = 1;
            dxTextureDesc.Usage = Usage.Dynamic;
            dxTextureDesc.CPUAccessFlags = 0x10000;

            // Get the replacement image buffer as B8G8R8A8Unorm data
            var config = Configuration.Default.Clone();
            config.PreferContiguousImageBuffers = true;
            using var transcodedImage = pngImage.CloneAs<Bgra32>(config);
            if (!transcodedImage.DangerousTryGetSinglePixelMemory(out var transcodedData))
            {
                throw new InvalidOperationException("Failed to get transcoded image data.");
            }

            // Create the new texture
            ID3D11Texture2D* dxNewTexture;
            SilkMarshal.ThrowHResult(dxDevice->CreateTexture2D(dxTextureDesc, null, &dxNewTexture));

            var dxResource = (ID3D11Resource*)dxNewTexture;
            var dxMappedSubresource = new MappedSubresource();
            SilkMarshal.ThrowHResult(dxContext->Map(dxResource, 0, Map.WriteDiscard, 0, ref dxMappedSubresource));

            // Perform a row-by-row copy of the replacement image to the new texture
            var src = (byte*)Unsafe.AsPointer(ref transcodedData.Span[0]);
            var dst = (byte*)dxMappedSubresource.PData;
            for (var i = 0; i < dxTextureDesc.Height; i++)
            {
                // TODO: This is broken somehow, fix it
                Buffer.MemoryCopy(src, dst, dxTextureDesc.Width * sizeof(Bgra32), dxTextureDesc.Width * sizeof(Bgra32));
                dst += dxMappedSubresource.RowPitch / sizeof(Bgra32);
                src += dxTextureDesc.Width;
            }

            dxContext->Unmap(dxResource, 0);

            // Bind the texture to the pipeline with a new resource view
            ID3D11ShaderResourceView* dxNewShaderView;
            SilkMarshal.ThrowHResult(dxDevice->CreateShaderResourceView(dxResource, null, &dxNewShaderView));

            // Swap out the final data
            _apricotTexture->Texture->MipLevel = 1;
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
        _hook?.Disable();
        _hook?.Dispose();
    }
}