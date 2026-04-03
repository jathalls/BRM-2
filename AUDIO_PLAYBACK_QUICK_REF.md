# Audio Playback - Quick Reference

## What Changed?

**File**: `/Users/justinHalls/RiderProjects/BRM-2/BPASpectrogramM/Views/AudioPlayer.xaml.cs`

**Key Changes**:
1. Added: `using CommunityToolkit.Maui.Core;`
2. Fixed: `MediaSourceFile` property type (now `MediaSource?` instead of `string`)
3. Improved: File path handling (universal URI creation)
4. Enhanced: MediaSource creation (robust handling of paths/URIs)
5. Better: Play button logic (proper event waiting)
6. Comprehensive: Retry logic (5 attempts with diagnostics)

## Expected Behavior

1. **Load Segment** → File created in cache
2. **MediaSource Created** → Console shows creation success
3. **Click Play** → Waits for MediaOpened event
4. **Play Executes** → Up to 5 retries if needed
5. **Audio Plays** → Position updates shown

## Quick Debug Checklist

- [ ] See "Segment file verified" in console?
- [ ] See "MediaSource created successfully"?
- [ ] See "MediaOpened event received"?
- [ ] See "Play() returned successfully"?
- [ ] See "IsPlaying: True"?
- [ ] Hear audio?

If any NO → Check **AUDIO_PLAYBACK_DIAGNOSTICS.md**

## Key Console Markers

**Success**:
```
[AudioPlayer] MediaSource created successfully
[AudioPlayer] MediaOpened event received
[Play] Attempt 1: Play() returned successfully
[Play] After Play() - IsPlaying: True
```

**Failure**:
```
[AudioPlayer] Error creating MediaSource
[AudioPlayer] MediaOpened timeout
[Play] Attempt X failed
[Play] Stack trace: ...
```

## If Audio Doesn't Play

**Step 1**: Check file was created (look for "Segment file verified")
**Step 2**: Check MediaSource created (look for "MediaSource created successfully")
**Step 3**: Check MediaOpened fired (look for "MediaOpened event received")
**Step 4**: Check Play() worked (look for "Play() returned successfully")
**Step 5**: See AUDIO_PLAYBACK_DIAGNOSTICS.md Solutions A-H

## Code Locations

| What | Where |
|------|-------|
| Type system | Line ~50-65 |
| File path handling | Line ~415-450 |
| MediaSource creation | Line ~30-75 |
| Play button logic | Line ~668-740 |
| Retry logic | Line ~748-785 |

## Test Commands

**Test File Loading**:
- Open app
- Select audio segment
- Check console for "Segment file verified"

**Test Play**:
- Click Play button
- Check console for success messages
- Listen for audio

**Test Diagnostics**:
- If no audio, check console output
- Match output to expected pattern
- Use AUDIO_PLAYBACK_DIAGNOSTICS.md

## Platform Notes

**macOS**:
- Uses AVPlayer internally
- Needs proper file:// URIs
- 5-second MediaOpened timeout
- 300ms delay after event

**Windows**:
- Same file:// URI format works
- Usually faster initialization
- Same retry logic applies

## When to Escalate

If you see these without any previous errors:
- All 5 Play() attempts fail
- MediaOpened never fires
- File creation fails

Then check Solutions A-H in **AUDIO_PLAYBACK_DIAGNOSTICS.md**

## Documentation Map

| Document | Purpose |
|----------|---------|
| AUDIO_PLAYBACK_COMPLETE.md | High-level overview |
| CROSS_PLATFORM_AUDIO_FIX.md | Technical details |
| AUDIO_PLAYBACK_FINAL_VALIDATION.md | Implementation checklist |
| AUDIO_PLAYBACK_DIAGNOSTICS.md | Troubleshooting guide |
| This file | Quick reference |

## Quick Links to Solutions

| Issue | Solution |
|-------|----------|
| MediaSource fails | Diagnostics → Solution A |
| Can't access file | Diagnostics → Solution B |
| Play() times out | Diagnostics → Solution C |
| Platform-specific | Diagnostics → Solution D |
| Audio silent | Diagnostics → Solution E |
| Play() doesn't start | Diagnostics → Solution F |
| Audio corrupted | Diagnostics → Solution G |
| Need more logging | Diagnostics → Solution H |

## Success Indicators

- ✅ Console shows "MediaSource created successfully"
- ✅ Console shows "MediaOpened event received"  
- ✅ Console shows "Play() returned successfully"
- ✅ Console shows "IsPlaying: True"
- ✅ Audio is audible
- ✅ Position updates in real-time

## Quick Test Script

```
1. Run app
2. Load audio file
3. Wait 2 seconds
4. Click Play
5. Listen for audio (should play within 5 seconds)
6. Check console for success messages
```

Expected success: Audio plays, position updates appear

Expected failure: Console shows error, check AUDIO_PLAYBACK_DIAGNOSTICS.md

