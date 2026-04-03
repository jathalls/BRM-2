# 🚀 QUICK START - Running BRM-2

## ⚡ Fastest Method (MacCatalyst)

### Option 1: Using the Launch Script

Open Terminal and run:

```bash
cd /Users/justinHalls/RiderProjects/BRM-2
chmod +x run-mac.sh
./run-mac.sh
```

The script will:
- ✅ Check dependencies
- ✅ Clean previous builds
- ✅ Restore packages
- ✅ Build for MacCatalyst
- ✅ Launch the app

---

### Option 2: In Rider

1. **Open the device selector** (dropdown next to Run button ▶)
2. **Select one of these:**
   - "My Mac (Mac Catalyst)"
   - "Local Mac"
   - Any option with "Mac Catalyst" or "MacCatalyst"
3. **Click Run** ▶ (green play button)

**If no devices appear:**
1. Go to `Run → Edit Configurations...`
2. Click `+` (Add New Configuration)
3. Select `.NET Launch Settings Profile`
4. Project: `BRM-2`
5. Launch Profile: `BRM-2`
6. Target Framework: `net10.0-maccatalyst`
7. Click OK
8. Now the device selector should show "My Mac"

---

### Option 3: Terminal Commands

```bash
# Navigate to project
cd /Users/justinHalls/RiderProjects/BRM-2/BRM-2

# Build
dotnet build -f net10.0-maccatalyst

# Run
dotnet run -f net10.0-maccatalyst
```

---

## 🔧 If You Get Errors

### Error: "MAUI workload not installed"

```bash
dotnet workload install maui
```

### Error: "Xcode not found" or "Command line tools"

```bash
# Install Xcode from App Store first, then:
sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer
sudo xcodebuild -license accept
```

### Error: "No compatible device selected"

**In Rider:**
1. Top toolbar → Click device dropdown (shows "Select Device" or similar)
2. If empty:
   - `Run → Edit Configurations...`
   - Select or create BRM-2 configuration
   - Target Framework: `net10.0-maccatalyst`
   - Save
3. Device dropdown should now show "My Mac (Mac Catalyst)"
4. Select it
5. Click Run ▶

**Or use Terminal method** (see Option 3 above)

---

## 📱 Testing the Audio Feature

Once the app launches:

1. **Load an audio file** (WAV format)
2. **Select a segment** on the spectrogram
3. **Choose speed** from dropdown:
   - `0.1x` = 1/10 speed (very slow) ← **Test this!**
   - `0.2x` = 1/5 speed
   - `1.0x` = Normal speed
   - `heterodyne` = Special processing
4. **Press Play** ▶
5. **Verify:**
   - Audio plays at selected speed
   - Pitch sounds natural (not distorted)
   - Position tracking works

---

## 🐛 Debugging in Rider

1. Set breakpoints in code
2. Click Debug button 🐞 (instead of Run ▶)
3. App will pause at breakpoints
4. Use Debug console to see output:
   - Look for `[AudioPlaybackService-Mac]` logs
   - Look for `[AudioPlayer]` logs

---

## ℹ️ Why MacCatalyst?

- ✅ **Fastest** - Runs natively on Mac
- ✅ **No simulator** - Direct execution
- ✅ **Full AVAudioEngine** - All audio features work
- ✅ **Easy debugging** - Standard Mac app
- ✅ **Quick iteration** - Fast build times

You can also test on iOS Simulator, but MacCatalyst is recommended for development.

---

## 📝 Summary

**Simplest way to run:**

```bash
cd /Users/justinHalls/RiderProjects/BRM-2
./run-mac.sh
```

**In Rider:**
```
Device Selector → "My Mac (Mac Catalyst)" → Run ▶
```

**That's it!** 🎉

---

See `HOW_TO_RUN.md` for detailed troubleshooting and additional options.
