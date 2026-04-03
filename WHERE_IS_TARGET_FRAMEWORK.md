# Where is the Target Framework Dropdown in Rider?

## 📍 Location: Rider's Top Toolbar

The Target Framework dropdown is in **Rider's main toolbar** at the top of the window.

---

## Visual Guide

### Layout of Rider's Toolbar (left to right):

```
┌────────────────────────────────────────────────────────────────┐
│  🔨 Build  ▶️ Run  🐞 Debug     [Configuration ▼] [Framework ▼] [Device ▼]  │
└────────────────────────────────────────────────────────────────┘
                                        ↑              ↑            ↑
                                        │              │            │
                                   Run Config    TARGET HERE    Device
```

### Specifically:

```
┌──────────────────────────────────────────────────────────┐
│ [BRM-2 ▼] [net10.0-maccatalyst ▼] [My Mac (Mac Cat..) ▼] │
└──────────────────────────────────────────────────────────┘
       ↑               ↑                        ↑
   Configuration   TARGET FRAMEWORK         Device
                   ← CLICK HERE!
```

---

## Step-by-Step: How to Find It

### 1. **Look at the VERY TOP of the Rider window**
   - Not in any side panels
   - Not in menus
   - In the **main toolbar** right below the menu bar

### 2. **Find the Run button ▶️** (green triangle)
   - It's usually on the left side of the toolbar

### 3. **Look to the RIGHT of the Run button**
   - You'll see several dropdowns in a row
   - The **second dropdown** is the Target Framework

### 4. **It will show something like:**
   - "Default" (bad - change this!)
   - "All Frameworks" (bad - change this!)
   - "net10.0-maccatalyst" (good! - use this)
   - "net10.0-ios" (good! - use this)
   - "net10.0-android" 
   - "net10.0-windows"

---

## If You DON'T See It...

### Option A: The Dropdown Might Be Hidden

Try this:

1. **Go to:** `View → Appearance → Toolbar`
2. **Make sure it's checked** ✓
3. The toolbar should appear at the top

### Option B: Access Via Run Configurations

If the toolbar still doesn't show the framework dropdown:

1. **Click:** `Run → Edit Configurations...`
2. **Select:** BRM-2 (in left panel)
3. **Look for:** "Target Framework" field
4. **Change to:** `net10.0-maccatalyst`
5. **Click:** OK

This achieves the same result!

---

## What It Should Look Like

### ❌ WRONG - Causes Build Errors:
```
┌────────────────────────────────────────────────┐
│ BRM-2    [Default ▼]    Device               │
└────────────────────────────────────────────────┘
```
Or:
```
┌────────────────────────────────────────────────┐
│ BRM-2    [All Frameworks ▼]    Device         │
└────────────────────────────────────────────────┘
```

### ✅ CORRECT - Builds Successfully:
```
┌────────────────────────────────────────────────┐
│ BRM-2    [net10.0-maccatalyst ▼]    My Mac    │
└────────────────────────────────────────────────┘
```

---

## Alternative: Just Use the Script!

If you can't find the dropdown or it's confusing, just run this instead:

```bash
cd /Users/justinHalls/RiderProjects/BRM-2
./run-mac.sh
```

This **bypasses Rider entirely** and builds correctly from the command line!

---

## Screenshot Reference

The toolbar location looks like this in a typical Rider window:

```
┌─────────────────────────────────────────────────────────────────┐
│ File  Edit  View  Navigate  Code  Refactor  Build  Run  Tools   │  ← Menu Bar
├─────────────────────────────────────────────────────────────────┤
│ 🔨 ▶️ 🐞 [BRM-2 ▼] [net10.0-maccatalyst ▼] [My Mac ▼]         │  ← TOOLBAR
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [Solution Explorer]            [Editor Area]                   │
│                                                                  │
│  ├─ BRM-2.sln                   Your code here                  │
│  ├─ BRM-2                                                       │
│  └─ BPASpectrogramM                                             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**The dropdown is in the toolbar, NOT in the menu bar!**

---

## Quick Visual Test

Look for these elements in order from left to right:

1. ✅ Green Run button ▶️
2. ✅ Configuration dropdown (shows "BRM-2" or "Debug")  
3. ✅ **Framework dropdown** ← THIS ONE!
4. ✅ Device dropdown (shows "My Mac" or device name)

If you see all four, you found it! The **third item** is your target framework selector.

---

## Still Can't Find It?

### Just run this command instead:

```bash
cd /Users/justinHalls/RiderProjects/BRM-2/BRM-2
dotnet build -f net10.0-maccatalyst
dotnet run -f net10.0-maccatalyst
```

Or use the script:

```bash
cd /Users/justinHalls/RiderProjects/BRM-2
./run-mac.sh
```

**Both will build correctly without needing to find the dropdown!**

---

## Summary

**Where:** Top toolbar, to the right of the Run button ▶️

**What to select:** `net10.0-maccatalyst`

**If you can't find it:** Use `./run-mac.sh` instead!

---

Hope this helps! The key is looking in the **main toolbar at the very top**, not in menus or panels.
