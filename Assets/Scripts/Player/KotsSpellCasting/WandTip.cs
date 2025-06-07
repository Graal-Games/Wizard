using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using AoeCast;
using Cinemachine;
using System;

public class WandTip : NetworkBehaviour
{
    [Header("Castpoint movement")]
    private float sensitivity = 2.5f; // Mouse sensitivity
    private float xRotation = 0f; // Current rotation around the x-axis
    private float yRotation = 0f;

    bool isAoeRaycastActive = false;

    //PlayerController playerController;
    bool isAoeVisualizationActive = true;


    [SerializeField]
    GameObject aoePlacementGO; // This should become a standardized VFX/ shader
    GameObject aoePlacementInstance;

    GameObject aoeSpellToSpawn;

    Vector3 aoeSpawnPosition;
    Quaternion aoeSpawnRotation;

    Quaternion newRotation;

    private Transform cameraTransform;


    public NetworkVariable<Vector3> wandTipPosition = new NetworkVariable<Vector3>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<Quaternion> wandTipRotation = new NetworkVariable<Quaternion>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);



    public bool IsAoeRaycastActive
    {
        get { return isAoeRaycastActive; }
        set { isAoeRaycastActive = value; }
    }

    private void Awake()
    {
        //if (cameraTransform == null)
        //{
        //    cameraTransform = FindObjectOfType<Camera>()?.transform;
        //}

        //// Set default rotation for the aoe spell to be used later for placement visualization and spawn/instantiation
        //float yRotation = transform.rotation.eulerAngles.y;
        //newRotation = Quaternion.Euler(0, yRotation, 0);
    }


    void UpdateRotAndPos()
    {
        wandTipPosition.Value = gameObject.transform.position;
        wandTipRotation.Value = gameObject.transform.rotation;
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (cameraTransform == null)
        {
            cameraTransform = FindObjectOfType<Camera>()?.transform;
        }

        // Set default rotation for the aoe spell to be used later for placement visualization and spawn/instantiation
        float yRotation = transform.rotation.eulerAngles.y;
        newRotation = Quaternion.Euler(0, yRotation, 0);
    }

    void Update()
    {
        if (!IsOwner) return;

        UpdateRotAndPos();

        WandAimDirection();

        // This can be placed in a method in a class
        if (IsAoeRaycastActive)
        {
            float vertical = Input.GetAxis("Vertical");

            Vector3 newPosition = transform.position + transform.forward * vertical;

            // This defines what layers are NOT ignored by the raycast
            int includedLayer = LayerMask.GetMask("FloorWallsAndObstacles");

            int layerMask = ~includedLayer;

            RaycastHit hit;
            Ray ray = new Ray(newPosition, transform.forward);

            // Debug.DrawRay(newPosition, transform.forward * 1000f, Color.red);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~layerMask))
            { //&&  ( hit.transform.name.Contains("Floor") || hit.transform.name.Contains("Pillar") )
                if (hit.transform.name != null)
                {
                    //Debug.LogFormat($"hit - x:{hit.point.x} - y:{hit.point.y} - z:{hit.point.z}");
                    //spawnLocationIsValid = true;

                    // ?? Does this function need to also be inside update?
                    AoePlacement();
                    aoeSpawnPosition = new Vector3(hit.point.x, hit.point.y + 0.0001f, hit.point.z);
                }

            }
        }
    }

    // This handles the AoE placement, which is the visualization of where the spell will be placed
    void AoePlacement()
    {

        //if (isAoeVisualizationActive)
        //{
            if (aoePlacementInstance != null) Destroy(aoePlacementInstance);


            aoePlacementInstance = Instantiate(aoePlacementGO, aoeSpawnPosition, newRotation);
            //isAoeVisualizationActive = false;

            if (aoePlacementInstance != null)
            {
                aoePlacementInstance.transform.position = aoeSpawnPosition;
                aoePlacementInstance.transform.rotation = aoeSpawnRotation;
            }
        //}

        //// This is what makes sure that the placement object is spawned where the raycast hits the floor, respectively
        //if (isAoeVisualizationActive == false)
        //{
        //    Debug.LogFormat($"isAoeVisualizationActive - {isAoeVisualizationActive}");

        //    aoePlacementInstance.transform.position = aoeSpawnPosition;
        //    aoePlacementInstance.transform.rotation = aoeSpawnRotation;

        //    isAoeVisualizationActive = true;
        //}
    }

    // This Moves the object from which discharge spells are cast
    //and aoes Through a raycast inside aoeCast.cs
    private void WandAimDirection()
    {
        transform.LookAt(cameraTransform);
        transform.localRotation *= Quaternion.Euler(-180, 0, -180);
    }


    public void ActivateAoePlacementVisualizer()
    {
        //Debug.Log("PLACEMENT METHOD ENTRY");
        IsAoeRaycastActive = true;
        //Debug.Log("PLACEMENT ACTIVITY STATUS: " + IsAoeRaycastActive);
    }

    public void DeactivateAoePlacementVisualizer()
    {
        Destroy(aoePlacementInstance);
        IsAoeRaycastActive = false;
    }

    // Return spawn location to spawn spell
    public (Quaternion, Vector3) GetAoeRotationAndPosition()
    {
        Quaternion rotation = aoeSpawnRotation;
        Vector3 position = aoeSpawnPosition;

        return (rotation, position);
    }

    public void SpawnAoe(GameObject aoeSpell)
    {
        aoeSpellToSpawn = aoeSpell;
        SpawnAoeRpc();
    }

    [Rpc(SendTo.Server)]
    void SpawnAoeRpc()
    {
        aoeSpellToSpawn.GetComponent<NetworkObject>().Spawn();

    }
}
