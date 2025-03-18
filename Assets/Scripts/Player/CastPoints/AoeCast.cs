using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AoeCast : NetworkBehaviour
{
    [Header("Standard Aoe Spells")]
    [SerializeField] GameObject arcaneAoePlacement;
    [SerializeField] GameObject arcaneAoe;

    [SerializeField] GameObject earthAoe;

    [SerializeField] GameObject fireAoe;


    [Header("Charm Aoe Spells")]
    [SerializeField] GameObject mistAoePlacement;
    [SerializeField] GameObject mistAoe;
    
    [SerializeField] GameObject defaultAoePlacement;
    [SerializeField] GameObject defaultAoe; // This is redundant and must be replaced just before cast is confirmed

    [Header("Aoe Barriers")]
    [SerializeField] GameObject arcaneBarrierAoePlacement;

    [SerializeField] GameObject arcaneBarrierAoe;

    [SerializeField] GameObject earthBarrier;
    [SerializeField] GameObject fireBarrier;
    [SerializeField] GameObject waterBarrier;
    [SerializeField] GameObject airBarrier;

    [Header("Aoe Long Barriers")]
    [SerializeField] GameObject longBarrierAoePlacement;
    [SerializeField] GameObject arcaneLongBarrierAoe;

    [SerializeField] GameObject waterLongBarrierAoe;
    [SerializeField] GameObject earthLongBarrierAoe;
    [SerializeField] GameObject fireLongBarrierAoe;
    [SerializeField] GameObject airLongBarrierAoe;



    [Header("AoE Globals")]
    GameObject chosenPlacementAoe;
    GameObject chosenAoeObject;


    bool isAoeRaycastActive = false;
    bool spawnLocationIsValid = false;
    Vector3 aoeSpawnPosition;
    Quaternion aoeSpawnRotation;

    bool isAoeVisualizationActive = true;
    GameObject aoePlacementInstance;

    //public NetworkVariable<Vector3> aoePlacementPosition = new NetworkVariable<Vector3>(default,
    //    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    
    public Vector3 AoeSpawnPosition 
    {
        get { return aoeSpawnPosition; } 
        set {  aoeSpawnPosition = value; } 
    }   

    /// <summary>
    ///  
    /// </summary>

    void Update()
    {
        if (isAoeRaycastActive)
        {
            float vertical = Input.GetAxis("Vertical");

            Vector3 newPosition = transform.position + transform.forward * vertical;
            
            // This defines what layers are NOT ignored by the raycast
            int includedLayer = LayerMask.GetMask("FloorWallsAndObstacles");

            int layerMask = ~includedLayer;

            RaycastHit hit;
            Ray ray = new Ray(newPosition, transform.forward);

            Debug.DrawRay(newPosition, transform.forward * 1000f, Color.green);
            
            // This 
            if ( Physics.Raycast(ray, out hit, Mathf.Infinity, ~layerMask) )
            { //&&  ( hit.transform.name.Contains("Floor") || hit.transform.name.Contains("Pillar") )
                if ( hit.transform.name != null  )
                {
                    //Debug.Log("Floor hit");
                    spawnLocationIsValid = true;

                    // ?? Does this function need to also be inside update?
                    AoePlacement();
                    aoeSpawnPosition = new Vector3(hit.point.x, -1f, hit.point.z);
                }
                
            }
            
            // Updates the Aoe placement gameobject (that shows where the actual Aoe object will be spawned) 's position
            // >> aoeSpawnPosition = new Vector3 (hit.point.x, -1f, hit.point.z);

            // This makes sure the AoE placement model is pointing in the right direction
            {
                float yRotation = transform.rotation.eulerAngles.y;
                Quaternion newRotation = Quaternion.Euler(0, yRotation, 0);

                aoeSpawnRotation = newRotation;
            }

            //Debug.LogFormat($"<color=red>{aoeSpawnRotation}</color>");
        }
    }

    // This handles the AoE placement, which is the visualization of where the spell will be placed
    void AoePlacement()
    {
        if ( isAoeVisualizationActive )
        {
            if (aoePlacementInstance != null) Destroy(aoePlacementInstance);

            float yRotation = transform.rotation.eulerAngles.y;
            Quaternion newRotation = Quaternion.Euler(0, yRotation, 0);

            aoePlacementInstance = Instantiate(chosenPlacementAoe, aoeSpawnPosition, newRotation);
            isAoeVisualizationActive = false;
        } else {

            // This doesn't work either
            // if (aoePlacementInstance) {
            //     Destroy(aoePlacementInstance);
            // }
            
        }

        // This is what makes sure that the placement object is spawned where the raycast hits the floor, respectively
        if ( isAoeVisualizationActive == false )
        {
            aoePlacementInstance.transform.position = aoeSpawnPosition;
            // Need to get rotation too ??
            aoePlacementInstance.transform.rotation = aoeSpawnRotation;
            //Debug.LogFormat($"<color=red>IS PLACING</color>");
        }
    }

    public void StopAoePlacement()
    {
        isAoeRaycastActive = false;
        Destroy( aoePlacementInstance );
    }

    public void StartCastAoe( string aoeToBeCast = null )
    {
        // This function selects the visual prefab that helps visualize the AoE spell being placed
        // After the prefab is selected through a switch case, a bool gate is open to begin the visualization process (inside Update)
        ActivateSpecificAoeObjectPlacementVisualization( aoeToBeCast );
        
        
        // Debug.Log("AOE CASTING");
    }

    public void StopCastAoe()
    {
        isAoeRaycastActive = false;
        // Debug.Log("AOE UN-CASTING");
    }

    public void ConfirmPlacement( string spellToBePlaced )
    {
        // Despawn the aoe1Placement
        // Stop the Aoe Cast and reset the gates
        
        Destroy(aoePlacementInstance);

        //chosenAoeObjectNetObj =  spawnedBeamInstance.GetComponent<NetworkObject>();

       
        ConfirmArcanePlacementServerRpc(aoeSpawnPosition, aoeSpawnRotation, spellToBePlaced);

        

        //Debug.Log("SPAWN ROT1:::" + aoeSpawnRotation);
        // GameObject aoeInstance = Instantiate(aoe1, spawnPosition, new Quaternion (0,0,0,0));
        // aoeInstance.GetComponent<NetworkObject>().Spawn();
    }


    // Make a seperate ServerRpc for each spell 
    [ServerRpc]
    void ConfirmArcanePlacementServerRpc(Vector3 spawnPosition, Quaternion aoeSpawnRotation, string aoeObjectToSpawn)
    {
        GameObject aoeObject = GetAoeGameobject(aoeObjectToSpawn);

        //Debug.Log("SPAWN ROT2:::" + aoeSpawnRotation);

        float yRotation = transform.rotation.eulerAngles.y;
        Quaternion newRotation = Quaternion.Euler(0, yRotation, 0);

        GameObject aoeInstance = Instantiate(aoeObject, spawnPosition, newRotation);
        aoeInstance.GetComponent<NetworkObject>().Spawn();

        
    }


    GameObject GetAoeGameobject(string aoeObjectToSpawn)
    {
        switch (aoeObjectToSpawn)
        {
            // Long Barriers
            case "Arcane Long Barrier":
                return arcaneLongBarrierAoe;

            case "Water Long Barrier":
                return waterLongBarrierAoe;

            case "Earth Long Barrier":
                return earthLongBarrierAoe;

            case "Air Long Barrier":
                return airLongBarrierAoe;

            case "Fire Long Barrier":
                return fireLongBarrierAoe;

            // Regular Barriers
            case "earth barrier":
                return earthBarrier;

            case "fire barrier":
                return fireBarrier;

            // AoEs
            case "Arcane Aoe":
                return arcaneAoe;

            case "Earth Aoe":
                return earthAoe;

            case "Fire Aoe":
                return fireAoe;

            case "Air Aoe":
                return null;


            case "mist":
                return mistAoe;
            default:
                return null;

        }
    }



    // *** TO MIGRATE 
    ///  This does not seem to be used
    [ServerRpc]
    void ConfirmMistPlacementServerRpc(Vector3 spawnPosition)
    {
        //Debug.Log("SPAWN POS2:::" + spawnPosition);
        GameObject aoeInstance = Instantiate(mistAoe, spawnPosition, new Quaternion (0,0,0,0));
        aoeInstance.GetComponent<NetworkObject>().Spawn();
    }

    // This chooses the placement object
    public void ChangeAoeLongBarrierElement(string spell)
    {
        switch (spell)
        {
            case "U": //WATER
                chosenPlacementAoe = longBarrierAoePlacement; // Is this nevcessa
                chosenAoeObject = waterLongBarrierAoe; // ?chosen aoe no longer fulfills a function?

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                return;
            case "I": //EARTH
                chosenPlacementAoe = longBarrierAoePlacement;
                chosenAoeObject = earthLongBarrierAoe;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                return;
            case "P": //FIRE
                chosenPlacementAoe = longBarrierAoePlacement;
                chosenAoeObject = fireLongBarrierAoe;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                return;
            case "O": //AIR
                chosenPlacementAoe = longBarrierAoePlacement;
                chosenAoeObject = airLongBarrierAoe;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                return;
        }
    }

    // Complete this
    public void ChangeAoeBarrierElement(string element)
    {
        switch (element)
        {
            case "U": // earth
                chosenPlacementAoe = arcaneBarrierAoePlacement;
                chosenAoeObject = earthBarrier;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                break;
            case "I": //water
                chosenPlacementAoe = arcaneBarrierAoePlacement;
                chosenAoeObject = waterBarrier;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                break;
            case "O": //air
                chosenPlacementAoe = arcaneBarrierAoePlacement;
                chosenAoeObject = airBarrier;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                break;
            case "P": //fire
                chosenPlacementAoe = arcaneBarrierAoePlacement;
                chosenAoeObject = fireBarrier;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                break;

        }
    }

    public void ChangeAoeSpell(string spell)
    {
        switch(spell)
        {
            case "Arcane Barrier":
                
                chosenPlacementAoe = arcaneBarrierAoePlacement;
                chosenAoeObject = arcaneBarrierAoe;
                
                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                return;

            case "Long Arcane Barrier":
                chosenPlacementAoe = longBarrierAoePlacement;
                chosenAoeObject = arcaneLongBarrierAoe;
                
                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                return;
            case "Long Water Barrier":
                chosenPlacementAoe = longBarrierAoePlacement;
                chosenAoeObject = waterLongBarrierAoe;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                return;
            case "Long Earth Barrier":
                chosenPlacementAoe = longBarrierAoePlacement;
                chosenAoeObject = earthLongBarrierAoe;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                return;
            case "Long Fire Barrier":
                chosenPlacementAoe = longBarrierAoePlacement;
                chosenAoeObject = fireLongBarrierAoe;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                return;
            case "Long Air Barrier":
                chosenPlacementAoe = longBarrierAoePlacement;
                chosenAoeObject = airLongBarrierAoe;

                isAoeRaycastActive = true;
                isAoeVisualizationActive = true;
                return;
        }

    }

    void ActivateSpecificAoeObjectPlacementVisualization(string aoeToBeCast = null)
    {
        switch(aoeToBeCast)
        {
            case "Arcane Aoe -1 [T]":

            chosenPlacementAoe = arcaneAoePlacement;
            chosenAoeObject = arcaneAoe;

            isAoeRaycastActive = true;
            isAoeVisualizationActive = true;

            break;

            case "Mist":

            chosenPlacementAoe = mistAoePlacement;
            chosenAoeObject = mistAoe;

            isAoeRaycastActive = true;
            isAoeVisualizationActive = true;

            break;

            default:

            chosenPlacementAoe = defaultAoePlacement;
            chosenAoeObject = defaultAoe;

            isAoeRaycastActive = true;
            isAoeVisualizationActive = true;

            return;

            
        }
    }

    
}
