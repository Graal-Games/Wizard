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

    private void Awake()
    {
        // Get all cameras in the scene
        //Camera[] allCameras = FindObjectsOfType<Camera>();

        //// Log each camera's details
        //foreach (Camera camera in allCameras)
        //{
        //    Debug.Log($"Camera: {camera.name}, Position: {camera.transform.position}, Rotation: {camera.transform.rotation}");
        //}

        
        //if (cameraTransform == null)
        //{
        //    cameraTransform = FindObjectOfType<Camera>()?.transform;
        //}
        //if (cameraTransform == null)
        //{
        //    // Find the camera tagged with "PlayerCamera"
        //    cameraTransform = GameObject.FindGameObjectWithTag("PlayerCamera")?.transform;

        //    // If the camera is still null, you might want to find it by other means
        //    if (cameraTransform == null)
        //    {
        //        cameraTransform = FindObjectOfType<Camera>()?.transform;
        //    }
        //}

    }


    private void Start()
    {

    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //gameObject.GetComponent<NetworkTransform>().enabled = true;
        Debug.Log($"IsOwner: " + IsOwner + " IsLocalPlayer: " + IsLocalPlayer);
        Debug.Log($"OwnerClientId: " + OwnerClientId);

        if (!IsOwner) return;

        if (beamStatus != null) beamStatus(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), true);

        StartCoroutine(DestroyBeamObject());

        newPlayerBehaviorScript = GetComponentInParent<NewPlayerBehavior>();

    }

    IEnumerator DestroyBeamObject()
    {
        yield return new WaitForSeconds(SpellDataScriptableObject.spellDuration);
        DestroySpellRpc();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log($"OwnerClientIdOwnerClientId: " + OwnerClientId);
        //if (!IsOwner) return;
        Debug.Log($"IsOwner: " + IsOwner + "- IsLocalPlayer: " + IsLocalPlayer + "- Parent: " + gameObject.transform.parent.GetChild(2));
        //gameObject.transform.position = ownerGameObject.transform.GetChild(2).transform.position;
        //gameObject.transform.rotation = ownerGameObject.transform.GetChild(2).transform.rotation;
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
