using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class BeamSpell : K_Spell
{

    private Transform cameraTransform;
    NetworkObject ownerGameObject;

    public delegate void BeamStatus(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehaviorScript, bool status);
    public static event BeamStatus beamStatus;

    Transform parentTransform;
    Quaternion parentRotation;

    NewPlayerBehavior newPlayerBehaviorScript;

    //private void Awake()
    //{
    //    if (gameObject.transform.parent.GetChild(2) != null)
    //    {
    //        gameObject.transform.position = gameObject.transform.parent.GetChild(2).gameObject.GetComponent<WandTip>().wandTipPosition.Value;
    //        gameObject.transform.rotation = gameObject.transform.parent.GetChild(2).gameObject.GetComponent<WandTip>().wandTipRotation.Value * Quaternion.Euler(90, 0, 90);
    //    }
    //}

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //gameObject.GetComponent<NetworkTransform>().enabled = true;
        Debug.Log($"IsOwner: " + IsOwner + " IsLocalPlayer: " + IsLocalPlayer);
        Debug.Log($"OwnerClientId: " + OwnerClientId);

        //if (!IsOwner) return;

        if (beamStatus != null) beamStatus(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), true);

        StartCoroutine(LifeTime(SpellDataScriptableObject.spellDuration, gameObject)); // This is to be converted to a timer to adequately enable interruption.

        newPlayerBehaviorScript = GetComponentInParent<NewPlayerBehavior>();

    }

    //IEnumerator DestroyBeamObject()
    //{
    //    yield return new WaitForSeconds(SpellDataScriptableObject.spellDuration);

    //    //gameObject.GetComponent<Collider>().enabled = false;

    //    if (beamStatus != null) beamStatus(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), false);

    //    DestroySpellRpc();
    //}


    void Update()
    {
        //Debug.Log($"OwnerClientIdOwnerClientId: " + OwnerClientId);
        Debug.Log($"IsOwner: " + IsOwner + "- IsLocalPlayer: " + IsLocalPlayer + "- Parent: " + gameObject.transform.parent.GetChild(2));
        
        // Get the wand tip and move the beam the same way it moves
        if (gameObject.transform.parent.GetChild(2) != null)
        {
            gameObject.transform.position = gameObject.transform.parent.GetChild(2).gameObject.GetComponent<WandTip>().wandTipPosition.Value;
            gameObject.transform.rotation = gameObject.transform.parent.GetChild(2).gameObject.GetComponent<WandTip>().wandTipRotation.Value * Quaternion.Euler(90, 0, 90);
        }
        // WandAimDirection();
    }

    private void WandAimDirection()
    {
        Debug.Log($"cameraTransform: " + cameraTransform + "- IsLocalPlayer: " + IsLocalPlayer + "- Parent: " + gameObject.transform.parent);

        transform.LookAt(cameraTransform);
        //transform.LookAt(gameObject.transform.parent.GetChild(3).gameObject.transform);
        transform.localRotation *= Quaternion.Euler(-90, 0, 90);
    }

    public override void Fire()
    {

    }
}
