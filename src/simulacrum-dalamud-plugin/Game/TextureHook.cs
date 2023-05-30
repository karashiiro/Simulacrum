using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Simulacrum.Game;

public class TextureHook : IDisposable
{
    private Hook<CreateApricotTextureFromTex>? _hook;
    private readonly SigScanner _sigScanner;
    private byte[]? _tex;

    public Texture Texture { get; private set; }
    public nint TexturePointer { get; private set; }

    public TextureHook(SigScanner sigScanner)
    {
        _sigScanner = sigScanner;
    }

    public unsafe void Initialize()
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

        var easyCreate = Marshal.GetDelegateForFunctionPointer<CreateApricotTextureFromTex>(addr2);
        PluginLog.Log($"CreateApricotTextureFromTex: ffxiv_dx11.exe+{addr2 - _sigScanner.Module.BaseAddress:X}");

        fixed (byte* tex = _tex)
        {
            var apricotTex = easyCreate(nint.Zero, (nint)tex, _tex.Length);
            var textureAddr = Marshal.ReadIntPtr(apricotTex + 16);
            Texture = Marshal.PtrToStructure<Texture>(textureAddr);
            TexturePointer = textureAddr;
            TextureUtils.DescribeTexture(Texture);
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