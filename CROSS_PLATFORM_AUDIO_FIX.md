# Cross-Platform Audio Playback Fix

## Problem Summary
MediaElement was loading the file correctly but failing to play audio on macOS and Windows. The file is valid (can be played by Apple Player) but the Play() command was not working.

## Root Causes Analyzed

1. **Timing Issues**: AVPlayer on macOS needs proper initialization time
2. **File Path Format**: Different platforms require different file path formats
3. **MediaSource Creation**: `MediaSource.FromFile()` needs to handle both file paths and file URIs correctly
4. **Event Synchronization**: Need to wait for MediaOpened before attempting playback

## Solutions Implemented

### 1. Improved File Path Handling
**Location**: CreateSegmentFile() method

Changed from platform-conditional logic to a universal approach:
```csharp
// Create proper file URI that works on all platforms
try
{
    // Use Uri class to properly format the file path
    Uri fileUriObj = new Uri(new FileInfo(segmentFile).FullName);
    fileUri = fileUriObj.AbsoluteUri;
    Debug.WriteLine($"[AudioPlayer] Converted to file URI: {fileUri}");
}
catch (Exception ex)
{
    // Fallback: ensure proper file:// protocol
    if (!fileUri.StartsWith("file://"))
    {
        fileUri = "file://" + segmentFile;
    }
}
```

**Why**: Both macOS and Windows benefit from proper URI formatting using the System.Uri class, which ensures path encoding and protocol handling.

### 2. Enhanced MediaSource Creation
**Location**: currentSegmentFile property setter

Now handles three cases:
```csharp
// 1. If it's already a file URI, use it as-is
if (value.StartsWith("file://"))
{
    pathToUse = value;
}
// 2. If it's a file path that exists, use directly
else if (File.Exists(value))
{
    pathToUse = value;
}
// 3. Otherwise, try to construct a proper URI
else
{
    Uri uri = new Uri(new FileInfo(value).FullName);
    pathToUse = uri.AbsoluteUri;
}

MediaSourceFile = MediaSource.FromFile(pathToUse);
```

**Why**: This approach is more robust and handles both relative and absolute paths, as well as URIs.

### 3. Proper MediaOpened Event Waiting
**Location**: btnPlay_Clicked() method

Now properly waits for MediaOpened:
```csharp
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
}
else
{
    Debug.WriteLine("[AudioPlayer] MediaOpened event received");
}

// Additional wait to ensure everything is initialized
await Task.Delay(300);
```

**Why**: Ensures AVPlayer (on macOS) has fully initialized before attempting playback.

### 4. Improved PlayWithRetry() Logic
**Location**: PlayWithRetry() method

Enhanced with:
- More attempts (5 instead of 3)
- Better logging at each step
- Exponential backoff with a cap of 2 seconds
- Checks IsLoaded and Source state before each attempt
- Comprehensive error reporting

```csharp
Debug.WriteLine($"[Play] Attempt {attempt}/{maxAttempts}: Calling mediaElement.Play()");
Debug.WriteLine($"[Play] Current state - IsLoaded: {mediaElement.IsLoaded}, Source is null: {mediaElement.Source == null}");

mediaElement.Play();

Debug.WriteLine($"[Play] Attempt {attempt}: Play() returned successfully");
Debug.WriteLine($"[Play] After Play() - Position: {mediaElement.Position}, IsPlaying: {mediaElement.IsPlaying}");
```

**Why**: Provides detailed diagnostics to identify where the playback fails and allows retries for initialization timing issues.

## Cross-Platform Compatibility

### macOS
- Uses System.Uri.AbsoluteUri for proper file path encoding
- MediaSource.FromFile() handles both paths and URIs
- MediaOpened event ensures AVPlayer is initialized
- 5-second timeout allows for slower initialization

### Windows
- System.Uri handles Windows file path format correctly
- file:// protocol is properly formatted
- Same retry logic and timeouts apply

## Expected Behavior After Fixes

1. **Load Audio Segment**
   - File created in cache directory
   - Console: `[AudioPlayer] Converted to file URI: file:///path/to/file.wav`
   - Console: `[AudioPlayer] MediaSource created successfully from: file:///path/to/file.wav`

2. **Click Play**
   - Console: `[AudioPlayer] Waiting for MediaOpened event...`
   - Console: `[AudioPlayer] MediaOpened event received` OR `[AudioPlayer] WARNING: MediaOpened timeout`
   - Console: `[Play] Attempt 1/5: Calling mediaElement.Play()`
   - Console: `[Play] Attempt 1: Play() returned successfully`
   - Console: `[Play] After Play() - Position: 00:00:00, IsPlaying: True`

3. **Audio Plays**
   - Position updates appear in console
   - Timer events log position changes
   - Playback continues until end or stopped

## Debugging Guide

### If Audio Still Doesn't Play

#### Check 1: File Creation
```
[AudioPlayer] Creating segment file: /path/to/segment_xxxx.wav
[AudioPlayer] Segment file verified: 12345 bytes
```

#### Check 2: MediaSource Creation
```
[AudioPlayer] Converted to file URI: file:///path/to/segment_xxxx.wav
[AudioPlayer] MediaSource created successfully from: file:///path/to/segment_xxxx.wav
```

#### Check 3: Play Attempt
```
[AudioPlayer] Waiting for MediaOpened event...
[AudioPlayer] MediaOpened event received
[Play] Attempt 1/5: Calling mediaElement.Play()
[Play] Attempt 1: Play() returned successfully
```

### If Seeing Errors

Look for:
- `[AudioPlayer] Error creating MediaSource from`: File path/URI issue
- `[Play] Attempt X failed`: Play command execution issue
- `[AudioPlayer] Timer error`: Position tracking issue

## Platform Testing Checklist

- [ ] **macOS**: Load segment, click Play, verify audio plays
- [ ] **Windows**: Load segment, click Play, verify audio plays
- [ ] **Pause/Resume**: Verify state management works
- [ ] **Speed Changes**: Verify playback continues after speed change
- [ ] **Long Files**: Verify playback works for longer duration files
- [ ] **Multiple Segments**: Verify loading different segments works

## Files Modified

- `/Users/justinHalls/RiderProjects/BRM-2/BPASpectrogramM/Views/AudioPlayer.xaml.cs`

## Key Changes Summary

| Area | Before | After |
|------|--------|-------|
| File URI | Platform-conditional conversion | Universal System.Uri approach |
| MediaSource Creation | Simple FromFile() | Handles paths, URIs, and fallbacks |
| Event Waiting | Polling for Source | Proper TaskCompletionSource for MediaOpened |
| Retry Logic | 3 attempts, simple backoff | 5 attempts, detailed logging, state checks |
| Error Handling | Generic exceptions | Specific error types and diagnostics |

