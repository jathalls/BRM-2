# How to Run BRM-2 in JetBrains Rider

## Error: "No Compatible Device Selected"

This error occurs when Rider doesn't have a target device/simulator selected for running the MAUI app.

---

## Quick Fix - Select a Device/Simulator

### For MacCatalyst (Easiest on Mac):

1. **In Rider's top toolbar:**
   - Look for the device selector dropdown (next to the Run button ▶)
   - Click the dropdown

2. **Select "My Mac (Mac Catalyst)"** or similar option
   - This will run the app natively on your Mac
   - No simulator needed
   - Fastest option for development

3. **Click the Run button** ▶ (or press `Ctrl+R` / `Cmd+R`)

### For iOS Simulator:

1. **Open Xcode first** (if not already open):
   - Open Xcode from Applications
   - Go to `Xcode > Settings > Platforms` (or `Xcode > Preferences > Components`)
   - Make sure iOS Simulators are installed

2. **In Rider:**
   - Click the device selector dropdown
   - Look for iOS Simulators (e.g., "iPhone 15 Pro", "iPad Pro")
   - Select one

3. **Click Run** ▶

---

## Step-by-Step: First Time Setup

### Option 1: Run on Mac (MacCatalyst) - RECOMMENDED

This is the simplest option and doesn't require any simulator:

```
1. Rider Toolbar → Device Selector (dropdown)
2. Select: "My Mac (Mac Catalyst)" or "Local Mac"
3. Configuration: Select "Debug" 
4. Target Framework: Select "net10.0-maccatalyst"
5. Click Run ▶
```

### Option 2: Run on iOS Simulator

Requires Xcode simulators to be installed:

```
1. Open Xcode → Settings → Platforms
2. Install iOS Simulator (if not already installed)
3. Close Xcode
4. In Rider → Device Selector
5. Select an iOS Simulator (e.g., "iPhone 15 Pro")
6. Target Framework: Select "net10.0-ios"
7. Click Run ▶
```

---

## Troubleshooting

### Problem: No devices show up in the dropdown

**Solution:**

1. **Verify .NET MAUI workload is installed:**
   ```bash
   dotnet workload list
   ```
   
   Should show:
   - `maui` (or `maui-maccatalyst`, `maui-ios`)
   
   If missing, install:
   ```bash
   dotnet workload install maui
   ```

2. **Restart Rider** after installing workloads

3. **Check Xcode Command Line Tools:**
   ```bash
   xcode-select -p
   ```
   
   Should output: `/Applications/Xcode.app/Contents/Developer`
   
   If not:
   ```bash
   sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer
   ```

### Problem: "No devices found" even with simulators installed

**Solution:**

1. **Launch Xcode once** to complete setup
2. **Accept Xcode license:**
   ```bash
   sudo xcodebuild -license accept
   ```
3. **List available simulators:**
   ```bash
   xcrun simctl list devices available
   ```
4. **Restart Rider**

### Problem: Build succeeds but can't select device

**Solution:**

1. **Check Run Configuration:**
   - `Run → Edit Configurations...`
   - Make sure "BRM-2" project is selected
   - Configuration: Debug
   - Target Framework: `net10.0-maccatalyst` or `net10.0-ios`
   - Click OK

2. **Clean and Rebuild:**
   - `Build → Clean Solution`
   - `Build → Rebuild Solution`

---

## Recommended Development Setup

For **fastest development cycle** on Mac:

1. **Use MacCatalyst** (runs natively on Mac)
   - No simulator overhead
   - Fast builds
   - Easy debugging
   - Full access to Mac resources

2. **Test on iOS Simulator** periodically
   - For iOS-specific features
   - For UI testing on different devices
   - Before final testing on physical device

3. **Physical device testing** before release
   - Real performance testing
   - Actual hardware features (camera, sensors, etc.)

---

## Current Project Configuration

Based on BRM-2.csproj:

- ✅ **Target Frameworks:**
  - `net10.0-android` (Android)
  - `net10.0-ios` (iOS)
  - `net10.0-maccatalyst` (Mac)
  - `net10.0-windows` (Windows, if on Windows)

- ✅ **Minimum OS Versions:**
  - iOS: 15.0+
  - MacCatalyst: 15.0+
  - Android: 21.0+ (Android 5.0)

---

## Running from Command Line (Alternative)

If Rider's device selection isn't working, you can run from terminal:

### MacCatalyst:
```bash
cd "/Users/justinHalls/RiderProjects/BRM-2/BRM-2"
dotnet build -f net10.0-maccatalyst
dotnet run -f net10.0-maccatalyst
```

### iOS Simulator:
```bash
cd "/Users/justinHalls/RiderProjects/BRM-2/BRM-2"
dotnet build -f net10.0-ios
# Then open in Xcode or use: 
# dotnet run -f net10.0-ios -- --device "iPhone 15 Pro"
```

---

## Next Steps After Selecting Device

Once you select a device and run:

1. **First build will take time** (2-5 minutes)
   - Downloads dependencies
   - Compiles for platform
   - Creates app bundle

2. **App should launch** automatically
   - MacCatalyst: Opens as Mac app
   - iOS Simulator: Opens in simulator

3. **To test audio playback:**
   - Load an audio file
   - Select a segment
   - Choose speed (e.g., "0.1x" for 1/10 speed)
   - Press Play ▶
   - Verify slow playback with original pitch

---

## Quick Command Reference

```bash
# List installed workloads
dotnet workload list

# Install MAUI workload
dotnet workload install maui

# List available simulators
xcrun simctl list devices available

# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore

# Build for MacCatalyst
dotnet build -f net10.0-maccatalyst

# Build for iOS
dotnet build -f net10.0-ios
```

---

**Updated:** March 10, 2026  
**For:** JetBrains Rider on macOS with .NET MAUI
