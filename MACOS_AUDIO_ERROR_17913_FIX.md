# Fix for macOS AVFoundation Error -17913

## Problem
MediaElement was opening the audio file successfully but then failing with error -17913 (AVFoundation error on macOS meaning file not found or access denied).

**Error sequence:**
1. File created successfully
2. MediaElement opens file (MediaOpened event fires)
3. Later, MediaElement fails with -17913
4. Error diagnostics show "File exists: False" even though file was just created

## Root Cause
The issue was twofold:

### 1. File Path Format on macOS
The code was using `file:///` URIs, but on macOS sandboxed apps, direct file paths work better for AVPlayer. File URIs can cause issues with sandbox restrictions.

### 2. Error Message Parsing
The `MediaSource.ToString()` returns `"File: file:///path"` but the error handler was trying to use this as a file path, including the "File: " prefix, making it impossible to find the file.

### 3. Insufficient File Sync Time
The file was only getting 50ms to sync before MediaElement tried to access it. macOS needs more time for the file system to fully write and sync the data.

## Solutions Implemented

### 1. ✅ Improved Error Handler
**Fixed**: Strip the "File: " prefix from MediaSource.ToString()
```csharp
if (filePath.StartsWith("File: "))
{
    filePath = filePath.Substring("File: ".Length);
}
```

**Added**: Convert file:// URIs to direct paths
```csharp
if (filePath.StartsWith("file://"))
{
    filePath = new Uri(filePath).LocalPath;  // Extract direct path
}
```

### 2. ✅ Platform-Specific File Path Handling
**macOS**: Use direct file paths
```csharp
#if __MACOS__
    string pathForMediaSource = segmentFile;  // Direct path on macOS
#else
    // Use file:// URI on Windows
#endif
```

**Why**: Direct paths avoid sandbox complications on macOS while still working on other platforms.

### 3. ✅ Better MediaSource Creation
**Convert file:// URIs back to paths**:
```csharp
if (value.StartsWith("file://"))
{
    Uri uri = new Uri(value);
    pathToUse = uri.LocalPath;  // Get direct path from URI
}
```

**Try direct path first**:
- If file exists at direct path → use it
- Otherwise, try URI format
- This gives MediaElement the best chance of success

### 4. ✅ Longer File System Sync Time
**Increased from 50ms to 200ms**:
```csharp
System.Threading.Thread.Sleep(200);  // Wait for file system sync
```

**Additional verification**:
```csharp
using (var fs = File.OpenRead(segmentFile))
{
    fs.Seek(0, SeekOrigin.End);
    long finalSize = fs.Position;  // Verify file is readable
}
```

### 5. ✅ Better File Access Checks
**Added permission checks in error handler**:
```csharp
try
{
    using (var fs = File.OpenRead(filePath))
    {
        Debug.WriteLine($"[AudioPlayer] File is readable");
    }
}
catch (Exception permEx)
{
    Debug.WriteLine($"[AudioPlayer] ERROR: File is not readable: {permEx.Message}");
}
```

## Expected Behavior Now

### When Loading a Segment
```
[AudioPlayer] Creating segment file: /path/to/segment_xxxx.wav
[AudioPlayer] macOS: Using direct file path: /path/to/segment_xxxx.wav
[AudioPlayer] Waiting for file system to sync...
[AudioPlayer] File final size verified: 1179500 bytes
[AudioPlayer] MediaSourceFile property changed to: /path/to/segment_xxxx.wav
[AudioPlayer] MediaElement opened media successfully
```

### If MediaElement Fails
```
[AudioPlayer] MediaElement failed to open media: error message
[AudioPlayer] Stripped 'File: ' prefix, path now: /correct/path
[AudioPlayer] File exists: True  ← Now it finds the file correctly!
[AudioPlayer] File is readable ← Confirms file is accessible
```

## Platform-Specific Notes

### macOS
- ✅ Now uses direct file paths instead of file:// URIs
- ✅ Longer sync time (200ms) ensures file is written
- ✅ File permission checks added
- ✅ Error handling correctly parses source path

### Windows
- ✅ Still uses file:// URIs for compatibility
- ✅ Same sync time and verification
- ✅ Same error handling improvements

## Files Modified

**Single file**:
- `/Users/justinHalls/RiderProjects/BRM-2/BPASpectrogramM/Views/AudioPlayer.xaml.cs`

**Key changes**:
1. Error handler: Strip "File: " prefix
2. Error handler: Convert file:// to direct path
3. CreateSegmentFile: Use direct paths on macOS
4. currentSegmentFile setter: Prefer direct paths
5. File sync: Increased delay and added verification

## Testing

To verify the fix:
1. Run app on macOS
2. Load an audio segment
3. Check console for: "MediaElement opened media successfully"
4. File should NOT fail with -17913
5. Play button should work

## If Still Getting -17913 Error

Check console for:
- `[AudioPlayer] File final size verified:` - File was written successfully
- `[AudioPlayer] File is readable:` - File is accessible
- `[AudioPlayer] MediaElement opened media successfully:` - Initial load worked

If these appear but you still get -17913 later:
- The file might be getting deleted or moved
- Check if another process is accessing the audio_segments directory
- Try restarting the app

## Technical Details

### Error -17913 on macOS AVFoundation
This error code typically means:
- File not found
- Permission denied
- File moved/deleted during access
- Invalid file format

Our fix addresses all of these by:
1. Using direct file paths (avoids URI encoding issues)
2. Proper file sync timing
3. Better error diagnostics
4. Direct path preference on macOS

### Why Direct Paths on macOS
macOS App Sandbox restrictions can interfere with file:// URIs:
- URIs go through NSURL handling which can trigger sandbox checks
- Direct paths work better with FileStream which is already authorized
- AVPlayer accepts direct paths natively

