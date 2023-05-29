using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using static Simulacrum.Game.GameFunctions;

namespace Simulacrum.Game;

public abstract class Primitive
{
    protected readonly SigScanner Scanner;

    protected nint PrimitiveServer;
    protected nint PrimitiveContext;

    protected PrimitiveServerCtor? CallPrimitiveServerCtor;
    protected PrimitiveServerInitialize? CallPrimitiveServerInitialize;
    protected PrimitiveServerLoadResource? CallPrimitiveServerLoadResource;
    protected PrimitiveServerBegin? CallPrimitiveServerBegin;
    protected PrimitiveServerSpursSortUnencumbered? CallPrimitiveServerSpursSortUnencumbered;
    protected PrimitiveServerRender? CallPrimitiveServerRender;
    protected PrimitiveContextDrawCommand? CallPrimitiveContextDrawCommand;
    protected KernelDeviceCreateVertexDeclaration? CallKernelDeviceCreateVertexDeclaration;
    protected KernelEnd? CallKernelEnd;
    protected nint KernelEndFunc;

    protected Primitive(SigScanner sigScanner)
    {
        Scanner = sigScanner;
    }

    public virtual void Initialize()
    {
        CallPrimitiveServerCtor =
            ScanFunc<PrimitiveServerCtor>("48 8d 05 ?? ?? ?? 01 48 c7 41 50 ff ff ff ff 48 89 01 33 c0 48 89 41 08");
        CallPrimitiveServerInitialize = ScanFunc<PrimitiveServerInitialize>(
            "48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 57 41 54 41 55 41 56 41 57 48 83 ec 40 48 8b bc 24 b0 00 00 00 44 8b ea");
        CallPrimitiveServerLoadResource =
            ScanFunc<PrimitiveServerLoadResource>("E8 ?? ?? FF FF 84 C0 75 08 32 C0 48 83 C4 20 5B C3 48 8B 4B 10");
        CallPrimitiveServerBegin =
            ScanFunc<PrimitiveServerBegin>("48 89 5c 24 08 57 48 83 ec 20 33 ff 48 8b d9 48 89 b9 90 00 00 00");
        CallPrimitiveServerSpursSortUnencumbered =
            ScanFunc<PrimitiveServerSpursSortUnencumbered>("40 53 48 83 ec 20 48 8b d9 48 8b 49 30 e8 ?? ?? ?? 00");
        CallPrimitiveServerRender =
            ScanFunc<PrimitiveServerRender>(
                "48 89 5c 24 10 48 89 6c 24 18 48 89 74 24 20 57 48 81 ec a0 00 00 00 48 8b 05 ?? ?? ?? ?? 48 33 c4 48 89 84 24 90 00 00 00 65 48 8b 04 25 58 00 00 00");
        CallPrimitiveContextDrawCommand =
            ScanFunc<PrimitiveContextDrawCommand>(
                "48 89 5C 24 10 48 89 6C 24 18 48 89 7C 24 20 41 56 48 83 EC ?? 41 8B E8");
        CallKernelDeviceCreateVertexDeclaration =
            ScanFunc<KernelDeviceCreateVertexDeclaration>(
                "E8 ?? ?? ?? 00 49 8B 8D 80 01 00 00 48 89 04 0E 49 8B 85 80 01 00 00");

        KernelEndFunc =
            Scanner.ScanText(
                "?? ?? ?? ?? ?? ?? ?? 57 41 54 41 57 48 8D AC ?? ?? ?? FF FF 48 81 EC ?? ?? 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? 00 00 F3 0F");
    }

    public IDisposable Subscribe(Action fn)
    {
        Hook<KernelEnd>? hook = null;

        hook = Hook<KernelEnd>.FromAddress(KernelEndFunc, (thisPtr, unk1) =>
        {
            // ReSharper disable once AccessToModifiedClosure
            var ret = hook!.Original(thisPtr, unk1);

            Begin();
            try
            {
                fn();
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, "Failed to call subscribe function");
            }
            finally
            {
                End();
            }

            return ret;
        });

        hook.Enable();

        return new HookSubscription<KernelEnd>(hook);
    }

    private void Begin()
    {
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerBegin);
        CallPrimitiveServerBegin(PrimitiveServer);
    }

    private void End()
    {
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerSpursSortUnencumbered);
        ArgumentNullException.ThrowIfNull(CallPrimitiveServerRender);
        CallPrimitiveServerSpursSortUnencumbered(PrimitiveServer);
        CallPrimitiveServerRender(PrimitiveServer);
    }

    private T ScanFunc<T>(string signature)
    {
        var addr = Scanner.ScanText(signature);
        PluginLog.Log($"{typeof(T).Name}: ffxiv_dx11.exe+{addr - Scanner.Module.BaseAddress:X}");
        return Marshal.GetDelegateForFunctionPointer<T>(addr);
    }
}