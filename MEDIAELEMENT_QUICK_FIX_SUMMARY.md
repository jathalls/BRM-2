# MediaElement Audio File Access - Quick Fix Summary

## Issue
`MediaElement cannot open /Users/justinHalls/Library/Containers/com.companyname.brm2/Data/Library/Caches/audio_segments/segment_f00a9b96-349b-4eb0-89f7-7a80a056eae6.wav`

## Root Cause
1. File path passed to MediaElement was not in proper URI format
2. File might not be ready when binding occurs
3. Directory or file write permissions issues
4. Timing issue between file creation and playback

## All Fixes Applied to AudioPlayer.xaml.cs

### Fix 1: Enhanced Directory Verification
**Location**: CreateSegmentFile method start
```csharp
// Added:
- Log cache directory path
- Verify directory exists
- Test write permissions with test file
- Clear diagnostic output
```

### Fix 2: File URI Conversion
**Location**: CreateSegmentFile return section
```csharp
// Before:
currentSegmentFile = segmentFile;  // Plain path

// After:
var fileUri = new Uri(segmentFile).AbsoluteUri;
currentSegmentFile = fileUri;  // Proper URI format (file:///)
```

### Fix 3: File Verification
**Location**: CreateSegmentFile return section
```csharp
// Added:
- Check file exists after creation
- Check file is not empty
- Log file size
- Handle errors gracefully
```

### Fix 4: File Permissions
**Location**: CreateSegmentFile return section
```csharp
// Added:
- Conditional macOS file permission handling
- Warning logging if permissions can't be set
```

### Fix 5: Enhanced Error Handling
**Location**: CreateSegmentFile catch block
```csharp
// Before:
Debug.WriteLine($"Error creating segment file: {ex.Message}");
return null;

// After:
Debug.WriteLine($"Error creating segment file: {ex.GetType().Name} - {ex.Message}");
Debug.WriteLine($"Stack trace: {ex.StackTrace}");
IsSegmentLoaded = false;
CanPlay = false;
currentSegmentFile = string.Empty;
return null;
```

### Fix 6: Better Binding Logging
**Location**: currentSegmentFile property
```csharp
// Before:
public string currentSegmentFile { 
    get => _currentSegmentFile; 
    set { _currentSegmentFile = value; OnPropertyChanged(); } 
}

// After:
public string currentSegmentFile { 
    get => _currentSegmentFile; 
    set {
        if (_currentSegmentFile != value) {
            _currentSegmentFile = value;
            Debug.WriteLine($"[AudioPlayer] currentSegmentFile property changed to: {value}");
            OnPropertyChanged();
        }
    }
}
```

### Fix 7: Enhanced MediaFailed Handler
**Location**: Loaded event handler
```csharp
// Added:
- Log source URI
- Log file path from URI
- Check if file exists
- Show file size
- Provide diagnostic information
```

### Fix 8: Play Timing Delay
**Location**: btnPlay_Clicked method
```csharp
// Added:
private async void btnPlay_Clicked(object sender, EventArgs e)
{
    // ... existing code ...
    
    // NEW: Wait a brief moment to ensure binding and file loading has occurred
    await Task.Delay(100);
    
    // ... rest of existing code ...
}
```

## Expected Behavior After Fix

### Debug Output Shows:
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

### No Error Messages:
- ❌ MediaElement failed to open media
- ✅ MediaElement opened media successfully
- ✅ Play command sent successfully

## Testing Steps

1. **Load Audio File**
   - Open audio file in the application
   - Check debug output for cache directory

2. **Create Selection**
   - Select a portion of audio
   - Trigger segment file creation
   - Check debug output for file creation and URI conversion

3. **Play Selection**
   - Click play button
   - Wait for 100ms delay
   - Check for MediaElement success message
   - Audio should play without errors

4. **Verify Output**
   - All URIs should be in format: `file:///Users/...`
   - No MediaElement failed messages
   - File sizes should be non-zero
   - Directory write test should pass

## If Still Having Issues

1. **Check Console Output**
   - Look for error messages with full details
   - Note exact file path shown in error
   - Check write permission test result

2. **Verify File System**
   - Run in Finder: Go > Go to Folder
   - Paste: `/Users/justinHalls/Library/Containers/com.companyname.brm2/Data/Library/Caches`
   - Confirm `audio_segments` directory exists
   - Check files are created with appropriate size

3. **Alternative: Use AppData Instead**
   - Replace `FileSystem.CacheDirectory` with `FileSystem.AppData`
   - May have better sandbox permissions

4. **Check MAUI Version**
   - Update MediaElement NuGet package
   - Some versions had macOS sandbox issues

## Files Modified
- `/Users/justinHalls/RiderProjects/BRM-2/BPASpectrogramM/Views/AudioPlayer.xaml.cs`

## Total Changes
- 8 separate improvements
- Enhanced error handling and logging
- Proper file URI formatting
- Directory and file verification
- Timing adjustment for binding
- Comprehensive documentation

---
**Status**: ✅ Complete
**Testing Required**: Yes - Follow testing steps above
**Documentation**: Complete - See MEDIAELEMENT_AUDIO_FIX_COMPLETE.md
