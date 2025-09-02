using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Component References")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private CinemachineFreeLook freeLookCamera;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float groundDrag;
    [SerializeField] private int jumpForce;

    // Private variables
    private Rigidbody rb;
    private Transform cameraTransform;
    private float baseMoveSpeedCache;
    private float verticalAxis = 0f;
    private float horizontalAxis = 0f;

    public override void OnNetworkSpawn()
    {
        // If this player object is not me, disable its camera and stop.
        if (!IsOwner)
        {
            if (freeLookCamera != null)
            {
                freeLookCamera.gameObject.SetActive(false);
            }
            return;
        }

        if (!IsOwner) return;

        baseMoveSpeedCache = moveSpeed;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        // If gameInput wasn't assigned in the Inspector, find it in the scene.
        if (gameInput == null)
        {
            gameInput = FindObjectOfType<GameInput>();
            if (gameInput == null)
            {
                Debug.LogError("CRITICAL: GameInput object not found in the scene! The player cannot be controlled.", this);
                return; // Stop execution if we can't find the input handler
            }
        }

        cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            Debug.LogError("CRITICAL: Main Camera not found! Make sure your camera is tagged 'MainCamera'.", this);
            return; // Stop execution to prevent further errors
        }

        // Setup Cinemachine
        if (freeLookCamera != null && followTarget != null && lookAtTarget != null)
        {
            freeLookCamera.Follow = followTarget;
            freeLookCamera.LookAt = lookAtTarget;
        }
        else
        {
            Debug.LogError("PlayerController is missing a reference to the FreeLookCamera, FollowTarget, or LookAtTarget!", this);
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        // In the future, you would check a NetworkVariable from PlayerStatusEffects here.
        // For example: if (playerStatusEffects.IsStunned.Value) return;

        HandleMovement();
    }

    // This is the NEW public method that PlayerStatusEffects will call.
    public void SetSlowed(bool isSlowed)
    {
        // This contains the logic from your old OnCharacterSlowChange method.
        moveSpeed = isSlowed ? (baseMoveSpeedCache / 2) : baseMoveSpeedCache;
        Debug.Log($"PlayerController: Slow status changed. New move speed is {moveSpeed}");
    }

    private void HandleMovement()
    {
        // This movement logic is already correct from our previous fixes.
        Vector2 inputVector = gameInput.GetMovementVector();
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        moveDir = Quaternion.AngleAxis(cameraTransform.rotation.eulerAngles.y, Vector3.up) * moveDir;
        moveDir.Normalize();

        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        if (cameraForward != Vector3.zero)
        {
            float rotateSpeed = 20f;
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }

        Vector3 localMoveDir = transform.InverseTransformDirection(moveDir);
        verticalAxis = localMoveDir.z;
        horizontalAxis = localMoveDir.x;
    }

    // Public getters for the Animator script
    public float getVerticalAxis() => verticalAxis;
    public float getHorizontalAxis() => horizontalAxis;
}