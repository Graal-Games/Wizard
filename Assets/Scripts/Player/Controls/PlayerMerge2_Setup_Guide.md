# PlayerMerge2 Setup Guide

## Overview
This guide helps you set up the PlayerMerge2 prefab with the new CharacterController-based movement system for third-person gameplay.

## Required Components

### 1. CharacterController
- **Height**: 2
- **Radius**: 0.5  
- **Center**: (0, 1, 0)
- **Slope Limit**: 45
- **Step Offset**: 0.3
- **Skin Width**: 0.08
- **Min Move Distance**: 0.001

### 2. CharacterControllerMovement Script
**Movement Settings:**
- Move Speed: 2
- Sprint Speed: 3.5
- Jump Height: 2
- Gravity: -9.81
- Ground Check Distance: 0.1
- Jump Cooldown: 0.2
- Movement Multiplier: 1
- Max Fall Speed: 20

**Camera Settings (for reference only - camera is controlled by Cinemachine):**
- Auto Find Camera: ✓
- Camera Transform: (leave empty, will auto-find)
- Eye Height: 1.6 (not used in third person)
- Mouse Sensitivity: 2 (not used - Cinemachine controls camera)
- Max Look Angle: 80 (not used - Cinemachine controls camera)

**Ground Detection:**
- Ground Layer Mask: Set to your ground layers (e.g., "Default", "Ground")

### 3. Networking Components
- **NetworkObject**: Required for multiplayer
- **ClientNetworkTransform**: For client-authoritative movement
  - Sync Position: ✓
  - Sync Rotation: ✓
  - Sync Scale: ✗

### 4. Animator (Optional)
If you have animations, ensure these parameters exist:
- "IsGrounded" (bool)
- "IsWalking" (bool)
- "IsJumping" (bool)

### 5. NO Rigidbody
- Do NOT add a Rigidbody component
- If other scripts require it, set it to Kinematic
- Remove NetworkRigidbody if present

## Camera Setup (Cinemachine)

### For Third-Person:
1. **Main Camera**: Add CinemachineBrain component
2. **Create FreeLook Camera**:
   - GameObject → Cinemachine → FreeLook Camera
   - Set **Follow** and **LookAt** to your player (will be set automatically by script)
   - Configure orbits for desired camera distance
   - Set input axes to "Mouse X" and "Mouse Y"

### FreeLook Settings:
- **Top Rig**: Height 4, Radius 5
- **Middle Rig**: Height 2.5, Radius 6
- **Bottom Rig**: Height 0.5, Radius 5
- **X Axis**: Mouse X, Speed 300, Accel/Decel Time 0.1
- **Y Axis**: Mouse Y, Speed 2, range 0.3 to 0.6

## Scene Setup

### GameInput Singleton
Either:
- Add GameInput prefab to scene, OR
- Add GameInputAutoSetup script to any GameObject
- The system will fall back to Unity's Input.GetAxis if GameInput is not found

## Controls

### Player Controls:
- **WASD**: Move (relative to camera view)
- **Space**: Jump
- **Shift** (hold): Sprint
- Movement makes character rotate to face direction

### Camera Controls (via Cinemachine):
- **Mouse**: Orbit camera around player
- **Scroll Wheel**: Zoom in/out (if configured in Cinemachine)

## Troubleshooting

### Character doesn't move
- Check CharacterController is not disabled
- Verify Ground Layer Mask includes your ground
- Ensure Time.timeScale is not 0

### Character rotates but doesn't face movement direction
- Check that HandleRotation is being called in Update
- Verify pendingMovement is being calculated correctly

### Camera doesn't follow player
- Ensure CinemachineBrain is on Main Camera
- Check FreeLook camera Follow/LookAt are set to player
- Verify player has "Player" tag or is found by script

### Animations not playing
- Check Animator component exists
- Verify parameter names match exactly:
  - "IsGrounded", "IsWalking", "IsJumping"
- Ensure Animator Controller is assigned

### Player launches into air
- Remove or disable Rigidbody
- Check for overlapping colliders
- Verify CharacterController settings

## Debug Options
Enable debug flags in CharacterControllerMovement:
- **Debug Movement**: Logs movement calculations
- **Debug Rotation**: Logs rotation info

Press 'C' in-game to log camera hierarchy info.

## Animation Setup - TWO OPTIONS

### Option 1: Modify Your Animator Controller (Recommended)
Add these Bool parameters to your existing Animator Controller:
- `IsGrounded`
- `IsWalking` 
- `IsJumping`

See `ANIMATION_SETUP_GUIDE.md` for detailed steps.

### Option 2: Use FlexibleAnimatorAdapter (No modifications needed!)
1. Add `FlexibleAnimatorAdapter` component to your player
2. It will list all your existing animator parameters in the console
3. In the inspector, enter your existing parameter names:
   - Walk Parameter Name: (e.g., "Speed", "Walk", "Movement")
   - Parameter Type: Float/Bool/Trigger
4. The adapter will automatically translate between your animator and the movement system

## Quick Setup Steps 