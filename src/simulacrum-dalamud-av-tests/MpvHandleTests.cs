namespace Simulacrum.AV.Tests;

public class MpvHandleTests
{
    [Fact]
    public void Ctor_Dispose_DoesNotThrow()
    {
        using var handle = new MpvHandle();
    }

    [Fact]
    public void SetOptionString_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        handle.SetOptionString("keep-open\0"u8, "always\0"u8);
    }

    [Fact]
    public void LoadFile_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        handle.LoadFile("https://dc6xbzf7ukys8.cloudfront.net/chugjug.m3u8");
    }
}