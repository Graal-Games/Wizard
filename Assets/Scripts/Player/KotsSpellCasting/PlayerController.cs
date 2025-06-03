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
        if (!IsLocalPlayer) return;

        _baseMoveSpeedCache = moveSpeed; // Cache the base movement speed for future references and calculations.

        this.rb = GetComponent<Rigidbody>();
        this.rb.freezeRotation = true;

        if (gameInput == null)
            gameInput = FindObjectOfType<GameInput>(); 
        if (cameraTransform == null)
        {
            cameraTransform = FindObjectOfType<Camera>()?.transform; 

            CinemachineFreeLook freeLook = FindObjectOfType<CinemachineFreeLook>();
            freeLook.Follow = followTarget;
            freeLook.LookAt = followTarget;
        }

        base.OnNetworkSpawn();
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
    private void PlayerMoveSpeedOnIsCastingBeam(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehaviorScript, bool status)
    {
        if (status)
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
        if (!IsLocalPlayer) return;

        if (movementStopped == true) return;

        HandleMovement();
        // Check if player is incapacitated
        // Migrate to playerBehaviour script?
        //HandleMovement();
    }

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

    public void HandleMovement()
    {

        Vector2 inputVector = gameInput.GetMovementVector();


        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        // Apply rotation base on the camare angle
        moveDir = Quaternion.AngleAxis(cameraTransform.rotation.eulerAngles.y, Vector3.up) * moveDir;

        moveDir.Normalize();


        float playerRadius = .7f;
        float moveDistance = moveSpeed * Time.deltaTime;
        Vector3 point2 = transform.position + Vector3.up * this.playerHeight;


        transform.position = GetNewPosition(
            transform.position,
           point2,
           playerRadius,
           moveDir,
           moveDistance
           );

        isWalking = moveDir != Vector3.zero;


        float rotateSpeed = 5f;

        // y axis rotation
        if (inputVector.y > 0)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
        }
        else if (inputVector.y < 0) {
            transform.forward = Vector3.Slerp(transform.forward, -moveDir, Time.deltaTime * rotateSpeed);
        }


        // x axis rotation
        if (inputVector.x > 0)
        {
            transform.right = Vector3.Slerp(transform.right, moveDir, Time.deltaTime * rotateSpeed);
        }
        else if (inputVector.x < 0)
        {
            transform.right = Vector3.Slerp(transform.right, -moveDir, Time.deltaTime * rotateSpeed);
        }


        this.rb.AddForce(Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed), ForceMode.Force);


        verticalAxis = gameInput.GetVerticalAxis();
        horizontalAxis = gameInput.GetHorizontalAxis();
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

    private Vector3 GetNewPosition(Vector3 position, Vector3 point2, float playerRadius, Vector3 moveDir, float moveDistance)
    {

        RaycastHit hit;

        // Define layer mask to exclude triggers
        int layerMask = whatIsGround.value & ~LayerMask.GetMask("Spell");

        bool canMove = !Physics.CapsuleCast(
           position,
           point2,
           playerRadius,
           moveDir,
           out hit,
           moveDistance,
           layerMask,
           QueryTriggerInteraction.Ignore
           );

        if (!canMove)
        {
            // Cannot move towards moveDir

            // Attempt only x movement
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;


            canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.CapsuleCast(
            position,
            point2,
            playerRadius,
            moveDirX,
            moveDistance
            );

            if (canMove)
            {
                // Can move only in the X
                return position += moveDirX * moveDistance;
            }
            else
            {
                // Cannot move towards X

                // Attempt only Z movement
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;

                canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.CapsuleCast(
                position,
                point2,
                playerRadius,
                moveDirZ,
                moveDistance
                );

                if (canMove)
                {
                    // Can move only in the Z
                    return position += moveDirZ * moveDistance;
                }
                else
                {
                    // Cannot move anywere, return original position
                    return position;
                }
            }
        }
        else
        {
            // Can move towards moveDir
            return position += moveDir * moveDistance;
        }
    }
}
