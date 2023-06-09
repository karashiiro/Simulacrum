namespace Simulacrum.Drawing.Common;

public interface IScreen
{
    /// <summary>
    /// Shows the provided render source on this screen.
    /// </summary>
    /// <param name="source"></param>
    void Show(IRenderSource source);
}