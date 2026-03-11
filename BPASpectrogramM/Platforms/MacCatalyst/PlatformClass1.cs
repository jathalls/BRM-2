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
    private AVAudioPcmBuffer? audioBuffer;
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

    public void LoadSegment(string filePath, TimeSpan startOffset, TimeSpan endOffset, WavFormatInfo format, double speedFactor = 1.0)
    {
#if __MACCATALYST__ || __IOS__
        try
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Loading segment: {filePath}");
            Debug.WriteLine($"[AudioPlaybackService-Mac] Range: {startOffset} to {endOffset}");
            Debug.WriteLine($"[AudioPlaybackService-Mac] Speed factor: {speedFactor}");

            Stop();

            this.speedFactor = speedFactor;
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

            this.startFrame = startFrame;
            this.endFrame = endFrame;

            // Create format with adjusted sample rate for speed control
            var originalFormat = audioFile.ProcessingFormat;
            var adjustedSampleRate = (double)originalFormat.SampleRate * speedFactor;

            // Create a new format with adjusted sample rate
            var adjustedFormat = new AVAudioFormat(
                originalFormat.CommonFormat,
                (uint)adjustedSampleRate,
                (uint)originalFormat.ChannelCount,
                false
            );

            Debug.WriteLine($"[AudioPlaybackService-Mac] Original sample rate: {originalFormat.SampleRate}, Adjusted: {adjustedSampleRate:F0}");

            // Read audio data from original file
            audioFile.FramePosition = startFrame;
            audioBuffer = new AVAudioPcmBuffer(originalFormat, (uint)frameCount);

            if (audioBuffer == null)
            {
                Debug.WriteLine("[AudioPlaybackService-Mac] Failed to create audio buffer");
                return;
            }

            NSError? readError = null;
            if (!audioFile.ReadIntoBuffer(audioBuffer, out readError))
            {
                Debug.WriteLine($"[AudioPlaybackService-Mac] Error reading audio data: {readError?.LocalizedDescription}");
                audioBuffer = null;
                return;
            }

            // Store the adjusted format for playback
            scheduledFormat = adjustedFormat;
            scheduledFrameCount = (uint)frameCount;

            Debug.WriteLine($"[AudioPlaybackService-Mac] Segment loaded - frames: {frameCount}, adjusted sample rate for speed control: {adjustedSampleRate:F0} Hz");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Error in LoadSegment: {ex.Message}");
            audioBuffer = null;
        }
#else
        Debug.WriteLine("[AudioPlaybackService-Mac] LoadSegment not available on this platform");
#endif
    }

    public void Play(double volume)
    {
#if __MACCATALYST__ || __IOS__
        try
        {
            if (playerNode == null || audioEngine == null || audioBuffer == null)
            {
                Debug.WriteLine("[AudioPlaybackService-Mac] Audio not loaded or engine not initialized");
                return;
            }

            Stop();

            Debug.WriteLine($"[AudioPlaybackService-Mac] Playing with speed factor: {speedFactor}");

            var playbackFormat = scheduledFormat ?? audioBuffer.Format;
            if (playbackFormat == null || scheduledFrameCount == 0)
            {
                Debug.WriteLine("[AudioPlaybackService-Mac] Missing playback format or frame count.");
                return;
            }

            // Connect nodes: player -> mainMixer
            audioEngine.Connect(playerNode, audioEngine.MainMixerNode, playbackFormat);

            // Set volume
            playerNode.Volume = (float)volume;

            // Start the engine
            audioEngine.StartAndReturnError(out NSError error);
            if (error != null)
            {
                Debug.WriteLine($"[AudioPlaybackService-Mac] Error starting engine: {error.LocalizedDescription}");
                return;
            }

            // Schedule the buffer for playback with the adjusted sample rate format
            // The audio data is played using the format's sample rate, achieving speed control
            playerNode.ScheduleBuffer(audioBuffer, () =>
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

            audioBuffer?.Dispose();
            audioBuffer = null;

            Debug.WriteLine("[AudioPlaybackService-Mac] Disposed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Error disposing: {ex.Message}");
        }
    }
}