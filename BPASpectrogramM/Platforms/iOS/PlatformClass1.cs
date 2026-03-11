using BPASpectrogramM.Interfaces;
using System.Diagnostics;
#if __MACCATALYST__ || __IOS__
using AVFoundation;
using Foundation;
#endif

namespace BPASpectrogramM.Platforms.iOS;

/// <summary>
/// MacCatalyst implementation of audio playback with sample rate manipulation using AVAudioEngine.
/// Uses AVAudioFile segment scheduling (no AVAudioPCMBuffer dependency).
/// </summary>
public class AudioPlaybackService : IAudioPlaybackService
{
#if __MACCATALYST__ || __IOS__
    private AVAudioEngine? audioEngine;
    private AVAudioPlayerNode? playerNode;
    private AVAudioFile? audioFile;
    private AVAudioPcmBuffer? audioBuffer;
    private AVAudioUnitTimePitch? timePitchUnit;
    private AVAudioFormat? segmentFormat;
#endif

    private double speedFactor = 1.0;
    private double currentPositionSeconds = 0;
    private double startOffsetSeconds = 0;
    private double endOffsetSeconds = 0;
    private bool isPlaying = false;
    private System.Timers.Timer? positionTimer;
    private DateTime playbackStartTime;
    private double positionAtStart = 0;

    // Segment frame range for ScheduleSegment
    private long segmentStartFrame = 0;
    private uint segmentFrameCount = 0;

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

            if (error != null || audioFile == null)
            {
                Debug.WriteLine($"[AudioPlaybackService-Mac] Error loading audio file: {error?.LocalizedDescription}");
                return;
            }

            var sampleRate = audioFile.ProcessingFormat.SampleRate;
            segmentStartFrame = (long)(startOffset.TotalSeconds * sampleRate);
            long endFrame = (long)(endOffset.TotalSeconds * sampleRate);
            long frameDelta = endFrame - segmentStartFrame;

            if (frameDelta <= 0)
            {
                Debug.WriteLine("[AudioPlaybackService-Mac] Invalid frame range");
                segmentFrameCount = 0;
                return;
            }

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
            audioFile.FramePosition = segmentStartFrame;
            audioBuffer = new AVAudioPcmBuffer(originalFormat, (uint)frameDelta);

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

            segmentFrameCount = (uint)frameDelta;
            segmentFormat = adjustedFormat;

            Debug.WriteLine($"[AudioPlaybackService-Mac] Segment prepared - frames: {segmentFrameCount}, adjusted sample rate for speed control: {adjustedSampleRate:F0} Hz");
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
            if (audioBuffer == null || playerNode == null || audioEngine == null)
            {
                Debug.WriteLine("[AudioPlaybackService-Mac] Audio not loaded or engine not initialized");
                return;
            }

            if (segmentFrameCount == 0)
            {
                Debug.WriteLine("[AudioPlaybackService-Mac] Segment frame count is zero");
                return;
            }

            Stop();

            Debug.WriteLine($"[AudioPlaybackService-Mac] Playing with speed factor: {speedFactor}");

            // Connect nodes directly (no time pitch unit for sample rate-based speed control)
            var formatToUse = segmentFormat ?? audioBuffer.Format;
            audioEngine.Connect(playerNode, audioEngine.MainMixerNode, formatToUse);

            // Set volume
            playerNode.Volume = (float)volume;

            // Start engine
            if (!audioEngine.Running)
            {
                audioEngine.StartAndReturnError(out NSError error);
                if (error != null)
                {
                    Debug.WriteLine($"[AudioPlaybackService-Mac] Error starting engine: {error.LocalizedDescription}");
                    return;
                }
            }

            // Schedule the buffer for playback with adjusted sample rate
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
#if __MACCATALYST__ || __IOS__
            playerNode?.Pause();
#endif
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
#if __MACCATALYST__ || __IOS__
            playerNode?.Stop();
#endif
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

#if __MACCATALYST__ || __IOS__
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

            segmentFormat?.Dispose();
            segmentFormat = null;
#endif

            Debug.WriteLine("[AudioPlaybackService-Mac] Disposed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlaybackService-Mac] Error disposing: {ex.Message}");
        }
    }
}