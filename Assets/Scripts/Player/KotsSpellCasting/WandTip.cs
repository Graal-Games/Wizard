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

    public LayerMask aimMask;


    public NetworkVariable<Vector3> wandTipPosition = new NetworkVariable<Vector3>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<Quaternion> wandTipRotation = new NetworkVariable<Quaternion>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<Vector3> aimDirection = new NetworkVariable<Vector3>(
    writePerm: NetworkVariableWritePermission.Owner);



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

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        aimDirection.Value = GetCenterScreenAimDirection();
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

                    if (hit.transform.name.Contains("Floor") || hit.transform.name.Contains("Stairs"))
                    {
                        // ?? Does this function need to also be inside update?
                        AoePlacement();
                        aoeSpawnPosition = new Vector3(hit.point.x, hit.point.y + 0.0001f, hit.point.z);
                    }
                    else
                    {
                        RaycastHit downHit;

                        if (Physics.Raycast(hit.point, Vector3.down, out downHit, Mathf.Infinity))
                        {
                            if (downHit.transform.gameObject.name.Contains("Floor") || downHit.transform.gameObject.name.Contains("Stairs"))
                            {
                                // Example: offset backward from hit direction
                                //Vector3 offsetDirection = (hit.point - ray.origin).normalized;
                                Vector3 offsetDirection = hit.normal;

                                // CALCULATE OFFSET DEPENDING ON DIRECTION AND OBSTACLE POSITION
                                // ?? Does this function need to also be inside update?
                                AoePlacement();

                                float offsetDistance = -3f;

                                aoeSpawnPosition = downHit.point - offsetDirection * offsetDistance + Vector3.up * 0.0001f;
                            }
                        }
                    }
                } 
            }
        }
    }

    // This handles the AoE placement, which is the visualization of where the spell will be placed
    void AoePlacement()
    {
        if (aoePlacementInstance != null) Destroy(aoePlacementInstance);


        aoePlacementInstance = Instantiate(aoePlacementGO, aoeSpawnPosition, newRotation);
        //isAoeVisualizationActive = false;

        if (aoePlacementInstance != null)
        {
            aoePlacementInstance.transform.position = aoeSpawnPosition;
            aoePlacementInstance.transform.rotation = aoeSpawnRotation;
        }
    }

    // This Moves the object from which discharge spells are cast
    //and aoes Through a raycast inside aoeCast.cs
    private void WandAimDirection()
    {
        transform.LookAt(cameraTransform);
        transform.localRotation *= Quaternion.Euler(-180, 0, -180);
    }


    // Returns the world-space direction from the wand to the dot in the center of the screen
    public Vector3 GetCenterScreenAimDirection()
    {
        // 1. Screen center
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f);

        // 2. Ray from camera through screen center
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        // 3. Determine world target
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, aimMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = ray.GetPoint(30f); // just shoot straight ahead infinitely
            targetPoint = hit.point; // just shoot straight ahead infinitely

        } else
        {
            targetPoint = ray.GetPoint(30f);
        }

        // 4. Direction from wand to that target
        return (targetPoint - transform.position).normalized;

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
