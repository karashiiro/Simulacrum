using System.Numerics;

namespace Simulacrum.Drawing.Common;

public interface IRenderSource
{
    void RenderTo(Span<byte> buffer);

    int PixelSize();

    Vector2 Size();

    // TODO: Does this make sense as an interface method?
    void Sync();
}