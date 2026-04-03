# WAV File Format Reference and Debugging Guide

## WAV File Structure (Standard PCM)

Every WAV file has this structure:
```
Offset  Size  Field           Value       Description
------  ----  -----           -----       -----------
0       4     ChunkID         "RIFF"      ASCII: R I F F
4       4     ChunkSize       N-8         File size minus 8 bytes
8       4     Format          "WAVE"      ASCII: W A V E

12      4     Subchunk1ID     "fmt "      ASCII: f m t + space
16      4     Subchunk1Size   16          Size of fmt chunk (16 for PCM)
20      2     AudioFormat     1           1 = PCM (uncompressed)
22      2     NumChannels     1 or 2      1=Mono, 2=Stereo, etc.
24      4     SampleRate      44100       Samples per second
28      4     ByteRate        xxx         = SampleRate * NumChannels * BitsPerSample / 8
32      2     BlockAlign      xxx         = NumChannels * BitsPerSample / 8
34      2     BitsPerSample   16 or 8    Bits per sample
36      4     Subchunk2ID     "data"      ASCII: d a t a
40      4     Subchunk2Size   N           Actual audio data size in bytes

44      N     AudioData       xxx...      Raw PCM audio samples
```

## Key Formulas

### 1. ChunkSize (must be correct for AVFoundation!)
```
ChunkSize = 36 + Subchunk2Size
```
This represents the file size MINUS 8 bytes (for ChunkID and ChunkSize fields themselves).

**CRITICAL**: This was the main bug! It was being left as 0.

### 2. ByteRate (affects playback timing)
```
ByteRate = SampleRate × NumChannels × BitsPerSample ÷ 8
```
Example: 
- 16kHz mono 16-bit: 16000 × 1 × 16 ÷ 8 = 32000 bytes/sec
- 44.1kHz stereo 16-bit: 44100 × 2 × 16 ÷ 8 = 176400 bytes/sec

### 3. BlockAlign (must match format)
```
BlockAlign = NumChannels × BitsPerSample ÷ 8
```
Example:
- Mono 16-bit: 1 × 16 ÷ 8 = 2 bytes per sample
- Stereo 16-bit: 2 × 16 ÷ 8 = 4 bytes per sample

### 4. Duration (calculated from audio data)
```
Duration (seconds) = Subchunk2Size ÷ ByteRate
Duration (seconds) = Subchunk2Size ÷ (SampleRate × NumChannels × BitsPerSample ÷ 8)
```

## Common WAV File Sizes

### 1 second of audio:
- 8kHz mono 8-bit: 8,000 bytes
- 16kHz mono 16-bit: 32,000 bytes
- 44.1kHz mono 16-bit: 88,200 bytes
- 44.1kHz stereo 16-bit: 176,400 bytes
- 384kHz mono 16-bit: 768,000 bytes

## Validation Checklist

When debugging WAV files, verify:

1. ✓ **RIFF Header Valid**
   ```
   bytes[0:4] == "RIFF" (0x52494646)
   ```

2. ✓ **ChunkSize is Correct**
   ```
   chunkSize = (FileSize - 8)
   chunkSize == (36 + dataSize)
   ```

3. ✓ **WAVE Header Valid**
   ```
   bytes[8:12] == "WAVE" (0x57415645)
   ```

4. ✓ **fmt Chunk Found**
   ```
   bytes[12:16] == "fmt " (0x666d7420)
   ```

5. ✓ **Audio Format Supported**
   ```
   bytes[20:22] == 1 (PCM format)
   ```

6. ✓ **ByteRate Calculated Correctly**
   ```
   byteRate == sampleRate * channels * bitsPerSample / 8
   ```

7. ✓ **BlockAlign Calculated Correctly**
   ```
   blockAlign == channels * bitsPerSample / 8
   ```

8. ✓ **data Chunk Found**
   ```
   bytes[36:40] == "data" (0x64617461)
   ```

## Debugging Commands (macOS)

### 1. View WAV Header in Hex
```bash
hexdump -C audio.wav | head -20
```

### 2. Check File Size
```bash
ls -lh audio.wav
```

### 3. Use FFprobe to Verify
```bash
ffprobe -v error -show_format -show_streams audio.wav
```

### 4. Convert and Fix WAV Files
```bash
ffmpeg -i corrupted.wav -acodec pcm_s16le -ar 44100 fixed.wav
```

## Error Code Reference

### AVFoundation Error Codes
- **-17913**: General format/file error (most common)
- **-17913**: File corruption or invalid header
- **-17913**: Sample rate mismatch
- **-3000**: File not found
- **-4**: Invalid parameter

## Testing with Different Sample Rates

The app supports multiple sample rates. When changing sample rate, ensure:

1. New SampleRate is properly set in header
2. ByteRate is recalculated: `ByteRate = SampleRate * Channels * Bits / 8`
3. BlockAlign doesn't change (depends on channels and bits, not sample rate)
4. ChunkSize is recalculated with new data size
5. Playback speed factor is properly applied

## Speed Modification Impact

When reducing playback speed (e.g., 0.1x), the audio data is stretched by:
1. Modifying the sample rate in the header: `NewSampleRate = OriginalSampleRate × SpeedFactor`
2. Keeping the audio data unchanged
3. This makes the player read data slower, resulting in slower playback

Example: 16kHz file at 0.1x:
- Original: SampleRate = 16000 Hz
- Modified: SampleRate = 1600 Hz
- Playback duration: 10x longer

## Heterodyne Mode

In heterodyne mode (frequency shift):
1. Audio data is processed to shift frequency
2. Sample rate is kept at a low value for slowed playback
3. The frequency-shifted audio data is written to file
4. This allows hearing high-frequency signals as audible frequencies

## Files to Monitor During Debugging

Location: `~/Library/Containers/com.companyname.brm2/Data/Library/Caches/audio_segments/`

Each file is named: `segment_{GUID}.wav`

To examine:
```bash
ls -lah ~/Library/Containers/com.companyname.brm2/Data/Library/Caches/audio_segments/
hexdump -C ~/Library/Containers/com.companyname.brm2/Data/Library/Caches/audio_segments/segment_*.wav | head -20
```

## Performance Notes

- Files are created in cache directory (temporary, may be cleared)
- Files should be cleaned up after playback stops
- Large audio files may take time to create the segment
- The 50ms delay after file creation is needed for macOS disk sync

## Future Enhancements

1. Add support for compressed WAV formats (ADPCM, etc.)
2. Implement audio resampling if needed
3. Add ID3 tag support
4. Implement streaming instead of pre-creating segments
5. Add support for other formats (MP3, OGG, FLAC)
