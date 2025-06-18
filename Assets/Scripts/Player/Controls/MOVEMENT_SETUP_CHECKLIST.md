# Movement System Setup Checklist

## Quick Fix Steps:

### 1. Add GameInput to Scene
- Create an empty GameObject in your scene
- Name it "GameInput" 
- Add the `GameInput.cs` script component to it
- This creates the singleton that handles input

### 2. On PlayerMerge2 Prefab, ensure these components exist:
- **CharacterController** component (Unity's built-in)
  - Height: 2
  - Radius: 0.5
  - Center: (0, 1, 0)
  
- **CharacterControllerMovement** script
  - Camera Transform: Assign your player's camera
  - Ground Layer: Set to "Ground" (make sure you have a Ground layer)
  
- **PlayerAnimator** script (if using animations)
  - Character Controller Movement: Should auto-find, but you can assign manually

### 3. Camera Setup
- Make sure the player has a Camera as a child object
- Position it around Y: 1.6 (eye level)
- Assign this camera to the "Camera Transform" field in CharacterControllerMovement

### 4. Layer Setup
- Create a "Ground" layer in Unity (Edit > Project Settings > Tags and Layers)
- Assign your floor/terrain objects to the "Ground" layer
- Set the Ground Layer mask in CharacterControllerMovement to "Ground"

### 5. Remove Old Components
Make sure these old components are REMOVED from PlayerMerge2:
- ❌ Rigidbody
- ❌ PlayerMovement
- ❌ PlayerController

## Debugging Steps:

1. **Add MovementDebugger script to any GameObject**
   - This will show in console if inputs are being detected
   - Will warn if GameInput is missing

2. **Check Console for errors**
   - Look for any null reference exceptions
   - Check for missing component warnings

3. **In Play Mode, check:**
   - Is the cursor locked? (it should be)
   - Can you see debug logs when pressing WASD?
   - Is the CharacterController component enabled?

## Common Issues:

- **Can't move at all**: GameInput singleton is missing from scene
- **Can move but no mouse look**: Camera Transform not assigned
- **Falling through floor**: Ground Layer not set correctly
- **NullReferenceException**: Component references not assigned

## Manual Component Assignment:
If auto-detection fails, manually assign in the inspector:
- CharacterControllerMovement > Camera Transform
- CharacterControllerMovement > Ground Layer
- PlayerAnimator > Character Controller Movement 