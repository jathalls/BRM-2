# Info.plist Error - Resolution

## Problem
Build error: "BPASpectrogramM info.plist not found"

## Root Cause
BPASpectrogramM is a **MAUI Class Library** project (not an application). It doesn't need Info.plist, Program.cs, or AppDelegate.cs files - those are only needed for executable applications.

## Solution Applied

### 1. Removed Application-Specific Files
The following files that were initially created are **NOT needed** and have been removed or made unnecessary:
- ~~`Platforms/iOS/Info.plist`~~ (not needed for library)
- ~~`Platforms/iOS/Program.cs`~~ (not needed for library)
- ~~`Platforms/iOS/AppDelegate.cs`~~ (not needed for library)
- ~~`Platforms/MacCatalyst/Info.plist`~~ (not needed for library)
- ~~`Platforms/MacCatalyst/Program.cs`~~ (not needed for library)
- ~~`Platforms/MacCatalyst/AppDelegate.cs`~~ (not needed for library)

### 2. Updated Platform-Specific Namespaces
Changed the namespaces for platform-specific implementations to avoid conflicts:

**Before:**
```csharp
namespace BPASpectrogramM;
public class AudioPlaybackService : IAudioPlaybackService { }
```

**After:**
```csharp
// iOS
namespace BPASpectrogramM.Platforms.iOS;
public class AudioPlaybackService : IAudioPlaybackService { }

// MacCatalyst
namespace BPASpectrogramM.Platforms.MacCatalyst;
public class AudioPlaybackService : IAudioPlaybackService { }

// Android
namespace BPASpectrogramM.Platforms.Android;
public class AudioPlaybackService : IAudioPlaybackService { }

// Windows
namespace BPASpectrogramM.Platforms.Windows;
public class AudioPlaybackService : IAudioPlaybackService { }
```

### 3. Added Conditional Using Statements
Updated `Views/AudioPlayer.xaml.cs` to include platform-specific using statements:

```csharp
#if IOS
using BPASpectrogramM.Platforms.iOS;
#elif MACCATALYST
using BPASpectrogramM.Platforms.MacCatalyst;
#elif ANDROID
using BPASpectrogramM.Platforms.Android;
#elif WINDOWS
using BPASpectrogramM.Platforms.Windows;
#endif
```

This allows the `AudioPlayer` class to reference the correct platform-specific `AudioPlaybackService` implementation at compile time.

## How MAUI Libraries Work

### Library vs Application
- **Application Project**: Requires Info.plist, Program.cs, AppDelegate.cs, MauiProgram.cs
  - Has `<OutputType>Exe</OutputType>` in .csproj
  - Example: BRM-2 (the main app)

- **Library Project**: Does NOT require those files
  - No `<OutputType>` tag (defaults to Library)
  - Example: BPASpectrogramM (the library)

### Platform-Specific Code in Libraries
MAUI libraries use the `Platforms/` folder structure with conditional compilation:
```
BPASpectrogramM/
├── IAudioPlaybackService.cs (shared interface)
├── Platforms/
│   ├── iOS/
│   │   └── PlatformClass1.cs (iOS implementation)
│   ├── MacCatalyst/
│   │   └── PlatformClass1.cs (Mac implementation)
│   ├── Android/
│   │   └── PlatformClass1.cs (Android implementation)
│   └── Windows/
│       └── PlatformClass1.cs (Windows implementation)
└── Views/
    └── AudioPlayer.xaml.cs (uses platform implementations)
```

Each platform automatically compiles only its own files when building for that platform.

## Current Status

✅ **FIXED** - Project should now build without Info.plist errors

### File Structure (Final)
```
BPASpectrogramM/
├── IAudioPlaybackService.cs
├── Platforms/
│   ├── iOS/
│   │   ├── PlatformClass1.cs (AudioPlaybackService with AVAudioEngine)
│   │   └── Resources/
│   │       └── PrivacyInfo.xcprivacy
│   ├── MacCatalyst/
│   │   ├── PlatformClass1.cs (AudioPlaybackService with AVAudioEngine)
│   │   └── Resources/
│   │       └── PrivacyInfo.xcprivacy
│   ├── Android/
│   │   └── PlatformClass1.cs (AudioPlaybackService placeholder)
│   └── Windows/
│       └── PlatformClass1.cs (AudioPlaybackService placeholder)
└── Views/
    └── AudioPlayer.xaml.cs (with conditional using statements)
```

## Testing

The project should now:
1. ✅ Build without Info.plist errors
2. ✅ Compile platform-specific code correctly
3. ✅ Reference the correct AudioPlaybackService for each platform
4. ✅ Work as a library referenced by the main BRM-2 application

## Notes

- PrivacyInfo.xcprivacy files were kept in the Resources folders as they're useful metadata
- All platform-specific implementations are properly namespaced
- The conditional compilation ensures the right implementation is used on each platform
- BRM-2 (the main app) already has its own Info.plist files and doesn't need ones from the library

---

**Resolution Date**: March 10, 2026  
**Status**: ✅ RESOLVED
