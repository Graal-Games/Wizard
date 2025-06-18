using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class CharacterControllerMovement : NetworkBehaviour, IMovementEffects
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -19.62f; // Earth's gravity * 2 for more responsive feel
    [SerializeField] private float groundCheckDistance = 0.1f;
    
    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private bool autoFindCamera = true; // Enable auto-finding camera
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 1.6f, 0f); // Camera position offset when following
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform playerBody;
    [SerializeField] private GameObject gameInputObject; // Optional: assign in inspector
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer = -1; // Default to Everything
    
    // Components
    private CharacterController characterController;
    private MonoBehaviour gameInputComponent; // Store as MonoBehaviour to avoid compilation issues
    private Animator animator;
    
    // Camera state
    private bool isCameraChild = false;
    private Transform cameraParent; // Store original camera parent
    
    // Movement state
    private Vector3 velocity;
    private bool isGrounded;
    private float verticalRotation = 0f;
    
    // Movement effects
    private float baseMoveSpeed;
    private float baseSprintSpeed;
    private bool isSlowed = false;
    private bool movementStopped = false;
    private float slowTimer = 0f;
    
    // Network variables for syncing state
    private NetworkVariable<bool> networkIsGrounded = new NetworkVariable<bool>();
    private NetworkVariable<bool> networkIsWalking = new NetworkVariable<bool>();
    private NetworkVariable<bool> networkIsJumping = new NetworkVariable<bool>();
    
    // Properties for external access
    public bool IsWalking => networkIsWalking.Value;
    public bool IsJumping => networkIsJumping.Value;
    public bool IsGrounded => networkIsGrounded.Value;
    public float MoveSpeed => moveSpeed;
    public bool IsSlowed => isSlowed;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component is missing!");
        }
        
        // Try to get animator
        animator = GetComponentInChildren<Animator>();
        
        // Store base speeds
        baseMoveSpeed = moveSpeed;
        baseSprintSpeed = sprintSpeed;
        
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
    }
    
    private void Start()
    {
        // Auto-find camera if enabled and not assigned
        if (autoFindCamera && cameraTransform == null)
        {
            FindCamera();
        }
        
        if (!IsLocalPlayer) return;
        
        // Setup camera for local player
        SetupCameraForLocalPlayer();
        
        // Try to find GameInput
        FindGameInput();
        
        // Lock cursor for FPS-style camera control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Subscribe to jump input if GameInput is available
        SubscribeToJumpInput();
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
            return;
        }
        
        // If not found as child, find main camera or any freelook camera in scene
        Camera[] allCameras = FindObjectsOfType<Camera>();
        Camera targetCamera = null;
        
        // First priority: FreeLook camera
        foreach (var cam in allCameras)
        {
            if (cam.name.ToLower().Contains("freelook") || cam.name.ToLower().Contains("free look"))
            {
                targetCamera = cam;
                Debug.Log($"Found FreeLook camera: {cam.name}");
                break;
            }
        }
        
        // Second priority: Main camera
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
        }
        else
        {
            Debug.LogError("No camera found! Please assign a camera or create one.");
        }
    }
    
    private void SetupCameraForLocalPlayer()
    {
        if (cameraTransform == null) return;
        
        if (!isCameraChild && IsLocalPlayer)
        {
            // Make the camera a child of the player for proper following
            cameraParent = cameraTransform.parent; // Store original parent
            cameraTransform.SetParent(transform);
            cameraTransform.localPosition = cameraOffset;
            cameraTransform.localRotation = Quaternion.identity;
            Debug.Log("Camera attached to local player for following.");
        }
        
        // Disable other player cameras if this is local player
        if (IsLocalPlayer)
        {
            // Find all other players and disable their cameras
            var allPlayers = FindObjectsOfType<CharacterControllerMovement>();
            foreach (var player in allPlayers)
            {
                if (player != this && player.cameraTransform != null)
                {
                    Camera otherCam = player.cameraTransform.GetComponent<Camera>();
                    if (otherCam != null)
                    {
                        otherCam.enabled = false;
                    }
                }
            }
        }
        else
        {
            // Disable camera for non-local players
            if (cameraTransform != null)
            {
                Camera cam = cameraTransform.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.enabled = false;
                }
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
    
    private void OnDestroy()
    {
        // Restore camera to original parent if we changed it
        if (!isCameraChild && cameraTransform != null && cameraParent != null)
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
    
    private void Update()
    {
        if (!IsLocalPlayer) return;
        
        // Handle slow timer
        if (slowTimer > 0)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0)
            {
                ExitCastMovementSlow();
            }
        }
        
        // Ground check
        isGrounded = Physics.CheckSphere(transform.position - new Vector3(0, characterController.height / 2 - characterController.radius + groundCheckDistance, 0), 
                                       characterController.radius - 0.05f, groundLayer);
        
        // Update network state
        if (IsOwner)
        {
            UpdateNetworkStateServerRpc(isGrounded, velocity.magnitude > 0.1f, velocity.y > 0);
        }
        
        if (!movementStopped)
        {
            HandleMovement();
            if (cameraTransform != null)
            {
                HandleMouseLook();
            }
            else if (autoFindCamera)
            {
                // Try to find camera again if it wasn't found initially
                FindCamera();
                SetupCameraForLocalPlayer();
            }
        }
        
        // Handle jump with direct input if GameInput is not available
        if (gameInputComponent == null && Input.GetKeyDown(KeyCode.Space))
        {
            OnJump(null, null);
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
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }
    
    private void HandleMovement()
    {
        // Get input
        Vector2 inputVector = GetMovementInput();
        
        // Calculate movement direction relative to camera
        Vector3 moveDirection = transform.right * inputVector.x + transform.forward * inputVector.y;
        
        // Apply movement
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
        
        // Apply gravity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
        
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
    
    private void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Rotate player body horizontally
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate camera vertically
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }
    
    private void OnJump(object sender, EventArgs e)
    {
        if (!IsLocalPlayer) return;
        
        if (isGrounded && !movementStopped)
        {
            // Calculate jump velocity based on desired height
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
        velocity += force;
    }
    
    public void StopMovement()
    {
        movementStopped = true;
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
        moveSpeed = newSpeed;
    }
    
    public void ApplyExternalForce(Vector3 force)
    {
        velocity += force;
    }
    
    public Vector3 GetVelocity()
    {
        return characterController.velocity;
    }
} 