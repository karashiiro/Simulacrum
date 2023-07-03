namespace Simulacrum.Drawing.Common;

public interface IMediaSource
{
    /// <summary>
    /// Renders data into the provided frame buffer.
    /// </summary>
    /// <param name="buffer">The buffer to render into.</param>
    void RenderTo(Span<byte> buffer);

    /// <summary>
    /// Renders data into the provided frame buffer.
    /// </summary>
    /// <param name="buffer">The buffer to render into.</param>
    /// <param name="delay">The time the caller should wait before rendering again.</param>
    void RenderTo(Span<byte> buffer, out TimeSpan delay);

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