# If Audio Still Doesn't Play - Diagnostic Guide

## Step 1: Verify File Creation
Run the app and load a segment. Check console for:
```
[AudioPlayer] Creating segment file: /path/to/segment_xxxxx.wav
[AudioPlayer] Segment file verified: XXXXX bytes
```

**If you don't see these messages:**
- Segment creation is failing
- Check: `CreateSegmentFile()` error messages
- Verify: Source audio file is valid WAV format

**If you see them:**
- Proceed to Step 2

## Step 2: Verify MediaSource Creation
Check console for:
```
[AudioPlayer] Converted to file URI: file:///path/to/segment_xxxxx.wav
[AudioPlayer] Using file path directly: /path/to/segment_xxxxx.wav
[AudioPlayer] MediaSource created successfully from: ...
```

**If you see "Error creating MediaSource":**
- Problem: `MediaSource.FromFile()` failing
- This is a CommunityToolkit issue
- Try the alternative approach (see Solution A below)

**If no MediaSource messages appear:**
- `currentSegmentFile` is not being set
- Check that `CreateSegmentFile()` actually calls `currentSegmentFile = fileUri`

**If you see them:**
- Proceed to Step 3

## Step 3: Verify MediaElement Loading
Check console for:
```
[AudioPlayer] MediaElement opened media successfully
OR
[AudioPlayer] Waiting for MediaOpened event...
[AudioPlayer] MediaOpened event received
```

**If you see MediaFailed instead:**
```
[AudioPlayer] MediaElement failed to open media: [error message]
```
- Problem: MediaElement can't load the file
- Check: File path/URI format is correct
- Check: File permissions on the system
- Try: Opening file with Apple Player to verify it's valid
- Try: Solution B below

**If you don't see any MediaOpened messages but no error:**
- Event might not be firing
- Try: Solution C below

**If you see them:**
- Proceed to Step 4

## Step 4: Verify Play Execution
Check console for:
```
[Play] Attempt 1/5: Calling mediaElement.Play()
[Play] Current state - IsLoaded: True, Source is null: False
[Play] Attempt 1: Play() returned successfully
[Play] After Play() - Position: 00:00:00, IsPlaying: True
```

**If Play() throws an exception:**
- Check the exception type and message
- Common: `InvalidOperationException` - try Solution C
- Common: `NotImplementedException` - platform issue
- Try: Solution D below

**If Play() succeeds but IsPlaying is False:**
- Play() returned but didn't actually start playback
- Try: Solution E below

**If you see retry attempts:**
- First attempt failed, retrying
- If all 5 attempts fail, try Solution F

## Solution A: Alternative MediaSource Creation

If `MediaSource.FromFile()` fails, try this alternative:

**Location**: currentSegmentFile setter, after creating pathToUse

**Replace**:
```csharp
MediaSourceFile = MediaSource.FromFile(pathToUse);
```

**With**:
```csharp
try
{
    MediaSourceFile = MediaSource.FromFile(pathToUse);
}
catch
{
    // Fallback: Try creating from URI
    try
    {
        Uri uri = new Uri(pathToUse);
        MediaSourceFile = MediaSource.FromUri(uri);
        Debug.WriteLine($"[AudioPlayer] Created MediaSource from URI instead");
    }
    catch (Exception ex2)
    {
        Debug.WriteLine($"[AudioPlayer] Both FromFile and FromUri failed: {ex2.Message}");
        throw;
    }
}
```

## Solution B: Check File Access Permissions

On macOS, the sandbox might be blocking file access:

**Add this to CreateSegmentFile() before returning:**
```csharp
// Verify file is readable
try
{
    using (var fs = File.OpenRead(segmentFile))
    {
        // File is readable
        Debug.WriteLine($"[AudioPlayer] File is readable");
    }
}
catch (Exception ex)
{
    Debug.WriteLine($"[AudioPlayer] ERROR: File is not readable: {ex.Message}");
    Debug.WriteLine($"[AudioPlayer] Attempting to fix permissions...");
    
    // Try to make file readable
    try
    {
        var info = new FileInfo(segmentFile);
        // Note: Setting permissions is platform-specific
        // On macOS, files should inherit permissions from directory
    }
    catch { }
}
```

## Solution C: Add Delay Before Play

If MediaOpened isn't firing or Play() is timing out, try a longer delay:

**Location**: btnPlay_Clicked, after MediaOpened wait

**Replace**:
```csharp
// Additional wait to ensure everything is initialized
await Task.Delay(300);
```

**With**:
```csharp
// Additional wait to ensure everything is initialized
// Increase if still having issues
await Task.Delay(1000); // 1 second
```

Or even longer if needed:
```csharp
await Task.Delay(2000); // 2 seconds
```

## Solution D: Check Platform-Specific Issues

**macOS Specific:**
- AVPlayer might need additional initialization
- Add platform check before Play():

```csharp
#if __MACOS__
    Debug.WriteLine("[Play] macOS detected - waiting extra 500ms for AVPlayer");
    await Task.Delay(500);
#endif
mediaElement.Play();
```

**Windows Specific:**
- Ensure file path uses proper Windows format
- Check that file:// URI is correctly formatted

```csharp
#if WINDOWS
    Debug.WriteLine($"[Play] Windows detected - source: {mediaElement.Source}");
#endif
mediaElement.Play();
```

## Solution E: Check Audio Settings

If Play() succeeds but no audio:

**Verify volume and output:**
```csharp
// In btnPlay_Clicked, add:
Debug.WriteLine($"[AudioPlayer] Volume: {mediaElement.Volume}");
Debug.WriteLine($"[AudioPlayer] IsMuted: {mediaElement.IsMuted}");
Debug.WriteLine($"[AudioPlayer] PlaybackRate: {mediaElement.PlaybackRate}");

// Also verify system volume isn't muted
// Check macOS: System Preferences > Sound
```

## Solution F: Check for Platform-Specific Play Method

Some versions might need an async play:

**Try changing**:
```csharp
mediaElement.Play();
```

**To**:
```csharp
await mediaElement.PlayAsync();
```

Or check if there's a different method:
```csharp
// Check what methods are available
var methods = mediaElement.GetType().GetMethods();
foreach (var method in methods)
{
    if (method.Name.Contains("Play", StringComparison.OrdinalIgnoreCase))
    {
        Debug.WriteLine($"[AudioPlayer] Available method: {method.Name}");
    }
}
```

## Solution G: Verify WAV File Format

The segment file might be corrupted during creation:

**Add to CreateSegmentFile() after file is created:**
```csharp
// Verify WAV file structure
try
{
    using (var fs = File.OpenRead(segmentFile))
    using (var br = new BinaryReader(fs))
    {
        string riff = new string(br.ReadChars(4));
        int fileSize = br.ReadInt32();
        string wave = new string(br.ReadChars(4));
        
        Debug.WriteLine($"[AudioPlayer] WAV Structure - RIFF: {riff}, Size: {fileSize}, WAVE: {wave}");
        
        if (riff != "RIFF" || wave != "WAVE")
        {
            throw new InvalidOperationException("Invalid WAV file structure");
        }
    }
}
catch (Exception ex)
{
    Debug.WriteLine($"[AudioPlayer] WAV validation failed: {ex.Message}");
    // This indicates a problem with segment creation
}
```

## Solution H: Enable Detailed Logging

For maximum diagnostics, add this to the top of btnPlay_Clicked:

```csharp
Debug.WriteLine("=== PLAY DIAGNOSTIC START ===");
Debug.WriteLine($"MediaSourceFile: {MediaSourceFile}");
Debug.WriteLine($"mediaElement.Source: {mediaElement.Source}");
Debug.WriteLine($"mediaElement.IsLoaded: {mediaElement.IsLoaded}");
Debug.WriteLine($"mediaElement.Position: {mediaElement.Position}");
Debug.WriteLine($"mediaElement.Duration: {mediaElement.Duration}");
Debug.WriteLine($"mediaElement.IsPlaying: {mediaElement.IsPlaying}");
Debug.WriteLine($"mediaElement.Volume: {mediaElement.Volume}");
Debug.WriteLine($"mediaElement.PlaybackRate: {mediaElement.PlaybackRate}");
Debug.WriteLine("=== PLAY DIAGNOSTIC END ===");
```

This will show the complete state before attempting to play.

## When to Report an Issue

If you've tried all these solutions and audio still doesn't play:

**Report with:**
1. Complete console output (from file load to play failure)
2. OS and version (macOS 14.x, Windows 11, etc.)
3. WAV file properties (sample rate, bit depth, channels)
4. Any platform-specific error messages
5. Results of diagnostic logging

## Quick Troubleshooting Flowchart

```
Does console show "Segment file verified"?
├─ NO → File creation is failing (check CreateSegmentFile)
└─ YES ↓

Does console show "MediaSource created successfully"?
├─ NO → MediaSource creation failing (try Solution A)
└─ YES ↓

Does console show "MediaOpened event received"?
├─ NO → Event not firing (try Solution C - longer delay)
└─ YES ↓

Does console show "Play() returned successfully"?
├─ NO → Play() failing (try Solutions C, D, F)
└─ YES ↓

Does console show "IsPlaying: True"?
├─ NO → Play() succeeded but playback not starting (try Solution E)
└─ YES ↓

Is audio audible?
├─ NO → Check system volume, Solution E
└─ YES → ✓ SUCCESS!
```

