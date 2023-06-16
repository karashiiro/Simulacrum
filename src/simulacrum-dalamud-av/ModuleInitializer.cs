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
        LoadLibrary(nativeLibraries, "OpenCL.dll");
        LoadLibrary(nativeLibraries, "avutil-57.dll");
        LoadLibrary(nativeLibraries, "soxr.dll");
        LoadLibrary(nativeLibraries, "swresample-4.dll");
        LoadLibrary(nativeLibraries, "swscale-6.dll");
        LoadLibrary(nativeLibraries, "aom.dll");
        LoadLibrary(nativeLibraries, "dav1d.dll");
        LoadLibrary(nativeLibraries, "iconv-2.dll");
        LoadLibrary(nativeLibraries, "ilbc.dll");
        LoadLibrary(nativeLibraries, "liblzma.dll");
        LoadLibrary(nativeLibraries, "libmp3lame.dll");
        LoadLibrary(nativeLibraries, "libsharpyuv.dll");
        LoadLibrary(nativeLibraries, "libwebp.dll");
        LoadLibrary(nativeLibraries, "libwebpmux.dll");
        LoadLibrary(nativeLibraries, "openh264-6.dll");
        LoadLibrary(nativeLibraries, "openjp2.dll");
        LoadLibrary(nativeLibraries, "opus.dll");
        LoadLibrary(nativeLibraries, "snappy.dll");
        LoadLibrary(nativeLibraries, "speex-1.dll");
        LoadLibrary(nativeLibraries, "theoradec.dll");
        LoadLibrary(nativeLibraries, "ogg.dll");
        LoadLibrary(nativeLibraries, "theoraenc.dll");
        LoadLibrary(nativeLibraries, "vorbis.dll");
        LoadLibrary(nativeLibraries, "vorbisenc.dll");
        LoadLibrary(nativeLibraries, "zlibd1.dll");
        LoadLibrary(nativeLibraries, "avcodec-59.dll");
        LoadLibrary(nativeLibraries, "bz2d.dll");
        LoadLibrary(nativeLibraries, "libxml2.dll");
        LoadLibrary(nativeLibraries, "modplug.dll");
        LoadLibrary(nativeLibraries, "mpg123.dll");
        LoadLibrary(nativeLibraries, "vorbisfile.dll");
        LoadLibrary(nativeLibraries, "openmpt.dll");
        if (File.Exists(ResolvePath("libcrypto-3-x64.dll")))
            LoadLibrary(nativeLibraries, "libcrypto-3-x64.dll");
        LoadLibrary(nativeLibraries, "srt.dll");
        LoadLibrary(nativeLibraries, "pthreadVC3d.dll");
        LoadLibrary(nativeLibraries, "ssh.dll");
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