namespace Simulacrum.AV.Tests;

public class MpvHandleTests
{
    private const string VideoUrl = "https://dc6xbzf7ukys8.cloudfront.net/chugjug.m3u8";

    [Fact]
    public void Ctor_Dispose_DoesNotThrow()
    {
        using var handle = new MpvHandle();
    }

    [Fact]
    public void SetOptionString_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        handle.SetOptionString("keep-open", "always");
    }

    [Fact]
    public void LoadFile_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        handle.LoadFile(VideoUrl);
    }

    [Fact(Skip = "Currently unclear how this works")]
    public void Seek_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        handle.LoadFile(VideoUrl);
        handle.Seek(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void Play_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        handle.Play();
    }

    [Fact]
    public void Pause_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        handle.Pause();
    }
}