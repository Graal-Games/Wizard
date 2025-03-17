using UnityEngine;
using System.Collections;
using System;
using Singletons;
using System.Security.Cryptography.X509Certificates;

// (Should) Handles all Ui spellcasting animations
// ** To rename
public class KeyUi : Singleton<KeyUi>
{
    // To change to [SerializeField]
    public Animator castSquareBuffer;
    //public Vector3 targetScale;
    //public float duration = 1.5f;
    //public bool animationIsComplete = true;

    public bool isAnimating = false;
    public bool isInterrupted = false;

    private float speedIncreaseThreshold = 0.5f;
    //private float speedIncreasePerSecond = 0.001f;

    //float timer = 0f;
    float time = 0f;
    string stateName;
    //bool IsRunningSqaure = false;

    // This controls the bolt casting outline square animation

    void Awake()
    {
        // Get the animator reference
        //this.gameObject.SetActive(true);
        
        castSquareBuffer = this.GetComponent<Animator>();
        
        //Debug.Log("StateName: " + );  
    }

    public void StopActionBufferSquareAnimation()
    {
        //isAnimating = false;
        castSquareBuffer.SetTrigger("hasEnded");
        Debug.Log("StateName: " + "END ANIM");
    }

    public void InterruptActionBufferSquareAnimation()
    {
        //isAnimating = false;
        castSquareBuffer.SetTrigger("isInterrupted");
    }

    public void StartAnimation()
    {
        Debug.Log("StateName: " + "START ANIM");
        // If the player is casting activate the outline
        //isAnimating = true;
        castSquareBuffer.SetTrigger("isCasting");
    }

    // I don't think this is being used
    void IncreaseAnimSpeedTimer()
    {
        time += Time.deltaTime * 1f;
        AnimationClip clip = castSquareBuffer.runtimeAnimatorController.animationClips[0];


        if (time > speedIncreaseThreshold)
        {
            castSquareBuffer.speed *= 4f;
            isAnimating = false;
        }

    }
}