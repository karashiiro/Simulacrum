using FFMpegCore;
using FFMpegCore.Pipes;

namespace Simulacrum.Drawing;

public class VideoLoader
{
    public static Stream LoadFrameFromFile(string path)
    {
        var ms = new MemoryStream();
        var sink = new StreamPipeSink(ms);
        FFMpegArguments
            .FromFileInput(path)
            .OutputToPipe(sink, options => options
                .WithVideoCodec("libx264")
                .ForceFormat("rawvideo")
                .ForcePixelFormat("bgra"))
            .ProcessSynchronously();
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}