using Simulacrum.Drawing.Common;

namespace Simulacrum.Drawing;

public class BlankRenderSource : IRenderSource
{
    public void RenderTo(Span<byte> buffer)
    {
        buffer.Clear();
    }

    public int PixelSize()
    {
        return 0;
    }

    public IntVector2 Size()
    {
        return new IntVector2(0, 0);
    }
}