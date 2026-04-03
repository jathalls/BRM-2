# Audio Playback Testing Guide

## Expected Behavior After Fixes

### Scenario 1: Load Audio File and Play
1. Start the application
2. Load an audio file (via segment selection)
3. The segment is created in the cache directory
4. `MediaSourceFile` property is set with the file path
5. Click the Play button
6. Audio should play
7. Playhead should move forward
8. Timer updates should show position updates

### Scenario 2: Stop/Resume Playback
1. Click Play to start audio
2. Click Pause to pause playback
3. Playback should pause without error
4. Clicking Play again should resume from paused position

### Scenario 3: Speed Changes
1. Start playback
2. Change speed from dropdown
3. Audio should continue playing at new speed
4. No crashes or initialization errors

## Debug Output Expected

When you click Play, you should see in the console:

```
[AudioPlayer] Play button clicked
[AudioPlayer] Waiting for media to load...
[AudioPlayer] Still waiting for source... (1/10)
[AudioPlayer] Still waiting for source... (2/10)
[AudioPlayer] Media source detected: File: /path/to/cache/audio_segments/segment_xxxx.wav
[AudioPlayer] MediaSourceFile property value: /path/to/cache/audio_segments/segment_xxxx.wav
[AudioPlayer] MediaElement source is: File: /path/to/cache/audio_segments/segment_xxxx.wav
[AudioPlayer] MediaElement IsLoaded: True
[Play] Attempt 1/3: Playing without seek
[AudioPlayer] Play command sent to MediaElement successfully
```

## If Audio Still Doesn't Play

### Check 1: File Exists
- Verify the segment file was created in the cache directory
- Check the file size is not zero
- Confirm file path in logs exists on disk

### Check 2: MediaSourceFile Property
- Verify "MediaSourceFile property changed to:" appears in logs
- Check that the path is not empty or "File:"

### Check 3: Binding
- Verify "MediaElement source is:" shows the file path, not empty
- If it shows "File:" with no path, the binding didn't update properly

### Check 4: Play Command
- Verify "[AudioPlayer] Play command sent to MediaElement successfully" appears
- If you see exceptions, they'll be logged just before this message

## Debugging Steps

### Enable Verbose Logging
In AudioPlayer.xaml.cs, look for Debug.WriteLine calls. They're already in place.

### Check File Creation
Look for these debug messages:
```
[AudioPlayer] Creating segment file: [path]
[AudioPlayer] Segment file verified: [size] bytes
[AudioPlayer] About to set MediaSourceFile to: [path]
[AudioPlayer] File exists at this path: True
[AudioPlayer] File size: [size] bytes
[AudioPlayer] MediaSourceFile now set to: [path]
```

### Check Binding Update
Look for:
```
[AudioPlayer] MediaSourceFile property changed to: [path]
```

### Check MediaElement Loading
Look for:
```
[AudioPlayer] Media source detected: File: [path]
[AudioPlayer] MediaElement IsLoaded: True
```

## Common Issues and Solutions

| Issue | Expected Output | Debug | Solution |
|-------|-----------------|-------|----------|
| "File:" shows as source | "File: /path/to/file.wav" | Source is empty | File path not being set to MediaSourceFile |
| AVPlayer initialization error | No error | Play command fails immediately | Wait loop isn't detecting source properly |
| Audio doesn't start | Play command succeeds but no sound | Check file size is >0 | WAV file is corrupt or empty |
| Playhead doesn't move | Timer running but position = 0 | Position stays at 0 | Audio might be loading but not actually playing |

## Key Properties to Monitor

In the IDE debugger, add watches for:
- `MediaSourceFile` - should contain the file path
- `mediaElement.Source` - should show "File: [path]"
- `mediaElement.IsLoaded` - should be true before playing
- `mediaElement.Position` - should increase when playing
- `mediaElement.IsPlaying` - should be true during playback

