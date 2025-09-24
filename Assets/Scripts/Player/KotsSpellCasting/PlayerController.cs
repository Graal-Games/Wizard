using Cinemachine;
using DebuffEffect;
using IncapacitationEffect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : NetworkBehaviour
{

    [Tooltip("The Cinemachine FreeLook camera for this player.")]
    public CinemachineFreeLook freeLookCamera;

    [SerializeField] private GameInput gameInput; 
    // [SerializeField] private Incapacitation incapacitationScript; 

    [SerializeField] private Transform cameraTransform;

    [SerializeField] private Transform followTarget;

    private Rigidbody rb;

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float groundDrag;
    [SerializeField] private int jumpForce;

    private float _baseMoveSpeedCache;

    private bool isSlowedByIncapacitation = false;

    private bool playerIsInCastMode = false;

    private bool isWalking;
    private bool isJumping;
    private bool preparingJumpImpulse;
    private bool preparingJumpToGround;

    private float verticalAxis = 0f;
    private float horizontalAxis = 0f;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask whatIsGround;
    private bool grounded;

    private bool movementStopped;
    private bool movementSlowed;

    Vector3 previousMoveDirection;
    Vector2 previousInputVector;

    float startingMoveSpeed = 0f;
    float inertiaDuration = 1f; // seconds
    float runUpElapsedDuration = 0f;

    bool isInertia = false;

    Vector3 moveDir = new Vector3();


    //#############################################################################################
    //##################################### Below queued for deletion #####################################
    //#############################################################################################
    //public override void OnNetworkSpawn()
    //{
    //    if (!IsServer && IsOwner) //Only send an RPC to the server on the client that owns the NetworkObject that owns this NetworkBehaviour instance
    //    {
    //        TestServerRpc(0, NetworkObjectId);
    //    }
    //}

    //[Rpc(SendTo.Server)]
    //void TestServerRpc(int value, ulong sourceNetworkObjectId)
    //{
    //    Debug.Log($"Server Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
    //    //TestClientRpc(value, sourceNetworkObjectId);
    //}

    //private void Awake()
    //{
    //    //if (!IsLocalPlayer) return;
    //    Debug.LogFormat("1 - IsClient: " + IsClient);
    //    Debug.LogFormat("1 - IsLocalPlayer: " + IsLocalPlayer);

    //    if (Instance != null)
    //    {
    //        Debug.LogError("There is more than one Player instance");
    //    }
    //    Instance = this;

    //    this.rb = GetComponent<Rigidbody>();

    //    if (gameInput == null)
    //        gameInput = FindObjectOfType<GameInput>();

    //    if (cameraTransform == null)
    //    {
    //        cameraTransform = FindObjectOfType<Camera>()?.transform;

    //        CinemachineFreeLook freeLook = FindObjectOfType<CinemachineFreeLook>();
    //        freeLook.Follow = followTarget;
    //        freeLook.LookAt = followTarget;
    //    }
    //}
    //#############################################################################################
    //##################################### Above queued for deletion #####################################
    //#############################################################################################


    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (freeLookCamera != null)
            {
                freeLookCamera.gameObject.SetActive(false);
            }
            return; // Stop execution for non-owners.
        }

        // --- All local player setup code goes below this line ---
        cameraTransform = Camera.main.transform;
        // Safety check to make sure the camera was found
        if (cameraTransform == null)
        {
            Debug.LogError("Main Camera could not be found in the scene! Ensure it has the 'MainCamera' tag.", this.gameObject);
            return;
        }

        _baseMoveSpeedCache = moveSpeed;

        this.rb = GetComponent<Rigidbody>();
        if (this.rb != null)
        {
            this.rb.freezeRotation = true;
        }

        // It's still better to assign GameInput and Camera via the inspector if possible
        if (gameInput == null)
            gameInput = FindObjectOfType<GameInput>();

        // This is the new, reliable way to set up the camera
        if (freeLookCamera != null)
        {
            if (followTarget != null)
            {
                freeLookCamera.Follow = followTarget;
                freeLookCamera.LookAt = followTarget;
            }
            else
            {
                Debug.LogError("Follow Target is not assigned in the PlayerController inspector!", this.gameObject);
            }
        }
        else
        {
            Debug.LogError("FreeLook Camera is not assigned in the PlayerController inspector!", this.gameObject);
        }
    }





    private void Start()
    {
        if (!IsLocalPlayer) return;

        // gameInput.OnJumpAction += GameInput_OnJumpAction;
        Incapacitation.playerIncapacitation += HandleStun; // This would go in the player script
        gameObject.GetComponentInParent<NewPlayerBehavior>().isSlowed.OnValueChanged += OnCharacterSlowChange;
        K_SpellLauncher.CastModeSpeedChange += HandleOnCastModeStartBehavior;
        BeamSpell.beamStatus += PlayerMoveSpeedOnIsCastingBeam;
    }





    // This method gets called when the application gains or loses focus, meaning when it becomes the active or inactive window.
    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            // Change below to locked to hide cursor
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }






    // Processes events that are emitted when casting beam and slows the movement down 
    //relatively to the existence of the beam
    private void PlayerMoveSpeedOnIsCastingBeam(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehaviorScript, bool isCastingBeam)
    {
        if (isCastingBeam)
        {
            CastModeSpeedSlow();
            // CastingLookSlow();
        } else
        {
            CastModeSpeedReset();
        }
    }





    
    // Processes emitted events when players begins casting to slow down movement
    // This method checks whether or not the player character is slowed by an incapacitation
    //and if he is, cast slow is not applied until the incapacitation has expired
    private void HandleOnCastModeStartBehavior(bool isInCastMode)
    {
        playerIsInCastMode = isInCastMode;

        if (!isSlowedByIncapacitation && isInCastMode)
        {
            CastModeSpeedSlow();
        } else if (!isSlowedByIncapacitation && !isInCastMode)
        {
            CastModeSpeedReset();
        }
    }





    private void CastModeSpeedSlow()
    {
        moveSpeed = (_baseMoveSpeedCache / 2);
    }





    // Resets the player speed back to normal
    private void CastModeSpeedReset()
    {
        moveSpeed = _baseMoveSpeedCache;
    }





    // Reverts the player speed back to normal after having been slowed
    private void OnCharacterSlowChange(bool previous, bool current)
    {
        if (previous == false && current == true)
        {
            moveSpeed /= 2;
            isSlowedByIncapacitation = true; // This bool is used in the HandleOnCastModeStartBehavior method above to prevent slow stacking
        }
        
        if (previous == true && current == false)
        {
            Debug.LogFormat($"<color=brown> NORMALIZE SPEED {moveSpeed}</color>");
            
            moveSpeed *= 2; // Of course the value is being halved atm and changes could be made here for more elaborate slow.

            isSlowedByIncapacitation = false;

            if (playerIsInCastMode)
            {
                CastModeSpeedSlow();
            }

            Debug.LogFormat($"<color=brown> NORMALIZE SPEED {moveSpeed}</color>");
        }
    }





    void HandleStun(ulong clientId, IncapacitationInfo info)
    {
        if (clientId != OwnerClientId) return;

        movementStopped = info.AffectsMovement; // The reason the movement incapacitation is made into a variable as such is for scalability, adding more spells with different effects is easier.
    }





    private void GameInput_OnJumpAction(object sender, EventArgs e)
    {
        if (grounded && !isJumping)
        {
            isJumping = true;
            preparingJumpImpulse = true;
            float jumpImpulseDelay = isWalking ? 0.5f : 0.8f;

            FuntionTimer.Create(ApplyJumpImpulse, jumpImpulseDelay, "jump_timer");
        }

    }





    private void ApplyJumpImpulse()
    {
        this.rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        preparingJumpImpulse = false;
    }






    private void Update()
    {
        if (!IsLocalPlayer) return;


        grounded = isGrounded();

        SpeedControl();

        // handle drag and jump
        if (grounded)
        {
            rb.drag = groundDrag;

            
            // todo check if used properly
            if (isJumping && !preparingJumpImpulse)
            {

                if (!preparingJumpToGround)
                {
                    preparingJumpToGround = true;
                    float jumpToGroundDelay = 1f;
                    FuntionTimer.Create(() =>
                    {
                        this.isJumping = false;
                        preparingJumpToGround = false;
                    }, jumpToGroundDelay);
                }
            }
        }
        else
        {
            rb.drag = 0;
        }
    }





    private void FixedUpdate()
    {
        // All physics and movement logic should be in FixedUpdate
        if (!IsOwner) return;

        if (movementStopped == true) return;

        HandleMovement();
    }




    void Inertia()
    {
        if (runUpElapsedDuration < inertiaDuration)
        {
            runUpElapsedDuration += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(runUpElapsedDuration / inertiaDuration);
            moveSpeed = Mathf.Lerp(startingMoveSpeed, _baseMoveSpeedCache, t);

            // Debug.Log(moveSpeed);
        } else
        {
            isInertia = true;
            previousMoveDirection = moveDir;
        }
    }




    private void HandleMovement()
    {
        // This movement logic is already correct from our previous fixes.
        Vector2 inputVector = gameInput.GetMovementVector();
        moveDir = new Vector3(inputVector.x, 0f, inputVector.y); // 1, 0, 0 LEFT // -1,0,0 Right // 0,0,1 forward // 0,0,-1 backward

        Vector2 bwd = new Vector2(0,-1);
        Vector2 fwd = new Vector2(0,1);
        Vector2 rgt = new Vector2(1,0);
        Vector2 lft = new Vector2(-1,0);
        Vector2 fwd_rgt = new Vector2(1,1);
        Vector2 fwd_lft = new Vector2(-1,1);
        Vector2 bwd_rgt = new Vector2(1,-1);
        Vector2 bwd_lft = new Vector2(-1,-1);


        //Debug.LogFormat($"MOVE DIR {moveDir}");
        //Debug.LogFormat($"Input Vector {inputVector}");

        moveDir = Quaternion.AngleAxis(cameraTransform.rotation.eulerAngles.y, Vector3.up) * moveDir;
        moveDir.Normalize();

        if ((previousInputVector == bwd && inputVector == fwd))
        {
            runUpElapsedDuration = 0f;
            previousInputVector = inputVector;
            Debug.LogFormat($"111 Input Vector {inputVector}");
        } 

        if ((previousInputVector == rgt && inputVector == lft))
        {
            runUpElapsedDuration = 0f;
            previousInputVector = inputVector;
            Debug.LogFormat($"222 Input Vector {inputVector}");
        }

        if (previousInputVector != inputVector)
        {
            runUpElapsedDuration = 0.2f;
            previousInputVector = inputVector;
            Debug.LogFormat($"333 Input Vector {inputVector}");
        }

        if (previousMoveDirection == moveDir && moveDir == Vector3.zero)
        {
            runUpElapsedDuration = 0f;
        }


        if (previousMoveDirection != moveDir)
        {
            Inertia();
        }

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


    // Record input from the player
    // Move the player character locally
    // Broadcast movement to the server
    // CP SR



    private bool isGrounded()
    {
        //return Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        return Physics.CheckSphere(this.transform.position, 0.2f, whatIsGround);
    }





    public float getVerticalAxis()
    {
        return verticalAxis;
    }





    public float getHorizontalAxis()
    {
        return horizontalAxis;
    }





    public bool IsWalking()
    {
        return isWalking;
    }





    public bool IsJumping()
    {
        return isJumping;
    }



    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }
}
