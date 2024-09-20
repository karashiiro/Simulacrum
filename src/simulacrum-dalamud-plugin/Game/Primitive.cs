using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using static Simulacrum.Game.GameFunctions;

namespace Simulacrum.Game;

public abstract class Primitive(ISigScanner sigScanner, IGameInteropProvider gameInteropProvider, IPluginLog log)
{
    protected readonly ISigScanner Scanner = sigScanner;
    protected readonly IGameInteropProvider GameInteropProvider = gameInteropProvider;
    protected readonly IPluginLog Log = log;

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
    protected EnvironmentManagerUpdate? CallEnvironmentManagerUpdate;
    protected nint EnvironmentManagerUpdate;

    public virtual void Initialize()
    {
        CallPrimitiveServerCtor =
            ScanFunc<PrimitiveServerCtor>("E8 ?? ?? ?? ?? 48 89 47 ?? 48 85 C0 74 ?? 48 8D 4D");
        CallPrimitiveServerInitialize = ScanFunc<PrimitiveServerInitialize>(
            "E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 45 33 C0 33 D2 B9 C8 00 00 00");
        CallPrimitiveServerLoadResource =
            ScanFunc<PrimitiveServerLoadResource>("E8 ?? ?? ?? ?? 84 C0 74 ?? 48 8B 4B ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 84 C0");
        CallPrimitiveServerBegin =
            ScanFunc<PrimitiveServerBegin>("48 89 5C 24 ?? 57 48 83 EC 20 33 FF 48 8B D9 48 89 B9 ?? ?? ?? ?? 45 33 C0");
        CallPrimitiveServerSpursSortUnencumbered =
            ScanFunc<PrimitiveServerSpursSortUnencumbered>("40 53 48 83 ec 20 48 8b d9 48 8b 49 30 e8 ?? ?? ?? 00");
        CallPrimitiveServerRender =
            ScanFunc<PrimitiveServerRender>(
                "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 81 EC A0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 65 48 8B 04 25");
        CallPrimitiveContextDrawCommand =
            ScanFunc<PrimitiveContextDrawCommand>(
                "E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0 0F 84 ?? ?? ?? ?? F3 0F 10 4B");
        CallKernelDeviceCreateVertexDeclaration =
            ScanFunc<KernelDeviceCreateVertexDeclaration>(
                "E8 ?? ?? ?? 00 49 8B 8D 80 01 00 00 48 89 04 0E 49 8B 85 80 01 00 00");

        EnvironmentManagerUpdate =
            Scanner.ScanText(
                "?? ?? ?? ?? ?? ?? ?? 57 41 54 41 57 48 8D AC ?? ?? ?? FF FF 48 81 EC ?? ?? 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? 00 00 F3 0F");
    }

    public IDisposable Subscribe(Action fn)
    {
        Hook<EnvironmentManagerUpdate>? hook = null;

        hook = GameInteropProvider.HookFromAddress<EnvironmentManagerUpdate>(EnvironmentManagerUpdate, (thisPtr, unk1) =>
        {
            Log.Info("Hit");
            // ReSharper disable once AccessToModifiedClosure
            var ret = hook!.Original(thisPtr, unk1);

            Begin();
            try
            {
                fn();
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to call subscribe function");
            }
            finally
            {
                End();
            }

            return ret;
        });

        hook.Enable();

        return new HookSubscription<EnvironmentManagerUpdate>(hook);
    }

    public IPrimitiveContext GetContext()
    {
        ArgumentNullException.ThrowIfNull(CallPrimitiveContextDrawCommand);
        return new UnmanagedPrimitiveContext(PrimitiveContext, CallPrimitiveContextDrawCommand);
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
        Log.Info($"{typeof(T).Name}: ffxiv_dx11.exe+{addr - Scanner.Module.BaseAddress:X}");
        return Marshal.GetDelegateForFunctionPointer<T>(addr);
    }
}