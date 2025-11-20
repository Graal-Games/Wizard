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
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerController : NetworkBehaviour
{

    [Header("References")]
    [Tooltip("The Cinemachine FreeLook camera for this player.")]
    [SerializeField] private CinemachineFreeLook freeLookCamera;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform followTarget;
    private Rigidbody rb;
    private PlayerInputActions playerInputActions;

    [Header("Movement Settings")]
    private float moveSpeed = 4f;
    [SerializeField] private float groundDrag = 5f;
    [SerializeField] private int jumpForce = 5;
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask whatIsGround;

    private float _baseMoveSpeedCache;
    private bool isWalking, isJumping, preparingJumpImpulse, preparingJumpToGround;
    private bool grounded, movementStopped, isSlowedByIncapacitation, playerIsInCastMode;
    private float verticalAxis, horizontalAxis;

    private Vector3 moveDir, previousMoveDirection;
    private Vector2 previousInputVector;
    private float startingMoveSpeed = 0f;
    private float inertiaDuration = 1f;
    private float runUpElapsedDuration = 0f;
    private bool isInertia = false;

    private bool isCasting = false;

    private bool isUsingMouseToMoveForward = false;
    bool lmb_pressed = false;
    bool rmb_pressed = false;
    Vector2 inputVector;



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



        playerInputActions = new PlayerInputActions();
        playerInputActions.PlayerMovement.Enable();
        playerInputActions.PlayerMovement.Movement.performed += OnMovementPerformed;
        playerInputActions.PlayerMovement.Movement.canceled += OnMovementPerformed;

        //// Subscribe to mouse button events
        //playerInputActions.PlayerMovement.Movement. += OnLeftMouseDown;
        //playerInputActions.PlayerMovement.LeftClick.canceled += OnLeftMouseUp;

        //playerInputActions.PlayerMovement.RightClick.performed += OnRightMouseDown;
        //playerInputActions.PlayerMovement.RightClick.canceled += OnRightMouseUp;




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





    //// This method gets called when the application gains or loses focus, meaning when it becomes the active or inactive window.
    //private void OnApplicationFocus(bool focus)
    //{
    //    if (focus)
    //    {
    //        // Change below to locked to hide cursor
    //        Cursor.lockState = CursorLockMode.None;
    //    }
    //    else
    //    {
    //        Cursor.lockState = CursorLockMode.None;
    //    }
    //}






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
        isCasting = true;
        moveSpeed = (_baseMoveSpeedCache / 4);
    }



    // Resets the player speed back to normal
    private void CastModeSpeedReset()
    {
        isCasting = false;
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
            rb.linearDamping = groundDrag;

            
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
            rb.linearDamping = 0;
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
            if (isCasting == false)
            {
                moveSpeed = Mathf.Lerp(startingMoveSpeed, _baseMoveSpeedCache, t);
            } 

            // Debug.Log(moveSpeed);
        } else
        {
            isInertia = true;
            previousMoveDirection = moveDir;
        }
    }

    // Handles logic to move forward with mouse LMB+RMB.
    private void OnMovementPerformed(InputAction.CallbackContext context)
    {
        // If the button pressed or released is the LMB handle the bool relatively.
        if (context.control.displayName.Contains("Left Button"))
        {
            lmb_pressed = !lmb_pressed;
            Debug.Log($"<color=orange>[LBM]: </color> {lmb_pressed}");
        }

        // If the button pressed or released is the RMB handle the bool relatively.
        if (context.control.displayName.Contains("Right Button"))
        {
            rmb_pressed = !rmb_pressed;
            Debug.Log($"<color=blue>[RBM]: </color> {rmb_pressed}");

        }

        // If both left and right mouse button are pressed resolve to true. Otherwise, resolve to false.
        isUsingMouseToMoveForward = lmb_pressed && rmb_pressed; 

        // Here’s your button info
        Debug.Log($"Pressed: {context.control.displayName} | Path: {context.control.path}");
    }


    private void HandleMovement()
    {


        // If both the LMB and RBM and pressed together, move the charater forward.
        if (isUsingMouseToMoveForward == true)
        {
            moveDir = new Vector3(0f, 0f, 1f);
        } else
        {
            // This movement logic is already correct from our previous fixes.
            inputVector = gameInput.GetMovementVector(); // The movement vector is set using the PlayerInput file inside the project assets.
            moveDir = new Vector3(inputVector.x, 0f, inputVector.y); // 1, 0, 0 LEFT // -1,0,0 Right // 0,0,1 forward // 0,0,-1 backward
        }

        // Vector2 bwd = new Vector2(0,-1).normalized;
        // Vector2 fwd = new Vector2(0,1).normalized;
        // Vector2 rgt = new Vector2(1,0).normalized;
        // Vector2 lft = new Vector2(-1,0).normalized;
        // Vector2 fwd_rgt = new Vector2(1,1).normalized;
        // Vector2 fwd_lft = new Vector2(-1,1).normalized;
        // Vector2 bwd_rgt = new Vector2(1,-1).normalized;
        // Vector2 bwd_lft = new Vector2(-1,-1).normalized;

        // Debug.LogFormat($"IIIIIIII 2222 Input Vector {inputVector}");


        moveDir = Quaternion.AngleAxis(cameraTransform.rotation.eulerAngles.y, Vector3.up) * moveDir;
        moveDir.Normalize();

        // If the player is running in a different direction, add slow for run-up
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
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // limit velocity if needed
        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }
}
