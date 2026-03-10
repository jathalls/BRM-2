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
    private TimeSpan startOffset = TimeSpan.Zero;
    private TimeSpan endOffset = TimeSpan.Zero;
    private WavFormatInfo fileFormat = new WavFormatInfo();

    private string _currentFrequency;
    public string CurrentFrequency
    {
        get { return HeterodyneFrequencykHz.ToString("F1") + " kHz"; }
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
    private MediaElement? mediaElement;  // Kept for fallback/heterodyne mode
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
            }
            else
            {
                useNativeAudioEngine = false;
                Debug.WriteLine("[AudioPlayer] No platform audio service; using MediaElement fallback");
            }

            // Also initialize MediaElement for heterodyne mode (fallback)
            InitializeMediaElement();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Error initializing audio services: {ex.Message}");
            useNativeAudioEngine = false;
            InitializeMediaElement();
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

    private void InitializeMediaElement()
    {
        try
        {
            // Get MediaElement from XAML or create new one
            mediaElement = this.FindByName<MediaElement>("mediaElement");
            
            if (mediaElement == null)
            {
                Debug.WriteLine("[AudioPlayer] MediaElement not found in XAML, creating new instance");
                mediaElement = new MediaElement();
                mediaElement.Volume = Volume;
            }
            else
            {
                Debug.WriteLine("[AudioPlayer] MediaElement found in XAML");
                mediaElement.Volume = Volume;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Error initializing MediaElement: {ex.Message}");
        }
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

            // Load into platform-specific audio service
            if (audioPlaybackService != null && useNativeAudioEngine)
            {
                try
                {
                    audioPlaybackService.LoadSegment(file, startOffset, endOffset, fileFormat);
                    Debug.WriteLine("[AudioPlayer] Audio loaded into platform-specific service");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AudioPlayer] Error loading into platform service: {ex.Message}");
                    useNativeAudioEngine = false;
                }
            }
            
            // Also load into MediaElement for heterodyne mode
            if (mediaElement != null)
            {
                try
                {
                    mediaElement.Source = MediaSource.FromFile(file);
                    currentPosition = (float)startOffset.TotalSeconds;
                    lastPosition = currentPosition;
                    Debug.WriteLine($"[AudioPlayer] Audio loaded into MediaElement");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AudioPlayer] Error setting MediaElement source: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Error loading segment: {ex.Message}");
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
            // Use platform-specific audio service for slow playback (sample rate manipulation)
            else if (audioPlaybackService != null && useNativeAudioEngine)
            {
                PlayWithNativeAudioEngine();
            }
            // Fallback to MediaElement
            else
            {
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
            
            audioPlaybackService?.Play(speedFactor, Volume);
            
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
                        OnPlayBackUpdated(new FileEventArgs(currentFile));
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
            Debug.WriteLine($"[AudioPlayer] Using MediaElement");
            
            mediaElement.Volume = Volume;

            // Try to set playback rate if supported (won't work for very slow speeds)
            try
            {
                if (speedFactor != 1.0)
                {
                    var playbackRateProperty = typeof(MediaElement).GetProperty("PlaybackRate");
                    if (playbackRateProperty != null && playbackRateProperty.CanWrite)
                    {
                        playbackRateProperty.SetValue(mediaElement, speedFactor);
                        Debug.WriteLine($"[AudioPlayer] Playback rate set to {speedFactor}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayer] Playback rate not supported: {ex.Message}");
            }

            // Play the media
            mediaElement.Play();
            lastPosition = currentPosition;

            // Start position tracking timer
            positionTimer?.Stop();
            positionTimer?.Dispose();
            positionTimer = new System.Timers.Timer(100);
            positionTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    if (isPlaying && mediaElement != null)
                    {
                        currentPosition = (float)mediaElement.Position.TotalSeconds;
                        OnPlayBackUpdated(new FileEventArgs(currentFile));

                        // Check if we've reached the end of the selection
                        if (currentPosition >= endOffset.TotalSeconds)
                        {
                            var selected = cmbSpeed.SelectedItem?.ToString() ?? "";
                            if (selected.Contains("heterodyne", StringComparison.CurrentCultureIgnoreCase))
                            {
                                Debug.WriteLine("[AudioPlayer] Looping heterodyne playback");
                                // Reload and restart for looping
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    mediaElement.Stop();
                                    mediaElement.Source = MediaSource.FromFile(currentFile);
                                    currentPosition = (float)startOffset.TotalSeconds;
                                    lastPosition = currentPosition;
                                    mediaElement.Play();
                                });
                            }
                            else
                            {
                                // Stop playback when we reach the end
                                MainThread.BeginInvokeOnMainThread(() => StopPlayback());
                            }
                        }

                        // Check if media has stopped by comparing position (no movement for 200ms)
                        if (currentPosition == lastPosition && isPlaying)
                        {
                            Debug.WriteLine("[AudioPlayer] Playback stopped (position not advancing)");
                            MainThread.BeginInvokeOnMainThread(() => StopPlayback());
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
        currentPosition = (float)Math.Max(startOffset.TotalSeconds, endOffset.TotalSeconds - 1.0);
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
