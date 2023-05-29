using System.Runtime.InteropServices;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Lumina.Data.Files;

namespace Simulacrum.Game;

public class TextureHook : IDisposable
{
    private Hook<CreateApricotTextureFromTex>? _hook;
    private readonly SigScanner _sigScanner;
    private readonly DataManager _dataManager;
    private byte[]? _tex;

    public Texture Texture { get; private set; }
    public nint TexturePointer { get; private set; }

    public TextureHook(SigScanner sigScanner, DataManager dataManager)
    {
        _sigScanner = sigScanner;
        _dataManager = dataManager;
    }

    public unsafe void Initialize()
    {
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

        var mapOverlay = _dataManager.GetFile<TexFile>("ui/uld/NaviMap_hr1.tex") ??
                         throw new InvalidOperationException("Could not load texture.");
        var mapOverlayData = mapOverlay.Data;
        _tex = GC.AllocateArray<byte>(mapOverlayData.Length, pinned: true);
        mapOverlayData.CopyTo(_tex.AsSpan());

        var easyCreate = Marshal.GetDelegateForFunctionPointer<CreateApricotTextureFromTex>(addr2);
        PluginLog.Log($"CreateApricotTextureFromTex: ffxiv_dx11.exe+{addr2 - _sigScanner.Module.BaseAddress:X}");

        fixed (byte* tex = _tex)
        {
            var apricotTex = easyCreate(nint.Zero, (nint)tex, _tex.Length);
            var textureAddr = Marshal.ReadIntPtr(apricotTex + 16);
            Texture = Marshal.PtrToStructure<Texture>(textureAddr);
            TexturePointer = textureAddr;
            PluginLog.Log(
                $"  vtbl: {(nint)Texture.vtbl:X}, Width: {Texture.Width}, Height: {Texture.Height}, Width2: {Texture.Width2}, Height2: {Texture.Height2}, Width3: {Texture.Width3}, Height3: {Texture.Height3}, D3D11Texture2D: {(nint)Texture.D3D11Texture2D:X}");
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate nint CreateApricotTextureFromTex(nint thisPtr, nint unk1, int unk2);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _hook?.Disable();
        _hook?.Dispose();
    }
}