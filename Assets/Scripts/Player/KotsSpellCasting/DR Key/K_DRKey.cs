using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static K_SpellLauncher;
using System;
using static IncapacitationEffect.Incapacitation;

public class K_DRKey : MonoBehaviour
{
    [Header("Key Parameters")]
    [SerializeField] private KeyCode keyCode;
    [SerializeField] private Color keyColor;
    [SerializeField] private bool castKey;

    [Header("Buffer Square Settings")]
    [SerializeField] private Color bufferSquareStartColor;
    [SerializeField] private Color bufferSquareFailColor;

    [Header("Component References")]
    [SerializeField] private Animator anim;
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private Image invisibleImage;
    [SerializeField] private Image bufferSquareImage;
    [SerializeField] private Image border;

    [HideInInspector] public bool invisible;
    [HideInInspector] public bool buffered;

    private const float INACTIVE_KEY_OPACITY = 0.2f;
    private const float INVISIBLE_IMG_ACTIVE_OPACITY = 0.5f;
    private bool active;
    private bool solvable;
    // Allows immediate casting on G without waiting for the buffer animation window
    [SerializeField] private bool allowImmediateCast = false;

    public event OnBufferFailedEventHandler OnBufferFailed;

    public delegate void OnPlayerFailedToSyncInputToBuffer();
    public static event OnPlayerFailedToSyncInputToBuffer onPlayerFailedToSyncInputToBuffer;


    // This is a reference to the animator
    public Animator Anim
    {
        get { return anim; }
    }




    private void OnEnable()
    {
        if (!castKey)
            SetActive(false);

        keyText.gameObject.SetActive(!invisible);
        invisibleImage.gameObject.SetActive(invisible);
        bufferSquareImage.color = bufferSquareStartColor;
        anim.speed = 1f;
        allowImmediateCast = false;
    }




    private void OnValidate()
    {
        if (castKey)
        {
            keyText.color = keyColor;
            invisibleImage.color = new Color(keyColor.r, keyColor.g, keyColor.b, INVISIBLE_IMG_ACTIVE_OPACITY);
            border.color = Color.black;
        }
        else
        {
            SetActive(false);
        }
    }




    public void SetActive(bool active)
    {
        if (active)
        {
            keyText.color = keyColor;
            invisibleImage.color = new Color(keyColor.r, keyColor.g, keyColor.b, INVISIBLE_IMG_ACTIVE_OPACITY);
            border.color = Color.black;

            solvable = !(buffered || castKey);

            if (buffered)
                StartCastBufferAnim();
        }
        else
        {
            Color inactiveKeyColor = new Color(keyColor.r, keyColor.g, keyColor.b, INACTIVE_KEY_OPACITY);

            keyText.color = inactiveKeyColor;
            invisibleImage.color = inactiveKeyColor;
            border.color = new Color(0f, 0f, 0f, INACTIVE_KEY_OPACITY);

            solvable = false;
            allowImmediateCast = false;
        }

        this.active = active;
    }




    /// <summary>
    /// Try to solve this DR key. If the pressedKey is the same as the
    /// keyCode of this DR key and the key is currently solvable (only
    /// applies if it's buffered), it'll return true. False otherwise.
    /// </summary>
    /// <param name="pressedKey">The user pressed key to test against.</param>
    /// <returns>Boolean value indicating if the solve suceeded.</returns>
    public bool TrySolve(KeyCode pressedKey)
    {
        Debug.LogFormat($"<color=red> >>>> TrySolve: {pressedKey} </color>");

        // Debug.LogFormat($"<color=red> >>>> TrySolve: {buffered} + {solvable} </color>");

        if (buffered && !solvable)
        {
            Debug.LogFormat($"<color=red> >>>> TrySolve: {pressedKey} </color>");

            BufferFailed();
        }


        return pressedKey == keyCode && solvable && active;
    }




    /// <summary>
    /// Determines if a spell can be casted at this moment, based on the
    /// solvable state. This function is only intended to be called if
    /// castKey = true;
    /// </summary>
    /// <returns> this method evaluates to true if the (DRKey) key is a castKey 
    /// and if it's currently solvable, otherwise it evaluates as false.</returns>
    public bool TryCast()
    {
        return castKey && (solvable || allowImmediateCast);
    }




    /// <summary>
    /// Executes the buffer animation once and returns to idle after it.
    /// This function it's only intended to be called if castKey = true.
    /// </summary>
    public void StartCastBufferAnim()
    {
        anim.speed = 1f;
        anim.SetTrigger("CastBuffer");
    }

    /// <summary>
    /// Executes the buffer animation once and returns to idle after it.
    /// This function it's only intended to be called if castKey = true.
    /// </summary>
    /// <param name="speed">The speed in float of the buffer animation, default 1f.</param>
    public void StartCastBufferAnim(float speed)
    {
        anim.speed = speed;
        anim.SetTrigger("CastBuffer");
    }

    public void StopCastBufferAnim()
    {
        //if (anim.GetCurrentAnimatorStateInfo(0).IsName("CastBuffer") || anim.IsInTransition(0))
        anim.SetTrigger("StopCastBuffer");
        allowImmediateCast = false;
    }




    public bool CheckIfIsInTransition()
    {
        Debug.LogFormat($"<color=blue> CheckIfIsInTransition </color>");

        bool isAnimating = !anim.GetCurrentAnimatorStateInfo(0).IsName("Idle") || anim.IsInTransition(0);

        return isAnimating;
    }




    /// <summary>
    /// This function will be called from the Animator component.
    /// Set the solvable property to true if solvable = 1, false otherwise.
    /// Using an int instead of a bool beacause the Animator can't pass
    /// bool values as function parameters.
    /// </summary>
    /// <param name="solvable">1 to set the solvable property to true,
    /// any other value will set it to false.</param>
    public void Animator_SetSolvable(int solvable)
    {
        this.solvable = solvable == 1;
    }

    public void SetAllowImmediateCast(bool value)
    {
        allowImmediateCast = value;
    }


    public void BufferFailed()
    {
        Debug.LogFormat($"<color=blue> 1 BufferFailed </color>");
        OnBufferFailed?.Invoke(); // This is never being called

        if (onPlayerFailedToSyncInputToBuffer != null) onPlayerFailedToSyncInputToBuffer();

        if (!castKey)
        {
            Debug.LogFormat($"<color=blue> 2 BufferFailed </color>");
            bufferSquareImage.color = bufferSquareFailColor;
            //anim.speed = 0f;
            OnBufferFailed?.Invoke();
            if (onPlayerFailedToSyncInputToBuffer != null) onPlayerFailedToSyncInputToBuffer();
        }

        //Debug.LogException(new Exception("Buffer Failed Exception"));

        // throw new Exception("Buffer Failed Exception");


    }
}
