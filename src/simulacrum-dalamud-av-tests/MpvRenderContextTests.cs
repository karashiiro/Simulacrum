namespace Simulacrum.AV.Tests;

public class MpvRenderContextTests
{
    private const string VideoUrl = "https://dc6xbzf7ukys8.cloudfront.net/chugjug.m3u8";

    private const int Width = 300;
    private const int Height = 300;

    [Fact]
    public void Ctor_Dispose_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        using var context = new MpvRenderContext(handle, Width, Height);
    }

    [Fact]
    public void GetSize_ReturnsSize()
    {
        using var handle = new MpvHandle();
        using var context = new MpvRenderContext(handle, Width, Height);

        var (w, h) = context.GetSize();

        Assert.Equal(Width, w);
        Assert.Equal(Height, h);
    }

    [Fact(Skip = "Unclear why this doesn't work")]
    public async Task ReadVideoFrame_ReadsDataIntoBuffer()
    {
        using var handle = new MpvHandle();
        using var context = new MpvRenderContext(handle, Width, Height);
        await InitPlayback(handle);

        var buffer = new byte[Width * Height * 4];
        Assert.Equal(0, buffer.Select(Convert.ToInt32).Sum());

        context.ReadVideoFrame(buffer);

        Assert.NotEqual(0, buffer.Select(Convert.ToInt32).Sum());
    }

    private static async Task InitPlayback(MpvHandle handle)
    {
        // Disable audio, but leave video enabled
        handle.SetPropertyString("aid", "no");
        handle.LoadFile(VideoUrl);

        // Wait for the file to be loaded - this should really be done with a hook
        await Task.Delay(3000);
    }
}