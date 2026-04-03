# Fix "No Compatible Device Selected" in Rider

## The Problem
You see "no compatible device selected" when trying to run BRM-2 in JetBrains Rider.

## The Solution (3 Steps)

### Step 1: Open Run Configuration Settings

In Rider's top menu:
```
Run → Edit Configurations...
```

OR click the configuration dropdown (next to the Run button) and select "Edit Configurations..."

---

### Step 2: Create/Edit BRM-2 Configuration

**If no configuration exists:**
1. Click `+` button (Add New Configuration)
2. Select `.NET Project`
3. Name it: `BRM-2`

**Configure these settings:**
- **Project:** `BRM-2` (select from dropdown)
- **Target Framework:** `net10.0-maccatalyst` (for Mac) or `net10.0-ios` (for simulator)
- **Configuration:** `Debug`
- **Use External Console:** ☐ (unchecked)

Click **Apply**, then **OK**

---

### Step 3: Select Device and Run

Now in the main toolbar:

1. **Configuration dropdown** should show "BRM-2"
2. **Device selector** should now show options:
   - "My Mac (Mac Catalyst)" ← **SELECT THIS**
   - OR "iPhone 15 Pro" (if using iOS simulator)
3. **Click Run** ▶ (green play button)

---

## Alternative: Command Line (If Rider Still Doesn't Work)

Open Terminal and run:

```bash
cd /Users/justinHalls/RiderProjects/BRM-2
chmod +x run-mac.sh
./run-mac.sh
```

This bypasses Rider's device selection entirely.

---

## Still Not Working?

### Check 1: MAUI Workload Installed?

In Terminal:
```bash
dotnet workload list
```

Should show `maui` in the list. If not:
```bash
dotnet workload install maui
```

Then **restart Rider**.

### Check 2: Xcode Configured?

In Terminal:
```bash
xcode-select -p
```

Should output: `/Applications/Xcode.app/Contents/Developer`

If not:
```bash
sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer
sudo xcodebuild -license accept
```

Then **restart Rider**.

### Check 3: Rebuild Solution

In Rider:
```
Build → Clean Solution
Build → Rebuild Solution
```

Then try running again.

---

## What Each Option Does

| Device Option | What It Does | Best For |
|--------------|--------------|----------|
| **My Mac (Mac Catalyst)** | Runs app natively on your Mac | ✅ **Recommended** - Fastest, easiest |
| **iPhone Simulator** | Runs in iOS simulator | Testing iOS-specific features |
| **Physical iPhone** | Runs on connected device | Final testing, real hardware |

---

## Expected Behavior After Fixing

1. ✅ Device selector shows "My Mac (Mac Catalyst)"
2. ✅ Clicking Run ▶ starts building
3. ✅ Build completes (first time: 2-5 min)
4. ✅ App launches on your Mac
5. ✅ You can load audio files and test playback

---

## Quick Visual Checklist

```
Rider Toolbar:
┌─────────────┬────────────────────┬───────┐
│ BRM-2 (cfg) │ My Mac (Mac Cat..) │  ▶    │
└─────────────┴────────────────────┴───────┘
      ↑              ↑                ↑
   Select         Select          Click
   Config         Device           Run
```

All three must be properly set!

---

**TL;DR:**
```
Run → Edit Configurations... → 
  Add BRM-2 → 
  Target: net10.0-maccatalyst → 
  OK →
  Device: "My Mac" →
  Run ▶
```

That's it! 🎉
