# 🔥 AVAudioPCMBuffer Error - COMPLETE FIX

## ⚡ IMMEDIATE SOLUTION

Run this script to clean and rebuild everything:

```bash
cd /Users/justinHalls/RiderProjects/BRM-2
chmod +x clean-build-run.sh
./clean-build-run.sh
```

This script will:
1. ✅ Clean ALL build artifacts (bin/obj folders)
2. ✅ Restore packages
3. ✅ Build BPASpectrogramM for MacCatalyst only
4. ✅ Build BRM-2 for MacCatalyst
5. ✅ Launch the app

---

## 🔍 What's Happening

The error occurs because old build cache may still be trying to compile iOS/Mac files for all platforms.

### What I've Fixed:

1. ✅ Added platform exclusions to `BPASpectrogramM.csproj`
2. ✅ Created clean build script
3. ✅ Created diagnostic script

### The Fix in .csproj:

```xml
<!-- iOS files only compile when building for iOS -->
<ItemGroup Condition="'$(TargetFramework)' != 'net10.0-ios'">
    <Compile Remove="Platforms\iOS\**\*.cs" />
</ItemGroup>

<!-- Mac files only compile when building for Mac -->
<ItemGroup Condition="'$(TargetFramework)' != 'net10.0-maccatalyst'">
    <Compile Remove="Platforms\MacCatalyst\**\*.cs" />
</ItemGroup>
```

---

## 📋 STEP-BY-STEP FIX

### Step 1: Clean Build (REQUIRED)

```bash
cd /Users/justinHalls/RiderProjects/BRM-2
chmod +x clean-build-run.sh
./clean-build-run.sh
```

**Why this is needed:** Old build artifacts may still reference platform files incorrectly.

---

### Step 2: If That Doesn't Work - Diagnostic

Run the diagnostic script:

```bash
chmod +x test-platform-builds.sh
./test-platform-builds.sh
```

This will test if the platform exclusions are working correctly.

---

### Step 3: Manual Clean (Nuclear Option)

If scripts don't work, manually clean everything:

```bash
cd /Users/justinHalls/RiderProjects/BRM-2

# Remove all build artifacts
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null

# Restore and build
cd BRM-2
dotnet restore
dotnet clean
dotnet build -f net10.0-maccatalyst
dotnet run -f net10.0-maccatalyst
```

---

## 🎯 In Rider (After Clean Build)

If using Rider:

1. **Close Rider completely** (Cmd+Q)

2. **Delete Rider's cache:**
   ```bash
   cd /Users/justinHalls/RiderProjects/BRM-2
   rm -rf .idea
   ```

3. **Reopen Rider**

4. **Let it re-index**

5. **Select:** `BRM-2 (MacCatalyst)` configuration

6. **Click Run** ▶️

---

## 🐛 Why The Error Persists

Even after fixing the .csproj, you might see the error because:

1. **Build Cache:** Old compiled files in `bin/obj` folders
2. **Rider Cache:** Rider caches project structure in `.idea` folder
3. **NuGet Cache:** Package references might be stale
4. **Multi-target Build:** Building all frameworks at once

**Solution:** Clean build removes all these caches.

---

## ✅ Expected Results

After running `clean-build-run.sh`:

### You should see:
```
🧹 Cleaning all projects...
✅ Clean complete

🗑️  Removing bin and obj folders...
✅ Folders removed

📥 Restoring NuGet packages...
✅ Packages restored

🔨 Building BPASpectrogramM library (MacCatalyst)...
✅ BPASpectrogramM built successfully

🔨 Building BRM-2 application (MacCatalyst)...
✅ BRM-2 built successfully

🚀 Launching BRM-2...
```

### You should NOT see:
```
❌ AVAudioPCMBuffer error
```

---

## 🆘 If Still Failing

### Check 1: Verify .csproj was updated

```bash
cd /Users/justinHalls/RiderProjects/BRM-2/BPASpectrogramM
grep -A 2 "Condition.*maccatalyst" BPASpectrogramM.csproj
```

Should show:
```xml
<ItemGroup Condition="'$(TargetFramework)' != 'net10.0-maccatalyst'">
    <Compile Remove="Platforms\MacCatalyst\**\*.cs" />
```

### Check 2: Verify platform files exist

```bash
ls -la Platforms/MacCatalyst/
```

Should show `PlatformClass1.cs`

### Check 3: Build with verbose output

```bash
cd BPASpectrogramM
dotnet build -f net10.0-maccatalyst -v detailed
```

Look for lines showing which files are being compiled.

---

## 📊 Summary

| Action | Command |
|--------|---------|
| **Clean & Build** | `./clean-build-run.sh` ← **DO THIS FIRST** |
| **Test Platforms** | `./test-platform-builds.sh` |
| **Manual Clean** | `find . -name "bin" -type d -exec rm -rf {} +` |
| **Build Mac Only** | `dotnet build -f net10.0-maccatalyst` |

---

## 🚀 THE SOLUTION - ONE COMMAND

```bash
cd /Users/justinHalls/RiderProjects/BRM-2
chmod +x clean-build-run.sh
./clean-build-run.sh
```

**This WILL fix the error by:**
- ✅ Removing all cached build files
- ✅ Building only for MacCatalyst
- ✅ Excluding iOS/Mac files from wrong platforms
- ✅ Launching the app

---

## ✨ AFTER IT WORKS

Once the app launches successfully:

1. **Load a WAV audio file**
2. **Select a segment on the spectrogram**
3. **Choose "0.1x" from speed dropdown**
4. **Press Play ▶**
5. **Verify slow playback with natural pitch!**

This tests the sample rate manipulation feature we implemented.

---

**Updated:** March 10, 2026  
**Status:** 🔧 Clean build script created - RUN IT NOW  
**Command:** `./clean-build-run.sh`
