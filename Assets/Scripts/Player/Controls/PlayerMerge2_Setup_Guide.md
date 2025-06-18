# PlayerMerge2 Setup Guide

## Required Components

The PlayerMerge2 prefab needs the following components to work with the new CharacterController movement system:

### Core Movement Components:
1. **CharacterController** (Unity Component)
   - Height: 2
   - Radius: 0.5
   - Center: (0, 1, 0)

2. **CharacterControllerMovement** (Script)
   - Move Speed: 5
   - Sprint Speed: 8
   - Jump Height: 2
   - Gravity: -19.62
   - Ground Check Distance: 0.1
   - Mouse Sensitivity: 2
   - Max Look Angle: 80
   - Camera Transform: (Assign main camera)
   - Ground Layer: "Ground" (layer mask)

3. **GameInput** (Script) - Should be singleton in scene

### Networking Components:
1. **NetworkObject** (Netcode component)
2. **ClientNetworkTransform** (For client-authoritative movement)

### Animation:
1. **PlayerAnimator** (Script)
   - Character Controller Movement: (Reference to CharacterControllerMovement component)

### Other Required Components:
1. **NewPlayerBehavior** (Script)
2. **Spellcasting** (Script) - Now uses IMovementEffects interface
3. **K_SpellLauncher** (Script)
4. **CapsuleCollider** (for spell interactions)
   - Height: 2
   - Radius: 0.5
   - Center: (0, 1, 0)

## Camera Setup:
- The camera should be a child of the player
- Position it at eye level (approximately Y: 1.6)
- The CharacterControllerMovement script will handle mouse look

## Layer Setup:
- Player GameObject: "Player" layer
- Ground/Floor: "Ground" layer

## Important Notes:
1. The old PlayerMovement and PlayerController scripts have been removed
2. Movement is now handled entirely by CharacterControllerMovement
3. All spell effects now use the IMovementEffects interface
4. WASD controls movement, Space jumps, Mouse controls camera look
5. The client sends input to the server, and ClientNetworkTransform handles synchronization

## Removed Components:
- Rigidbody (replaced by CharacterController)
- PlayerMovement script
- PlayerController script 