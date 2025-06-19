# Walking Animation Setup Guide

## The Problem
The console shows: "Parameter 'IsWalking' does not exist"

This means your Animator Controller doesn't have the required parameters that the movement script is trying to control.

## Quick Fix Steps

### 1. Find Your Animator Controller
- Look at your player's **Animator** component in the Inspector
- Note the **Controller** field - this shows which Animator Controller is being used
- Find this controller in your Project window (usually in an Animations folder)

### 2. Add Required Parameters
1. **Double-click** the Animator Controller to open the Animator window
2. Click the **Parameters** tab (usually on the left side)
3. Click the **"+"** button and select **Bool**
4. Add these exact parameter names (case-sensitive!):
   - `IsGrounded`
   - `IsWalking`
   - `IsJumping`

### 3. Set Up Basic Walking Animation
1. In the Animator window, you should see your animation states (Idle, Walk, etc.)
2. **Right-click** on your **Idle** animation state
3. Select **Make Transition**
4. Click on your **Walk** animation state
5. Click the white arrow (transition) you just created
6. In the Inspector, under **Conditions**, click **"+"**
7. Set the condition to: **IsWalking** = **true**

### 4. Set Up Return to Idle
1. **Right-click** on your **Walk** animation state
2. Select **Make Transition**
3. Click on your **Idle** animation state
4. Click the transition arrow
5. Add condition: **IsWalking** = **false**

## Testing Your Setup

1. Add the **AnimatorParameterSetup** script to your player (temporarily)
2. Play the scene - it will check and report if parameters are set up correctly
3. Press **G** to enable debug mode in the movement script
4. Move with WASD - you should see "Walking: True" in the console
5. The walk animation should now play!

## Common Issues

### Animation plays but character slides
- Check **Apply Root Motion** on the Animator component
- Usually this should be **unchecked** for CharacterController movement

### Animation is too fast/slow
- In the Animator, click on your Walk animation state
- Adjust the **Speed** multiplier in the Inspector

### Character doesn't turn when moving
- This is normal - the movement script handles rotation
- The character should automatically face the movement direction

## Advanced Setup (Optional)

### Smooth Transitions
1. Click on transition arrows
2. Adjust **Transition Duration** (0.1-0.25 is usually good)
3. Uncheck **Has Exit Time** for immediate transitions

### Jump Animation
1. From **Any State**, make transition to **Jump**
2. Condition: **IsJumping** = **true**
3. From **Jump** to **Idle**: **IsGrounded** = **true**

### Blend Trees (Smooth direction changes)
Instead of single Walk animation:
1. Right-click → Create State → From New Blend Tree
2. Double-click the Blend Tree
3. Add walk animations for different directions
4. Use **IsWalking** to transition to blend tree

## Debug Helper

The AnimatorParameterSetup script will output exactly what's wrong. Look for:
```
=== MISSING REQUIRED ANIMATOR PARAMETERS ===
Animator Controller 'YourControllerName' is missing:
  ❌ IsWalking (Bool)
```

This tells you exactly what to add! 