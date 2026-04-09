# Quick Release Packaging Guide

## TL;DR - Fastest Way to Package

### For macOS Testing:
```bash
cd /Users/justinHalls/RiderProjects/BRM-2
chmod +x package-release.sh
./package-release.sh maccatalyst
# Output: BRM-2-Release-macOS-YYYYMMDD.zip
```

### For Windows Testing:
```bash
./package-release.sh windows x64
# Output: BRM-2-Release-Windows-YYYYMMDD.zip
```

### For Android Testing:
```bash
./package-release.sh android
# Look for .apk files in BRM-2/bin/Release/net10.0-android/publish/
```

---

## What You Get

- ✅ Optimized release build (smaller, faster)
- ✅ All dependencies included
- ✅ Ready to distribute to testers
- ✅ Timestamped zip files for versioning

---

## For Different Target Computers

| Target OS | Command | How to Run |
|-----------|---------|-----------|
| **macOS (Intel)** | `./package-release.sh maccatalyst` | Double-click BRM-2.app |
| **macOS (Apple Silicon)** | `./package-release.sh maccatalyst` | Double-click BRM-2.app |
| **Windows (64-bit)** | `./package-release.sh windows x64` | Run BRM-2.exe |
| **Windows (32-bit)** | `./package-release.sh windows x86` | Run BRM-2.exe |
| **Android** | `./package-release.sh android` | `adb install app.apk` |

---

## Manual Build (No Script)

### macOS:
```bash
dotnet publish -f net10.0-maccatalyst -c Release -p:RuntimeIdentifiers=maccatalyst-x64;maccatalyst-arm64
# App is at: BRM-2/bin/Release/net10.0-maccatalyst/maccatalyst-x64/publish/BRM-2.app
```

### Windows:
```bash
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifier=win-x64
# Exe is at: BRM-2/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/
```

### Android:
```bash
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk
# APK is at: BRM-2/bin/Release/net10.0-android/publish/
```

---

## Tips

- **First build takes 3-5 minutes**, subsequent builds are faster
- **Zip the entire publish folder** if you're unsure what to share
- **Test on the exact OS version** you plan to support
- **Release builds are 30-50% smaller** than debug builds
- **Don't include the entire `bin/` or `obj/` folders** - just the publish output

---

## File Structure After Publishing

```
BRM-2-Release-macOS.zip
└── BRM-2.app/
    ├── Contents/
    │   ├── MacOS/
    │   ├── Resources/
    │   └── ...
    └── (app bundle)
```

or

```
BRM-2-Release-Windows.zip
├── BRM-2.exe
├── BRM-2.dll
├── runtimes/
└── (all dependencies)
```

---

## Troubleshooting

**Script not executable:**
```bash
chmod +x package-release.sh
```

**Build fails:**
```bash
dotnet clean
dotnet restore
./package-release.sh maccatalyst
```

**Need specific architecture:**
```bash
# Apple Silicon (M1/M2/M3)
./package-release.sh maccatalyst arm64

# Intel Mac
./package-release.sh maccatalyst x64

# Windows 32-bit
./package-release.sh windows x86
```

---

## Files in This Directory

- **`PACKAGE_RELEASE_BUILD.md`** - Complete detailed guide
- **`package-release.sh`** - Automated packaging script
- **`QUICK_RELEASE_PACKAGING.md`** - This file (quick reference)

---

Done! Your package is ready to share with testers. 🚀
