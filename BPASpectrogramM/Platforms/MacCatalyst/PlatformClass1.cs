using BPASpectrogramM.Interfaces;
using System.Diagnostics;
#if __MACCATALYST__ || __IOS__
using AVFoundation;
using Foundation;
#endif

namespace BPASpectrogramM.Platforms.MacCatalyst;

/// <summary>
/// MacCatalyst implementation of audio playback with sample rate manipulation using AVAudioEngine
/// </summary>
public class AudioPlaybackService : IAudioPlaybackService
{
#if __MACCATALYST__ || __IOS__
    private AVAudioEngine? audioEngine;
    private AVAudioPlayerNode? playerNode;
    private AVAudioFile? audioFile;
    private long startFrame = 0;
    private long endFrame = 0;
    private AVAudioUnitTimePitch? timePitchUnit;
#endif
    
    private double speedFactor = 1.0;
    private double currentPositionSeconds = 0;
    private double startOffsetSeconds = 0;
    private double endOffsetSeconds = 0;
    private bool isPlaying = false;
    private System.Timers.Timer? positionTimer;
    private DateTime playbackStartTime;
    private double positionAtStart = 0;
    private AVAudioFormat? scheduledFormat;
    private uint scheduledFrameCount;
    
    public bool IsPlaying => isPlaying;
    public event EventHandler? PlaybackEnded;

    public AudioPlaybackService()
    {
        InitializeAudioEngine();
    }

    private void InitializeAudioEngine()
    {
#if __MACCATALYST__ || __IOS__
        try
        {
            audioEngine = new AVAudioEngine();
            playerNode = new AVAudioPlayerNode();
            timePitchUnit = new AVAudioUnitTimePitch();
            
            audioEngine.AttachNode(playerNode);
            audioEngine.AttachNode(timePitchUnit);
            
            Debug.WriteLine("[AudioPlaybackService-Mac] Audio engine initialized");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Error initializing audio engine: {ex.Message}");
        }
#else
        Debug.WriteLine("[AudioPlaybackService-Mac] Not available on this platform");
#endif
    }

    public void LoadSegment(string filePath, TimeSpan startOffset, TimeSpan endOffset, WavFormatInfo format)
    {
#if __MACCATALYST__ || __IOS__
        try
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Loading segment: {filePath}");
            Debug.WriteLine($"[AudioPlaybackService-Mac] Range: {startOffset} to {endOffset}");
            
            Stop();
            
            startOffsetSeconds = startOffset.TotalSeconds;
            endOffsetSeconds = endOffset.TotalSeconds;
            currentPositionSeconds = startOffsetSeconds;
            
            var url = NSUrl.FromFilename(filePath);
            audioFile = new AVAudioFile(url, out NSError error);
            
            if (error != null)
            {
                Debug.WriteLine($"[AudioPlaybackService-Mac] Error loading audio file: {error.LocalizedDescription}");
                return;
            }
            
            // Calculate frame range for the segment
            long startFrame = (long)(startOffset.TotalSeconds * audioFile.ProcessingFormat.SampleRate);
            long endFrame = (long)(endOffset.TotalSeconds * audioFile.ProcessingFormat.SampleRate);
            long frameCount = endFrame - startFrame;
            
            if (frameCount <= 0)
            {
                Debug.WriteLine("[AudioPlaybackService-Mac] Invalid frame count");
                return;
            }
            
            // Read the segment into a buffer
            audioFile.FramePosition = startFrame;
            startFrame=(long)(startOffset.TotalSeconds * audioFile.ProcessingFormat.SampleRate);
            endFrame=(long)(endOffset.TotalSeconds * audioFile.ProcessingFormat.SampleRate);
            if (endFrame <= startFrame)
            {
                return;
            }
            var localFormat = audioFile.ProcessingFormat;
            var localFrameCount = (uint)(endFrame - startFrame);
            scheduledFormat = localFormat;
            scheduledFrameCount = localFrameCount;
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Error in LoadSegment: {ex.Message}");
        }
#else
        Debug.WriteLine("[AudioPlaybackService-Mac] LoadSegment not available on this platform");
#endif
    }

    public void Play(double speedFactorParam, double volume)
    {
#if __MACCATALYST__ || __IOS__
        try
        {
            if ( playerNode == null || audioEngine == null || timePitchUnit == null)
            {
                Debug.WriteLine("[AudioPlaybackService-Mac] Audio not loaded or engine not initialized");
                return;
            }
            
            Stop();
            
            speedFactor = speedFactorParam;
            Debug.WriteLine($"[AudioPlaybackService-Mac] Playing with speed factor: {speedFactor}");
            
            // Configure the time pitch unit
            // Rate controls playback speed (0.03125 to 32.0)
            // Setting rate < 1.0 slows down playback
            // Pitch is set to 0 to maintain original pitch despite rate change
            //timePitchUnit.Rate = (float)Math.Max(0.03125, Math.Min(32.0, speedFactor));
            //timePitchUnit.Pitch = 0; // Keep original pitch
            var playbackFormat = scheduledFormat ?? audioFile?.ProcessingFormat;
            if (playbackFormat == null || scheduledFrameCount == 0)
            {
                Debug.WriteLine("[AudioPlaybackService-Mac] Missing playback format or frame count.");
                return;
            }
            
            // Connect nodes: player -> timePitch -> mainMixer
            //var format = audioBuffer.Format;
            audioEngine.Connect(playerNode, timePitchUnit, playbackFormat);
            audioEngine.Connect(timePitchUnit, audioEngine.MainMixerNode, playbackFormat);
            
            // Set volume
            playerNode.Volume = (float)volume;
            
            // Start the engine
            
                audioEngine.StartAndReturnError(out NSError error);
                if (error != null)
                {
                    Debug.WriteLine($"[AudioPlaybackService-Mac] Error starting engine: {error.LocalizedDescription}");
                    return;
                }
            

            var frameCoubnt = (uint)(endFrame - startFrame);
            // Schedule the buffer for playback
            playerNode.ScheduleSegment(audioFile,startFrame,scheduledFrameCount,null, () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Debug.WriteLine("[AudioPlaybackService-Mac] Playback completed");
                    isPlaying = false;
                    PlaybackEnded?.Invoke(this, EventArgs.Empty);
                });
            });
            
            // Start playing
            playerNode.Play();
            isPlaying = true;
            playbackStartTime = DateTime.Now;
            positionAtStart = currentPositionSeconds;
            
            // Start position tracking
            StartPositionTracking();
            
            Debug.WriteLine("[AudioPlaybackService-Mac] Playback started");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Error in Play: {ex.Message}");
            isPlaying = false;
        }
#else
        Debug.WriteLine("[AudioPlaybackService-Mac] Play not available on this platform");
#endif
    }

    private void StartPositionTracking()
    {
        positionTimer?.Stop();
        positionTimer?.Dispose();
        
        positionTimer = new System.Timers.Timer(100);
        positionTimer.Elapsed += (sender, e) =>
        {
            if (isPlaying)
            {
                var elapsed = (DateTime.Now - playbackStartTime).TotalSeconds;
                currentPositionSeconds = positionAtStart + (elapsed * speedFactor);
                
                // Check if we've reached the end
                if (currentPositionSeconds >= endOffsetSeconds)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Stop();
                        PlaybackEnded?.Invoke(this, EventArgs.Empty);
                    });
                }
            }
        };
        positionTimer.Start();
    }

    public void Pause()
    {
        try
        {
            playerNode?.Pause();
            isPlaying = false;
            positionTimer?.Stop();
            Debug.WriteLine("[AudioPlaybackService-Mac] Playback paused");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Error pausing: {ex.Message}");
        }
    }

    public void Stop()
    {
        try
        {
            isPlaying = false;
            positionTimer?.Stop();
            playerNode?.Stop();
            currentPositionSeconds = startOffsetSeconds;
            Debug.WriteLine("[AudioPlaybackService-Mac] Playback stopped");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Error stopping: {ex.Message}");
        }
    }

    public double GetPosition()
    {
        return currentPositionSeconds;
    }

    public void Dispose()
    {
        try
        {
            Stop();
            positionTimer?.Dispose();
            positionTimer = null;
            
            audioEngine?.Stop();
            audioEngine?.Dispose();
            audioEngine = null;
            
            playerNode?.Dispose();
            playerNode = null;
            
            timePitchUnit?.Dispose();
            timePitchUnit = null;
            
            audioFile?.Dispose();
            audioFile = null;
            
            
            
            Debug.WriteLine("[AudioPlaybackService-Mac] Disposed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Error disposing: {ex.Message}");
        }
    }
}