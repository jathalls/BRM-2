using System.Diagnostics;

namespace BPASpectrogramM
{
    /// <summary>
    /// WAV format information extracted from file header
    /// </summary>
    public class WavFormatInfo
    {
        public int SampleRate { get; set; }
        public int ChannelCount { get; set; }
        public int BitsPerSample { get; set; }
        public int ByteRate { get; set; }
        public int BlockAlign { get; set; }
        public long AudioDataStartPosition { get; set; }
        public int AudioDataSize { get; set; }
        
        public TimeSpan Duration => TimeSpan.FromSeconds(AudioDataSize/ByteRate);
    }

    /// <summary>
    /// A .NET Maui replacement for NAudio's AudioFileReader, which is not compatible with .NET Maui. 
    /// This class reads WAV file headers directly without external audio libraries.
    /// </summary>
    public class AudioFileReaderM : IDisposable
    {
        private bool disposedValue;
        private FileStream? _fileStream;
        private BinaryReader? _reader;
        private int _currentPosition = 0;
        private int _selectionStartPosition = 0;
        private int _selectionEndPosition = -1; // -1 means no selection, read to end of file

        public WavFormatInfo? FormatInfo { get; private set; }
        public bool IsValid => FormatInfo != null && _fileStream != null;

        public int SampleRate => FormatInfo?.SampleRate ?? 0;
        public int Channels => FormatInfo?.ChannelCount ?? 1;
        public int BitsPerSample => FormatInfo?.BitsPerSample ?? 16;
        public double TotalDuration => FormatInfo != null && SampleRate > 0 
            ? (double)FormatInfo.AudioDataSize / (SampleRate * Channels * (BitsPerSample / 8))
            : 0;

        public AudioFileReaderM(string filePath)
        {
            try
            {
                Debug.WriteLine($"[AudioFileReaderM] Opening file: {filePath}");
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Audio file not found: {filePath}");
                }

                // Open file stream
                _fileStream = File.OpenRead(filePath);
                _reader = new BinaryReader(_fileStream);

                // Parse WAV header
                FormatInfo = ReadWavHeader();

                if (FormatInfo == null)
                {
                    throw new InvalidOperationException("Failed to parse WAV file header");
                }

                Debug.WriteLine($"[AudioFileReaderM] File opened successfully");
                Debug.WriteLine($"[AudioFileReaderM] Sample Rate: {SampleRate}, Channels: {Channels}, Bits: {BitsPerSample}");
                Debug.WriteLine($"[AudioFileReaderM] Audio Data Size: {FormatInfo.AudioDataSize}, Start Position: {FormatInfo.AudioDataStartPosition}");
                Debug.WriteLine($"[AudioFileReaderM] Total Duration: {TotalDuration:F2} seconds");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioFileReaderM] Error: {ex.Message}");
                Debug.WriteLine($"[AudioFileReaderM] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[AudioFileReaderM] InnerException: {ex.InnerException.Message}");
                }
                Dispose();
            }
        }

        /// <summary>
        /// Reads and parses the WAV file header to extract format information
        /// </summary>
        private WavFormatInfo? ReadWavHeader()
        {
            try
            {
                _fileStream!.Seek(0, SeekOrigin.Begin);

                // Read RIFF header
                string riffHeader = new string(_reader!.ReadChars(4));
                if (riffHeader != "RIFF")
                {
                    Debug.WriteLine($"[ReadWavHeader] Invalid RIFF header: {riffHeader}");
                    return null;
                }

                int fileSize = _reader.ReadInt32();
                string waveHeader = new string(_reader.ReadChars(4));
                if (waveHeader != "WAVE")
                {
                    Debug.WriteLine($"[ReadWavHeader] Invalid WAVE header: {waveHeader}");
                    return null;
                }

                WavFormatInfo info = new WavFormatInfo();
                bool fmtChunkFound = false;
                bool dataChunkFound = false;

                // Read chunks
                while (_fileStream.Position < _fileStream.Length && !dataChunkFound)
                {
                    string chunkId = new string(_reader.ReadChars(4));
                    int chunkSize = _reader.ReadInt32();

                    if (chunkId == "fmt ")
                    {
                        // Read format subchunk
                        short audioFormat = _reader.ReadInt16();
                        if (audioFormat != 1) // PCM only
                        {
                            Debug.WriteLine($"[ReadWavHeader] Only PCM format (1) is supported, found: {audioFormat}");
                            return null;
                        }

                        info.ChannelCount = _reader.ReadInt16();
                        info.SampleRate = _reader.ReadInt32();
                        info.ByteRate = _reader.ReadInt32();
                        info.BlockAlign = _reader.ReadInt16();
                        info.BitsPerSample = _reader.ReadInt16();

                        // Skip any extra bytes in fmt chunk
                        if (chunkSize > 16)
                        {
                            _reader.ReadBytes(chunkSize - 16);
                        }

                        fmtChunkFound = true;
                        Debug.WriteLine($"[ReadWavHeader] Format chunk found - Channels: {info.ChannelCount}, SampleRate: {info.SampleRate}, Bits: {info.BitsPerSample}");
                    }
                    else if (chunkId == "data")
                    {
                        // Record data chunk position and size
                        info.AudioDataStartPosition = _fileStream.Position;
                        info.AudioDataSize = chunkSize;
                        dataChunkFound = true;
                        Debug.WriteLine($"[ReadWavHeader] Data chunk found - Size: {chunkSize}, Position: {info.AudioDataStartPosition}");
                    }
                    else
                    {
                        // Skip unknown chunks
                        _fileStream.Seek(chunkSize, SeekOrigin.Current);
                    }

                    // Handle padding (word-aligned)
                    if (chunkSize % 2 != 0)
                    {
                        _fileStream.Seek(1, SeekOrigin.Current);
                    }
                }

                if (!fmtChunkFound || !dataChunkFound)
                {
                    Debug.WriteLine("[ReadWavHeader] Missing fmt or data chunk");
                    return null;
                }

                return info;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReadWavHeader] Error parsing WAV header: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Selects a time range to read from. Only audio data between start and end TimeSpans will be read.
        /// Call this before reading to restrict the read range. Use Select(TimeSpan.Zero, TimeSpan.MaxValue) to reset.
        /// </summary>
        /// <param name="start">Start time offset</param>
        /// <param name="end">End time offset</param>
        public void Select(TimeSpan start, TimeSpan end)
        {
            if (FormatInfo == null || _fileStream == null)
            {
                Debug.WriteLine("[AudioFileReaderM] FormatInfo or FileStream is null");
                return;
            }

            try
            {
                // Validate time range
                if (start < TimeSpan.Zero)
                {
                    start = TimeSpan.Zero;
                }
                if (end <= start)
                {
                    Debug.WriteLine("[AudioFileReaderM] Select: end time must be greater than start time");
                    return;
                }

                long bytesPerSample = Channels * (BitsPerSample / 8);
                
                // Convert time offsets to byte positions
                long startBytes = (long)(start.TotalSeconds * SampleRate * bytesPerSample);
                long endBytes = (long)(end.TotalSeconds * SampleRate * bytesPerSample);

                // Clamp to valid audio data range
                startBytes = Math.Max(0, Math.Min(FormatInfo.AudioDataSize, startBytes));
                endBytes = Math.Max(0, Math.Min(FormatInfo.AudioDataSize, endBytes));

                // Seek to start position
                _fileStream.Seek(FormatInfo.AudioDataStartPosition + startBytes, SeekOrigin.Begin);
                _currentPosition = (int)startBytes;
                _selectionStartPosition = (int)startBytes;
                _selectionEndPosition = (int)endBytes;

                Debug.WriteLine($"[AudioFileReaderM] Selected range: {start:hh\\:mm\\:ss\\.fff} to {end:hh\\:mm\\:ss\\.fff}");
                Debug.WriteLine($"[AudioFileReaderM] Selection byte range: {startBytes} to {endBytes} ({endBytes - startBytes} bytes)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioFileReaderM] Select error: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets the selection to read the entire file from start to end
        /// </summary>
        public void ResetSelection()
        {
            if (FormatInfo == null)
            {
                Debug.WriteLine("[AudioFileReaderM] FormatInfo is null");
                return;
            }

            _selectionStartPosition = 0;
            _selectionEndPosition = -1;
            Debug.WriteLine("[AudioFileReaderM] Selection reset to entire file");
        }

        /// <summary>
        /// Seeks to a specific time offset in the audio file
        /// </summary>
        public void Seek(TimeSpan timeOffset)
        {
            if (FormatInfo == null || _fileStream == null)
            {
                Debug.WriteLine("[AudioFileReaderM] FormatInfo or FileStream is null");
                return;
            }

            try
            {
                long bytesPerSample = Channels * (BitsPerSample / 8);
                long seekBytes = (long)(timeOffset.TotalSeconds * SampleRate * bytesPerSample);
                long seekPosition = FormatInfo.AudioDataStartPosition + seekBytes;

                // Clamp to valid range
                seekPosition = Math.Max(FormatInfo.AudioDataStartPosition, 
                    Math.Min(FormatInfo.AudioDataStartPosition + FormatInfo.AudioDataSize, seekPosition));

                _fileStream.Seek(seekPosition, SeekOrigin.Begin);
                _currentPosition = (int)(seekPosition - FormatInfo.AudioDataStartPosition);

                Debug.WriteLine($"[AudioFileReaderM] Seeked to {timeOffset:hh\\:mm\\:ss}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioFileReaderM] Seek error: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads audio data into the provided buffer respecting the selected range
        /// </summary>
        public int Read(float[] buffer)
        {
            if (FormatInfo == null || _reader == null || _fileStream == null)
            {
                Debug.WriteLine("[AudioFileReaderM] FormatInfo, Reader, or FileStream is null");
                return 0;
            }

            try
            {
                // Determine the effective end position (selection end or file end)
                int effectiveEndPosition = _selectionEndPosition >= 0 
                    ? _selectionEndPosition 
                    : FormatInfo.AudioDataSize;

                // Check if we've reached the end of the selected range
                if (_currentPosition >= effectiveEndPosition)
                {
                    return 0;
                }

                int bytesPerSample = Channels * (BitsPerSample / 8);
                int bytesAvailable = effectiveEndPosition - _currentPosition;
                int samplesAvailable = bytesAvailable / bytesPerSample;
                int samplesToRead = Math.Min(buffer.Length, samplesAvailable);

                if (samplesToRead <= 0)
                {
                    return 0;
                }

                // Read samples based on bit depth
                int samplesRead = 0;
                switch (BitsPerSample)
                {
                    case 8:
                        samplesRead = Read8BitSamples(buffer, samplesToRead);
                        break;
                    case 16:
                        samplesRead = Read16BitSamples(buffer, samplesToRead);
                        break;
                    case 24:
                        samplesRead = Read24BitSamples(buffer, samplesToRead);
                        break;
                    case 32:
                        samplesRead = Read32BitSamples(buffer, samplesToRead);
                        break;
                    default:
                        Debug.WriteLine($"[AudioFileReaderM] Unsupported bit depth: {BitsPerSample}");
                        return 0;
                }

                _currentPosition += samplesRead * bytesPerSample;
                return samplesRead;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioFileReaderM] Read error: {ex.Message}");
                return 0;
            }
        }

        private int Read8BitSamples(float[] buffer, int sampleCount)
        {
            int samplesRead = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                byte sample = _reader!.ReadByte();
                buffer[i] = (sample - 128) / 128f;
                samplesRead++;
            }
            return samplesRead;
        }

        private int Read16BitSamples(float[] buffer, int sampleCount)
        {
            int samplesRead = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = _reader!.ReadInt16();
                buffer[i] = sample / 32768f;
                samplesRead++;
            }
            return samplesRead;
        }

        private int Read24BitSamples(float[] buffer, int sampleCount)
        {
            int samplesRead = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                byte[] bytes = _reader!.ReadBytes(3);
                int sample = bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
                if ((sample & 0x800000) != 0) sample |= unchecked((int)0xFF000000);
                buffer[i] = sample / 8388608f;
                samplesRead++;
            }
            return samplesRead;
        }

        private int Read32BitSamples(float[] buffer, int sampleCount)
        {
            int samplesRead = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                int sample = _reader!.ReadInt32();
                buffer[i] = sample / 2147483648f;
                samplesRead++;
            }
            return samplesRead;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Debug.WriteLine("[AudioFileReaderM] Disposing resources");
                    _reader?.Dispose();
                    _reader = null;
                    _fileStream?.Dispose();
                    _fileStream = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
