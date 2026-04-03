# Fix for Audio Playback Error -17913 (AVFoundation Error on macOS)

## Problem
The MediaElement was failing to play WAV audio files with error -17913, which is an AVFoundation error on macOS that typically indicates:
- Invalid file format or corrupted WAV headers
- File permissions issues
- File not being fully written to disk before playback

## Root Causes Identified

### 1. **Missing WAV Header Information**
The `WavFileHeader.Write()` method was not setting the critical `chunkSize` field, which is required for valid WAV file headers. This field must contain the file size minus 8 bytes.

**Fix:** Updated `WavFileHeader.Write()` to:
- Calculate and set `chunkSize = 36 + dataChunkSize`
- Recalculate `byteRate` when sample rate changes
- Recalculate `blockAlign` to ensure consistency

### 2. **byteRate and blockAlign Not Updated**
When the sample rate was modified for playback speed changes, these fields were not recalculated, resulting in invalid WAV headers.

**Fix:** The `Write()` method now recalculates these fields:
```csharp
byteRate = sampleRate * numChannels * bitsPerSample / 8;
blockAlign = (short)(numChannels * bitsPerSample / 8);
```

### 3. **Missing Channel Count Information**
The WAV header wasn't being properly initialized with the source file's channel count.

**Fix:** Added proper channel count and bits per sample from source file:
```csharp
newHeader.numChannels = (Int16)sourceReader.Channels;
newHeader.bitsPerSample = (Int16)sourceReader.BitsPerSample;
```

### 4. **File Not Fully Flushed Before Playback**
Files were being closed immediately after writing without proper flushing, and no delay for disk write completion.

**Fix:** Added explicit `Flush()` and a 50ms delay:
```csharp
dest.Flush(); // Explicit flush
// FileStream closed and disposed here
System.Threading.Thread.Sleep(50); // Allow disk write to complete
```

### 5. **URI Formatting Issues on macOS**
The file URI conversion might not handle special characters or sandbox paths correctly.

**Fix:** Improved URI generation:
```csharp
Uri uri = new Uri(new FileInfo(fileUri).FullName);
fileUri = uri.AbsoluteUri;
```

## Changes Made

### File: `BPASpectrogramM/WavFileHeader.cs`
- Updated `Write()` method to properly calculate and set all header fields
- Ensures byteRate and blockAlign are recalculated based on current sample rate

### File: `BPASpectrogramM/Views/AudioPlayer.xaml.cs`
- Enhanced `CreateSegmentFile()` method with:
  - Proper channel count initialization
  - Explicit flush and disk sync delay
  - Better error diagnostics
  - File size validation (minimum 44 bytes)
  
- Added `ValidateWavFile()` helper method to:
  - Verify WAV file structure before playback
  - Check RIFF and WAVE headers
  - Validate fmt chunk exists
  - Log detailed format information
  
- Enhanced `MediaFailed` event handler with:
  - Better file path extraction
  - WAV header validation on failure
  - Detailed diagnostic logging

## Testing Recommendations

1. **Test with various audio files:**
   - Different sample rates (8000 Hz, 16000 Hz, 44100 Hz, 48000 Hz, 384000 Hz)
   - Different channel counts (mono, stereo)
   - Different bit depths (8-bit, 16-bit, 24-bit, 32-bit)

2. **Test speed variations:**
   - 1.0x normal speed
   - 0.2x slow speed
   - 0.1x very slow
   - 0.05x ultra-slow
   - Heterodyne mode

3. **Monitor logs:**
   - Check for "WAV file is valid!" messages
   - Verify file sizes match expected calculations
   - Confirm URI conversion succeeded

## Verification

The following debug logs indicate successful operation:
```
[AudioPlayer] Segment file created successfully: ...segment_xxx.wav
[AudioPlayer] Segment file verified: xxxx bytes
[AudioPlayer.ValidateWavFile] WAV file is valid!
[AudioPlayer] Converted path to URI: file://...
[AudioPlayer] MediaElement opened media successfully
```

If you see error logs, the audio file may still be corrupted or the URI format is incorrect.

## Additional Notes

- The error -17913 is specific to AVFoundation on macOS
- On iOS and Android, different audio frameworks are used
- The cache directory should be accessible within the app sandbox
- Files are automatically cleaned up when the audio player is disposed

## Future Improvements

1. Consider using a dedicated temp directory with explicit cleanup
2. Add audio codec detection and validation
3. Implement audio resampling if needed
4. Add support for other audio formats besides PCM WAV
