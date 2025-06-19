# Camera Zoom Debug Guide

## Quick Setup Check

1. **Do you have a Cinemachine FreeLook Camera?**
   - Go to: GameObject > Cinemachine > FreeLook Camera
   - Name it something clear like "PlayerFreeLookCamera"

2. **Configure the FreeLook Camera:**
   - **Follow**: Set to your Player GameObject
   - **Look At**: Set to your Player GameObject
   - **Binding Mode**: World Space (for third-person)
   - **Input Axis Name**:
     - X Axis: "Mouse X"
     - Y Axis: "Mouse Y"

3. **Add the Zoom Script:**
   - Select your Player GameObject
   - Add Component > SimpleCameraZoom
   - Settings:
     - Zoom Speed: 3
     - Min Zoom: 2 (closest)
     - Max Zoom: 10 (farthest)
     - **Enable Debug Logs: ✓** (CHECK THIS!)

## Testing Steps

1. **Run the game**
2. **Check Console** for these messages:
   - `[SimpleCameraZoom] Starting camera zoom setup...`
   - `[SimpleCameraZoom] Found type with: ...`
   - `[SimpleCameraZoom] Successfully found FreeLook camera: ...`

3. **Press Z key** to see camera details in console

4. **NEW: Press Shift+Z** to see ALL camera fields and properties (for debugging)

5. **Scroll mouse wheel** and watch for:
   - `[SimpleCameraZoom] Scroll detected: ...`
   - `[SimpleCameraZoom] Found m_Orbits as a FIELD` (or PROPERTY)
   - `[SimpleCameraZoom] Rig X radius set to: ...`
   - `[SimpleCameraZoom] Zoom applied! Current level: ...`

## Common Issues

### Issue: "Could not find m_Orbits property!"
**This is NOW FIXED!** The updated script tries both fields and properties with various names.
If you still see this:
1. Hold **Shift** and press **Z** to see all camera members
2. Look for any field/property containing "orbit" or "rig"
3. Report the exact names shown in the console

### Issue: "No FreeLook camera found"
**Solution:**
1. Make sure Cinemachine is installed (Package Manager > Cinemachine)
2. Create a FreeLook camera (not just a Virtual Camera)
3. The camera must be active in the scene

### Issue: Scroll detected but no zoom
**Possible causes:**
1. Camera radius values might be locked
2. Another script might be overriding the values
3. The zoom range (2-10) might be too subtle

**Try:**
- Increase Max Zoom to 20 or 30
- Check if other scripts modify the camera

### Issue: Can't scroll at all
**Check:**
1. Is another UI element capturing scroll input?
2. Is the game window focused?
3. Try in Play mode, not just Scene view

## Alternative Solutions

### Option 1: FOV-Based Zoom (Most Reliable)
If orbit radius zoom isn't working, try the **AlternativeZoom** script:
1. Add Component > **AlternativeZoom** to your player
2. Settings:
   - Min FOV: 20 (zoomed in)
   - Max FOV: 60 (zoomed out)  
   - Zoom Speed: 3
3. This changes Field of View instead of camera distance

### Option 2: Manual Testing
1. While game is running, find your FreeLook camera in Hierarchy
2. Select it and look at the Inspector
3. Manually change the Radius values in Top/Middle/Bottom rigs
4. If manual changes work but script doesn't, there's a refresh issue

### Option 3: Use CharacterControllerMovement Zoom
The CharacterControllerMovement script has built-in zoom:
1. Enable "Debug Movement" ✓
2. Scroll and watch for "Called InvalidateCache" or "Called OnValidate" messages

## Manual Camera Check

In the Unity Inspector while the game is running:
1. Find your FreeLook camera in the Hierarchy
2. Expand the "Orbits" section
3. Watch the "Radius" values of Top, Middle, and Bottom rigs
4. They should change when you scroll

If they don't change, something is blocking the zoom functionality. 