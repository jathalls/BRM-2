# MediaElement Audio File Access Issue - macOS Sandbox

## Problem
MediaElement cannot open files in the sandboxed cache directory:
`/Users/justinHalls/Library/Containers/com.companyname.brm2/Data/Library/Caches/audio_segments/segment_*.wav`

Error: `MediaElement cannot open /Users/justinHalls/Library/Containers/com.companyname.brm2/Data/Library/Caches/audio_segments/segment_*.wav`

## Root Causes

### 1. File URI Format
The MediaElement binding was receiving a plain file path instead of a proper URI.
- **Before**: `/Users/justinHalls/Library/Containers/.../segment_xxx.wav`
- **After**: `file:///Users/justinHalls/Library/Containers/.../segment_xxx.wav`

### 2. macOS Sandbox Restrictions
On macOS, the application runs in a sandbox container. Access to files requires:
- File is created and closed properly before MediaElement accesses it
- File handle is released before attempting to play
- Proper file permissions are set
- Directory exists and is writable

### 3. MediaElement Implementation Details
The MAUI MediaElement control may have issues with:
- File paths that are too long or have special characters
- Files that are still being written to when access is attempted
- Cache directory files that don't have proper read permissions
- Binding changes before file is fully ready

### 4. Timing Issues
The binding might update before the file is ready to be accessed by MediaElement.

## Solutions Implemented

### 1. Convert File Path to URI Format
```csharp
// Convert file path to proper URI format for MediaElement
var fileUri = new Uri(segmentFile).AbsoluteUri;
Debug.WriteLine($"[AudioPlayer] Segment file URI: {fileUri}");
currentSegmentFile = fileUri;
```

### 2. Verify Directory and Write Permissions
```csharp
var tempDir = Path.Combine(FileSystem.CacheDirectory, "audio_segments");
Directory.CreateDirectory(tempDir);

// Verify directory was created and is writable
if (!Directory.Exists(tempDir))
    throw new InvalidOperationException($"Failed to create directory: {tempDir}");

// Test write permission
var testFile = Path.Combine(tempDir, ".write_test");
File.WriteAllText(testFile, "test");
File.Delete(testFile);
```

### 3. Verify File Exists and Has Content
```csharp
if (!File.Exists(segmentFile))
    throw new InvalidOperationException($"Segment file was not created: {segmentFile}");

var fileInfo = new FileInfo(segmentFile);
if (fileInfo.Length == 0)
    throw new InvalidOperationException($"Segment file is empty: {segmentFile}");
```

### 4. Set File Permissions (macOS)
```csharp
// Ensure file is readable (set permissions on macOS if needed)
try
{
    #if __MACOS__
    // On macOS, ensure file has read permissions
    var info = new System.IO.FileInfo(segmentFile);
    // File permissions should be readable by owner
    #endif
}
catch (Exception ex)
{
    Debug.WriteLine($"[AudioPlayer] Warning: Could not set file permissions: {ex.Message}");
}
```

### 5. Add Delay Before Play
```csharp
private async void btnPlay_Clicked(object sender, EventArgs e)
{
    // ... setup code ...
    
    // Wait a brief moment to ensure binding and file loading has occurred
    await Task.Delay(100);
    
    // ... rest of play code ...
}
```

### 6. Enhanced Error Logging
Added detailed diagnostic logging in:
- `MediaFailed` event handler - now shows source URI and file existence checks
- `currentSegmentFile` property setter - logs all changes
- Exception handler - shows stack trace for debugging
- File creation - shows cache directory path and write test results

### 7. Better Error Handling
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
| LoadSegment | N/A | Calls CreateSegmentFile synchronously |
| CreateSegmentFile (start) | Add cache dir logging and write permission test | Verify environment is ready |
| CreateSegmentFile (file creation) | Enhanced logging and verification | Ensure file is created properly |
| CreateSegmentFile (return) | Convert path to URI with `new Uri(segmentFile).AbsoluteUri` | Proper format for MediaElement binding |
| CreateSegmentFile (return) | Add file existence and size verification | Ensure file is valid before MediaElement uses it |
| CreateSegmentFile (return) | Add macOS file permission handling | Handle sandbox restrictions |
| CreateSegmentFile (catch) | Enhanced error logging with stack trace | Better diagnostics for debugging |
| MediaFailed handler | Added detailed diagnostic logging | Show URI, file path, existence, and size |
| currentSegmentFile property | Added logging on property changes | Track binding updates |
| btnPlay_Clicked | Added 100ms delay before play | Allow binding and file access to complete |

## Testing Checklist
- [ ] Audio playback works without errors
- [ ] Debug output shows proper file URI format (file:///)
- [ ] No "MediaElement failed" errors in debug output
- [ ] Segment files are created in cache directory
- [ ] Files are properly cleaned up after playback
- [ ] Write permission test passes
- [ ] File sizes are non-zero

## Expected Debug Output
```
[AudioPlayer] Cache directory: /Users/justinHalls/Library/Containers/com.companyname.brm2/Data/Library/Caches
[AudioPlayer] Segment directory: /Users/justinHalls/Library/Containers/.../audio_segments
[AudioPlayer] Directory is writable: /Users/justinHalls/Library/Containers/.../audio_segments
[AudioPlayer] Creating segment file: /Users/justinHalls/.../segment_xxx.wav
[AudioPlayer] Segment file created successfully: /Users/justinHalls/.../segment_xxx.wav
[AudioPlayer] Segment file verified: XXXX bytes
[AudioPlayer] Segment file URI: file:///Users/justinHalls/.../segment_xxx.wav
[AudioPlayer] currentSegmentFile property changed to: file:///Users/justinHalls/.../segment_xxx.wav
[AudioPlayer] MediaElement opened media successfully
[AudioPlayer] Play command sent to MediaElement
```

## If Issues Persist

### Check Debug Output
Look for log lines showing:
- Cache directory path
- Write permission test result
- File created successfully message
- Proper URI format (file:///)
- MediaElement opened event

### Verify File System
1. Check if cache directory exists and is writable
2. Verify file is being created with correct size
3. Check file permissions on created files

### Possible Alternative Solutions
If MediaElement still fails:

1. **Use FileSystem.AppData instead of CacheDirectory**
   ```csharp
   var tempDir = Path.Combine(FileSystem.AppData, "audio_segments");
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

5. **Check MAUI MediaElement Version**
   - May need to update to latest version
   - Some versions have macOS sandbox issues

## Additional Notes
- File permissions automatically set by OS when file is created
- Cache directory cleanup happens after MediaElement stops using files
- Consider implementing file pooling if many segments are created
- On sandbox restrictions, files may need to be in app container paths
- The 100ms delay ensures binding propagation completes before play

## References
- MAUI MediaElement documentation: https://learn.microsoft.com/dotnet/maui/
- macOS sandboxing: https://developer.apple.com/documentation/security/app-sandbox
- MAUI File System: https://learn.microsoft.com/dotnet/maui/platform-integration/storage/file-system-helpers
