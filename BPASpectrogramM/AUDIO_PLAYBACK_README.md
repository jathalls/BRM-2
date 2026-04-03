# Audio Playback Sample Rate Manipulation

## Overview
This implementation adds proper sample rate manipulation for audio playback in BPASpectrogramM, allowing playback at very slow speeds (down to 1/10 or slower) by changing the apparent sample rate rather than just using playback rate.

## Problem
The original implementation used AVPlayer (MediaElement) with rate changes, but AVPlayer's rate property doesn't actually change the sample rate - it just speeds up or slows down playback. This doesn't work well for very slow speeds like 0.1x (1/10 speed).

## Solution
Implemented platform-specific audio playback services that properly manipulate the sample rate:

### iOS & MacCatalyst
- Uses `AVAudioEngine` with `AVAudioUnitTimePitch` 
- `AVAudioUnitTimePitch.Rate` property allows playback speed from 0.03125x to 32x
- Maintains original pitch while changing playback speed
- Properly handles audio segments with precise time ranges

### Android & Windows
- Placeholder implementations provided
- Can be enhanced with platform-specific APIs:
  - Android: `SoundPool` or `AudioTrack` with custom sample rate
  - Windows: `NAudio` or Windows Media Foundation APIs

## Key Components

### 1. IAudioPlaybackService (Cross-platform interface)
```csharp
public interface IAudioPlaybackService
{
    void LoadSegment(string filePath, TimeSpan startOffset, TimeSpan endOffset, WavFormatInfo format);
    void Play(double speedFactor, double volume);
    void Pause();
    void Stop();
    double GetPosition();
    bool IsPlaying { get; }
    event EventHandler? PlaybackEnded;
    void Dispose();
}
```

### 2. Platform-Specific Implementations
- **iOS**: `Platforms/iOS/PlatformClass1.cs` - AudioPlaybackService using AVAudioEngine
- **MacCatalyst**: `Platforms/MacCatalyst/PlatformClass1.cs` - Same implementation as iOS
- **Android**: `Platforms/Android/PlatformClass1.cs` - Placeholder
- **Windows**: `Platforms/Windows/PlatformClass1.cs` - Placeholder

### 3. AudioPlayer Integration
The `AudioPlayer.xaml.cs` now:
- Initializes platform-specific audio service on startup
- Uses native audio engine for slow playback (0.1x, 0.2x, etc.)
- Falls back to MediaElement for heterodyne mode
- Handles both playback systems seamlessly

## Usage

The speed selector in the UI supports:
- **1.0x**: Normal speed (can use either system)
- **0.2x**: 1/5 speed (uses native audio engine)
- **0.1x**: 1/10 speed (uses native audio engine)
- **0.05x**: 1/20 speed (uses native audio engine)
- **heterodyne**: Special heterodyne processing mode (uses MediaElement)

## Technical Details

### AVAudioUnitTimePitch
The iOS/Mac implementation uses `AVAudioUnitTimePitch` which:
- Supports rate from 0.03125 (1/32) to 32.0 (32x)
- Setting `Pitch = 0` maintains original pitch
- Works with `AVAudioEngine` node graph: PlayerNode -> TimePitch -> MainMixer

### Buffer Management
- Reads audio segment into `AVAudioPCMBuffer`
- Schedules buffer for playback with completion handler
- Tracks position independently with timer

### Position Tracking
- Uses system timer (100ms interval) to track playback position
- Calculates position based on elapsed time * speed factor
- Fires PlaybackEnded event when segment completes

## Benefits

1. **True Slow Playback**: Plays audio at 1/10 speed or slower while maintaining quality
2. **Pitch Preservation**: Original pitch is maintained regardless of playback speed
3. **Platform-Optimized**: Uses native APIs for best performance and quality
4. **Backward Compatible**: Falls back to MediaElement when needed
5. **Extensible**: Easy to add implementations for other platforms

## Future Enhancements

### Android
Implement using `AudioTrack` with custom buffer management:
```csharp
// Resample audio data to lower sample rate
// Feed to AudioTrack with original sample rate
// Achieves time stretching effect
```

### Windows  
Implement using NAudio's `VarispeedSampleProvider`:
```csharp
// Use NAudio library for audio manipulation
// Supports time stretching and pitch shifting
```

### Additional Features
- Real-time heterodyne processing through AVAudioEngine
- Spectral processing effects
- Audio filters and EQ
