using BPASpectrogramM.Interfaces;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if WINDOWS
using BPASpectrogramM.Platforms.Windows;
#endif

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

    private string _currentSegmentFile= string.Empty;
    public string currentSegmentFile { get => _currentSegmentFile; set { _currentSegmentFile = value; OnPropertyChanged(); } }

    private TimeSpan startOffset = TimeSpan.Zero;
    private TimeSpan endOffset = TimeSpan.Zero;
    private WavFormatInfo fileFormat = new WavFormatInfo();
    private HeterodyneModifier? heterodyneModifier = null;

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

    private bool _canPlay = false;

    

    public bool CanPlay { get=> _canPlay; set { _canPlay = value; OnPropertyChanged(); } }

    private double heterodyneFrequencykHz = 50.0;
    public double HeterodyneFrequencykHz
    {
        get { return heterodyneFrequencykHz; }
        set
        {
            heterodyneFrequencykHz = value;
            GetSpeedFactor(); // Update heterodyne modifier with new frequency
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentFrequency));
            
            if (speedFactor < 0) LoadSegment(currentFile, startOffset, endOffset); // Reload segment to apply new heterodyne frequency
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
        //InitializeAudioServices();
        Loaded += (s, e) =>
        {
            Debug.WriteLine("[AudioPlayer] View loaded, initializing audio services and event handlers");
            mediaElement.MediaOpened += (sender, args) =>
            {
                Debug.WriteLine("[AudioPlayer] MediaElement opened media successfully");
            };

            mediaElement.MediaFailed += (sender, args) =>
            {
                Debug.WriteLine($"[AudioPlayer] MediaElement failed to open media: {args.ErrorMessage}");
            };

            mediaElement.MediaEnded += (sender, args) =>
            {
                Debug.WriteLine("[AudioPlayer] MediaElement reached end of media");
                GetSpeedFactor();
                if (speedFactor<0)
                {
                    Debug.WriteLine("[AudioPlayer] Looping heterodyne playback");
                    // Restart looping
                    mediaElement.Stop();
                    mediaElement.SeekTo(TimeSpan.Zero);
                    mediaElement.Play();
                    currentPosition = 0.0f;
                    lastPosition = 0.0f;
                }
                else
                {
                    StopPlayback();
                    isPlaying = false;
                }
            };
             
        };
     
    }

   

    public void LoadSegment(string file, TimeSpan startOffsetTimeSpan, TimeSpan endOffsetTimeSpan)
    {
        currentFile = file;
        startOffset = startOffsetTimeSpan;
        endOffset = endOffsetTimeSpan;
        
        if (!File.Exists(file))
        {
            Debug.WriteLine($"[AudioPlayer] File not found: {file}");
            return;
        }

        Debug.WriteLine($"[AudioPlayer] Audio Format - Sample Rate: {fileFormat.SampleRate}, Channels: {fileFormat.ChannelCount}, Bits: {fileFormat.BitsPerSample}");
        CreateSegmentFile(file, startOffsetTimeSpan,endOffsetTimeSpan);

    }



    /// <summary>
    /// Generates a new file in FileSystem.CacheDirectory.audioSegments
    /// containing only the selected segment of the original audio file, and with
    /// the file format changed for speed reduction, or the audio data
    /// heterodyned and the currently selected frequency
    /// </summary>
    /// <param name="sourceFile"></param>
    /// <param name="startOffset"></param>
    /// <param name="endOffset"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private string CreateSegmentFile(string sourceFile, TimeSpan startOffset, TimeSpan endOffset)
    {
        try
        {
            var tempDir = Path.Combine(FileSystem.CacheDirectory, "audio_segments");
            Directory.CreateDirectory(tempDir);

            var segmentFile = Path.Combine(tempDir, $"segment_{Guid.NewGuid()}.wav");
            currentSegmentFile = segmentFile;

            Debug.WriteLine($"[AudioPlayer] Creating segment file: {segmentFile}");
            Debug.WriteLine($"[AudioPlayer] Start: {startOffset.TotalSeconds}s, End: {endOffset.TotalSeconds}s");

            // Copy segment of audio file
            using (var sourceReader = new AudioFileReaderM(sourceFile))
            {
                if (!sourceReader.IsValid)
                {
                    throw new InvalidOperationException("Source file is not a valid WAV file");
                }
                fileFormat=sourceReader.FormatInfo ?? new WavFormatInfo();

                // Calculate byte positions
                int bytesPerSample = sourceReader.BitsPerSample / 8;
                long startByte = (long)((long)(startOffset.TotalSeconds * sourceReader.SampleRate) * (long)sourceReader.Channels * (long)bytesPerSample);
                long endByte = (long)((long)(endOffset.TotalSeconds * (long)sourceReader.SampleRate)     * (long)sourceReader.Channels * (long)bytesPerSample);
                long segmentSize = endByte - startByte;
                Debug.WriteLine($"[AudioPlayer] Calculated byte range for segment: Start Byte={startByte}, End Byte={endByte}, Segment Size={segmentSize} bytes");

                using (var source = File.OpenRead(sourceFile))
                {
                    Debug.WriteLine($"[AudioPlayer] Opened source file: {sourceFile} at Position {source.Position}");
                    long newPos = startByte;
                    newPos+= (sourceReader.FormatInfo?.AudioDataStartPosition) ?? 0L;
                    //source?.Seek(sourceReader?.FormatInfo?.AudioDataStartPosition??0+startByte, SeekOrigin.Begin);
                    source.Position= newPos;
                    Debug.WriteLine($"[AudioPlayer] Seeked to start byte: {source.Position}");
                    using (var dest = File.Create(segmentFile))
                    {

                        WavFileHeader newHeader = sourceReader.Header ?? new WavFileHeader();
                        newHeader.dataChunkSize = (int)segmentSize;
                        double SpeedFactor = this.GetSpeedFactor();
                        if (SpeedFactor > 0)
                        {
                            newHeader.sampleRate = (int)(sourceReader.SampleRate * SpeedFactor);
                        }
                        //source.Read(header, 0, 44);
                        //dest.Write(header, 0, 44);
                        //var headerBytes = newHeader.ToByteArray();
                        //dest.Write(newHeader, 0, 44);
                        Debug.WriteLine($"[AudioPlayer] Writing new WAV header to segment file with Sample Rate: {newHeader.sampleRate}, Data Chunk Size: {newHeader.dataChunkSize}");
                        int ndestPos = newHeader.Write(dest);
                        Debug.WriteLine($"[AudioPlayer] Finished writing WAV header, dest position: {ndestPos}");

                        // Update data chunk size in header (bytes 40-43)
                        //byte[] sizeBytes = BitConverter.GetBytes((uint)segmentSize);
                        //dest.Seek(40, SeekOrigin.Begin);
                        //dest.Write(sizeBytes, 0, 4);

                        // Copy audio data
                        //source.Seek(44 + startByte, SeekOrigin.Begin);
                        dest.Seek(44, SeekOrigin.Begin);
                        Debug.WriteLine($"[AudioPlayer] Starting to copy audio data for segment, source position: {source.Position}, dest position: {dest.Position}");
                        byte[] buffer = new byte[65536];
                        long bytesRemaining = segmentSize;

                        while (bytesRemaining > 0)
                        {
                            int toRead = (int)Math.Min(buffer.Length, bytesRemaining);
                            
                            
                            int read = source?.Read(buffer, 0, toRead) ?? 0;
                            if (read == 0) break;
                            
                                ProcessBuffer(ref buffer, speedFactor);
                            
                            dest.Write(buffer, 0, read);
                            bytesRemaining -= read;
                            Debug.WriteLine($"[AudioPlayer] Copied {read} bytes, {bytesRemaining} bytes remaining");
                            Debug.WriteLine($"[AudioPlayer] Current source position: {source.Position}, dest position: {dest.Position}");
                        }
                    }
                }
            }

            Debug.WriteLine($"[AudioPlayer] Segment file created successfully: {segmentFile}");
            IsSegmentLoaded = true;
            CanPlay = true;
            return segmentFile;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Error creating segment file: {ex.Message}");
            return null;
        }
    }

    public bool IsSegmentLoaded = false;

    private void ProcessBuffer(ref byte[] buffer, double speedFactor)
    {
        if (speedFactor < 0)
        {
            if (heterodyneModifier == null)
            {
                GetSpeedFactor();
                if (heterodyneModifier == null)
                {
                    Debug.WriteLine("[AudioPlayer] HeterodyneModifier not initialized for heterodyne mode");
                    return;
                }

            }
            try
            {
                Span<short> samples = MemoryMarshal.Cast<byte, short>(buffer.AsSpan());
                for (int i = 0; i < samples.Length; i++)
                {
                    float sample = samples[i] / 32768f; // Convert to float
                    float processedSample = heterodyneModifier.ProcessSample(sample, 0); // Assuming mono for simplicity
                    samples[i] = (short)(processedSample * 32768f); // Convert back to short
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayer.ProcessBuffer] Error processing buffer for heterodyne: {ex.Message}");
            }
        }
        else
        {
            try
            {
                Span<short> samples = MemoryMarshal.Cast<byte, short>(buffer.AsSpan());
                for (int i = 0; i < samples.Length; i++)
                {
                    float sample = samples[i] / 32768f; // Convert to float
                    float processedSample = sample * 4.0f;
                    samples[i] = (short)(clamp(processedSample)); // Convert back to short
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayer.ProcessBuffer] Error processing buffer for heterodyne: {ex.Message}");
            }
        }
    }

    private short clamp(float processedSample)
    {
        return (short)Math.Clamp(processedSample*32768.0f, short.MinValue,short.MaxValue);
    }

    private IDispatcherTimer? timer;

    private async void btnPlay_Clicked(object sender, EventArgs e)
    {
        if (isPlaying) StopPlayback();
        CanPlay = false;
        isPlaying = true;
        Debug.WriteLine("[AudioPlayer] Play button clicked");
        mediaElement.Volume = Volume;
       
        Debug.WriteLine($"[AudioPlayer] Set MediaElement source to: {currentSegmentFile}");
        timer=Dispatcher.CreateTimer();
        timer.Interval =TimeSpan.FromMilliseconds(50);
        timer.Tick += (s, args) =>
        {
            try
            {
                if (mediaElement != null)
                {
                    currentPosition = (float)mediaElement.Position.TotalSeconds*(float)(speedFactor>0?speedFactor:1.0);
                    CurrentPosition = (float)startOffset.TotalSeconds + currentPosition;
                    OnPlayBackUpdated(new FileEventArgs(currentFile));
                }
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayer] Timer error: {ex.Message}");
            }
        };
        timer.Start();
        GetSpeedFactor();
        if(speedFactor < 0)
        {
            mediaElement.ShouldLoopPlayback = true;
        }
        else
        {
            mediaElement.ShouldLoopPlayback = false;
        }
        mediaElement.SeekTo(TimeSpan.Zero);
        mediaElement.Play();
        Debug.WriteLine("[AudioPlayer] Play command sent to MediaElement");
        // await PlayAudioAsync();
        
    }



    private double GetSpeedFactor()
    {
        var selected = cmbSpeed.SelectedItem?.ToString() ?? "1x";
        
        if (selected.EndsWith("x"))
        {
            selected = selected.TrimEnd('x');
        }
        if(selected.Contains("heterodyne", StringComparison.CurrentCultureIgnoreCase))
        {
            this.speedFactor = -1.0; // Use -1.0 as a special value to indicate heterodyne mode
            heterodyneModifier = new HeterodyneModifier(fileFormat, cutoffFrequency: 5000f, heterodyneFrequency: (float)(HeterodyneFrequencykHz * 1000));
            return -1.0; // Use -1.0 as a special value to indicate heterodyne mode
        }
        if (double.TryParse(selected, out double speedFactor))
        {
            this.speedFactor = speedFactor;
            return speedFactor;
        }

        Debug.WriteLine($"[AudioPlayer] Invalid speed factor selected '{selected}'. Defaulting to 1.0x");
        this.speedFactor = 1.0;
        return 1.0;
    }

    private void btnPause_Clicked(object sender, EventArgs e)
    {
        

        // Pause platform-specific audio service
        if (isPlaying && CanPlay)
        {
            Debug.WriteLine("[AudioPlayer] Resumiong Playback");
            CanPlay = false;
            timer?.Start();
            mediaElement.Play();
        }
        else
        {
            Debug.WriteLine("[AudioPlayer] Pausing Playback");
            // Pause MediaElement
            mediaElement?.Pause();

            
            timer?.Stop();
            CanPlay = true;
        }
    }

    private void btnStop_Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine("[AudioPlayer] Stopping Playback");
        StopPlayback();
        
    }

    private void StopPlayback()
    {
        if (isPlaying)
        {
            try
            {
                mediaElement?.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayer] Error stopping MediaElement: {ex.Message}");
            }
        }
        isPlaying = false;

        
       

        timer?.Stop();
        currentPosition = (float)startOffset.TotalSeconds;
        lastPosition = currentPosition;
        CurrentPosition = currentPosition; // Update the bound property
        CanPlay = true;
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
        var pos=mediaElement?.Position;
        currentPosition = (float)startOffset.TotalSeconds+((float)(pos?.TotalSeconds ?? 0.0)*(float)(speedFactor>0?speedFactor:1.0f))  ;
        
        return currentPosition;
    }

    internal void Stop()
    {
        Debug.WriteLine("[AudioPlayer] Disposing Audio Player Resources");
        
        StopPlayback();
        timer = null;
        currentPosition = 0.0f;
        lastPosition = 0.0f;
        CanPlay = true;
        // Clean up temporary segment file
        try
        {
            if (!string.IsNullOrEmpty(currentSegmentFile) && File.Exists(currentSegmentFile))
            {
                string path = Path.GetDirectoryName(currentSegmentFile);
                var files=Directory.EnumerateFiles(path??string.Empty);
                foreach (var file in files??Enumerable.Empty<string>())
                {
                    try
                    {
                        File.Delete(file);
                        Debug.WriteLine($"[AudioPlayer] Deleted temporary segment file: {file}");
                    }
                    catch { }
                }
                IsSegmentLoaded = false;
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
        
        
        
        // Dispose MediaElement
        mediaElement?.Dispose();
        mediaElement = null;
        
        GC.SuppressFinalize(this);
    }

    private void cmbSpeedChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        Debug.WriteLine($"[AudioPlayer] Speed selection changed for {currentFile}");
        if (string.IsNullOrEmpty(currentFile)) return; 
                GetSpeedFactor(); // Update heterodyne modifier with new frequency
        Debug.WriteLine($"[AudioPlayer] New speed factor: {speedFactor}, Heterodyne mode: {speedFactor < 0}");
        LoadSegment(currentFile,  startOffset, endOffset); // Reload segment to apply new speed/heterodyne settings
    }
}
