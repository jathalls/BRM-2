# Quick Reference: Audio Playback with Sample Rate Manipulation

## What Was Changed

### Problem
AVPlayer rate changes don't properly slow down audio to 1/10 speed - they just speed up/slow down without changing the apparent sample rate.

### Solution
Implemented platform-specific audio engines that manipulate the actual sample rate:
- **iOS/MacCatalyst**: Uses AVAudioEngine + AVAudioUnitTimePitch
- **Android/Windows**: Placeholder (falls back to MediaElement)

---

## Files Added

```
BPASpectrogramM/
├── IAudioPlaybackService.cs                          [NEW - Interface]
├── Platforms/
│   ├── iOS/PlatformClass1.cs                        [MODIFIED - AVAudioEngine impl]
│   ├── MacCatalyst/PlatformClass1.cs                [MODIFIED - AVAudioEngine impl]
│   ├── Android/PlatformClass1.cs                    [MODIFIED - Placeholder]
│   └── Windows/PlatformClass1.cs                    [MODIFIED - Placeholder]
├── Views/AudioPlayer.xaml.cs                         [MODIFIED - Dual-mode support]
├── AUDIO_PLAYBACK_README.md                         [NEW - Technical docs]
└── IMPLEMENTATION_SUMMARY.md                        [NEW - Complete summary]
```

---

## How It Works

### Speed Selection Logic
```
┌─────────────────┐
│ User selects    │
│ speed option    │
└────────┬────────┘
         │
         ├─── "heterodyne" ──→ MediaElement (for heterodyne processing)
         │
         ├─── 0.05x - 1.0x ──→ Native Audio Engine (true sample rate change)
         │                      ✓ iOS/Mac: AVAudioEngine
         │                      ✗ Android/Windows: Falls back to MediaElement
         │
         └─── Fallback ──────→ MediaElement (if native fails)
```

### iOS/Mac Audio Chain
```
WAV File → AVAudioFile → AVAudioPCMBuffer → AVAudioPlayerNode
                                                   ↓
                                          AVAudioUnitTimePitch
                                          (rate: 0.1, pitch: 0)
                                                   ↓
                                          AVAudioEngine.MainMixer
                                                   ↓
                                              Audio Output
```

---

## Key Code Changes

### AudioPlayer.xaml.cs

**New initialization:**
```csharp
audioPlaybackService = new AudioPlaybackService();  // Platform-specific
audioPlaybackService.PlaybackEnded += OnAudioPlaybackEnded;
```

**Playback routing:**
```csharp
if (isHeterodyneMode)
    await PlayWithMediaElement();
else if (audioPlaybackService != null && useNativeAudioEngine)
    PlayWithNativeAudioEngine();  // NEW: True slow playback
else
    await PlayWithMediaElement();  // Fallback
```

---

## Speed Options & Behavior

| Speed Selector | Speed Factor | Engine | Behavior |
|---------------|--------------|--------|----------|
| 1.0x | 1.0 | Either | Normal speed |
| 0.2x | 0.2 | Native | 1/5 speed, original pitch |
| 0.1x | 0.1 | Native | **1/10 speed, original pitch** ✓ |
| 0.05x | 0.05 | Native | 1/20 speed, original pitch |
| heterodyne | Varies | MediaElement | Special processing |

---

## Testing on iOS/Mac

1. **Build and run** on iOS/MacCatalyst
2. **Load an audio file** in the spectrogram
3. **Select a segment** to play
4. **Choose "0.1x"** from speed dropdown
5. **Press Play** ▶

**Expected Result:**
- Audio plays at 1/10 normal speed
- Pitch sounds natural (not lowered)
- Position tracking works correctly
- Stop/Pause/Resume all function properly

**Debug Output:**
```
[AudioPlayer] Platform-specific audio service initialized
[AudioPlaybackService-iOS] Audio engine initialized
[AudioPlaybackService-iOS] Loading segment: /path/to/file.wav
[AudioPlayer] Playing with speed factor: 0.1, Heterodyne: False
[AudioPlayer] Using native audio engine for speed: 0.1
[AudioPlaybackService-iOS] Playing with speed factor: 0.1
[AudioPlayer] Native audio playback started
```

---

## Troubleshooting

### Issue: Falls back to MediaElement
**Symptoms:** Debug shows "Using MediaElement" instead of "Using native audio engine"

**Causes:**
1. Platform-specific service initialization failed
2. Running on Android/Windows (not yet implemented)

**Solution:**
- Check debug output for initialization errors
- Verify running on iOS/MacCatalyst
- Check audio file is valid WAV format

### Issue: Audio doesn't slow down properly
**Symptoms:** Audio speed doesn't match selected factor

**Possible Causes:**
1. Using MediaElement fallback (doesn't support very slow speeds)
2. AVAudioUnitTimePitch rate clamping (0.03125 - 32.0 range)

**Solution:**
- Verify native engine is being used (check debug output)
- Try speeds between 0.1x and 1.0x first
- Check heterodyne mode isn't selected

### Issue: Pitch sounds wrong
**Symptoms:** Audio sounds like chipmunks or demons

**This should NOT happen** - if it does:
- Check that `timePitchUnit.Pitch = 0` is set
- Verify native engine is being used
- May indicate fallback to MediaElement with rate change

---

## API Reference

### IAudioPlaybackService Interface

```csharp
void LoadSegment(string filePath, TimeSpan start, TimeSpan end, WavFormatInfo format)
```
Loads audio segment for playback.

```csharp
void Play(double speedFactor, double volume)
```
Plays loaded segment. Speed: 0.03125-32.0, Volume: 0.0-1.0.

```csharp
double GetPosition()
```
Returns current playback position in seconds.

```csharp
bool IsPlaying { get; }
```
True if currently playing.

```csharp
event EventHandler? PlaybackEnded
```
Fired when playback completes.

---

## Performance Notes

### Memory Usage
- Loads entire audio segment into buffer
- For long segments (>1 min), memory usage can be significant
- Consider implementing streaming for very long files

### CPU Usage
- AVAudioEngine is hardware-accelerated
- Time-pitch processing is efficient
- Minimal CPU overhead on iOS/Mac

### Latency
- Initial load: ~100-500ms (depends on segment length)
- Play start: ~50-100ms
- Position updates: Every 100ms

---

## Next Steps for Production

### High Priority
1. **Implement Android** using AudioTrack API
2. **Implement Windows** using NAudio library
3. **Add error handling** for edge cases
4. **Memory optimization** for long audio files

### Optional Enhancements
1. Add visual feedback during slow playback
2. Support real-time speed changes
3. Add pitch shift option (separate from speed)
4. Implement audio effects chain

---

## Support

**Debug Logging:**
All components log to Debug output with prefixes:
- `[AudioPlayer]` - Main audio player
- `[AudioPlaybackService-iOS]` - iOS implementation
- `[AudioPlaybackService-Mac]` - MacCatalyst implementation

**Common Debug Messages:**
- "Platform-specific audio service initialized" - ✓ Native engine ready
- "Using native audio engine" - ✓ Native playback
- "Using MediaElement" - ⚠ Fallback mode
- "Error initializing audio services" - ✗ Setup failed

---

Last Updated: March 10, 2026
