using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using static Beam;
using System;

public class PlayerMovement : NetworkBehaviour
{

    private float movementSlowAmount = 0;
    private float movementSlowTime = 0f;
    bool isSlowed = false;
    private bool leftButton;                    //identifies whether the left mouse button has been pressed
    private bool rightButton;                   //identifies whether the right mouse button has been pressed
    private bool forwardsButton;                //identifies whether both the left and right mouse button has been pressed
    private int timerLimit;                      //a timer for the amount of time the left or right mouse button can be held before action is taken
    private int leftTimer;                       //a counter to see how long the left mouse button has been held down
    private int rightTimer;                      //a counter to see how long the right mouse button has been held down
    private float mainCharSpeed = 4f;                  //change this to set character speed
    private bool moveForward = true;
    float scrollWheel;
    float forceMagnitude = 5f;

    PlayerBehavior playerBehavior;

    [SerializeField] AnimationStateController animationStateController;

    private bool isAnimationTriggered = false;

    public float MovementSlowTime
    {
        set { movementSlowTime = value; }
        get { return movementSlowTime; }
    }

    public float MovementSlowAmount
    {
        set { movementSlowAmount = value; }
        get { return movementSlowAmount; }
    }

    public bool IsSlowed
    {
        set { isSlowed = value; }
        get { return isSlowed; } 
    }

    public float MainCharSpeed
    {
        set { mainCharSpeed = value; }
        get { return mainCharSpeed; }
    }

    void Awake()
    {
        playerBehavior = GetComponent<PlayerBehavior>();

        Beam.beamExists += ResetPlayerSpeed;
    }



    private void ResetPlayerSpeed(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool isAlive)
    {
        if (!isAlive)
        {
            EnterCastMovementSlow(5, 1);
        }
        
    }



    public void SpellInflictedMovementSlow()
    {
        // movementSlowTime = slowTimeAmount;

        if (movementSlowTime > 0f && isSlowed == true)
        {
            // Debug.Log("SLOWING: " + movementSlowTime);
            movementSlowTime -= Time.deltaTime;
            //Debug.Log("Time left: " + movementSlowTime.ToString("F1"));
        } else {
            MovementSlowTime = 0;
            MovementSlowAmount = 0;
            isSlowed = false;
        }
    }



    // Slows the character speed and animation
    public void EnterCastMovementSlow(float newMoveSpeedValue, float animationMultiplier)
    {
        playerBehavior.AnimationStateController.GetComponent<Animator>().SetFloat("newSpeed", 0.3f);
        MainCharSpeed = 2;
    }

    public void ExitCastMovementSlow()
    {
        playerBehavior.AnimationStateController.GetComponent<Animator>().SetFloat("newSpeed", 1f);
        MainCharSpeed = 5;
    }



    public void Dodge()
    {
        // If space is pressed
        // Add force to the player character
        if (playerBehavior.isOnFloor)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Add a force to the Rigidbody along the object's up direction
                this.gameObject.GetComponent<Rigidbody>().AddForce(transform.up * forceMagnitude, ForceMode.Impulse);
            }
        }
        
    }



    public void Movement()
    {
        // Enable this below when you want to freeze player movement after they die
        //if (isAlive)
        //{
        //if (!playerBehavior.isOnFloor) return;
            SpellInflictedMovementSlow();
            
            scrollWheel = Input.GetAxis("Mouse ScrollWheel");

            Vector3 forwardBackward = Vector3.zero;
            Vector3 leftRight = Vector3.zero;

            Vector3 diagonalFrontRight = Vector3.zero;
            Vector3 diagonalFrontleft = Vector3.zero;

            Vector3 diagonalBackRight = Vector3.zero;
            Vector3 diagonalBackleft = Vector3.zero; 

            // ** Needs serious refactor - Code's a trainwreck
            //identifies what mouse buttons have been pressed
            if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.A))
            {
                // Possibly remove all this here and place the transform here
                leftButton = true;
                rightButton = false;
                forwardsButton = false;
            } else {
                leftButton = false;
            }

            // if w is pressed: move forward
            // if d is pressed move right
            // if d AND w are pressed move diagonally to the right


            // if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D))
            // {
            //     // Move diagonally to the right
            //     Vector3 diagonalFrontRightDirection = new Vector3(1, 0, 1).normalized;

            //     diagonalFrontRight = diagonalFrontRightDirection.normalized * Time.deltaTime * (mainCharSpeed - movementSlowAmount);
            //     transform.Translate(diagonalFrontRight);
            //     Debug.LogFormat($"<color=orange>{diagonalFrontRightDirection.magnitude}</color>");
            // }
            

            if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A))
            {
                // Move diagonally to the left
            }


            if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.D))
            {
                rightButton = true;
                forwardsButton = false;
                leftButton = false;
            } else {
                rightButton = false;
            }


            if ((Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D)))
            {
                
                forwardsButton = true;
                leftButton = false;
                rightButton = true;
                //Debug.LogFormat($"<color=orange>Diagonal</color>");
                animationStateController.RunForwardAnimation(false);
                
            } else if ((Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A))) 
            {
                forwardsButton = true;
                leftButton = true;
                rightButton = false;
                // Debug.LogFormat($"<color=orange>Diagonal</color>");
                animationStateController.RunForwardAnimation(false);

            } else if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.D)) {

                forwardsButton = true;
                leftButton = false;
                rightButton = true;
                //Debug.LogFormat($"<color=orange>Diagonal</color>");
                animationStateController.RunBackwardsAnimation(false);
                animationStateController.RunForwardAnimation(false);

            } else if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A)) {
                forwardsButton = true;
                leftButton = true;
                rightButton = false;
                //Debug.LogFormat($"<color=orange>Diagonal</color>");
                animationStateController.RunBackwardsAnimation(false);
                animationStateController.RunForwardAnimation(false);
            }
            else if ((Input.GetMouseButton(0) && Input.GetMouseButton(1) 
            || Input.GetKey(KeyCode.W) 
            || Input.GetKey(KeyCode.S)
            && !Input.GetKey(KeyCode.D)
            && !Input.GetKey(KeyCode.A))) 
            {
                
                forwardsButton = true;
                leftButton = false;
                rightButton = false;
                //Debug.LogFormat($"<color=orange>Forward</color>");

            } else {
                //animationStateController.RunBackwardsAnimation(true);
                forwardsButton = false;
            }

            if (forwardsButton == false && leftButton == false && rightButton == false)
            {
                if (!isAnimationTriggered)
                {
                    animationStateController.IdleAnimation(true);
                    isAnimationTriggered = true;
                }
                else if (isAnimationTriggered)
                {
                    // Reset the flag if the condition is no longer true
                    isAnimationTriggered = false;
                }
                //animationStateController.IdleAnimation(true);

            } else if (forwardsButton == true || leftButton == true || rightButton == true)
            {
                //StartCoroutine(DelayIdleAnimation());
                animationStateController.IdleAnimation(false);
            }

            //If the left button has been pressed a timer starts
            //if no other button is pressed then when the timer
            //runs out it triggers the left mouse button action
            // This is done as a check for when both buttons are pressed 
            //for forward movement
            if (leftButton)
            {
                animationStateController.RunLeftAnimation(true);
                //Debug.Log("left detected");
                leftTimer++;

                leftRight = Vector3.left * Time.deltaTime * (mainCharSpeed - movementSlowAmount);
                transform.Translate(leftRight);

                if (leftTimer >= timerLimit)
                {
                    //Debug.Log("moving left");
                    leftButton = false;
                    leftTimer = 0;
                }
            } else {
                animationStateController.RunLeftAnimation(false);
            }

            //If the right button has been pressed a time starts
            //if no other button is pressed then when the timer
            //runs out it triggers the right mouse button action
            // >> added physics for movement
            if (rightButton)
            {
                animationStateController.RunRightAnimation(true);
                //Debug.Log("right detected");
                rightTimer++;

                leftRight = Vector3.right * Time.deltaTime * (mainCharSpeed - movementSlowAmount);
                transform.Translate(leftRight);

                if (rightTimer >= timerLimit)
                {
                    //Debug.Log("moving right");
                    rightButton = false;
                    rightTimer = 0;
                }

            } else {
                animationStateController.RunRightAnimation(false);
            }



            // toggle forward movement
            if (Input.GetAxis("Mouse ScrollWheel") > 0f || (Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.D)
            && !Input.GetKey(KeyCode.A)) || ((Input.GetMouseButton(0) && Input.GetMouseButton(1))) )
            {
                animationStateController.RunForwardAnimation(true);
                moveForward = true;
            }
            // toggle backward movement
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f || (Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.D)
            && !Input.GetKey(KeyCode.A)))
            {
                animationStateController.RunBackwardsAnimation(true);
                moveForward = false;
            }



            // if (Input.GetKey(KeyCode.W))
            // {
            //     forwardsButton = true;
            //     animationStateController.RunForwardAnimation(true);
            //         forwardBackward = Vector3.forward * Time.deltaTime * (mainCharSpeed - movementSlowAmount);
            //         transform.Translate(forwardBackward);
            // } else if (Input.GetKey(KeyCode.S)) {
            //     forwardsButton = true;
            //     animationStateController.RunBackwardsAnimation(true);
            //         forwardBackward = Vector3.back * Time.deltaTime * (mainCharSpeed - movementSlowAmount);
            //         transform.Translate(forwardBackward);
            // } else {
            //     animationStateController.RunForwardAnimation(false);
            //     animationStateController.RunBackwardsAnimation(false);
            // }

            //if both buttons are pressed then the forwards
            //action activates
            // >> added physics for movement
            if (forwardsButton)
            {                
                if (moveForward == true || Input.GetKey(KeyCode.W))
                {
                    forwardBackward = Vector3.forward * Time.deltaTime * (mainCharSpeed - movementSlowAmount);
                    transform.Translate(forwardBackward);
                    

                } else if (moveForward == false || Input.GetKey(KeyCode.S)){
                    forwardBackward = Vector3.back * Time.deltaTime * (mainCharSpeed - movementSlowAmount);
                    transform.Translate(forwardBackward);
                }
            
                forwardsButton = false;
                leftButton = false;
                rightButton = false;
                leftTimer = 0;
                rightTimer = 0;
            } else {
                animationStateController.RunForwardAnimation(false);
                animationStateController.RunBackwardsAnimation(false);
            }


        // } else {
        //     return;
        // }
    }
}
