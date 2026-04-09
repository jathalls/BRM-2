# Packaging Release Build for Testing

This guide explains how to package your .NET MAUI application (BRM-2) for testing on another computer.

## Quick Overview

Your project targets multiple platforms:
- **macOS** (via MacCatalyst)
- **iOS**
- **Android**
- **Windows**

The packaging method depends on which platform you want to test on.

---

## 1. Build Release Configuration

First, clean and build the release version for your target platform:

```bash
cd /Users/justinHalls/RiderProjects/BRM-2

# Clean previous builds
dotnet clean

# Build for your target platform (examples below)
```

---

## 2. Platform-Specific Packaging

### **macOS/MacCatalyst**

To package for macOS testing on another Mac:

```bash
# Build and publish for MacCatalyst (Intel)
dotnet publish -f net10.0-maccatalyst -c Release

# Or for Apple Silicon
dotnet publish -f net10.0-maccatalyst -c Release -p:RuntimeIdentifier=maccatalyst-arm64

# Or for both architectures (recommended)
dotnet publish -f net10.0-maccatalyst -c Release -p:RuntimeIdentifiers=maccatalyst-x64;maccatalyst-arm64
```

**Output Location:** `BRM-2/bin/Release/net10.0-maccatalyst/maccatalyst-*/publish/`

**To share:** Zip the `.app` bundle or create a `.pkg` installer.

---

### **Windows**

To package for Windows testing:

```bash
# Build and publish for Windows
dotnet publish -f net10.0-windows10.0.19041.0 -c Release

# Or for specific architecture
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifier=win-x64
```

**Output Location:** `BRM-2/bin/Release/net10.0-windows10.0.19041.0/win-*/publish/`

**To share:** Create a `.zip` file with the entire publish folder, or use MSIX packaging.

---

### **Android**

To package for Android testing:

```bash
# Build AAB (Android App Bundle) for Google Play
dotnet publish -f net10.0-android -c Release

# Or build APK for direct installation
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

**Output Location:** `BRM-2/bin/Release/net10.0-android/publish/`

**Files:**
- `.aab` - Upload to Google Play or use bundletool
- `.apk` - Sideload directly on Android devices

---

### **iOS**

To package for iOS testing:

```bash
# Build for device
dotnet publish -f net10.0-ios -c Release

# Or for specific architecture
dotnet publish -f net10.0-ios -c Release -p:RuntimeIdentifier=ios-arm64
```

**Output Location:** `BRM-2/bin/Release/net10.0-ios/`

**To share:** Use Ad Hoc provisioning or TestFlight for distribution.

---

## 3. Recommended Packaging Workflow

### **For Testing on Same OS Type (Easiest)**

```bash
#!/bin/bash
# Example: Package macOS version

PLATFORM="maccatalyst"
ARCH="maccatalyst-x64;maccatalyst-arm64"
OUTPUT_DIR="Release-Package"

# Clean and build
dotnet clean
dotnet publish -f net10.0-${PLATFORM} -c Release -p:RuntimeIdentifiers=${ARCH}

# Package for distribution
BUILD_OUTPUT="BRM-2/bin/Release/net10.0-${PLATFORM}/maccatalyst-x64/publish"
mkdir -p ${OUTPUT_DIR}
cp -r "${BUILD_OUTPUT}/BRM-2.app" "${OUTPUT_DIR}/"
zip -r "BRM-2-Release-macOS.zip" "${OUTPUT_DIR}"

echo "✓ Package created: BRM-2-Release-macOS.zip"
```

---

## 4. Using the Publish Folder Directly

For quick testing, you can share the entire publish folder:

```bash
# After publishing
cd BRM-2/bin/Release/net10.0-{platform}/{rid}/publish

# Create a zip for easy transfer
zip -r ~/Desktop/BRM-2-Release.zip .
```

---

## 5. Automated Build & Package Script

Create a script at the root of your project:

**File: `package-release.sh`**

```bash
#!/bin/bash
set -e

PLATFORM=${1:-"maccatalyst"}
OUTPUT_DIR="Release-Package"

echo "📦 Building release for $PLATFORM..."
dotnet clean
dotnet publish -f net10.0-${PLATFORM} -c Release

echo "📁 Creating package..."
mkdir -p ${OUTPUT_DIR}

case ${PLATFORM} in
    maccatalyst)
        BUILD_OUTPUT="BRM-2/bin/Release/net10.0-maccatalyst/maccatalyst-x64/publish"
        cp -r "${BUILD_OUTPUT}/BRM-2.app" "${OUTPUT_DIR}/"
        zip -r "BRM-2-Release-macOS.zip" "${OUTPUT_DIR}"
        echo "✓ Package: BRM-2-Release-macOS.zip"
        ;;
    windows)
        BUILD_OUTPUT="BRM-2/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish"
        cp -r "${BUILD_OUTPUT}/"* "${OUTPUT_DIR}/"
        zip -r "BRM-2-Release-Windows.zip" "${OUTPUT_DIR}"
        echo "✓ Package: BRM-2-Release-Windows.zip"
        ;;
    android)
        echo "✓ APK/AAB available in: BRM-2/bin/Release/net10.0-android/publish/"
        ;;
esac

rm -rf ${OUTPUT_DIR}
```

**Usage:**

```bash
chmod +x package-release.sh
./package-release.sh maccatalyst
```

---

## 6. Distribution Steps

### **For macOS:**
1. Run the packaging script above
2. Send `BRM-2-Release-macOS.zip` to tester
3. Tester extracts and double-clicks `BRM-2.app`

### **For Windows:**
1. Run packaging script above
2. Send `BRM-2-Release-Windows.zip` to tester
3. Tester extracts and runs `BRM-2.exe`

### **For Android:**
1. Build APK: `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk`
2. Find `.apk` file in publish folder
3. Send to tester and they install with: `adb install app.apk`

### **For iOS:**
1. Build: `dotnet publish -f net10.0-ios -c Release`
2. Use Xcode to create an Ad Hoc build or use TestFlight for beta testing

---

## 7. Troubleshooting

**Build fails with missing dependencies:**
```bash
dotnet restore
dotnet clean
dotnet publish -f net10.0-{platform} -c Release
```

**Need to target specific CPU architecture:**
```bash
# For Intel Mac
dotnet publish -f net10.0-maccatalyst -c Release -p:RuntimeIdentifier=maccatalyst-x64

# For Apple Silicon
dotnet publish -f net10.0-maccatalyst -c Release -p:RuntimeIdentifier=maccatalyst-arm64
```

**Verify build output:**
```bash
# Check what was created
ls -lah BRM-2/bin/Release/net10.0-{platform}/*/publish/
```

---

## 8. Quick Commands Reference

```bash
# Build release only
dotnet build -f net10.0-{platform} -c Release

# Publish release
dotnet publish -f net10.0-{platform} -c Release

# Publish with specific runtime
dotnet publish -f net10.0-{platform} -c Release -p:RuntimeIdentifier={rid}

# List available target frameworks
dotnet build -h | grep -i "framework"
```

---

## Platform RIDs (Runtime Identifiers)

```
macOS:           maccatalyst-x64, maccatalyst-arm64
Windows:         win-x64, win-x86, win-arm64
Android:         android-x64, android-x86, android-arm64
iOS:             ios-arm64
```

---

## Notes

- **Release builds** are optimized for performance (smaller, faster)
- **First publish** takes longer as it downloads dependencies
- **Subsequent publishes** are faster (incremental builds)
- All dependencies are included in the publish folder (self-contained)
- Test on similar OS versions to deployment target

---

For questions or issues, check the official MAUI documentation:
https://learn.microsoft.com/en-us/dotnet/maui/
