using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Simulacrum.AV;

public static class ModuleInitializer
{
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The \'ModuleInitializer\' attribute should not be used in libraries")]
    public static void Initialize()
    {
        /*
         * Manually pre-load dependencies so that shadow-loading doesn't break our assembly.
         * This invokes AssemblyLoadContext.LoadUnmanagedDll.
         * https://github.com/goatcorp/Dalamud/issues/1238
         * https://learn.microsoft.com/en-us/dotnet/api/System.Runtime.InteropServices.NativeLibrary.Load?view=net-7.0
         */
        var nativeLibraries = new List<nint>();
        LoadLibrary(nativeLibraries, "avutil-57.dll");
        LoadLibrary(nativeLibraries, "swresample-4.dll");
        LoadLibrary(nativeLibraries, "swscale-6.dll");
        LoadLibrary(nativeLibraries, "avcodec-59.dll");
        LoadLibrary(nativeLibraries, "avformat-59.dll");

        var assemblyLoadContext = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
        if (assemblyLoadContext == null) return;
        nativeLibraries.Reverse();
        assemblyLoadContext.Unloading += _ => { nativeLibraries.ForEach(NativeLibrary.Free); };
    }

    private static void LoadLibrary(ICollection<nint> handles, string assemblyName)
    {
        var handle = NativeLibrary.Load(
            ResolvePath(assemblyName),
            Assembly.GetExecutingAssembly(),
            DllImportSearchPath.SafeDirectories);
        handles.Add(handle);
    }

    private static string ResolvePath(string assemblyPath)
    {
        var location = Assembly.GetCallingAssembly().Location;
        var targetLocation = Path.Join(location, "..", assemblyPath);
        return targetLocation;
    }
}