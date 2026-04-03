namespace BPASpectrogramM;

/// <summary>
/// Cross-platform interface for audio playback with sample rate manipulation
/// </summary>
public interface IAudioPlaybackService
{
    /// <summary>
    /// Load an audio file segment for playback
    /// </summary>
    /// <param name="filePath">Path to the audio file</param>
    /// <param name="startOffset">Start time offset</param>
    /// <param name="endOffset">End time offset</param>
    /// <param name="format">Audio format information</param>
    void LoadSegment(string filePath, TimeSpan startOffset, TimeSpan endOffset, WavFormatInfo format);
    
    /// <summary>
    /// Play the loaded audio segment with specified speed factor
    /// </summary>
    /// <param name="speedFactor">Speed factor (e.g., 0.1 for 1/10 speed)</param>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    void Play(double speedFactor, double volume);
    
    /// <summary>
    /// Pause playback
    /// </summary>
    void Pause();
    
    /// <summary>
    /// Stop playback
    /// </summary>
    void Stop();
    
    /// <summary>
    /// Get the current playback position in seconds
    /// </summary>
    double GetPosition();
    
    /// <summary>
    /// Check if audio is currently playing
    /// </summary>
    bool IsPlaying { get; }
    
    /// <summary>
    /// Event raised when playback reaches the end
    /// </summary>
    event EventHandler? PlaybackEnded;
    
    /// <summary>
    /// Clean up resources
    /// </summary>
    void Dispose();
}
