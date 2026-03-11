using BPASpectrogramM.Interfaces;
using System.Diagnostics;

namespace BPASpectrogramM.Platforms.Android;

/// <summary>
/// Android fallback implementation using MediaElement
/// Note: This is a fallback that uses MediaElement. For true sample rate manipulation,
/// consider using Android's SoundPool or writing a custom AudioTrack implementation.
/// </summary>
public class AudioPlaybackService : IAudioPlaybackService
{
    private string? currentFilePath;
    private TimeSpan startOffset;
    private TimeSpan endOffset;
    private double speedFactor = 1.0;
    private double volume = 1.0;
    private bool isPlaying = false;
    
    public bool IsPlaying => isPlaying;
    public event EventHandler? PlaybackEnded;

    public void LoadSegment(string filePath, TimeSpan startOffsetParam, TimeSpan endOffsetParam, WavFormatInfo format, double speedFactor = 1.0)
    {
        currentFilePath = filePath;
        startOffset = startOffsetParam;
        endOffset = endOffsetParam;
        this.speedFactor = speedFactor;
        Debug.WriteLine($"[AudioPlaybackService-Android] Segment loaded: {filePath}, speed: {speedFactor}");
    }

    public void Play(double volumeParam)
    {
        volume = volumeParam;
        isPlaying = true;
        Debug.WriteLine($"[AudioPlaybackService-Android] Playing with speed: {speedFactor}");
        // Platform-specific implementation would go here
        // For now, this is a stub that would need to be implemented with Android AudioTrack
    }

    public void Pause()
    {
        isPlaying = false;
        Debug.WriteLine("[AudioPlaybackService-Android] Paused");
    }

    public void Stop()
    {
        isPlaying = false;
        Debug.WriteLine("[AudioPlaybackService-Android] Stopped");
    }

    public double GetPosition()
    {
        return startOffset.TotalSeconds;
    }

    public void Dispose()
    {
        Stop();
        Debug.WriteLine("[AudioPlaybackService-Android] Disposed");
    }
}