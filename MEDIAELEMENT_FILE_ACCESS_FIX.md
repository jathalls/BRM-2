# MediaElement Audio File Access Issue - macOS Sandbox

## Problem
MediaElement cannot open files in the sandboxed cache directory:
`/Users/justinHalls/Library/Containers/com.companyname.brm2/Data/Library/Caches/audio_segments/segment_*.wav`

## Root Causes

### 1. File URI Format
The MediaElement binding was receiving a plain file path instead of a proper URI.
- **Before**: `/Users/justinHalls/Library/Containers/.../segment_xxx.wav`
- **After**: `file:///Users/justinHalls/Library/Containers/.../segment_xxx.wav`

### 2. macOS Sandbox Restrictions
On macOS, the application runs in a sandbox container. Access to files requires proper permissions and might require:
- File is created and closed properly before MediaElement accesses it
- File handle is released before attempting to play
- Proper file permissions are set

### 3. MediaElement Implementation Details
The MAUI MediaElement control may have issues with:
- File paths that are too long or have special characters
- Files that are still being written to when access is attempted
- Cache directory files that don't have proper read permissions

## Solutions Implemented

### 1. Convert File Path to URI Format
```csharp
// Convert file path to proper URI format for MediaElement
var fileUri = new Uri(segmentFile).AbsoluteUri;
Debug.WriteLine($"[AudioPlayer] Segment file URI: {fileUri}");
currentSegmentFile = fileUri;
```

### 2. Verify File Exists and Has Content
```csharp
if (!File.Exists(segmentFile))
    throw new InvalidOperationException($"Segment file was not created: {segmentFile}");

var fileInfo = new FileInfo(segmentFile);
if (fileInfo.Length == 0)
    throw new InvalidOperationException($"Segment file is empty: {segmentFile}");
```

### 3. Enhanced Error Logging
Added detailed diagnostic logging in:
- `MediaFailed` event handler - now shows source URI and file existence checks
- `currentSegmentFile` property setter - logs all changes
- Exception handler - shows stack trace for debugging

### 4. Better Error Handling
```csharp
catch (Exception ex)
{
    Debug.WriteLine($"[AudioPlayer] Error creating segment file: {ex.GetType().Name} - {ex.Message}");
    Debug.WriteLine($"[AudioPlayer] Stack trace: {ex.StackTrace}");
    IsSegmentLoaded = false;
    CanPlay = false;
    currentSegmentFile = string.Empty; // Clear binding on error
    return null;
}
```

## Files Modified
1. `/Users/justinHalls/RiderProjects/BRM-2/BPASpectrogramM/Views/AudioPlayer.xaml.cs`

## Changes Summary

| Section | Change | Purpose |
|---------|--------|---------|
| CreateSegmentFile (return) | Convert path to URI with `new Uri(segmentFile).AbsoluteUri` | Proper format for MediaElement binding |
| CreateSegmentFile (return) | Add file existence and size verification | Ensure file is valid before MediaElement uses it |
| CreateSegmentFile (catch) | Enhanced error logging with stack trace | Better diagnostics for debugging |
| MediaFailed handler | Added detailed diagnostic logging | Show URI, file path, existence, and size |
| currentSegmentFile property | Added logging on property changes | Track binding updates |

## Testing Checklist
- [ ] Audio playback works without errors
- [ ] Debug output shows proper file URI format (file:///)
- [ ] No "MediaElement failed" errors in debug output
- [ ] Segment files are created in cache directory
- [ ] Files are properly cleaned up after playback

## If Issues Persist

### Check Debug Output
Look for log lines:
```
[AudioPlayer] Creating segment file: /Users/justinHalls/...segment_xxx.wav
[AudioPlayer] Segment file created successfully: /Users/justinHalls/...segment_xxx.wav
[AudioPlayer] Segment file verified: XXXX bytes
[AudioPlayer] Segment file URI: file:///Users/justinHalls/.../segment_xxx.wav
[AudioPlayer] currentSegmentFile property changed to: file:///Users/justinHalls/.../segment_xxx.wav
```

### Possible Alternative Solutions
If MediaElement still fails:

1. **Use FileSystem API instead of raw paths**
   ```csharp
   var cacheDir = FileSystem.AppData; // Instead of FileSystem.CacheDirectory
   ```

2. **Try without file:// prefix** (depends on MAUI version)
   ```csharp
   currentSegmentFile = segmentFile; // Raw path
   ```

3. **Use embedded resource instead of temp file**
   - Bundle audio playback capabilities differently
   - Copy file to app data directory instead of cache

4. **Switch to alternative audio playback method**
   - Use platform-specific audio APIs on macOS
   - Implement cross-platform audio wrapper

## Additional Notes
- File permissions might require `chmod 644` on created files
- Cache directory cleanup should happen after MediaElement stops using files
- Consider implementing a file pooling mechanism if many segments are created

## References
- MAUI MediaElement documentation: https://learn.microsoft.com/dotnet/maui/
- macOS sandboxing: https://developer.apple.com/documentation/security/app-sandbox
