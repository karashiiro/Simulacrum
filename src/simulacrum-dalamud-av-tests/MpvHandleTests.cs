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

    [Fact]
    public void Seek_WithoutFile_ThrowsMpvException()
    {
        using var handle = new MpvHandle();
        Assert.Throws<MpvException>(() => handle.Seek(TimeSpan.FromSeconds(15)));
    }

    [Fact]
    public async Task Seek_WithFile_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        await InitPlayback(handle);
        handle.Seek(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public async Task Seek_WithFile_CanSeekBackwards()
    {
        using var handle = new MpvHandle();
        await InitPlayback(handle);
        handle.Seek(TimeSpan.FromSeconds(15));
        handle.Seek(TimeSpan.FromSeconds(0));
    }

    [Fact]
    public void Play_WithoutFile_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        handle.Play();
    }

    [Fact]
    public async Task Play_WithFile_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        await InitPlayback(handle);
        handle.Play();
    }

    [Fact]
    public void Pause_WithoutFile_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        handle.Pause();
    }

    [Fact]
    public async Task Pause_WithFile_DoesNotThrow()
    {
        using var handle = new MpvHandle();
        await InitPlayback(handle);
        handle.Pause();
    }

    private static async Task InitPlayback(MpvHandle handle)
    {
        // Disable video, but leave audio enabled because at least one stream must be active for mpv_seek to succeed
        handle.SetPropertyString("vid", "no");
        handle.LoadFile(VideoUrl);

        // Wait for the file to be loaded - this should really be done with a hook
        await Task.Delay(1000);
    }
}