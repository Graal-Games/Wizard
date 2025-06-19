using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

// Extension methods for Animator
public static class AnimatorExtensions
{
    public static bool HasParameter(this Animator animator, int parameterHash)
    {
        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash)
                return true;
        }
        return false;
    }
}

/// <summary>
/// Third-person character controller with Cinemachine support.
/// Features:
/// - WASD movement relative to camera view
/// - Space to jump
/// - Character rotates to face movement direction
/// - Works with Cinemachine FreeLook for third-person camera
/// - Network-ready with Netcode for GameObjects
/// - Movement effects support (slow, knockback, etc.)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CharacterControllerMovement : NetworkBehaviour, IMovementEffects
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float sprintSpeed = 3.5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float jumpCooldown = 0.2f; // Prevent jump spam
    [SerializeField] private float movementMultiplier = 1f; // Global movement speed multiplier for easy tuning
    [SerializeField] private float maxFallSpeed = 20f;  // Maximum falling speed to prevent extreme velocities
    
    [Header("Movement Inertia")]
    [SerializeField] private float acceleration = 8f; // How quickly to reach max speed
    [SerializeField] private float deceleration = 12f; // How quickly to stop
    [SerializeField] private float airAcceleration = 4f; // Acceleration while in air
    [SerializeField] private float airDeceleration = 6f; // Deceleration while in air
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float maxLookAngle = 80f; // Maximum vertical look angle
    [SerializeField] private float eyeHeight = 1.6f; // Height of eyes from character base
    [SerializeField] private bool autoFindCamera = true; // Enable auto-finding camera
    
    [Header("Camera Zoom")]
    [SerializeField] private bool enableZoom = true; // Toggle to enable/disable zoom
    [SerializeField] private float zoomSpeed = 2f; // How fast the camera zooms
    [SerializeField] private float minZoomDistance = 2f; // Minimum zoom distance
    [SerializeField] private float maxZoomDistance = 10f; // Maximum zoom distance
    
    [Header("Startup")]
    [SerializeField] private float stabilizationTime = 0.3f; // Reduced to see if it helps
    
    [Header("Debug")]
    [SerializeField] private bool debugMovement = false; // Disabled by default
    [SerializeField] private bool debugRotation = false; // Disabled for production
    
    [Header("References")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private GameObject gameInputObject; // Optional: assign in inspector
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer = -1; // Default to Everything
    [SerializeField] private float groundPositionOffset = 0f; // Manual adjustment for character height
    
    // Component References
    private CharacterController characterController;
    private Animator animator;
    private MonoBehaviour gameInputComponent; // Store as MonoBehaviour to avoid compilation issues
    private Camera mainCamera; // Cache main camera reference
    
    // Movement state
    private Vector3 velocity;
    private float currentSpeed;
    private Vector3 currentHorizontalVelocity; // For smooth acceleration/deceleration
    private bool isGrounded = false;
    private bool wasGrounded = false; // For landing detection
    private float lastGroundedTime = 0f;
    private bool jumpRequested = false;
    private float lastJumpTime = 0f;
    
    // Base movement speeds (for restoration after effects)
    private float baseMoveSpeed;
    private float baseSprintSpeed;
    
    // Rotation state
    private float verticalRotation = 0f; // For camera tilt (not used in third person but kept for compatibility)
    
    // Movement effects
    private bool isSlowed = false;
    private bool movementStopped = false;
    private float slowTimer = 0f; // For timed slow effects
    
    // Stabilization
    private bool isStabilized = false;
    private float stabilizationTimer = 0f;
    private float startupTime; // Time when Start() was called
    
    // Validation
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;
    
    // Movement calculation
    private Vector3 pendingMovement = Vector3.zero;
    
    // Position stabilization
    private Vector3 lastStablePosition;
    private float positionStableTime = 0f;
    private const float POSITION_STABILITY_THRESHOLD = 0.001f; // Movement smaller than this is ignored
    private const float STABILITY_TIME_REQUIRED = 0.1f; // Time before locking position
    
    // Animation parameters (must match the parameter names in the Animator exactly)
    private readonly int animIsGrounded = Animator.StringToHash("IsGrounded");
    private readonly int animIsWalking = Animator.StringToHash("IsWalking");
    private readonly int animIsJumping = Animator.StringToHash("IsJumping");
    
    // For Cinemachine detection
    private bool isUsingCinemachine = false;
    
    // Camera state (for detection and cleanup)
    private bool isCameraChild = false;
    private Transform cameraParent; // Store original camera parent
    private GameObject cameraHolder; // For camera positioning (not used in third person)
    private GameObject cinemachineTarget; // For Cinemachine follow target
    
    // Zoom state
    private Component freeLookCamera; // Store FreeLook camera reference (as Component for easier access)
    private float currentZoomLevel = 1f; // 0 = min zoom, 1 = max zoom
    private float[] originalTopRigRadius;
    private float[] originalMidRigRadius;
    private float[] originalBottomRigRadius;
    
    // Properties for external access
    public bool IsWalking => networkIsWalking.Value;
    public bool IsJumping => networkIsJumping.Value;
    public bool IsGrounded => networkIsGrounded.Value;
    public float MoveSpeed => moveSpeed;
    public bool IsSlowed => isSlowed;
    
    // Network variables for syncing state
    private NetworkVariable<bool> networkIsGrounded = new NetworkVariable<bool>();
    private NetworkVariable<bool> networkIsWalking = new NetworkVariable<bool>();
    private NetworkVariable<bool> networkIsJumping = new NetworkVariable<bool>();
    
    private bool IsLocalPlayer
    {
        get
        {
            try
            {
                // For single-player or non-networked games
                if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsClient)
                {
                    return true;
                }
                
                // For networked games
                return IsOwner;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error checking IsLocalPlayer: {e.Message}. Defaulting to true.");
                return true;
            }
        }
    }
    
    private void Awake()
    {
        Debug.Log($"=== CharacterControllerMovement Awake() on {gameObject.name} ===");
        
        // Get required component
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError($"No CharacterController found on {gameObject.name}! Adding one...");
            characterController = gameObject.AddComponent<CharacterController>();
            
            // Set default values for CharacterController
            characterController.height = 2f;
            characterController.center = new Vector3(0, 1f, 0);
            characterController.radius = 0.5f;
        }
        
        Debug.Log($"CharacterController found/created: Height={characterController.height}, Center={characterController.center}, Radius={characterController.radius}");
        
        // Validate CharacterController settings
        if (characterController.stepOffset > characterController.height * 0.3f)
        {
            Debug.LogWarning($"CharacterController Step Offset ({characterController.stepOffset}) seems high. This might cause movement issues.");
        }
        
        if (characterController.minMoveDistance != 0)
        {
            Debug.LogWarning($"CharacterController Min Move Distance is {characterController.minMoveDistance}. Setting to 0 is recommended.");
            characterController.minMoveDistance = 0;
        }
        
        // Check for Rigidbody and ensure it's configured properly
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Freeze rotation on Rigidbody to prevent physics-based rotation
            rb.freezeRotation = true;
            Debug.Log("Found Rigidbody - freezing rotation to prevent physics interference");
        }
        
        // Try to get animator
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            Debug.Log($"Found animator on {animator.gameObject.name}");
            
            // Log all parameters in the animator
            Debug.Log($"Animator has {animator.parameters.Length} parameters:");
            foreach (var param in animator.parameters)
            {
                Debug.Log($"  - Parameter: '{param.name}' (hash: {param.nameHash}, type: {param.type})");
            }
            
            // Check for our expected parameters
            Debug.Log($"Looking for parameters with hashes - IsGrounded: {animIsGrounded}, IsWalking: {animIsWalking}, IsJumping: {animIsJumping}");
        }
        else
        {
            Debug.LogError("No Animator component found! Walking animations will not work. Make sure the character model has an Animator component.");
        }
        
        // Initialize NetworkVariables with default values
        networkIsGrounded.Value = false;
        networkIsWalking.Value = false;
        networkIsJumping.Value = false;
        
        // Disable conflicting scripts on the same GameObject
        DisableConflictingScripts();
        
        Debug.Log("CharacterControllerMovement Awake() completed");
    }
    
    private void DisableConflictingScripts()
    {
        // Disable old movement scripts if they exist
        var oldMovement = GetComponent("PlayerMovement");
        if (oldMovement != null)
        {
            ((MonoBehaviour)oldMovement).enabled = false;
            Debug.LogWarning("Disabled old PlayerMovement script to prevent conflicts");
        }
        
        var oldController = GetComponent("PlayerController");
        if (oldController != null)
        {
            ((MonoBehaviour)oldController).enabled = false;
            Debug.LogWarning("Disabled old PlayerController script to prevent conflicts");
        }
        
        // Disable any camera controller that might interfere
        var cameraController = GetComponent("CameraController");
        if (cameraController != null)
        {
            ((MonoBehaviour)cameraController).enabled = false;
            Debug.LogWarning("Disabled CameraController script to prevent conflicts");
        }
    }
    
    private void OnEnable()
    {
        // Reset velocity when enabled to prevent accumulated values
        velocity = Vector3.zero;
        jumpRequested = false;
        isStabilized = false;
        
        if (debugMovement)
        {
            Debug.Log("CharacterControllerMovement enabled - velocity reset to zero");
        }
    }
    
    private void Start()
    {
        Debug.Log("=== CharacterControllerMovement Start() ===");
        startupTime = Time.time;
        
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"IsLocalPlayer: {IsLocalPlayer}");
        Debug.Log($"IsOwner: {IsOwner}");
        Debug.Log($"NetworkManager exists: {NetworkManager.Singleton != null}");
        
        if (!IsLocalPlayer)
        {
            Debug.Log("Not local player - disabling component");
            enabled = false;
            return;
        }
        
        // Validate initial position
        ValidateAndFixTransform();
        
        // Force position to ground on spawn
        if (characterController != null)
        {
            // Do a quick ground snap
            RaycastHit hit;
            // Cast from slightly above the character to ensure we hit the ground
            Vector3 rayStart = transform.position + Vector3.up * 2f;
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, groundLayer))
            {
                // Calculate the exact position where the bottom of the capsule should be
                // The capsule's bottom is at: position.y + center.y - height/2
                // We want this to equal hit.point.y (ground level)
                // So: position.y = hit.point.y - center.y + height/2
                
                float targetY = hit.point.y - characterController.center.y + (characterController.height / 2f) + groundPositionOffset;
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                
                if (debugMovement)
                {
                    Debug.Log($"=== Ground Snap Debug ===");
                    Debug.Log($"Hit point Y: {hit.point.y}");
                    Debug.Log($"CharacterController - Height: {characterController.height}, Center: {characterController.center}");
                    Debug.Log($"Calculated Y position: {targetY}");
                    Debug.Log($"Final position: {transform.position}");
                    Debug.Log($"Bottom of capsule should be at Y: {transform.position.y + characterController.center.y - characterController.height/2f}");
                    Debug.Log("========================");
                }
            }
            else
            {
                Debug.LogError($"Failed to snap to ground! Make sure:");
                Debug.LogError($"1. Your floor/ground objects have the correct layer");
                Debug.LogError($"2. Ground Layer Mask is set correctly (current value: {groundLayer.value})");
                Debug.LogError($"3. The character is spawned above ground, not inside it");
            }
        }
        
        // Auto-find camera if enabled and not assigned
        if (autoFindCamera && cameraTransform == null)
        {
            FindCamera();
        }
        
        if (!IsLocalPlayer) return;
        
        // Log ground layer info
        if (debugMovement)
        {
            Debug.Log($"Ground Layer Mask Value: {groundLayer.value}, Binary: {System.Convert.ToString(groundLayer.value, 2)}");
            string[] layers = new string[32];
            for (int i = 0; i < 32; i++)
            {
                if ((groundLayer.value & (1 << i)) != 0)
                {
                    layers[i] = LayerMask.LayerToName(i);
                    Debug.Log($"Ground check will include layer {i}: {layers[i]}");
                }
            }
        }
        
        // Setup camera for local player
        SetupCameraForLocalPlayer();
        
        // Try to find GameInput
        FindGameInput();
        
        // Subscribe to jump input if GameInput is available
        SubscribeToJumpInput();
        
        // Give Cinemachine time to initialize
        if (isUsingCinemachine)
        {
            StartCoroutine(DelayedCinemachineSetup());
        }
        
        // Initial ground check
        CheckGrounded();
        
        // Start stabilization coroutine
        StartCoroutine(StabilizationRoutine());
        
        // Validate Time settings
        if (Time.fixedDeltaTime > 0.025f || Time.fixedDeltaTime < 0.015f)
        {
            Debug.LogWarning($"Fixed timestep is {Time.fixedDeltaTime}. Recommended value is 0.02 (50Hz). This may affect movement speed!");
        }
        
        // Store base speeds
        baseMoveSpeed = moveSpeed;
        baseSprintSpeed = sprintSpeed;
        
        // Initialize velocity
        velocity = Vector3.zero;
        
        // Initialize rotation values
        verticalRotation = 0f;
        
        // Store initial valid position
        lastValidPosition = transform.position;
        lastValidRotation = transform.rotation;
        lastStablePosition = transform.position; // Initialize stable position
        
        // Set default ground layer if not set
        if (groundLayer == -1)
        {
            groundLayer = LayerMask.GetMask("Ground");
            if (groundLayer == 0)
            {
                Debug.LogWarning("Ground layer not found! Using Default layer. Create a 'Ground' layer for better results.");
                groundLayer = LayerMask.GetMask("Default");
            }
        }
        
        // Cache main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
            if (mainCamera != null && mainCamera.tag != "MainCamera")
            {
                Debug.LogWarning($"Found camera '{mainCamera.name}' but it's not tagged as MainCamera. Consider tagging it properly.");
            }
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("No camera found in scene! Movement will use fallback world-space controls.");
        }
    }
    
    private IEnumerator StabilizationRoutine()
    {
        // Wait for stabilization period
        yield return new WaitForSeconds(stabilizationTime);
        
        // Final position validation
        ValidateAndFixTransform();
        
        // Reset velocity to ensure clean start
        velocity = Vector3.zero;
        
        // Ensure we're grounded
        CheckGrounded();
        if (isGrounded)
        {
            velocity.y = 0f;
        }
        
        // Mark as stabilized
        isStabilized = true;
        
        Debug.Log($"Character movement stabilized and ready. Position: {transform.position}, Velocity: {velocity}");
    }
    
    private IEnumerator DelayedCinemachineSetup()
    {
        // Wait for 1 frame to ensure Cinemachine is fully initialized
        yield return null;
        SetCinemachineTarget();
    }
    
    private void ValidateAndFixTransform()
    {
        Vector3 currentPos = transform.position;
        
        // Check for NaN or extreme positions
        if (float.IsNaN(currentPos.x) || float.IsNaN(currentPos.y) || float.IsNaN(currentPos.z) ||
            float.IsInfinity(currentPos.x) || float.IsInfinity(currentPos.y) || float.IsInfinity(currentPos.z))
        {
            Debug.LogError($"NaN/Infinity position detected! Resetting to last valid position. Current: {currentPos}");
            transform.position = lastValidPosition;
            velocity = Vector3.zero; // Reset velocity when position is invalid
            return;
        }
        
        // Check for positions that are too far from origin (likely errors)
        float maxDistance = 1000f;
        if (currentPos.magnitude > maxDistance)
        {
            Debug.LogError($"Position too far from origin: {currentPos}. Resetting to last valid position.");
            transform.position = lastValidPosition;
            velocity = Vector3.zero; // Reset velocity when position is extreme
            return;
        }
        
        // Check for extreme velocity values
        if (Mathf.Abs(velocity.y) > maxFallSpeed * 2f)
        {
            Debug.LogError($"Extreme velocity detected: {velocity}. Resetting velocity.");
            velocity.y = Mathf.Sign(velocity.y) * maxFallSpeed;
        }
        
        // Additional check for sudden position changes
        if (lastValidPosition != Vector3.zero)
        {
            float distanceMoved = Vector3.Distance(currentPos, lastValidPosition);
            float maxDistancePerFrame = 50f * Time.deltaTime; // Max 50 units per second
            
            if (distanceMoved > maxDistancePerFrame)
            {
                Debug.LogWarning($"Sudden position change detected! Distance: {distanceMoved}, Max allowed: {maxDistancePerFrame}");
                // Don't reset position here as it might be a valid teleport, but log it
            }
        }
        
        // Update last valid position if current position is reasonable
        lastValidPosition = currentPos;
        
        // Check and fix rotation
        Quaternion rot = transform.rotation;
        if (float.IsNaN(rot.x) || float.IsNaN(rot.y) || float.IsNaN(rot.z) || float.IsNaN(rot.w))
        {
            Debug.LogWarning($"Invalid rotation detected on {gameObject.name}. Resetting to last valid rotation.");
            transform.rotation = lastValidRotation;
        }
        else
        {
            lastValidRotation = rot;
        }
    }
    
    private void FindCamera()
    {
        // First try to find camera as child
        Camera childCamera = GetComponentInChildren<Camera>();
        if (childCamera != null)
        {
            cameraTransform = childCamera.transform;
            isCameraChild = true;
            Debug.Log($"Found camera as child: {childCamera.name}");
            
            // Check if child camera has Cinemachine
            var cinemachineBrain = childCamera.GetComponent(System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine"));
            if (cinemachineBrain != null)
            {
                isUsingCinemachine = true;
            }
            return;
        }
        
        // Search all cameras and prioritize FreeLook
        Camera[] allCameras = FindObjectsOfType<Camera>();
        Camera targetCamera = null;
        bool foundFreeLook = false;
        
        // First priority: FreeLook camera (check all cameras for FreeLook first)
        foreach (var cam in allCameras)
        {
            if (cam.name.ToLower().Contains("freelook") || cam.name.ToLower().Contains("free look"))
            {
                targetCamera = cam;
                foundFreeLook = true;
                Debug.Log($"Found FreeLook camera: {cam.name}");
                break;
            }
        }
        
        // If no FreeLook found, then check for Main camera
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera != null)
            {
                Debug.Log($"Using main camera: {targetCamera.name}");
            }
        }
        
        // Third priority: Any camera
        if (targetCamera == null && allCameras.Length > 0)
        {
            targetCamera = allCameras[0];
            Debug.Log($"Using first available camera: {targetCamera.name}");
        }
        
        if (targetCamera != null)
        {
            cameraTransform = targetCamera.transform;
            cameraParent = cameraTransform.parent; // Store original parent
            isCameraChild = false;
            
            // Check if we're using Cinemachine (but after we've selected the camera)
            var cinemachineBrain = FindObjectOfType(System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine"));
            if (cinemachineBrain != null)
            {
                isUsingCinemachine = true;
                // If we found a FreeLook camera, that's what we'll use, not the brain camera
                if (!foundFreeLook)
                {
                    // Only use brain camera if we didn't find a FreeLook
                    cameraTransform = ((Component)cinemachineBrain).transform;
                }
                Debug.Log($"Detected Cinemachine setup. Using camera: {cameraTransform.name}");
            }
        }
        else
        {
            Debug.LogError("No camera found! Please assign a camera or create one.");
        }
    }
    
    private void SetupCameraForLocalPlayer()
    {
        if (!IsLocalPlayer) return;
        
        // For third-person with Cinemachine, we don't parent the camera
        // Instead, we set up the Cinemachine virtual camera to follow this player
        
        if (isUsingCinemachine)
        {
            Debug.Log("Setting up Cinemachine for third-person camera");
            
            // Find FreeLook camera and set this player as the follow/look target
            var freeLookType = System.Type.GetType("Cinemachine.CinemachineFreeLook, Cinemachine");
            if (freeLookType != null)
            {
                var freeLookCameras = FindObjectsOfType(freeLookType);
                foreach (var cam in freeLookCameras)
                {
                    // Set Follow target
                    var followProp = cam.GetType().GetProperty("Follow");
                    if (followProp != null)
                    {
                        followProp.SetValue(cam, transform);
                        Debug.Log($"Set FreeLook camera '{((Component)cam).name}' to follow player");
                    }
                    
                    // Set LookAt target (usually the same as Follow for third person)
                    var lookAtProp = cam.GetType().GetProperty("LookAt");
                    if (lookAtProp != null)
                    {
                        lookAtProp.SetValue(cam, transform);
                        Debug.Log($"Set FreeLook camera '{((Component)cam).name}' to look at player");
                    }
                    
                    // Store the FreeLook camera reference for zoom
                    if (freeLookCamera == null)
                    {
                        freeLookCamera = cam as Component;
                        StoreOriginalRadiusValues(cam);
                    }
                }
            }
            
            // Also check for regular virtual cameras
            var virtualCameraType = System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
            if (virtualCameraType != null)
            {
                var virtualCameras = FindObjectsOfType(virtualCameraType);
                foreach (var cam in virtualCameras)
                {
                    var followProp = cam.GetType().GetProperty("Follow");
                    if (followProp != null)
                    {
                        followProp.SetValue(cam, transform);
                    }
                    
                    var lookAtProp = cam.GetType().GetProperty("LookAt");
                    if (lookAtProp != null)
                    {
                        lookAtProp.SetValue(cam, transform);
                    }
                }
            }
        }
        
        // Don't lock cursor for third-person games
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void SetCinemachineTarget()
    {
        // For third-person, set the player as the Follow and LookAt target
        // This is called after Cinemachine has initialized
        
        Debug.Log("Setting Cinemachine targets for third-person camera");
        
        // Set FreeLook cameras
        var freeLookType = System.Type.GetType("Cinemachine.CinemachineFreeLook, Cinemachine");
        if (freeLookType != null)
        {
            var freeLookCameras = FindObjectsOfType(freeLookType);
            foreach (var cam in freeLookCameras)
            {
                var followProp = cam.GetType().GetProperty("Follow");
                if (followProp != null)
                {
                    followProp.SetValue(cam, transform);
                }
                
                var lookAtProp = cam.GetType().GetProperty("LookAt");
                if (lookAtProp != null)
                {
                    lookAtProp.SetValue(cam, transform);
                }
                
                // Store the FreeLook camera reference for zoom
                if (freeLookCamera == null)
                {
                    freeLookCamera = cam as Component;
                    StoreOriginalRadiusValues(cam);
                }
                
                Debug.Log($"Updated FreeLook camera targets: {((Component)cam).name}");
            }
        }
        
        // Set regular virtual cameras
        var virtualCameraType = System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
        if (virtualCameraType != null)
        {
            var virtualCameras = FindObjectsOfType(virtualCameraType);
            foreach (var cam in virtualCameras)
            {
                var followProp = cam.GetType().GetProperty("Follow");
                if (followProp != null)
                {
                    followProp.SetValue(cam, transform);
                }
                
                var lookAtProp = cam.GetType().GetProperty("LookAt");
                if (lookAtProp != null)
                {
                    lookAtProp.SetValue(cam, transform);
                }
            }
        }
    }
    
    private void StoreOriginalRadiusValues(object freeLookCam)
    {
        try
        {
            var orbitsProperty = freeLookCam.GetType().GetProperty("m_Orbits");
            if (orbitsProperty != null)
            {
                var orbits = orbitsProperty.GetValue(freeLookCam);
                if (orbits != null && orbits.GetType().IsArray)
                {
                    var orbitsArray = (Array)orbits;
                    originalTopRigRadius = new float[1];
                    originalMidRigRadius = new float[1];
                    originalBottomRigRadius = new float[1];
                    
                    for (int i = 0; i < orbitsArray.Length && i < 3; i++)
                    {
                        var orbit = orbitsArray.GetValue(i);
                        if (orbit != null)
                        {
                            var radiusField = orbit.GetType().GetField("m_Radius");
                            if (radiusField != null)
                            {
                                float radius = (float)radiusField.GetValue(orbit);
                                switch (i)
                                {
                                    case 0: originalTopRigRadius[0] = radius; break;
                                    case 1: originalMidRigRadius[0] = radius; break;
                                    case 2: originalBottomRigRadius[0] = radius; break;
                                }
                                Debug.Log($"Stored radius for rig {i}: {radius}");
                            }
                        }
                    }
                    Debug.Log($"Stored original radius values - Top: {originalTopRigRadius[0]}, Mid: {originalMidRigRadius[0]}, Bottom: {originalBottomRigRadius[0]}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to store original radius values: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up camera holder
        if (cameraHolder != null)
        {
            Destroy(cameraHolder);
        }
        
        // Clean up Cinemachine target
        if (cinemachineTarget != null)
        {
            Destroy(cinemachineTarget);
        }
        
        // Restore camera to original parent if we changed it
        if (!isCameraChild && cameraTransform != null && cameraParent != null && !isUsingCinemachine)
        {
            cameraTransform.SetParent(cameraParent);
        }
        
        // Unsubscribe from jump event if needed
        if (gameInputComponent != null)
        {
            var eventInfo = gameInputComponent.GetType().GetEvent("OnJumpAction");
            if (eventInfo != null)
            {
                var handler = new EventHandler(OnJump);
                eventInfo.RemoveEventHandler(gameInputComponent, handler);
            }
        }
    }
    
    private void FindGameInput()
    {
        // First try to use the assigned GameObject
        if (gameInputObject != null)
        {
            gameInputComponent = gameInputObject.GetComponent("GameInput") as MonoBehaviour;
        }
        
        // If not found, try to find the singleton instance using reflection
        if (gameInputComponent == null)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                var component = obj.GetComponent("GameInput");
                if (component != null)
                {
                    gameInputComponent = component as MonoBehaviour;
                    Debug.Log("Found GameInput in scene");
                    break;
                }
            }
        }
        
        if (gameInputComponent == null)
        {
            Debug.LogWarning("GameInput not found! Movement will use direct Input.GetAxis instead.");
        }
    }
    
    private void SubscribeToJumpInput()
    {
        if (gameInputComponent != null)
        {
            // Use reflection to subscribe to the jump event
            var eventInfo = gameInputComponent.GetType().GetEvent("OnJumpAction");
            if (eventInfo != null)
            {
                var handler = new EventHandler(OnJump);
                eventInfo.AddEventHandler(gameInputComponent, handler);
            }
        }
    }
    
    private void Update()
    {
        // Safety check
        if (characterController == null)
        {
            Debug.LogError($"CharacterController is null in Update! GameObject: {gameObject.name}");
            return;
        }
        
        if (!IsLocalPlayer) return;
        
        // Don't process anything during stabilization period
        if (!isStabilized)
        {
            return;
        }
        
        // Validate transform at start of update
        ValidateAndFixTransform();
        
        // Handle slow timer
        if (slowTimer > 0)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0)
            {
                ExitCastMovementSlow();
            }
        }
        
        // Check for jump input in Update (for better responsiveness)
        if (Input.GetKeyDown(KeyCode.Space) && gameInputComponent == null)
        {
            OnJump(null, null);
        }
        
        // Alternative animation update (in case LateUpdate isn't working)
        if (animator != null && Time.frameCount % 5 == 0) // Update every 5 frames
        {
            // Try to set animator parameters directly
            try
            {
                animator.SetBool("IsGrounded", isGrounded);
                animator.SetBool("IsWalking", pendingMovement.magnitude > 0.01f);
                animator.SetBool("IsJumping", velocity.y > 0);
                
                if (debugMovement)
                {
                    Debug.Log($"Direct Animator Update - Grounded: {isGrounded}, Walking: {pendingMovement.magnitude > 0.01f}, Jumping: {velocity.y > 0}");
                }
            }
            catch (Exception e)
            {
                if (debugMovement)
                {
                    Debug.LogError($"Failed to set animator parameters: {e.Message}");
                }
            }
        }
        
        // Runtime speed adjustment for testing (only in editor)
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            movementMultiplier = 0.5f;
            Debug.Log($"Movement multiplier set to {movementMultiplier}");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            movementMultiplier = 1f;
            Debug.Log($"Movement multiplier set to {movementMultiplier}");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            movementMultiplier = 1.5f;
            Debug.Log($"Movement multiplier set to {movementMultiplier}");
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            // Debug camera hierarchy
            Debug.Log("=== Camera Debug Info ===");
            Debug.Log($"Player Position: {transform.position}, Rotation: {transform.eulerAngles}");
            if (cameraHolder != null)
            {
                Debug.Log($"CameraHolder LocalPos: {cameraHolder.transform.localPosition}, LocalRot: {cameraHolder.transform.localEulerAngles}");
            }
            if (cameraTransform != null)
            {
                Debug.Log($"Camera LocalPos: {cameraTransform.localPosition}, LocalRot: {cameraTransform.localEulerAngles}");
                Debug.Log($"Camera WorldPos: {cameraTransform.position}, WorldRot: {cameraTransform.eulerAngles}");
            }
            Debug.Log($"Vertical Rotation: {verticalRotation}");
            Debug.Log("========================");
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            // Debug CharacterController settings
            Debug.Log("=== CharacterController Info ===");
            Debug.Log($"Height: {characterController.height}");
            Debug.Log($"Center: {characterController.center}");
            Debug.Log($"Radius: {characterController.radius}");
            Debug.Log($"Skin Width: {characterController.skinWidth}");
            Debug.Log($"Step Offset: {characterController.stepOffset}");
            Debug.Log($"Slope Limit: {characterController.slopeLimit}");
            Debug.Log($"Transform Position: {transform.position}");
            Debug.Log($"Bottom of capsule Y: {transform.position.y + characterController.center.y - characterController.height/2f}");
            Debug.Log("================================");
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            // Debug complete state
            Debug.Log("=== COMPLETE DEBUG STATE ===");
            Debug.Log($"-- Movement --");
            Debug.Log($"IsGrounded: {isGrounded}");
            Debug.Log($"Velocity: {velocity}");
            Debug.Log($"Pending Movement: {pendingMovement}");
            Debug.Log($"Move Speed: {moveSpeed}, Sprint Speed: {sprintSpeed}");
            
            Debug.Log($"-- Animation --");
            Debug.Log($"Animator: {(animator != null ? "Found" : "NULL")}");
            if (animator != null)
            {
                Debug.Log($"Animator Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");
                Debug.Log($"Network IsWalking: {networkIsWalking.Value}");
                Debug.Log($"Network IsJumping: {networkIsJumping.Value}");
                Debug.Log($"Network IsGrounded: {networkIsGrounded.Value}");
            }
            
            Debug.Log($"-- Camera/Zoom --");
            Debug.Log($"IsUsingCinemachine: {isUsingCinemachine}");
            Debug.Log($"FreeLook Camera: {(freeLookCamera != null ? "Found" : "NULL")}");
            Debug.Log($"Current Zoom Level: {currentZoomLevel}");
            
            Debug.Log($"-- Position --");
            Debug.Log($"Ground Position Offset: {groundPositionOffset}");
            Debug.Log($"Transform Y: {transform.position.y}");
            Debug.Log($"Bottom of Capsule Y: {transform.position.y + characterController.center.y - characterController.height/2f}");
            Debug.Log("============================");
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            // Test simple forward movement
            Debug.Log("=== TEST: Moving forward without camera ===");
            Vector3 testMovement = transform.forward * moveSpeed * Time.deltaTime;
            characterController.Move(testMovement);
            Debug.Log($"Moved {testMovement.magnitude} units forward");
            Debug.Log($"New position: {transform.position}");
        }
        else if (Input.GetKeyDown(KeyCode.G))
        {
            // Toggle debug mode
            debugMovement = !debugMovement;
            Debug.Log($"Debug movement mode: {(debugMovement ? "ENABLED" : "DISABLED")}");
        }
        #endif
        
        if (!movementStopped)
        {
            // Handle rotation in Update for smooth camera movement
            HandleRotation();
            
            // Calculate movement but don't apply it yet (will be applied in FixedUpdate)
            CalculateMovement();
        }
        
        // Handle camera zoom
        HandleCameraZoom();
    }
    
    private void FixedUpdate()
    {
        // Safety check
        if (characterController == null)
        {
            Debug.LogError($"CharacterController is null in FixedUpdate! GameObject: {gameObject.name}");
            return;
        }
        
        if (!IsLocalPlayer) return;
        
        // Don't process physics during stabilization period
        if (!isStabilized)
        {
            // Just ensure we're grounded during stabilization
            CheckGrounded();
            // Reset velocity to prevent accumulation
            velocity = Vector3.zero;
            return;
        }
        
        // Always validate transform in FixedUpdate to catch physics issues
        ValidateAndFixTransform();
        
        // Check grounded state
        CheckGrounded();
        
        // Apply physics-based movement in FixedUpdate
        ApplyMovement();
        
        // Update network state
        if (IsOwner)
        {
            // Check if we're actually moving horizontally (not just falling)
            bool isWalking = currentHorizontalVelocity.magnitude > 0.1f;
            UpdateNetworkStateServerRpc(isGrounded, isWalking, velocity.y > 0);
        }
        
        // Final validation at end of FixedUpdate
        ValidateAndFixTransform();
    }
    
    private void LateUpdate()
    {
        if (!IsLocalPlayer) return;
        
        // Update animator parameters
        if (animator != null)
        {
            // Set the animation parameters based on network state
            if (animator.HasParameter(animIsGrounded))
                animator.SetBool(animIsGrounded, isGrounded);
            
            if (animator.HasParameter(animIsWalking))
                animator.SetBool(animIsWalking, networkIsWalking.Value);
            
            if (animator.HasParameter(animIsJumping))
                animator.SetBool(animIsJumping, networkIsJumping.Value);
            
            if (debugMovement)
            {
                Debug.Log($"Animator Update - Grounded: {isGrounded}, Walking: {networkIsWalking.Value}, Jumping: {networkIsJumping.Value}");
            }
        }
        
        // Store current rotation as last valid
        lastValidRotation = transform.rotation;
    }
    
    private void CheckGrounded()
    {
        wasGrounded = isGrounded;
        
        // Use multiple methods for ground detection
        bool groundedByController = characterController.isGrounded;
        
        // Sphere check at the bottom of the capsule
        float bottomY = characterController.center.y - characterController.height / 2f;
        Vector3 sphereCenter = transform.position + Vector3.up * (bottomY + characterController.radius);
        float checkRadius = characterController.radius * 0.9f;
        bool groundedBySphere = Physics.CheckSphere(sphereCenter, checkRadius, groundLayer);
        
        // Raycast check from center of capsule downward
        Vector3 rayStart = transform.position + characterController.center;
        float rayDistance = (characterController.height / 2f) + groundCheckDistance + 0.1f;
        bool groundedByRaycast = Physics.Raycast(rayStart, Vector3.down, rayDistance, groundLayer);
        
        // Combined ground check - more lenient
        isGrounded = groundedByController || groundedBySphere || groundedByRaycast;
        
        // Landing detection - reset velocity when we land
        if (!wasGrounded && isGrounded)
        {
            // We just landed
            if (velocity.y < -5f) // Only log significant landings
            {
                if (debugMovement)
                    Debug.Log($"Landed with velocity: {velocity.y}");
            }
            
            // Reset downward velocity on landing
            if (velocity.y < 0)
            {
                velocity.y = -0.1f; // Small downward force to stick to ground (reduced from -2f)
            }
        }
        
        if (debugMovement)
        {
            Debug.Log($"Ground Detection - Controller: {groundedByController}, Sphere: {groundedBySphere}, " +
                     $"Raycast: {groundedByRaycast}, Final: {isGrounded}, Velocity.Y: {velocity.y:F2}");
        }
    }
    
    private void HandleRotation()
    {
        // For third-person: Player rotates to face movement direction
        // Camera rotation is handled by Cinemachine FreeLook
        
        // Use current velocity for smoother rotation that matches actual movement
        if (currentHorizontalVelocity.magnitude > 0.1f)
        {
            // Calculate the target rotation based on movement direction
            Quaternion targetRotation = Quaternion.LookRotation(currentHorizontalVelocity.normalized);
            
            // Smoothly rotate the player to face movement direction
            float rotationSpeed = 10f;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            if (debugRotation)
            {
                Debug.Log($"Rotating player to face movement direction: {currentHorizontalVelocity.normalized}");
            }
        }
    }
    
    private Vector2 GetMovementInput()
    {
        // Try to get input from GameInput component first
        if (gameInputComponent != null)
        {
            try
            {
                var method = gameInputComponent.GetType().GetMethod("GetMovementVector");
                if (method != null)
                {
                    var result = method.Invoke(gameInputComponent, null);
                    if (result is Vector2)
                    {
                        return (Vector2)result;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to get movement from GameInput: {e.Message}");
            }
        }
        
        // Fallback to direct input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        // Clamp input values
        h = Mathf.Clamp(h, -1f, 1f);
        v = Mathf.Clamp(v, -1f, 1f);
        
        return new Vector2(h, v);
    }
    
    private void CalculateMovement()
    {
        // Get input
        Vector2 inputVector = GetMovementInput();
        
        // Calculate movement direction relative to camera
        pendingMovement = Vector3.zero;
        
        if (inputVector.magnitude > 0.01f) // Dead zone
        {
            // Use cached camera or try to find one
            if (mainCamera == null)
            {
                mainCamera = Camera.main ?? Camera.current ?? FindObjectOfType<Camera>();
                
                if (mainCamera == null)
                {
                    Debug.LogError("No camera found! Movement cannot be calculated. Please ensure there's a camera in the scene tagged as 'MainCamera'");
                    // Fallback to world-space movement
                    pendingMovement = new Vector3(inputVector.x, 0, inputVector.y);
                    return;
                }
            }
            
            // Get camera forward and right directions
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;
            
            // Project onto horizontal plane (remove Y component)
            cameraForward.y = 0f;
            cameraForward.Normalize();
            cameraRight.y = 0f;
            cameraRight.Normalize();
            
            // Calculate movement relative to camera view
            pendingMovement = cameraRight * inputVector.x + cameraForward * inputVector.y;
            
            // Normalize to prevent diagonal speed boost
            if (pendingMovement.magnitude > 1f)
            {
                pendingMovement.Normalize();
            }
            
            if (debugMovement)
            {
                Debug.Log($"Camera-relative movement: Input({inputVector.x}, {inputVector.y}) -> World({pendingMovement.x}, {pendingMovement.z})");
            }
        }
    }
    
    private void ApplyMovement()
    {
        // Position stabilization - detect if we're stationary
        bool isStationary = pendingMovement.magnitude < 0.01f && Mathf.Abs(velocity.y) < 0.1f && isGrounded;
        
        if (isStationary)
        {
            // Track how long we've been stationary
            positionStableTime += Time.fixedDeltaTime;
            
            if (positionStableTime > STABILITY_TIME_REQUIRED)
            {
                // Lock to stable position to prevent micro-oscillations
                Vector3 currentPos = transform.position;
                Vector3 diff = currentPos - lastStablePosition;
                
                // If we've moved less than the threshold, snap back to stable position
                if (diff.magnitude < POSITION_STABILITY_THRESHOLD)
                {
                    transform.position = lastStablePosition;
                    velocity = Vector3.zero;
                    currentHorizontalVelocity = Vector3.zero; // Reset horizontal velocity when stabilized
                    
                    if (debugMovement)
                    {
                        Debug.Log($"Position stabilized. Diff was: {diff.magnitude}");
                    }
                    return; // Skip the rest of movement processing
                }
            }
        }
        else
        {
            // We're moving, reset stability tracking
            positionStableTime = 0f;
            lastStablePosition = transform.position;
        }
        
        // Debug log velocity at start
        if (debugMovement && Mathf.Abs(velocity.y) > 5f)
        {
            Debug.LogWarning($"High velocity at start of ApplyMovement: {velocity}");
        }
        
        // Clamp velocity at start to prevent any accumulated values
        if (Mathf.Abs(velocity.y) > 50f)
        {
            Debug.LogError($"Excessive velocity detected: {velocity}. Clamping to safe values.");
            velocity.y = Mathf.Clamp(velocity.y, -50f, 50f);
        }
        
        // Apply movement with inertia
        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        targetSpeed *= movementMultiplier; // Apply global multiplier
        
        // Calculate target velocity based on input
        Vector3 targetVelocity = pendingMovement * targetSpeed;
        
        // Apply acceleration/deceleration for smooth movement
        float accel = acceleration;
        float decel = deceleration;
        
        // Use different values in air
        if (!isGrounded)
        {
            accel = airAcceleration;
            decel = airDeceleration;
        }
        
        // Smoothly interpolate current velocity to target velocity
        if (targetVelocity.magnitude > 0.01f)
        {
            // Accelerating towards target
            currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetVelocity, accel * Time.fixedDeltaTime);
        }
        else
        {
            // Decelerating to stop
            currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, Vector3.zero, decel * Time.fixedDeltaTime);
            
            // Stop completely when very slow
            if (currentHorizontalVelocity.magnitude < 0.1f)
            {
                currentHorizontalVelocity = Vector3.zero;
            }
        }
        
        // Apply the smoothed movement
        if (currentHorizontalVelocity.magnitude > 0.01f)
        {
            Vector3 movement = currentHorizontalVelocity * Time.fixedDeltaTime;
            
            // Apply additional threshold to prevent micro-movements
            if (movement.magnitude < POSITION_STABILITY_THRESHOLD)
            {
                if (debugMovement)
                {
                    Debug.Log($"Movement too small, ignoring: {movement.magnitude}");
                }
                movement = Vector3.zero;
            }
            
            if (debugMovement && movement.magnitude > 0f)
            {
                Debug.Log($"Movement Debug - Target Speed: {targetSpeed}, Current Velocity: {currentHorizontalVelocity.magnitude}, DeltaTime: {Time.fixedDeltaTime}, Movement: {movement.magnitude}/frame");
            }
            
            // Ensure movement is valid
            if (movement != Vector3.zero && !float.IsNaN(movement.x) && !float.IsNaN(movement.y) && !float.IsNaN(movement.z))
            {
                characterController.Move(movement);
            }
        }
        
        // Handle jump
        if (jumpRequested && isGrounded && Time.time - lastJumpTime > jumpCooldown)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            velocity.y = Mathf.Clamp(velocity.y, 0f, 20f);
            lastJumpTime = Time.time;
            jumpRequested = false;
            
            if (debugMovement)
            {
                Debug.Log($"Jump initiated! Velocity.y set to: {velocity.y}");
            }
        }
        
        // Apply gravity with smoother ground handling
        float previousVelY = velocity.y;
        
        if (isGrounded)
        {
            // When grounded, only apply minimal downward force if we're not already on the ground
            if (velocity.y < 0)
            {
                // Use a much smaller value to prevent bouncing
                velocity.y = -0.1f; // Reduced from -2f
            }
            // If we're grounded but have upward velocity (from jump), don't interfere
        }
        else
        {
            // Apply gravity when in air
            velocity.y += gravity * Time.fixedDeltaTime;
            
            // Clamp falling speed to prevent extreme velocities
            if (velocity.y < -maxFallSpeed)
            {
                velocity.y = -maxFallSpeed;
                if (debugMovement)
                {
                    Debug.LogWarning($"Clamped falling speed to max: {maxFallSpeed}");
                }
            }
        }
        
        // Debug log large velocity changes
        if (debugMovement && Mathf.Abs(previousVelY - velocity.y) > 10f)
        {
            Debug.LogError($"HUGE velocity change! Previous: {previousVelY}, New: {velocity.y}, Grounded: {isGrounded}");
        }
        
        // Final safety check before applying
        if (Mathf.Abs(velocity.y) > maxFallSpeed * 1.5f)
        {
            Debug.LogError($"Dangerous velocity detected: {velocity.y}. Hard clamping!");
            velocity.y = Mathf.Sign(velocity.y) * maxFallSpeed;
        }
        
        // Apply velocity movement only if needed
        if (!isGrounded || Mathf.Abs(velocity.y) > 0.01f) // Added threshold to prevent tiny movements
        {
            Vector3 velocityMovement = new Vector3(0, velocity.y * Time.fixedDeltaTime, 0);
            
            // Only apply if the movement is significant enough
            if (Mathf.Abs(velocityMovement.y) > 0.0001f) // Prevent micro-movements
            {
                if (debugMovement && Mathf.Abs(velocityMovement.y) > 0.5f)
                {
                    Debug.Log($"Velocity movement this frame: {velocityMovement.y} units");
                }
                
                if (!float.IsNaN(velocityMovement.y))
                {
                    characterController.Move(velocityMovement);
                }
            }
        }
        
        // Clear pending movement
        pendingMovement = Vector3.zero;
    }
    
    private void OnJump(object sender, EventArgs e)
    {
        if (!IsLocalPlayer) return;
        
        // Set jump request flag (will be processed in FixedUpdate)
        if (isGrounded && !movementStopped && Time.time - lastJumpTime > jumpCooldown)
        {
            jumpRequested = true;
        }
    }
    
    [ServerRpc]
    private void UpdateNetworkStateServerRpc(bool grounded, bool walking, bool jumping)
    {
        networkIsGrounded.Value = grounded;
        networkIsWalking.Value = walking;
        networkIsJumping.Value = jumping;
    }
    
    // IMovementEffects implementation
    public void EnterCastMovementSlow(float newMoveSpeedValue, float animationMultiplier)
    {
        isSlowed = true;
        moveSpeed = newMoveSpeedValue;
        sprintSpeed = newMoveSpeedValue * 1.5f;
        
        if (animator != null)
        {
            animator.SetFloat("newSpeed", animationMultiplier);
        }
    }
    
    public void ExitCastMovementSlow()
    {
        isSlowed = false;
        moveSpeed = baseMoveSpeed;
        sprintSpeed = baseSprintSpeed;
        slowTimer = 0f;
        
        if (animator != null)
        {
            animator.SetFloat("newSpeed", 1f);
        }
    }
    
    public void ApplyMovementSlow(float slowAmount, float duration)
    {
        float slowedSpeed = baseMoveSpeed * (1f - slowAmount);
        EnterCastMovementSlow(slowedSpeed, 1f - slowAmount);
        slowTimer = duration;
    }
    
    public void ApplyKnockback(Vector3 force)
    {
        // Validate knockback force
        if (!float.IsNaN(force.x) && !float.IsNaN(force.y) && !float.IsNaN(force.z))
        {
            // Clamp knockback force more aggressively
            force = Vector3.ClampMagnitude(force, 20f);
            
            // Prevent excessive upward force
            force.y = Mathf.Clamp(force.y, -10f, 10f);
            
            if (debugMovement)
            {
                Debug.Log($"Applying knockback: {force}");
            }
            
            velocity += force;
            
            // Clamp total velocity after adding knockback
            velocity = Vector3.ClampMagnitude(velocity, 30f);
        }
    }
    
    public void StopMovement()
    {
        movementStopped = true;
        jumpRequested = false;
    }
    
    public void ResumeMovement()
    {
        movementStopped = false;
    }
    
    public float GetCurrentMoveSpeed()
    {
        return moveSpeed;
    }
    
    // Public methods for external systems
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Clamp(newSpeed, 0f, 50f);
    }
    
    public void ApplyExternalForce(Vector3 force)
    {
        // Validate force
        if (!float.IsNaN(force.x) && !float.IsNaN(force.y) && !float.IsNaN(force.z))
        {
            // More aggressive clamping for external forces
            force = Vector3.ClampMagnitude(force, 20f);
            
            // Prevent excessive upward force
            force.y = Mathf.Clamp(force.y, -10f, 10f);
            
            if (debugMovement)
            {
                Debug.Log($"Applying external force: {force}");
            }
            
            velocity += force;
            
            // Clamp total velocity after adding force
            velocity = Vector3.ClampMagnitude(velocity, 30f);
        }
    }
    
    public Vector3 GetVelocity()
    {
        return characterController.velocity;
    }
    
    // Public method to reset camera alignment if it gets out of sync
    public void ResetCameraAlignment()
    {
        if (cameraHolder != null)
        {
            cameraHolder.transform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
        }
        
        if (cinemachineTarget != null)
        {
            cinemachineTarget.transform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
        }
        
        if (isCameraChild && cameraTransform != null && cameraHolder == null && cinemachineTarget == null)
        {
            cameraTransform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
        }
    }
    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (debugMovement)
        {
            Debug.Log($"Collision detected with: {hit.gameObject.name}, Normal: {hit.normal}, Point: {hit.point}");
            
            // Check if we're being pushed upward
            if (hit.normal.y < -0.5f)
            {
                Debug.LogWarning($"Being pushed up by collision! Normal.y: {hit.normal.y}");
                // Prevent upward velocity accumulation from collision
                if (velocity.y > 0)
                {
                    velocity.y = 0f;
                }
            }
        }
        
        // If we hit something while falling, reduce downward velocity
        if (velocity.y < -10f && hit.normal.y > 0.5f)
        {
            velocity.y = -0.1f; // Reset to small downward force (reduced from -2f)
            if (debugMovement)
            {
                Debug.Log("Hit ground while falling fast - reducing velocity");
            }
        }
        
        // Prevent sideways sliding on steep slopes
        if (isGrounded && hit.normal.y < 0.7f && hit.normal.y > 0.1f)
        {
            // We're on a slope - apply a counter-force
            Vector3 slopeForce = Vector3.ProjectOnPlane(Vector3.down, hit.normal);
            characterController.Move(slopeForce * 0.1f * Time.deltaTime);
        }
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || characterController == null) return;
        
        // Draw sphere check
        float bottomY = characterController.center.y - characterController.height / 2f;
        Vector3 sphereCenter = transform.position + Vector3.up * (bottomY + characterController.radius);
        float checkRadius = characterController.radius * 0.9f;
        
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(sphereCenter, checkRadius);
        
        // Draw raycast
        Vector3 rayStart = transform.position + characterController.center;
        float rayDistance = (characterController.height / 2f) + groundCheckDistance + 0.1f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * rayDistance);
        
        // Draw CharacterController capsule
        Gizmos.color = Color.yellow;
        Vector3 p1 = transform.position + characterController.center + Vector3.up * (characterController.height / 2f - characterController.radius);
        Vector3 p2 = transform.position + characterController.center - Vector3.up * (characterController.height / 2f - characterController.radius);
        Gizmos.DrawWireSphere(p1, characterController.radius);
        Gizmos.DrawWireSphere(p2, characterController.radius);
    }
    #endif
    
    private void HandleCameraZoom()
    {
        // Check if zoom is enabled
        if (!enableZoom) return;
        
        // Always check for scroll input first
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        // Log scroll input even without camera for debugging
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            Debug.Log($"=== ZOOM DEBUG ===");
            Debug.Log($"Scroll input detected: {scrollInput}");
            Debug.Log($"IsUsingCinemachine: {isUsingCinemachine}");
            Debug.Log($"FreeLookCamera: {(freeLookCamera != null ? "FOUND" : "NULL")}");
        }
        
        // Try to find FreeLook camera if we don't have it yet (always try, not just when using Cinemachine)
        if (freeLookCamera == null)
        {
            // First try the standard way
            var freeLookType = System.Type.GetType("Cinemachine.CinemachineFreeLook, Cinemachine");
            if (freeLookType != null)
            {
                var freeLookCameras = FindObjectsOfType(freeLookType);
                if (freeLookCameras.Length > 0)
                {
                    freeLookCamera = freeLookCameras[0] as Component;
                    StoreOriginalRadiusValues(freeLookCamera);
                    isUsingCinemachine = true; // Set this to true if we find a FreeLook camera
                    Debug.Log($"Found FreeLook camera for zoom: {((Component)freeLookCamera).name}");
                }
                else
                {
                    Debug.LogWarning("FreeLook type found but no FreeLook cameras in scene!");
                }
            }
            else
            {
                // Try alternative namespace
                freeLookType = System.Type.GetType("Cinemachine.CinemachineFreeLook, Unity.Cinemachine");
                if (freeLookType != null)
                {
                    var freeLookCameras = FindObjectsOfType(freeLookType);
                    if (freeLookCameras.Length > 0)
                    {
                        freeLookCamera = freeLookCameras[0] as Component;
                        StoreOriginalRadiusValues(freeLookCamera);
                        isUsingCinemachine = true;
                        Debug.Log($"Found FreeLook camera (Unity.Cinemachine) for zoom: {((Component)freeLookCamera).name}");
                    }
                }
                else
                {
                    if (debugMovement && Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning("Could not find Cinemachine.CinemachineFreeLook type. Is Cinemachine installed?");
                    }
                }
            }
        }
        
        if (freeLookCamera == null)
        {
            if (Mathf.Abs(scrollInput) > 0.01f) // Only log when trying to zoom
            {
                Debug.LogWarning("Camera zoom not working: FreeLook camera not found. Make sure you have a Cinemachine FreeLook camera in the scene.");
            }
            return;
        }
        
        // Apply zoom if we have scroll input
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // Update zoom level (inverted so scroll up zooms in)
            currentZoomLevel = Mathf.Clamp01(currentZoomLevel - scrollInput * zoomSpeed);
            
            // Apply zoom to FreeLook camera
            try
            {
                // Try to get orbits as a FIELD first (not property)
                var orbitsField = freeLookCamera.GetType().GetField("m_Orbits", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                object orbits = null;
                
                if (orbitsField != null)
                {
                    orbits = orbitsField.GetValue(freeLookCamera);
                    if (debugMovement)
                        Debug.Log("Found m_Orbits as a FIELD");
                }
                else
                {
                    // Try as a property
                    var orbitsProperty = freeLookCamera.GetType().GetProperty("m_Orbits",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (orbitsProperty != null)
                    {
                        orbits = orbitsProperty.GetValue(freeLookCamera);
                        if (debugMovement)
                            Debug.Log("Found m_Orbits as a PROPERTY");
                    }
                }
                
                if (orbits != null && orbits.GetType().IsArray)
                {
                    var orbitsArray = (Array)orbits;
                    
                    for (int i = 0; i < orbitsArray.Length && i < 3; i++)
                    {
                        var orbit = orbitsArray.GetValue(i);
                        if (orbit != null)
                        {
                            // Try to find radius field with different approaches
                            var radiusField = orbit.GetType().GetField("m_Radius",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            
                            if (radiusField == null)
                            {
                                // Try alternative names
                                string[] radiusNames = { "Radius", "radius", "_radius" };
                                foreach (string name in radiusNames)
                                {
                                    radiusField = orbit.GetType().GetField(name,
                                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                    if (radiusField != null) break;
                                }
                            }
                            
                            if (radiusField != null)
                            {
                                // Interpolate between min and max zoom distance
                                // currentZoomLevel: 0 = zoomed in (min distance), 1 = zoomed out (max distance)
                                float newRadius = Mathf.Lerp(minZoomDistance, maxZoomDistance, currentZoomLevel);
                                
                                radiusField.SetValue(orbit, newRadius);
                                
                                Debug.Log($"Set rig {i} radius to: {newRadius} (zoom level: {currentZoomLevel:F2})");
                            }
                            else
                            {
                                Debug.LogError($"Could not find radius field on orbit {i}");
                            }
                        }
                    }
                    
                    // Apply changes back if needed
                    if (orbitsField != null)
                    {
                        orbitsField.SetValue(freeLookCamera, orbitsArray);
                    }
                    
                    Debug.Log($"Camera zoom applied! Level: {currentZoomLevel:F2} (0=close, 1=far)");
                    
                    // Try to force the camera to update
                    try
                    {
                        // Look for an InvalidateCache or Update method
                        var invalidateMethod = freeLookCamera.GetType().GetMethod("InvalidateCache",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (invalidateMethod != null)
                        {
                            invalidateMethod.Invoke(freeLookCamera, null);
                            if (debugMovement) Debug.Log("Called InvalidateCache on FreeLook camera");
                        }
                        
                        // Try OnValidate
                        var onValidateMethod = freeLookCamera.GetType().GetMethod("OnValidate",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (onValidateMethod != null)
                        {
                            onValidateMethod.Invoke(freeLookCamera, null);
                            if (debugMovement) Debug.Log("Called OnValidate on FreeLook camera");
                        }
                        
                        // Force the component to update
                        if (freeLookCamera is Component comp)
                        {
                            comp.SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (debugMovement) Debug.LogWarning($"Could not call update methods: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError("Could not find orbits field/property on FreeLook camera! Make sure you have a Cinemachine FreeLook camera.");
                    
                    // Log available members for debugging
                    if (debugMovement && Input.GetKey(KeyCode.LeftShift))
                    {
                        Debug.Log("=== Available FreeLook Camera Members ===");
                        var fields = freeLookCamera.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        foreach (var field in fields)
                        {
                            if (field.Name.ToLower().Contains("orbit") || field.Name.ToLower().Contains("rig"))
                            {
                                Debug.Log($"Field: {field.Name} ({field.FieldType})");
                            }
                        }
                        Debug.Log("========================================");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to apply zoom: {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }
    }
} 