namespace BPASpectrogramM.Interfaces;

/// <summary>
/// Interface for platform-specific audio playback with segment support and speed control.
/// </summary>
public interface IAudioPlaybackService : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether audio is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Event raised when playback of the current segment has ended.
    /// </summary>
    event EventHandler? PlaybackEnded;

    /// <summary>
    /// Loads an audio segment from the specified file.
    /// </summary>
    /// <param name="filePath">The path to the audio file.</param>
    /// <param name="startOffset">The start time of the segment.</param>
    /// <param name="endOffset">The end time of the segment.</param>
    /// <param name="format">The audio format information.</param>
    void LoadSegment(string filePath, TimeSpan startOffset, TimeSpan endOffset, WavFormatInfo format);

    /// <summary>
    /// Starts playback with the specified speed factor and volume.
    /// </summary>
    /// <param name="speedFactor">The playback speed factor (1.0 = normal speed).</param>
    /// <param name="volume">The volume level (0.0 to 1.0).</param>
    void Play(double speedFactor, double volume);

    /// <summary>
    /// Pauses the current playback.
    /// </summary>
    void Pause();

    /// <summary>
    /// Stops the current playback and resets the position.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets the current playback position in seconds.
    /// </summary>
    /// <returns>The current position in seconds.</returns>
    double GetPosition();
}
