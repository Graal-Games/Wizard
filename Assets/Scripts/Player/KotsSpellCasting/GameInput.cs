using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{

    private const string PLAYER_PREFS_BINDINGS = "InputBindings";

    public static GameInput Instance { get; private set; }

    public event EventHandler OnJumpAction;

    private PlayerInputActions playerInputActions;

    private void Awake()
    {
        Instance = this;

        playerInputActions = new PlayerInputActions();

        playerInputActions.PlayerMovement.Enable();

        playerInputActions.PlayerMovement.Jump.performed += Jump_performed;
    }

    private void Jump_performed(InputAction.CallbackContext obj)
    {
        OnJumpAction?.Invoke(this, EventArgs.Empty);
    }

    private void OnDestroy()
    {
        playerInputActions?.Dispose();
    }

    public Vector2 GetMovementVectorNormalized() {

        Vector2 inputVector = playerInputActions.PlayerMovement.Movement.ReadValue<Vector2>();

        return inputVector.normalized;

    }

    public Vector2 GetMovementVector()
    {

        Vector2 inputVector = playerInputActions.PlayerMovement.Movement.ReadValue<Vector2>();


        return inputVector;

    }

    internal float GetVerticalAxis()
    {
        return Input.GetAxis("Vertical");
    }

    internal float GetHorizontalAxis()
    {
        return Input.GetAxis("Horizontal");
    }
}
