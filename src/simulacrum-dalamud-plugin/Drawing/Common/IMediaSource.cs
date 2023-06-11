namespace Simulacrum.Drawing.Common;

public interface IMediaSource
{
    /// <summary>
    /// Renders data into the provided frame buffer.
    /// </summary>
    /// <param name="buffer">The buffer to render into.</param>
    void RenderTo(Span<byte> buffer);

    /// <summary>
    /// The pixel width of the input source.
    /// </summary>
    /// <returns></returns>
    int PixelSize();

    /// <summary>
    /// The dimensions of the input source, in pixels.
    /// </summary>
    /// <returns></returns>
    IntVector2 Size();
}