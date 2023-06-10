namespace Simulacrum.Drawing.Common;

public interface IScreen
{
    /// <summary>
    /// Shows the provided render source on this screen. It is the responsibility of
    /// the screen implementation to resize any buffers accordingly.
    /// </summary>
    /// <param name="source"></param>
    void Show(IRenderSource source);
}