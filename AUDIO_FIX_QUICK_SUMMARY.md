# Audio Playback Error -17913 - FIX SUMMARY

## Issue
Your audio player was encountering error `-17913` (AVFoundation error on macOS) when trying to play audio segments, with messages like:
```
[AudioPlayer] MediaElement failed to open media: The operation could not be completed - An unknown error occurred (-17913)
[AudioPlayer] Attempted source: File: file:///Users/justinHalls/Library/Containers/com.companyname.brm2/Data/Library/Caches/audio_segments/segment_xxx.wav
```

## Root Cause
The WAV files being created had **invalid or incomplete header information**, which prevented macOS's AVFoundation framework from reading them. Specifically:

1. ❌ **Missing RIFF chunk size calculation** - The critical `chunkSize` field was never being set
2. ❌ **stale byteRate and blockAlign** - These weren't recalculated when sample rates changed
3. ❌ **Missing channel/bit information** - Source file format wasn't properly propagated to header
4. ❌ **File not fully written** - No flush or sync delay before playback attempt
5. ❌ **Poor error diagnostics** - Hard to identify WAV file corruption

## Changes Made

### 1. **WavFileHeader.cs** (Line 43-53)
```csharp
// Now properly calculates and sets:
chunkSize = 36 + dataChunkSize;
byteRate = sampleRate * numChannels * bitsPerSample / 8;
blockAlign = (short)(numChannels * bitsPerSample / 8);
```

### 2. **AudioPlayer.xaml.cs - CreateSegmentFile()** 
- ✅ Added channel count initialization
- ✅ Added bit depth initialization  
- ✅ Added explicit `Flush()` call
- ✅ Added 50ms disk-write delay
- ✅ Added file size validation (minimum 44 bytes)
- ✅ Added improved URI conversion for macOS

### 3. **AudioPlayer.xaml.cs - ValidateWavFile()** (New method)
Validates WAV files before playback:
- Checks RIFF header
- Checks WAVE header
- Verifies fmt chunk exists
- Logs detailed format information
- Provides diagnostic output on failure

### 4. **AudioPlayer.xaml.cs - MediaFailed event handler**
Enhanced with:
- Better file path extraction
- WAV header validation on playback failure
- Detailed diagnostic logging

## What to Expect Now

When playing audio, you should see logs like:
```
[AudioPlayer] Writing new WAV header to segment file with Sample Rate: 38400, Channels: 1, Bits: 16, Data Chunk Size: xxxxx
[AudioPlayer] Segment file created successfully: /path/to/segment_xxx.wav
[AudioPlayer] Segment file verified: xxxxx bytes
[AudioPlayer.ValidateWavFile] Format: 1, Channels: 1, Sample Rate: 38400, Bits: 16
[AudioPlayer.ValidateWavFile] WAV file is valid!
[AudioPlayer] MediaElement opened media successfully
```

## If Issues Persist

If you still see error -17913:

1. **Check the logs** - Look for "WAV file is valid!" message
2. **Verify file sizes** - Data chunk size should match calculated size
3. **Test with different speeds** - Try 1.0x first to isolate speed-related issues
4. **Check file permissions** - Cache directory should be accessible

## Testing

To verify the fix:
1. Load an audio segment
2. Try playing at normal speed (1.0x) ✅
3. Try different speed settings (0.2x, 0.1x, etc.) ✅
4. Try heterodyne mode ✅
5. Monitor the debug output for "WAV file is valid!" ✅

## Files Modified

- `/Users/justinHalls/RiderProjects/BRM-2/BPASpectrogramM/WavFileHeader.cs`
- `/Users/justinHalls/RiderProjects/BRM-2/BPASpectrogramM/Views/AudioPlayer.xaml.cs`

All changes maintain backward compatibility and improve error diagnostics.
