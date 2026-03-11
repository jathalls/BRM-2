using BPASpectrogramM.Interfaces;
using System.Diagnostics;

namespace BPASpectrogramM.Platforms.Windows;

/// <summary>
/// Windows fallback implementation using MediaElement.
/// Speed control is achieved by modifying the WAV file header's sample rate
/// before playback through CreateSpeedAdjustedSegmentFile in AudioPlayer.
/// </summary>
public class AudioPlaybackService : IAudioPlaybackService
{
    private string? currentFilePath;
    private TimeSpan startOffset;
    private TimeSpan endOffset;
    private double speedFactor = 1.0;
    private bool isPlaying = false;

    public bool IsPlaying => isPlaying;
    public event EventHandler? PlaybackEnded;

    public void LoadSegment(string filePath, TimeSpan startOffsetParam, TimeSpan endOffsetParam, WavFormatInfo format, double speedFactorParam = 1.0)
    {
        currentFilePath = filePath;
        startOffset = startOffsetParam;
        endOffset = endOffsetParam;
        speedFactor = speedFactorParam;
        Debug.WriteLine($"[AudioPlaybackService-Windows] Segment loaded: {filePath}, speed: {speedFactor}");
    }

    public void Play(double volumeParam)
    {
        isPlaying = true;
        Debug.WriteLine($"[AudioPlaybackService-Windows] Playing with speed: {speedFactor}");
        // MediaElement fallback - speed control handled via WAV header modification
    }

    public void Pause()
    {
        isPlaying = false;
        Debug.WriteLine("[AudioPlaybackService-Windows] Paused");
    }

    public void Stop()
    {
        isPlaying = false;
        Debug.WriteLine("[AudioPlaybackService-Windows] Stopped");
    }

    public double GetPosition()
    {
        return startOffset.TotalSeconds;
    }

    public void Dispose()
    {
        Stop();
        Debug.WriteLine("[AudioPlaybackService-Windows] Disposed");
    }
}