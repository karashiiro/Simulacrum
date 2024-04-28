using Simulacrum.Game;

namespace Simulacrum.Drawing.Common;

public interface IScreen
{
    /// <summary>
    /// Shows the provided render source on this screen. It is the responsibility of
    /// the screen implementation to resize any buffers accordingly.
    /// </summary>
    /// <param name="source"></param>
    void Show(IMediaSource source);

    /// <summary>
    /// Returns the aspect ratio of this screen, with a denominator of 1. If the screen
    /// does not have an aspect ratio, this returns 0.
    /// </summary>
    /// <returns></returns>
    float GetAspectRatio();

    /// <summary>
    /// Returns the location of this screen.
    /// </summary>
    /// <returns></returns>
    Location GetLocation();
}