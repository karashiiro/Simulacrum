namespace Simulacrum.AV.Tests;

public class MpvHandleTests
{
    [Fact]
    public void Ctor_Dispose_DoesNotThrow()
    {
        using var handle = new MpvHandle();
    }
}