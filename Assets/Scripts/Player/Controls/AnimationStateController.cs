using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AnimationStateController : NetworkBehaviour
{
    [SerializeField] Animator characterAnimator;


    void Start()
    {
        characterAnimator = GetComponent<Animator>();
        //Debug.Log(" characterAnimator " + characterAnimator);
    }



    public void RunForwardAnimation(bool value)
    {
        RunForwardAnimationServerRpc(value);
    }



    [ServerRpc]
    void RunForwardAnimationServerRpc(bool value)
    {
        characterAnimator.SetBool("isRunningForward", value);
    }



    public void RunBackwardsAnimation(bool value)
    {
        RunBackwardsAnimationServerRpc(value);
    }



    [ServerRpc]
    void RunBackwardsAnimationServerRpc(bool value)
    {
        characterAnimator.SetBool("isRunningBack", value);
    }



    public void RunRightAnimation(bool value)
    {
        RunRightAnimationServerRpc(value);
    }



    [ServerRpc]
    void RunRightAnimationServerRpc(bool value)
    {
        characterAnimator.SetBool("isRunningRight", value);
    }



    public void RunLeftAnimation(bool value)
    {
        RunLeftAnimationServerRpc(value);
    }



    [ServerRpc]
    void RunLeftAnimationServerRpc(bool value)
    {
        characterAnimator.SetBool("isRunningLeft", value);
    }



    public void IdleAnimation(bool value)
    {
        IdleAnimationServerRpc(value);
    }



    [ServerRpc]
    void IdleAnimationServerRpc(bool value)
    {
        characterAnimator.SetBool("isIdle", value);
    }

    

    public void CastSwingAnimation()
    {
        characterAnimator.SetTrigger("attack");
    }
}
