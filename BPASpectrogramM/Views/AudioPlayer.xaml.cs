using BPASpectrogramM.Interfaces;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Devices;
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

    private string _currentSegmentFile = string.Empty;

    public string currentSegmentFile
    {
        get => _currentSegmentFile;
        set
        {
            Debug.WriteLine($"[AudioPlayer.set file] currentSegmentFile: {value} replaces {_currentSegmentFile}");
            if (_currentSegmentFile != value)
            {
                _currentSegmentFile = value;
                try
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        MediaSourceFile = null;
                        Debug.WriteLine($"[AudioPlayer.set file] currentSegmentFile cleared");
                        return;
                    }

                    // Try creating MediaSource from the path/URI
                    // First, ensure we have a proper file path or URI
                    string pathToUse = value;

                    // If it's already a file URI, try to convert to direct path first for better compatibility
                    if (value.StartsWith("file://"))
                    {
                        Debug.WriteLine($"[AudioPlayer.set file] Found file:// URI: {value}");
                        try
                        {
                            // Convert file:// URI to direct path for better macOS compatibility
                            Uri uri = new Uri(value);
                            pathToUse = uri.LocalPath;
                            Debug.WriteLine($"[AudioPlayer.set file] Converted URI to path: {pathToUse}");
                        }
                        catch
                        {
                            Debug.WriteLine($"[AudioPlayer.set file] Failed to convert URI, using as-is");
                            pathToUse = value;
                        }
                    }
                    // If it's a file path, try to use it directly first (works on macOS and Windows)
                    else if (File.Exists(value))
                    {
                        Debug.WriteLine($"[AudioPlayer.set file] Using file path directly: {value}");
                        pathToUse = value;
                    }
                    else
                    {
                        // File not found at given path, try as URI
                        Debug.WriteLine($"[AudioPlayer.set file] File not found at: {value}, attempting to construct URI");
                        try
                        {
                            Uri uri = new Uri(new FileInfo(value).FullName);
                            pathToUse = uri.AbsoluteUri;
                            Debug.WriteLine($"[AudioPlayer.set file] Constructed URI: {pathToUse}");
                        }
                        catch
                        {
                            Debug.WriteLine($"[AudioPlayer.set file] Could not construct URI, using path as-is");
                            pathToUse = value;
                        }
                    }

                    // Create MediaSource from the path/URI
                    MediaSourceFile = MediaSource.FromFile(pathToUse);
                    Debug.WriteLine($"[AudioPlayer.set file:98] currentSegmentFile changed to: {value}");
                    Debug.WriteLine($"[AudioPlayer.set file:99] MediaSource created successfully from: {pathToUse}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        $"[AudioPlayer.set file:106] Error creating MediaSource from '{value}': {ex.GetType().Name} - {ex.Message}");
                    Debug.WriteLine($"[AudioPlayer.set file:107] Stack trace: {ex.StackTrace}");
                    MediaSourceFile = null;
                }

                OnPropertyChanged();
            }
        }
    }

    private MediaSource? _mediaSourceFile = null;

    public MediaSource? MediaSourceFile
    {
        get => _mediaSourceFile;
        set
        {
            if (_mediaSourceFile != value)
            {
                _mediaSourceFile = value;
                Debug.WriteLine($"[AudioPlayer] MediaSourceFile property changed to: {value}");
                OnPropertyChanged();
            }
            else
            {
                Debug.WriteLine($"[AudioPlayer.set MSF] not changing Media Source:- {_mediaSourceFile}");
            }
        }
    }

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



    public bool CanPlay
    {
        get => _canPlay;
        set
        {
            _canPlay = value;
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
            GetSpeedFactor(); // Update heterodyne modifier with new frequency

            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentFrequency));

            if (speedFactor < 0)
                LoadSegment(currentFile, startOffset, endOffset); // Reload segment to apply new heterodyne frequency
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
                // Signal that media is ready for playback
                mediaOpenedTcs?.TrySetResult(true);
            };

            mediaElement.MediaFailed += (sender, args) =>
            {
                Debug.WriteLine($"[AudioPlayer] MediaElement failed to open media: {args.ErrorMessage}");
                Debug.WriteLine($"[AudioPlayer] Attempted source: {mediaElement.Source}");
                Debug.WriteLine($"[AudioPlayer] Source type: {mediaElement.Source?.GetType().Name}");

                // Signal failure so we don't wait indefinitely
                mediaOpenedTcs?.TrySetException(
                    new InvalidOperationException($"MediaElement failed: {args.ErrorMessage}"));

                // Try to provide more diagnostic information
                if (mediaElement.Source != null)
                {
                    var sourceString = mediaElement.Source.ToString();
                    Debug.WriteLine($"[AudioPlayer] Source string: {sourceString}");

                    // Check if it's a file URI and verify file exists
                    if (!string.IsNullOrEmpty(sourceString) &&
                        (sourceString.StartsWith("filesystem://") || sourceString.StartsWith("File:") ||
                         sourceString.Contains("/")))
                    {
                        try
                        {
                            string filePath = sourceString;
                            
                            Debug.WriteLine($"[AudioPlayer] Original source string: '{filePath}'");
                            Debug.WriteLine($"[AudioPlayer] String length: {filePath.Length}");

                            // Remove "File: " prefix that MediaSource.ToString() adds - try multiple variations
                            while (filePath.StartsWith("File: "))
                            {
                                filePath = filePath.Substring(6);  // Remove "File: " (6 characters)
                                Debug.WriteLine($"[AudioPlayer] Stripped 'File: ' prefix, path now: {filePath}");
                            }
                            
                            // Also try with different spacing/case
                            if (filePath.StartsWith("file: "))
                            {
                                filePath = filePath.Substring(6);
                                Debug.WriteLine($"[AudioPlayer] Stripped 'file: ' prefix, path now: {filePath}");
                            }

                            // Handle file:// URIs - convert to direct path
                            if (filePath.StartsWith("file://"))
                            {
                                try
                                {
                                    Uri uri = new Uri(filePath);
                                    filePath = uri.LocalPath;
                                    Debug.WriteLine($"[AudioPlayer] Converted file:// URI to path: {filePath}");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[AudioPlayer] Failed to parse file:// URI: {ex.Message}");
                                    filePath = filePath.Replace("file://", "");
                                }
                            }
                            // Handle filesystem:// URIs
                            else if (filePath.StartsWith("filesystem://"))
                            {
                                try
                                {
                                    Uri uri = new Uri(filePath);
                                    filePath = uri.LocalPath;
                                    Debug.WriteLine($"[AudioPlayer] Converted filesystem:// URI to path: {filePath}");
                                }
                                catch
                                {
                                    filePath = filePath.Replace("filesystem://", "");
                                }
                            }

                            // Remove leading slash if present on macOS and file doesn't exist
                            if (filePath.StartsWith("/") && !File.Exists(filePath))
                            {
                                filePath = filePath.TrimStart('/');
                                Debug.WriteLine($"[AudioPlayer] Removed leading slash, path now: {filePath}");
                            }

                            Debug.WriteLine($"[AudioPlayer] Final resolved file path: {filePath}");
                            Debug.WriteLine($"[AudioPlayer] File exists: {File.Exists(filePath)}");

                            if (File.Exists(filePath))
                            {
                                var fileInfo = new FileInfo(filePath);
                                Debug.WriteLine($"[AudioPlayer] File size: {fileInfo.Length} bytes");

                                // Check file permissions on macOS
                                try
                                {
                                    using (var fs = File.OpenRead(filePath))
                                    {
                                        Debug.WriteLine($"[AudioPlayer] File is readable");
                                    }
                                }
                                catch (Exception permEx)
                                {
                                    Debug.WriteLine($"[AudioPlayer] ERROR: File is not readable: {permEx.Message}");
                                }

                                // Validate WAV file header
                                try
                                {
                                    using (var fs = File.OpenRead(filePath))
                                    using (var br = new BinaryReader(fs))
                                    {
                                        string riff = new string(br.ReadChars(4));
                                        int fileSize = br.ReadInt32();
                                        string wave = new string(br.ReadChars(4));

                                        if (riff != "RIFF")
                                        {
                                            Debug.WriteLine($"[AudioPlayer] ERROR: Invalid RIFF header!");
                                        }

                                        if (wave != "WAVE")
                                        {
                                            Debug.WriteLine($"[AudioPlayer] ERROR: Invalid WAVE header!");
                                        }

                                        Debug.WriteLine(
                                            $"[AudioPlayer] WAV Header - RIFF: {riff}, Size: {fileSize}, WAVE: {wave}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[AudioPlayer] Error validating WAV header: {ex.Message}");
                                }
                            }
                            else // !File.Exists
                            {
                                Debug.WriteLine($"[AudioPlayer] ERROR: File not found at path: {filePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[AudioPlayer] Error extracting file path from URI: {ex.Message}");
                            Debug.WriteLine($"[AudioPlayer] Stack trace: {ex.StackTrace}");
                        }
                    }
                }
            };

        };




        mediaElement.MediaEnded += (sender, args) =>
        {
            Debug.WriteLine("[AudioPlayer] MediaElement reached end of media");
            GetSpeedFactor();
            if (speedFactor < 0)
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
            Debug.WriteLine($"[AudioPlayer.CSF] Cache directory: {FileSystem.CacheDirectory}");
            Debug.WriteLine($"[AudioPlayer.CSF] Segment directory: {tempDir}");
            
            Directory.CreateDirectory(tempDir);
            
            // Verify directory was created and is writable
            if (!Directory.Exists(tempDir))
            {
                throw new InvalidOperationException($"Failed to create directory: {tempDir}");
            }
            
            // Test write permission
            var testFile = Path.Combine(tempDir, ".write_test");
            try
            {
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                Debug.WriteLine($"[AudioPlayer.CSF] Directory is writable: {tempDir}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayer.CSF] Directory write permission denied: {ex.Message}");
                throw;
            }

            var segmentFile = Path.Combine(tempDir, $"segment_{Guid.NewGuid()}.wav");
            //currentSegmentFile = segmentFile;

            Debug.WriteLine($"[AudioPlayer.CSF] Creating segment file: {segmentFile}");
            Debug.WriteLine($"[AudioPlayer.CSF] Start: {startOffset.TotalSeconds}s, End: {endOffset.TotalSeconds}s");

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
                Debug.WriteLine($"[AudioPlayer.CSF] Calculated byte range for segment: Start Byte={startByte}, End Byte={endByte}, Segment Size={segmentSize} bytes");

                using (var source = File.OpenRead(sourceFile))
                {
                    Debug.WriteLine($"[AudioPlayer.CSF] Opened source file: {sourceFile} at Position {source.Position}");
                    long newPos = startByte;
                    newPos+= (sourceReader.FormatInfo?.AudioDataStartPosition) ?? 0L;
                    //source?.Seek(sourceReader?.FormatInfo?.AudioDataStartPosition??0+startByte, SeekOrigin.Begin);
                    source.Position= newPos;
                    Debug.WriteLine($"[AudioPlayer.CSF] Seeked to start byte: {source.Position}");
                    using (var dest = File.Create(segmentFile))
                    {

                        WavFileHeader newHeader = sourceReader.Header ?? new WavFileHeader();
                        newHeader.dataChunkSize = (int)segmentSize;
                        double SpeedFactor = this.GetSpeedFactor();
                        if (SpeedFactor > 0)
                        {
                            newHeader.sampleRate = (int)(sourceReader.SampleRate * SpeedFactor);
                        }
                        // Update numChannels to match source
                        newHeader.numChannels = (Int16)sourceReader.Channels;
                        newHeader.bitsPerSample = (Int16)sourceReader.BitsPerSample;
                        
                        Debug.WriteLine($"[AudioPlayer.CSF] Writing new WAV header to segment file with Sample Rate: {newHeader.sampleRate}, Channels: {newHeader.numChannels}, Bits: {newHeader.bitsPerSample}, Data Chunk Size: {newHeader.dataChunkSize}");
                        int ndestPos = newHeader.Write(dest);
                        Debug.WriteLine($"[AudioPlayer.CSF] Finished writing WAV header, dest position: {ndestPos}");

                        // Copy audio data
                        dest.Seek(44, SeekOrigin.Begin);
                        Debug.WriteLine($"[AudioPlayer.CSF] Starting to copy audio data for segment, source position: {source.Position}, dest position: {dest.Position}");
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
                            Debug.WriteLine($"[AudioPlayer.CSF] Copied {read} bytes, {bytesRemaining} bytes remaining");
                        }
                        // Flush to ensure all data is written
                        dest.Flush();
                    } // FileStream closed and disposed here
                    
                    // Add longer delay to ensure file is fully written to disk on macOS
                    // macOS needs more time for file system synchronization
                    Debug.WriteLine($"[AudioPlayer] Waiting for file system to sync...");
                    System.Threading.Thread.Sleep(200);
                    
                    // Additional check: Force file system sync if available
                    try
                    {
                        // Try to open and close the file again to ensure it's accessible
                        using (var fs = File.OpenRead(segmentFile))
                        {
                            fs.Seek(0, SeekOrigin.End);
                            long finalSize = fs.Position;
                            Debug.WriteLine($"[AudioPlayer.CSF] File final size verified: {finalSize} bytes");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[AudioPlayer] ERROR: File is not accessible after creation: {ex.Message}");
                        throw;
                    }
                }
            }
            
            try
            {
                using (var fs = File.OpenRead(segmentFile))
                {
                    // File is readable
                    Debug.WriteLine($"[AudioPlayer.CSF2] File is readable");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayer.CSF2] ERROR: File is not readable: {ex.Message}");
                Debug.WriteLine($"[AudioPlayer.CSF2] Attempting to fix permissions...");
    
                // Try to make file readable
                try
                {
                    var info = new FileInfo(segmentFile);
                    // Note: Setting permissions is platform-specific
                    // On macOS, files should inherit permissions from directory
                }
                catch { }
            }

            Debug.WriteLine($"[AudioPlayer.CSF2] Segment file created successfully: {segmentFile}");
            Debug.WriteLine("=============================================================");
            
            // Verify file was created and has content
            if (!File.Exists(segmentFile))
            {
                throw new InvalidOperationException($"Segment file was not created: {segmentFile}");
            }
            
            var fileInfo = new FileInfo(segmentFile);
            if (fileInfo.Length < 44) // At minimum should have the WAV header
            {
                throw new InvalidOperationException($"Segment file is too small: {fileInfo.Length} bytes (minimum 44 bytes for header)");
            }
            
            Debug.WriteLine($"[AudioPlayer.CSF2] Segment file verified: {fileInfo.Length} bytes");
            
            // Convert file path based on platform
            string pathForMediaSource;
            
            // Use direct file paths on all platforms
            // The currentSegmentFile setter will handle conversions if needed
            // Direct paths are more reliable across platforms than file:// URIs
            pathForMediaSource = segmentFile;
            Debug.WriteLine($"[AudioPlayer] Using direct file path: {pathForMediaSource}");
            
            // Log platform info for diagnostics
            Debug.WriteLine($"[AudioPlayer] Current platform: {DeviceInfo.Platform}");
            
            Debug.WriteLine($"[AudioPlayer] Final path for MediaSource: {pathForMediaSource}");

            
            // Validate the WAV file before setting as source
            if (!ValidateWavFile(segmentFile))
            {
                Debug.WriteLine($"[AudioPlayer] WARNING: WAV file validation failed for {segmentFile}");
                // Continue anyway, but log the warning
            }
            
            // Update the binding property with the proper file path
            // This will trigger currentSegmentFile setter which creates the MediaSource
            Debug.WriteLine($"[AudioPlayer] About to set currentSegmentFile to: {pathForMediaSource}");
            Debug.WriteLine($"[AudioPlayer] File exists at this path: {File.Exists(segmentFile)}");
            Debug.WriteLine($"[AudioPlayer] File size: {new FileInfo(segmentFile).Length} bytes");
            currentSegmentFile = pathForMediaSource;
            Debug.WriteLine($"[AudioPlayer.CSF2:624] currentSegmentFile now set to: {currentSegmentFile}");
            Debug.WriteLine($"[AudioPlayer.CSF2:625] MediaSourceFile now set to: {MediaSourceFile}");
            
            IsSegmentLoaded = true;
            CanPlay = true;
            return segmentFile;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Error creating segment file: {ex.GetType().Name} - {ex.Message}");
            Debug.WriteLine($"[AudioPlayer] Stack trace: {ex.StackTrace}");
            IsSegmentLoaded = false;
            CanPlay = false;
            currentSegmentFile = string.Empty; // Clear the binding on error
            MediaSourceFile = null;    // Clear MediaElement source on error
            return null;
        }
    }

    /// <summary>
    /// Validates that a WAV file has a proper header structure
    /// </summary>
    private bool ValidateWavFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[AudioPlayer.ValidateWavFile] File not found: {filePath}");
                return false;
            }

            using (var fs = File.OpenRead(filePath))
            {
                if (fs.Length < 44)
                {
                    Debug.WriteLine($"[AudioPlayer.ValidateWavFile] File too small: {fs.Length} bytes (needs at least 44)");
                    return false;
                }

                using (var br = new BinaryReader(fs))
                {
                    // Read RIFF header
                    string riff = new string(br.ReadChars(4));
                    if (riff != "RIFF")
                    {
                        Debug.WriteLine($"[AudioPlayer.ValidateWavFile] Invalid RIFF header: {riff}");
                        return false;
                    }

                    int fileSize = br.ReadInt32();
                    Debug.WriteLine($"[AudioPlayer.ValidateWavFile] File size from header: {fileSize}, actual: {fs.Length - 8}");

                    string wave = new string(br.ReadChars(4));
                    if (wave != "WAVE")
                    {
                        Debug.WriteLine($"[AudioPlayer.ValidateWavFile] Invalid WAVE header: {wave}");
                        return false;
                    }

                    // Check for fmt chunk
                    string fmt = new string(br.ReadChars(4));
                    if (fmt != "fmt ")
                    {
                        Debug.WriteLine($"[AudioPlayer.ValidateWavFile] fmt chunk not found: {fmt}");
                        return false;
                    }

                    int fmtSize = br.ReadInt32();
                    Debug.WriteLine($"[AudioPlayer.ValidateWavFile] fmt chunk size: {fmtSize}");

                    // Read format info
                    short audioFormat = br.ReadInt16();
                    short channels = br.ReadInt16();
                    int sampleRate = br.ReadInt32();
                    int byteRate = br.ReadInt32();
                    short blockAlign = br.ReadInt16();
                    short bitsPerSample = br.ReadInt16();

                    Debug.WriteLine($"[AudioPlayer.ValidateWavFile] Format: {audioFormat}, Channels: {channels}, Sample Rate: {sampleRate}, Bits: {bitsPerSample}");

                    if (audioFormat != 1)
                    {
                        Debug.WriteLine($"[AudioPlayer.ValidateWavFile] Unsupported audio format: {audioFormat}");
                        return false;
                    }

                    Debug.WriteLine($"[AudioPlayer.ValidateWavFile] WAV file is valid!");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer.ValidateWavFile] Exception: {ex.Message}");
            return false;
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

    private TaskCompletionSource<bool>? mediaOpenedTcs;

    private async void btnPlay_Clicked(object sender, EventArgs e)
    {
        if (isPlaying) StopPlayback();
        CanPlay = false;
        isPlaying = true;
        Debug.WriteLine("[AudioPlayer] Play button clicked");
        Debug.WriteLine($"[AudioPlayer] Current state - Source: {mediaElement.Source}, IsLoaded: {mediaElement.IsLoaded}");
        
        try
        {
            // Create a new task completion source for this playback attempt
            mediaOpenedTcs = new TaskCompletionSource<bool>();
            
            // Wait for MediaOpened event with a timeout
            var openedTask = mediaOpenedTcs.Task;
            var timeoutTask = Task.Delay(5000);
            
            Debug.WriteLine("[AudioPlayer] Waiting for MediaOpened event...");
            var completedTask = await Task.WhenAny(openedTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                Debug.WriteLine("[AudioPlayer] WARNING: MediaOpened timeout after 5 seconds");
                // Continue anyway, media might be loaded
            }
            else
            {
                Debug.WriteLine("[AudioPlayer] MediaOpened event received");
            }
            
            // Additional wait to ensure everything is initialized
            await Task.Delay(300);
            
            mediaElement.Volume = Volume;
            
            Debug.WriteLine($"[AudioPlayer] Before Play - Source: {mediaElement.Source}, IsLoaded: {mediaElement.IsLoaded}");
            Debug.WriteLine($"[AudioPlayer] MediaSourceFile: {MediaSourceFile}");
            
            // Set up position timer
            timer = Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += (s, args) =>
            {
                try
                {
                    if (mediaElement != null && isPlaying)
                    {
                        currentPosition = (float)mediaElement.Position.TotalSeconds * (float)(speedFactor > 0 ? speedFactor : 1.0);
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
            if (speedFactor < 0)
            {
                mediaElement.ShouldLoopPlayback = true;
            }
            else
            {
                mediaElement.ShouldLoopPlayback = false;
            }

            // Attempt to play with retry logic
            await PlayWithRetry();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioPlayer] Exception in Play: {ex.GetType().Name} - {ex.Message}");
            Debug.WriteLine($"[AudioPlayer] Stack trace: {ex.StackTrace}");
            isPlaying = false;
            CanPlay = true;
        }
    }

    private async Task PlayWithRetry()
    {
        int maxAttempts = 5;
        int delayMs = 200;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Debug.WriteLine($"[Play] Attempt {attempt}/{maxAttempts}: Calling mediaElement.Play()");
                Debug.WriteLine($"[Play] Current state - IsLoaded: {mediaElement.IsLoaded}, Source is null: {mediaElement.Source == null}");
                
                mediaElement.Play();
                
                Debug.WriteLine($"[Play] Attempt {attempt}: Play() returned successfully");
                Debug.WriteLine($"[Play] After Play() - Position: {mediaElement.Position}, IsPlaying: {mediaElement.CurrentState.ToString()}");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                Debug.WriteLine($"[Play] Attempt {attempt} failed with {ex.GetType().Name}: {ex.Message}");
                Debug.WriteLine($"[Play] Waiting {delayMs}ms before retry...");
                await Task.Delay(delayMs);
                delayMs = Math.Min(delayMs * 2, 2000); // Cap backoff at 2 seconds
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Play] Final attempt {attempt} failed: {ex.GetType().Name}: {ex.Message}");
                Debug.WriteLine($"[Play] Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        Debug.WriteLine($"[Play] All {maxAttempts} attempts failed - giving up");
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
