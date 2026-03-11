using BPASpectrogramM.Interfaces;
using CommunityToolkit.Maui.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BPASpectrogramM.Views;

public partial class AudioPlayer : ContentView, INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event EventHandler<FileEventArgs>? PlayBackUpdated;
    protected void OnPlayBackUpdated(FileEventArgs e)
    {
        PlayBackUpdated?.Invoke(this, e);
    }

    private string currentFile = string.Empty;
    private string currentSegmentFile = string.Empty; // Path to temporary segment file
    private TimeSpan startOffset = TimeSpan.Zero;
    private TimeSpan endOffset = TimeSpan.Zero;
    private WavFormatInfo fileFormat = new WavFormatInfo();

    private string _currentFrequency;
    public string CurrentFrequency
    {
        get { return HeterodyneFrequencykHz.ToString("F1") + " kHz"; }
    }

    private double _currentPosition = 0.0;
    public double CurrentPosition
    {
        get { return _currentPosition; }
        set
        {
            if (_currentPosition != value)
            {
                _currentPosition = value;
                OnPropertyChanged();
            }
        }
    }

    private double _volume = 1.0;
    public double Volume
    {
        get { return _volume; }
        set
        {
            _volume = Math.Clamp(value, 0.0, 1.0);
            OnPropertyChanged();
        }
    }

    private double heterodyneFrequencykHz = 50.0;
    public double HeterodyneFrequencykHz
    {
        get { return heterodyneFrequencykHz; }
        set
        {
            heterodyneFrequencykHz = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentFrequency));
        }
    }

    private IAudioPlaybackService? audioPlaybackService;
    private System.Timers.Timer? positionTimer;
    private float currentPosition = 0.0f;
    private float lastPosition = 0.0f;
    private bool isPlaying = false;
    private double speedFactor = 1.0;
    private bool useNativeAudioEngine = true;

    public AudioPlayer()
    {
        InitializeComponent();
        BindingContext = this;
        InitializeAudioServices();
    }

    private partial IAudioPlaybackService? CreatePlatformAudioPlaybackService();

    private void InitializeAudioServices()
    {
        try
        {
            // Resolve platform implementation via per-platform partial class.
            audioPlaybackService = CreatePlatformAudioPlaybackService();
            if (audioPlaybackService != null)
            {
                audioPlaybackService.PlaybackEnded += OnAudioPlaybackEnded;
                Debug.WriteLine("[AudioPlayer] Platform-specific audio service initialized");
                useNativeAudioEngine = true; // Enable native audio engine for speed control
            }
            else
            {
                useNativeAudioEngine = false;
                Debug.WriteLine("[AudioPlayer] No platform audio service; using MediaElement fallback");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Error initializing audio services: {ex.Message}");
            useNativeAudioEngine = false;
        }
    }
    
    private void OnAudioPlaybackEnded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var selected = cmbSpeed.SelectedItem?.ToString() ?? "";
            if (selected.Contains("heterodyne", StringComparison.CurrentCultureIgnoreCase))
            {
                Debug.WriteLine("[AudioPlayer] Looping playback");
                // Restart for looping
                PlayAudioAsync();
            }
            else
            {
                StopPlayback();
            }
        });
    }

    public void LoadSegment(string file, TimeSpan startOffsetTimeSpan, TimeSpan endOffsetTimeSpan)
    {
        try
        {
            Debug.WriteLine($"[AudioPlayer] Loading Audio Segment: {file} from {startOffsetTimeSpan} to {endOffsetTimeSpan}");

            if (!File.Exists(file))
            {
                Debug.WriteLine($"[AudioPlayer] File not found: {file}");
                return;
            }

            currentFile = file;
            startOffset = startOffsetTimeSpan;
            endOffset = endOffsetTimeSpan;

            // Read format info from file
            using (var reader = new AudioFileReaderM(file))
            {
                if (!reader.IsValid)
                {
                    Debug.WriteLine("[AudioPlayer] Failed to read audio file format");
                    return;
                }

                fileFormat = new WavFormatInfo
                {
                    SampleRate = reader.SampleRate,
                    ChannelCount = reader.Channels,
                    BitsPerSample = reader.BitsPerSample
                };

                Debug.WriteLine($"[AudioPlayer] Audio Format - Sample Rate: {fileFormat.SampleRate}, Channels: {fileFormat.ChannelCount}, Bits: {fileFormat.BitsPerSample}");
            }

            // Create temporary segment file for playback
            try
            {
                currentSegmentFile = CreateSegmentFile(file, startOffsetTimeSpan, endOffsetTimeSpan);

                // Load into MediaElement
                if (mediaElement != null)
                {
                    mediaElement.Source = MediaSource.FromFile(currentSegmentFile);
                    Debug.WriteLine($"[AudioPlayer] Audio segment loaded into MediaElement from: {currentSegmentFile}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayer] Error creating segment file: {ex.Message}");
                // Fallback to loading the full file
                if (mediaElement != null)
                {
                    mediaElement.Source = MediaSource.FromFile(file);
                    Debug.WriteLine("[AudioPlayer] Fallback: loaded full file into MediaElement");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Error loading segment: {ex.Message}");
        }
    }

    private string CreateSegmentFile(string sourceFile, TimeSpan startOffset, TimeSpan endOffset)
    {
        try
        {
            var tempDir = Path.Combine(FileSystem.CacheDirectory, "audio_segments");
            Directory.CreateDirectory(tempDir);

            var segmentFile = Path.Combine(tempDir, $"segment_{Guid.NewGuid()}.wav");

            Debug.WriteLine($"[AudioPlayer] Creating segment file: {segmentFile}");
            Debug.WriteLine($"[AudioPlayer] Start: {startOffset.TotalSeconds}s, End: {endOffset.TotalSeconds}s");

            // Copy segment of audio file
            using (var sourceReader = new AudioFileReaderM(sourceFile))
            {
                if (!sourceReader.IsValid)
                {
                    throw new InvalidOperationException("Source file is not a valid WAV file");
                }

                // Calculate byte positions
                int bytesPerSample = sourceReader.BitsPerSample / 8;
                long startByte = (long)(startOffset.TotalSeconds * sourceReader.SampleRate * sourceReader.Channels * bytesPerSample);
                long endByte = (long)(endOffset.TotalSeconds * sourceReader.SampleRate * sourceReader.Channels * bytesPerSample);
                long segmentSize = endByte - startByte;

                using (var source = File.OpenRead(sourceFile))
                using (var dest = File.Create(segmentFile))
                {
                    // Copy WAV header (first 44 bytes typically)
                    source.Seek(0, SeekOrigin.Begin);
                    byte[] header = new byte[44];
                    source.Read(header, 0, 44);
                    dest.Write(header, 0, 44);

                    // Update data chunk size in header (bytes 40-43)
                    byte[] sizeBytes = BitConverter.GetBytes((uint)segmentSize);
                    dest.Seek(40, SeekOrigin.Begin);
                    dest.Write(sizeBytes, 0, 4);

                    // Copy audio data
                    source.Seek(44 + startByte, SeekOrigin.Begin);
                    dest.Seek(44, SeekOrigin.Begin);

                    byte[] buffer = new byte[65536];
                    long bytesRemaining = segmentSize;

                    while (bytesRemaining > 0)
                    {
                        int toRead = (int)Math.Min(buffer.Length, bytesRemaining);
                        int read = source.Read(buffer, 0, toRead);
                        if (read == 0) break;

                        dest.Write(buffer, 0, read);
                        bytesRemaining -= read;
                    }
                }
            }

            Debug.WriteLine($"[AudioPlayer] Segment file created successfully: {segmentFile}");
            return segmentFile;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Error creating segment file: {ex.Message}");
            throw;
        }
    }

    private async void btnPlay_Clicked(object sender, EventArgs e)
    {
        btnPlay.IsEnabled = false;
        await PlayAudioAsync();
        btnPlay.IsEnabled = true;
    }

     private async Task PlayAudioAsync()
    {
        if (string.IsNullOrEmpty(currentFile))
        {
            Debug.WriteLine("[AudioPlayer] File is not available");
            return;
        }

        try
        {
            isPlaying = true;
            speedFactor = GetSpeedFactor();
            var selected = cmbSpeed.SelectedItem?.ToString() ?? "";
            bool isHeterodyneMode = selected.Contains("heterodyne", StringComparison.CurrentCultureIgnoreCase);

            Debug.WriteLine($"[AudioPlayer] Playing with speed factor: {speedFactor}, Heterodyne: {isHeterodyneMode}");

            // Use heterodyne mode with MediaElement
            if (isHeterodyneMode)
            {
                await PlayWithMediaElement();
            }
            // Use platform-specific audio service for speed control (sample rate manipulation)
            else if (audioPlaybackService != null && useNativeAudioEngine)
            {
                PlayWithNativeAudioEngine();
            }
            // Fallback to MediaElement (no speed control support on this platform)
            else
            {
                if (speedFactor != 1.0)
                {
                    Debug.WriteLine("[AudioPlayer] WARNING: Speed control not supported on this platform. Playing at normal speed.");
                }
                await PlayWithMediaElement();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Playback error: {ex.Message}");
            isPlaying = false;
        }
    }

    private void PlayWithNativeAudioEngine()
    {
        try
        {
            Debug.WriteLine($"[AudioPlayer] Using native audio engine for speed: {speedFactor}");

            // Reload segment with speed factor for sample rate-based speed control
            audioPlaybackService?.LoadSegment(currentFile, startOffset, endOffset, fileFormat, speedFactor);

            // Play with adjusted sample rate (speed factor is now part of the loaded segment)
            audioPlaybackService?.Play(Volume);

            // Start position tracking timer
            positionTimer?.Stop();
            positionTimer?.Dispose();
            positionTimer = new System.Timers.Timer(100);
            positionTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    if (audioPlaybackService != null && audioPlaybackService.IsPlaying)
                    {
                        currentPosition = (float)audioPlaybackService.GetPosition();
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CurrentPosition = currentPosition;
                            OnPlayBackUpdated(new FileEventArgs(currentFile));
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AudioPlayer] Timer error: {ex.Message}");
                }
            };
            positionTimer.Start();

            Debug.WriteLine("[AudioPlayer] Native audio playback started");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Native audio error: {ex.Message}");
            isPlaying = false;
        }
    }

    private async Task PlayWithMediaElement()
    {
        if (mediaElement == null)
        {
            Debug.WriteLine("[AudioPlayer] MediaElement not available");
            return;
        }

        try
        {
            Debug.WriteLine($"[AudioPlayer] Using MediaElement (no speed control support)");

            // MediaElement does not support arbitrary sample rates
            // Always play the original segment at normal speed
            string segmentToPlay = currentSegmentFile;

            mediaElement.Volume = Volume;

            // Play the media
            try
            {
                mediaElement.Source = MediaSource.FromFile(segmentToPlay);
                mediaElement.Play();
                Debug.WriteLine("[AudioPlayer] Play command sent to MediaElement");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayer] Error calling Play(): {ex.Message}");
                isPlaying = false;
                return;
            }

            currentPosition = 0.0f; // Segment file starts at 0
            lastPosition = currentPosition;
            CurrentPosition = (float)startOffset.TotalSeconds; // Set to start of segment in original timeline

            // Start position tracking timer
            positionTimer?.Stop();
            positionTimer?.Dispose();
            positionTimer = new System.Timers.Timer(50); // More frequent updates

            int noMovementCount = 0;

            positionTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    if (isPlaying && mediaElement != null)
                    {
                        currentPosition = (float)mediaElement.Position.TotalSeconds;
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            // Adjust position to reflect original file timeline
                            CurrentPosition = (float)startOffset.TotalSeconds + currentPosition;
                            OnPlayBackUpdated(new FileEventArgs(currentFile));
                        });

                        // Check if we've reached the end of the segment (now the entire file)
                        var segmentDuration = endOffset - startOffset;
                        if (currentPosition >= segmentDuration.TotalSeconds)
                        {
                            var selected = cmbSpeed.SelectedItem?.ToString() ?? "";
                            if (selected.Contains("heterodyne", StringComparison.CurrentCultureIgnoreCase))
                            {
                                Debug.WriteLine("[AudioPlayer] Looping heterodyne playback");
                                // Restart looping
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    mediaElement.Stop();
                                    mediaElement.Play();
                                    currentPosition = 0.0f;
                                    lastPosition = 0.0f;
                                });
                            }
                            else
                            {
                                // Stop playback when we reach the end
                                MainThread.BeginInvokeOnMainThread(() => StopPlayback());
                            }
                        }

                        // Check if media has stopped by comparing position (no movement)
                        if (Math.Abs(currentPosition - lastPosition) < 0.001f && isPlaying)
                        {
                            noMovementCount++;
                            if (noMovementCount > 5) // 5 consecutive checks with no movement
                            {
                                Debug.WriteLine($"[AudioPlayer] Playback stopped (position not advancing: {currentPosition}s)");
                                MainThread.BeginInvokeOnMainThread(() => StopPlayback());
                            }
                        }
                        else
                        {
                            noMovementCount = 0; // Reset if position is advancing
                        }

                        lastPosition = currentPosition;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AudioPlayer] Timer error: {ex.Message}");
                }
            };
            positionTimer.Start();

            Debug.WriteLine("[AudioPlayer] MediaElement playback started");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] MediaElement playback error: {ex.Message}");
            isPlaying = false;
        }
    }

    private double GetSpeedFactor()
    {
        var selected = cmbSpeed.SelectedItem?.ToString() ?? "1x";
        
        if (selected.EndsWith("x"))
        {
            selected = selected.TrimEnd('x');
        }

        if (double.TryParse(selected, out double speedFactor))
        {
            return speedFactor;
        }

        Debug.WriteLine($"[AudioPlayer] Invalid speed factor selected '{selected}'. Defaulting to 1.0x");
        return 1.0;
    }

    private void btnPause_Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine("[AudioPlayer] Pausing Playback");
        
        // Pause platform-specific audio service
        audioPlaybackService?.Pause();
        
        // Pause MediaElement
        mediaElement?.Pause();
        
        isPlaying = false;
        positionTimer?.Stop();
        btnPlay.IsEnabled = true;
    }

    private void btnStop_Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine("[AudioPlayer] Stopping Playback");
        StopPlayback();
    }

    private void StopPlayback()
    {
        isPlaying = false;

        // Stop platform-specific audio service
        audioPlaybackService?.Stop();

        // Stop MediaElement
        mediaElement?.Stop();

        positionTimer?.Stop();
        currentPosition = (float)startOffset.TotalSeconds;
        lastPosition = currentPosition;
        CurrentPosition = currentPosition; // Update the bound property
        btnPlay.IsEnabled = true;
    }

    private void btnRewind_Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine("[AudioPlayer] Rewinding Playback");
        StopPlayback();
    }

    private void btnFastForward_Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine("[AudioPlayer] Fast Forwarding Playback");
        StopPlayback();
        currentPosition = (float)Math.Min(endOffset.TotalSeconds - 1.0, endOffset.TotalSeconds);
        CurrentPosition = currentPosition;
    }

    internal double GetPosition()
    {
        // Try to get position from native audio service first
        if (audioPlaybackService != null && audioPlaybackService.IsPlaying)
        {
            return audioPlaybackService.GetPosition();
        }
        
        return currentPosition;
    }

    internal void Stop()
    {
        Debug.WriteLine("[AudioPlayer] Disposing Audio Player Resources");
        isPlaying = false;

        // Stop both audio services
        audioPlaybackService?.Stop();
        mediaElement?.Stop();

        positionTimer?.Stop();
        positionTimer?.Dispose();
        positionTimer = null;
        currentPosition = 0.0f;
        lastPosition = 0.0f;

        // Clean up temporary segment file
        try
        {
            if (!string.IsNullOrEmpty(currentSegmentFile) && File.Exists(currentSegmentFile))
            {
                File.Delete(currentSegmentFile);
                Debug.WriteLine($"[AudioPlayer] Cleaned up temporary segment file");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Error cleaning up segment file: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        
        // Dispose platform-specific audio service
        audioPlaybackService?.Dispose();
        audioPlaybackService = null;
        
        // Dispose MediaElement
        mediaElement?.Dispose();
        mediaElement = null;
        
        GC.SuppressFinalize(this);
    }
}
