# Audio Playback Implementation - Summary of Changes

## Date
March 10, 2026

## Problem Statement
The BPASpectrogramM audio player was using AVPlayer (MediaElement) with rate changes for slow playback. However, AVPlayer's rate property doesn't actually change the sample rate - it just speeds up or slows down playback, which doesn't work well for very slow speeds like 0.1x (1/10 speed).

## Solution Implemented
Created a platform-specific audio playback system using native APIs that properly manipulate the sample rate for true slow-motion audio playback.

---

## Files Created

### 1. IAudioPlaybackService.cs
**Location:** `/BPASpectrogramM/IAudioPlaybackService.cs`

Cross-platform interface defining the contract for audio playback services:
- `LoadSegment()` - Load audio file segment
- `Play()` - Play with speed factor and volume
- `Pause()` / `Stop()` - Playback control
- `GetPosition()` - Current position tracking
- `IsPlaying` - Playback state
- `PlaybackEnded` event - Completion notification

### 2. Platform-Specific Implementations

#### iOS Implementation
**Location:** `/BPASpectrogramM/Platforms/iOS/PlatformClass1.cs`

Features:
- Uses `AVAudioEngine` for audio processing
- Uses `AVAudioPlayerNode` for playback control
- Uses `AVAudioUnitTimePitch` for sample rate manipulation
- Supports speed range: 0.03125x to 32x (1/32 to 32 times)
- Maintains original pitch while changing speed
- Precise position tracking with timer
- Proper resource cleanup

#### MacCatalyst Implementation
**Location:** `/BPASpectrogramM/Platforms/MacCatalyst/PlatformClass1.cs`

Identical to iOS implementation, optimized for Mac platform.

#### Android Implementation
**Location:** `/BPASpectrogramM/Platforms/Android/PlatformClass1.cs`

Placeholder implementation for future enhancement.
Recommendations:
- Use `AudioTrack` with custom buffer management
- Implement sample rate conversion
- Consider `SoundPool` for short sounds

#### Windows Implementation
**Location:** `/BPASpectrogramM/Platforms/Windows/PlatformClass1.cs`

Placeholder implementation for future enhancement.
Recommendations:
- Use NAudio library's `VarispeedSampleProvider`
- Implement Windows Media Foundation APIs
- Consider WASAPI for low-latency playback

### 3. Documentation
**Location:** `/BPASpectrogramM/AUDIO_PLAYBACK_README.md`

Comprehensive documentation covering:
- Problem description
- Solution architecture
- Technical details
- Usage instructions
- Platform-specific notes
- Future enhancement suggestions

---

## Files Modified

### AudioPlayer.xaml.cs
**Location:** `/BPASpectrogramM/Views/AudioPlayer.xaml.cs`

**Key Changes:**

1. **Added Fields:**
   ```csharp
   private IAudioPlaybackService? audioPlaybackService;
   private bool useNativeAudioEngine = true;
   ```

2. **New Initialization:**
   - `InitializeAudioServices()` - Creates platform-specific audio service
   - `OnAudioPlaybackEnded()` - Handles playback completion events

3. **Enhanced LoadSegment():**
   - Loads audio into both native service and MediaElement
   - Supports dual-mode operation (native + fallback)

4. **Refactored PlayAudioAsync():**
   - Routes to appropriate playback method based on mode
   - Heterodyne mode → MediaElement
   - Slow playback → Native audio engine
   - Fallback → MediaElement

5. **New Methods:**
   - `PlayWithNativeAudioEngine()` - Uses platform-specific service
   - `PlayWithMediaElement()` - Uses MediaElement (refactored from original)

6. **Updated Control Methods:**
   - `btnPause_Clicked()` - Controls both services
   - `StopPlayback()` - Stops both services
   - `GetPosition()` - Gets position from active service
   - `Dispose()` - Cleans up both services

---

## Technical Architecture

### Audio Processing Chain (iOS/Mac)

```
Audio File
    ↓
AVAudioFile (segment loaded into buffer)
    ↓
AVAudioPCMBuffer
    ↓
AVAudioPlayerNode
    ↓
AVAudioUnitTimePitch (rate: 0.03125 - 32.0, pitch: 0)
    ↓
AVAudioEngine.MainMixerNode
    ↓
Audio Output
```

### Playback Mode Selection

```
User selects speed
    ↓
    ├── "heterodyne" → MediaElement
    │                  (for heterodyne processing)
    │
    ├── 0.05x - 1.0x → Native Audio Engine
    │                  (for sample rate manipulation)
    │
    └── Fallback     → MediaElement
                       (if native engine fails)
```

---

## Speed Options

The UI speed selector (`cmbSpeed`) supports:

| Option | Speed | Engine Used | Notes |
|--------|-------|-------------|-------|
| 1.0x | Normal | Either | Full speed |
| 0.2x | 1/5 speed | Native | True slow motion |
| 0.1x | 1/10 speed | Native | Very slow, maintains quality |
| 0.05x | 1/20 speed | Native | Ultra slow |
| heterodyne | Variable | MediaElement | Special processing mode |

---

## Benefits of New Implementation

1. **True Slow Playback**
   - Properly slows down audio by manipulating sample rate
   - Works down to 1/32 speed (0.03125x)
   - Maintains audio quality

2. **Pitch Preservation**
   - Original pitch maintained at all speeds
   - No "chipmunk" or "demon" effects

3. **Platform Optimization**
   - Uses native APIs for best performance
   - Leverages hardware acceleration where available

4. **Backward Compatibility**
   - Falls back to MediaElement when needed
   - Maintains heterodyne mode functionality
   - Graceful degradation on unsupported platforms

5. **Extensibility**
   - Interface-based design allows easy platform additions
   - Clear separation of concerns
   - Easy to test and maintain

---

## Testing Recommendations

### On iOS/MacCatalyst:
1. Test playback at 0.1x speed - should play at 1/10 speed with clear audio
2. Test playback at 0.05x speed - should play at 1/20 speed
3. Verify pitch remains unchanged at all speeds
4. Test pause/resume functionality
5. Test stop and position reset
6. Verify proper cleanup on disposal

### On Android/Windows:
1. Currently falls back to MediaElement
2. Test that playback still works (may not achieve true slow speed)
3. Implement native solutions for production use

### General:
1. Test heterodyne mode still works
2. Test segment looping
3. Test volume control
4. Test playback position tracking
5. Test rapid play/pause/stop sequences
6. Memory leak testing (play/stop many times)

---

## Future Enhancements

### High Priority:
1. Implement Android native audio playback using AudioTrack
2. Implement Windows native audio using NAudio
3. Add real-time heterodyne processing through AVAudioEngine

### Medium Priority:
1. Add audio filters and EQ
2. Add spectral processing effects
3. Support for additional audio formats
4. Waveform visualization during playback

### Low Priority:
1. Record slow-motion output
2. Export processed audio
3. Batch processing capabilities

---

## Known Limitations

1. **Android/Windows:** Currently use placeholder implementations (fall back to MediaElement)
2. **Very Slow Speeds:** Below 0.03125x not supported on iOS/Mac
3. **Memory Usage:** Loading large segments into memory (consider streaming for very long files)
4. **Format Support:** Currently optimized for WAV files

---

## Build Configuration

The project uses .NET MAUI's single project structure:
- Platform-specific code in `Platforms/` folders
- Automatically compiled only for target platform
- No conditional compilation needed
- Interface provides cross-platform abstraction

---

## Support & Maintenance

For issues or questions:
1. Check Debug console for detailed logging
2. All methods include comprehensive debug output
3. Each platform prefixes logs with platform name
4. Position tracking and state changes are logged

## Version
Initial implementation - March 10, 2026
