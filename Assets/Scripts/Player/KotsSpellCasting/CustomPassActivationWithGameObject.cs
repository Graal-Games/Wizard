using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// I couldn't figure out how to add an asmdef to the folder where this is located
// Nor move it out of the folder within which it is found
// So I resorted to events

public class CustomPassActivationWithGameObject : MonoBehaviour
{
    public CustomPassVolume customPassVolume;
    public GameObject triggerObject1;
    public GameObject triggerObject2;
    public float fadeRadius = 10f;

    public Material fullScreenShader;
    [SerializeField, ReadOnly] float activateBlood = 1.0f;
    [SerializeField, ReadOnly] float deactivateBlood = 0.0f;
    public float activateDistortion;
    public float deactivateDistortion;
    public Vector2 activateWater;
    public Vector2 deactivateWater;

    private string BloodShader = "_BloodActivation";
    private string DistortionShader = "_DistortionActivation";
    private string WaterShader = "_WaterActivation";

    //[SerializeField] ShaderActivation shaderScript;
    NewPlayerBehavior newPlayerBehavior;

    private Transform player;

    private float alpha;

    private Color red;

    private Dictionary<string, Action> shadersMap = new Dictionary<string, Action>();

    ulong parentId;

    Collider parentCollider;

    float duration;


    //public void GetPlayer(Transform playerReference)
    //{
    //    player = playerReference;

    //    if (player != null)
    //    {
    //        transform.SetParent(player, true);
    //    }

    //    StartShader();
    //}

    void Start()
    {
        fullScreenShader.SetFloat(DistortionShader, deactivateDistortion);
        fullScreenShader.SetFloat(BloodShader, deactivateBlood);
        fullScreenShader.SetVector(WaterShader, deactivateWater);
        red = new Color(1, 0, 0, alpha);

        customPassVolume.targetCamera = FindObjectOfType<Camera>();

        shadersMap.Add("Blood", ActivateBloodShader);
        shadersMap.Add("Water", ActivateWaterShader);        
        shadersMap.Add("Distortion", ActivateDistortionShader);
        shadersMap.Add("WaterAndDistortion", ActivateWaterAndDistortionShader);

        parentCollider = GetComponentInParent<Collider>();
        //newPlayerBehavior = gameObject.GetComponentInParent<NewPlayerBehavior>();

        NewPlayerBehavior.shaderActivation += HandleShaderActivation;

        Debug.Log("Custom pass Collider: " + parentCollider);
    }

    // This takes in the name of the shader to activate
    public void HandleShaderActivation(ulong clientId, string shaderKey, float seconds)
    {
        Action shaderValue;

        duration = seconds;

        // Get the method associated with the key passed (name of shader)
        if (shadersMap.TryGetValue(shaderKey, out shaderValue))
        {
            // Invoke the method using the delegate
            shaderValue.Invoke();
        }
    }

    void ActivateBloodShader()
    {
        // Activate the fog and set the fade radius of the custom pass
        fullScreenShader.SetFloat(BloodShader, activateBlood);
        fullScreenShader.SetColor("_FogColor1", red);

        Debug.Log("BloodActivation value is: " + fullScreenShader.GetFloat(BloodShader));
        customPassVolume.fadeRadius = fadeRadius;

        StartCoroutine(DeactivateBloodShader(duration));
    }

    IEnumerator DeactivateBloodShader(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        fullScreenShader.SetFloat(BloodShader, deactivateBlood);
    }

    void ActivateDistortionShader()
    {
        fullScreenShader.SetFloat(DistortionShader, activateDistortion);
        Debug.Log("DistortionActivation value is: " + fullScreenShader.GetFloat(DistortionShader));
        customPassVolume.fadeRadius = fadeRadius;
    }

    IEnumerator DeactivateDistortionShader(int seconds)
    {
        yield return new WaitForSeconds(seconds);
    }


    void ActivateWaterShader()
    {
        fullScreenShader.SetFloat(DistortionShader, activateDistortion);
        Debug.Log("DistortionActivation value is: " + fullScreenShader.GetFloat(DistortionShader));
        customPassVolume.fadeRadius = fadeRadius;
    }

    IEnumerator DeactivateWaterShader(int seconds)
    {
        yield return new WaitForSeconds(seconds);
    }


    // FOr this one to work we would need to pass in two different durations
    // one for the water and the other for the distortion
    // Is there a more efficient solution here?
    void ActivateWaterAndDistortionShader()
    {

    }
}