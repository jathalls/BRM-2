# Audio Playback Fix Summary

## Problem
AudioPlayer was not playing audio when the Play button was clicked. Error: `System.InvalidOperationException: AVPlayer.CurrentItem is not yet initialized`

## Root Causes Identified

1. **Missing Namespace Import** (Initial Issue)
   - Code was calling `MediaSource.FromFile()` without the proper namespace
   - Fixed by removing the incorrect `using CommunityToolkit.Maui.Core.Media;` statement

2. **Type Mismatch in MediaSourceFile Property**
   - Property was declared as `MediaSource` type instead of `string`
   - The XAML binding `Source="{Binding MediaSourceFile}"` expects a string path
   - MediaElement will convert the string to MediaSource internally

3. **Binding Not Being Updated**
   - The CreateSegmentFile method was setting `currentSegmentFile` property
   - But the XAML was bound to `MediaSourceFile` property
   - These needed to be kept in sync

4. **AVPlayer Initialization Timing**
   - Attempting to seek/play before AVPlayer.CurrentItem was initialized
   - MediaOpened event was unreliable/timing out

5. **File URI Format on macOS**
   - macOS might need direct file paths instead of file:// URIs
   - Different platforms have different requirements

## Fixes Applied

### 1. Fixed MediaSourceFile Property Type
**File:** `/Users/justinHalls/RiderProjects/BRM-2/BPASpectrogramM/Views/AudioPlayer.xaml.cs`

Changed:
```csharp
private MediaSource _mediaSourceFile = string.Empty;
public MediaSource MediaSourceFile
```

To:
```csharp
private string _mediaSourceFile = string.Empty;
public string MediaSourceFile
{
    get => _mediaSourceFile;
    set
    {
        if (_mediaSourceFile != value)
        {
            _mediaSourceFile = value;
            Debug.WriteLine($"[AudioPlayer] MediaSourceFile property changed to: {value}");
            OnPropertyChanged();
        }
    }
}
```

### 2. Updated Binding in CreateSegmentFile Method
Ensured that `MediaSourceFile` is set (the actual bound property):
```csharp
MediaSourceFile = fileUri;  // Set the actual property that's bound to MediaElement.Source
```

Also added error handling:
```csharp
MediaSourceFile = string.Empty;    // Clear MediaElement source on error
```

### 3. Improved Play Button Logic
**Removed:**
- Unreliable TaskCompletionSource waiting for MediaOpened
- Problematic SeekTo(TimeSpan.Zero) call that triggered AVPlayer initialization error

**Added:**
- Direct polling for Source to be available (10 attempts × 200ms = 2 seconds max wait)
- Simple Play() call without seeking (media starts at position 0 naturally)
- Retry logic with exponential backoff for Play command

### 4. Platform-Specific File Path Handling
On macOS, use direct file paths instead of file:// URIs:
```csharp
#if WINDOWS
// Only convert to file:// URI on Windows
if (!fileUri.StartsWith("file://"))
{
    Uri uri = new Uri(new FileInfo(fileUri).FullName);
    fileUri = uri.AbsoluteUri;
}
#endif
```

### 5. Enhanced Debugging
Added comprehensive logging:
```csharp
Debug.WriteLine($"[AudioPlayer] MediaSourceFile property value: {MediaSourceFile}");
Debug.WriteLine($"[AudioPlayer] MediaElement source is: {mediaElement.Source}");
Debug.WriteLine($"[AudioPlayer] About to set MediaSourceFile to: {fileUri}");
Debug.WriteLine($"[AudioPlayer] File exists at this path: {File.Exists(segmentFile)}");
Debug.WriteLine($"[AudioPlayer] File size: {new FileInfo(segmentFile).Length} bytes");
```

## How It Works Now

1. User creates/loads audio segment via LoadSegment()
2. CreateSegmentFile() creates a WAV file with the segment audio
3. File is set to `MediaSourceFile` property (which triggers XAML binding)
4. User clicks Play button
5. Play button waits up to 2 seconds for MediaElement to have Source loaded
6. Calls Play() method (no seeking)
7. Play() is retried with exponential backoff if it fails (handles late initialization)

## Testing Recommendations

1. Load an audio file and select a segment
2. Click Play button
3. Check console output for:
   - "MediaSourceFile property changed to: [path]"
   - "File exists at this path: True"
   - "Positive file size in bytes"
   - "Play command sent to MediaElement successfully"

4. Verify audio plays (check position updates in timer)

## Files Modified

- `/Users/justinHalls/RiderProjects/BRM-2/BPASpectrogramM/Views/AudioPlayer.xaml.cs`

## Key Changes Summary

| Issue | Fix |
|-------|-----|
| Wrong namespace for MediaSource | Removed incorrect using statement |
| Type mismatch (MediaSource vs string) | Changed MediaSourceFile back to string type |
| Property not being set | Added MediaSourceFile assignment in CreateSegmentFile |
| AVPlayer timing error | Removed seek, added polling for source |
| Unreliable event waiting | Replaced with direct polling |
| File path format on macOS | Added platform-specific URI handling |

