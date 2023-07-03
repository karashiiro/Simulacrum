using NAudio.Wave;

namespace Simulacrum.Drawing.Common;

public interface IMediaSource
{
    /// <summary>
    /// Renders data into the provided frame buffer.
    /// </summary>
    /// <param name="buffer">The buffer to render into.</param>
    /// <param name="delay">The time the caller should wait before rendering again.</param>
    void RenderTo(Span<byte> buffer, out TimeSpan delay);

    /// <summary>
    /// Retrieves a <see cref="IWaveProvider"/> which can be used to render audio data.
    /// </summary>
    /// <returns>The audio source.</returns>
    IWaveProvider WaveProvider();

    /// <summary>
    /// Creates an observable over new audio data being buffered.
    /// </summary>
    /// <returns></returns>
    IObservable<bool> OnAudioBuffered();

    /// <summary>
    /// Creates an observable over audio playback being resumed.
    /// </summary>
    /// <returns></returns>
    IObservable<bool> OnAudioPlay();

    /// <summary>
    /// Creates an observable over audio playback being paused.
    /// </summary>
    /// <returns></returns>
    IObservable<bool> OnAudioPause();

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