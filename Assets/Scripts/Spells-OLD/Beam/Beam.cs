using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using Singletons;
using SpellsManager;

public class Beam : NetworkBehaviour, IChannelable
{
    public float damage = 3f;

    private float startTime;

    //float timerDuration = 2f;

    public NetworkVariable<float> timerDuration = new NetworkVariable<float>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool hasHitShield;

    private bool isHitPlayer = false;

    Transform castPoint;

    Quaternion beamRotation;

    float sensitivity = 100f;

    private float xRotation = 0f; // Current rotation around the x-axis

    private float yRotation = 0f;

    float addToDuration;

    // Create a delegate and event from it
    public delegate void BeamLifeStatus(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool status);
    public static event BeamLifeStatus beamExists;

    public delegate void BeamHitPlayer(bool status, ulong clientId, string location);
    public static event BeamHitPlayer beamHitPlayer;

    public Action<bool, ulong, string> beamHitStatus;


    //[SerializeField] GameObject spellsManagerGO;
    //SpellsManager spellsManager;
    //public NetworkVariable<bool> beamIsAlive = new NetworkVariable<bool>(default,
    //NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    //PlayerSpellHandler playerSpellHandler;
    ulong targetPlayerId;


    void Start()
    {
        //if (!IsLocalPlayer) return;
        
        startTime = Time.time;

        SpellsManager.Beam.CurrentBeamInstance(this.gameObject);

    }



    public float Damage()
    {
        return 3f;
    }



    // public bool IsHitPlayer(bool value)
    // {
    //     return value;
    // }



    public bool IsHitPlayer
    {
        get
        {
            return isHitPlayer;
        }
        set
        {
            isHitPlayer = value;
        }
    }



    public override void OnGainedOwnership()
    {
        timerDuration.Value = 2;
        
        base.OnGainedOwnership();
    }



    // private void MoveWithPlayerLook()
    // {
    //     float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
    //     float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

    //     xRotation -= mouseY;
    //     yRotation += mouseX; 

    //     xRotation = Mathf.Clamp(xRotation, -90f, 25f);

    //     //This works properly now:
    //     transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    // }



    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        AliveTime();
        CheckIfBeamCastingIsCanceled();     
    }



    private void CheckIfBeamCastingIsCanceled()
    {
        if (SpellsManager.Beam.BeamCancelled() == true)
        {
            DestroyBeam();
            //Debug.LogFormat($"Message: <color=blue>BEAM CANCELLED </color>");
        }   
        
    }



    public void Upkeep(float upkeepDurationAmount)
    {
        addToDuration += upkeepDurationAmount;
    }
    


    void AliveTime(GameObject target = null) 
    {
        float elapsedTime = Time.time - startTime;

        timerDuration.Value += addToDuration;

        if (elapsedTime >= timerDuration.Value)
        {
            Debug.Log("Owner:  " + OwnerClientId + " timerDuration:  " + timerDuration.Value + "  elapsedTime: " + elapsedTime);
            IsHitPlayer = false;

            if (beamHitPlayer != null) beamHitPlayer(false, OwnerClientId, " Duration exhausted ");
            DestroyBeam();
            
            return;
        }

        addToDuration = 0;
    }

    
    // When the beam spawn trigger an event that asserts that the beam exists (..., true)
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        //beamIsAlive.Value = true;

        timerDuration.Value = 2;

        if (beamExists != null) beamExists(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), true);
        
        base.OnNetworkSpawn();
    }



    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        
        if (beamExists != null) beamExists(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), false);

        //beamIsAlive.Value = false;

        base.OnNetworkDespawn();
    }



    public void DestroyBeam()
    {
            SpellsManager.Beam.BeamDestroyed(OwnerClientId, true);

            this.gameObject.SetActive(false);
            DestroyBeamServerRpc();

    }



    [ServerRpc(RequireOwnership = false)]
    private void DestroyBeamServerRpc()
    {
        NetworkObject.Despawn(this.gameObject);
        return;
    }



     void OnTriggerEnter(Collider other)
    {
        Debug.Log("Beam collided with: " + other.gameObject);

        if (other.gameObject.name.Contains("SphereShield"))
        {
            hasHitShield = true;
            //other.gameObject.GetComponent<PlayerBehav
        }

        // if (other.gameObject.name.Contains("Player"))
        // {

        // }
    }



    void OnTriggerExit(Collider other)
    {
        // if (other.gameObject.name.Contains("Player"))
        // {

        // }
    }
    
}

internal interface IChannelable
{
    public void Upkeep(float upkeepDurationAmount);
}