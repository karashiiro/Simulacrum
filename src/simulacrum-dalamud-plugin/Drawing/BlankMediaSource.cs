using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class BlankMediaSource : IMediaSource
{
    public void RenderTo(Span<byte> buffer)
    {
        buffer.Clear();
    }

    public void RenderTo(Span<byte> buffer, out TimeSpan delay)
    {
        RenderTo(buffer);
        delay = TimeSpan.Zero;
    }

    public int PixelSize()
    {
        return 0;
    }

    public IntVector2 Size()
    {
        return IntVector2.Empty;
    }
}