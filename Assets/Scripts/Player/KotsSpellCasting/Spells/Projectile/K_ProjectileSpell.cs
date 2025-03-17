using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class K_ProjectileSpell : K_Spell
{
    //[SerializeField]
    //private K_SpellData spellDataScriptableObject;
    bool spellHasSpawned = false;
    //string spellType;
    //bool hasHitShield = false;
    //bool hasHitPlayer = false;
    private bool triggerEntered = false;

    public NetworkVariable<float> directDamageAmount = new NetworkVariable<float>();

    // This is the locally saved GUID
    public string localSpellId;

    // This NV saves the GUID on the NetworkedObject instance of the same spell and is used to destroy the local instance of the projectile
    public NetworkVariable<FixedString128Bytes> spellId = new NetworkVariable<FixedString128Bytes>();

    public delegate void DestroyLocalProjectileInstance(FixedString128Bytes spellId);
    public static event DestroyLocalProjectileInstance projectileInstance;

    //public NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>(default,
    //    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    //public NetworkVariable<Vector3> previousPosition = new NetworkVariable<Vector3>(default,
    //    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    Vector3 currentPosition;
    Vector3 previousPosition;

    public float rayDistance = 5f; // Distance the ray will travel
   // public LayerMask collisionMask; // Optional: Set specific layers to interact with

    // Array of 12 directions (unit vectors)
    private static readonly Vector3[] directions = new Vector3[]
    {
        Vector3.up,               // +Y
        Vector3.down,             // -Y
        Vector3.left,             // -X
        Vector3.right,            // +X
        Vector3.forward,          // +Z
        Vector3.back,             // -Z
        new Vector3(1, 1, 0).normalized,    // Diagonal (XY)
        new Vector3(-1, 1, 0).normalized,   // Diagonal (-XY)
        new Vector3(1, -1, 0).normalized,   // Diagonal (X-Y)
        new Vector3(-1, -1, 0).normalized,  // Diagonal (-X-Y)
        new Vector3(0, 1, 1).normalized,    // Diagonal (YZ)
        new Vector3(0, 1, -1).normalized    // Diagonal (Y-Z)
    };


    private void Start()
    {
        //spellType = SpellDataScriptableObject.element.ToString();
        // Add logic to disable rb depending on which spell is used

        // Generates the local GUID value
        localSpellId = Guid.NewGuid().ToString();

        //InitializePrevPosRpc();
        // Initialize the previous position
        previousPosition = transform.position;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = false;
        rb.isKinematic = false;

        spellHasSpawned = true;
        //Fire();

        directDamageAmount.Value = SpellDataScriptableObject.directDamageAmount;

        StartCoroutine(LifeTime(SpellDataScriptableObject.spellDuration, gameObject));

    }


    // gamemangaer >> collects all local to network objects >> handles destroying each

    public override void OnNetworkDespawn()
    {
        //if (!IsOwner) return;    
        // This destroys the local instance of the projectile
        //gameObject.transform.parent.GetComponent<K_SpellLauncher>().HandleDestroyProjectile(localSpellId);

        base.OnNetworkDespawn();


    }

    //[Rpc(SendTo.Server)]
    //void InitializePrevPosRpc()
    //{
    //    previousPosition.Value = transform.position;
    //}


    //public void Update()
    //{
    //    // Loop through the 12 directions using a standard for loop
    //    for (int i = 0; i < directions.Length; i++)
    //    {
    //        Vector3 direction = directions[i];

    //        // Perform the raycast
    //        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, rayDistance))
    //        {
    //            //Debug.Log($"Hit {hit.collider.name} at {hit.point}, Distance: {hit.distance} units in direction {direction}");
    //            Debug.Log($"1111111111111111Hit {hit.collider.name} at {hit.point}, Distance: {hit.distance} units in direction {direction}");

    //            // Check if the distance is less than 0.3 and output the log if true
    //            if (hit.distance <= 5f)
    //            {
    //                Debug.Log($"222222222222222Hit {hit.collider.name} at {hit.point}, Distance: {hit.distance} units in direction {direction}");

    //                // This checks if the player has been hit and if the OnTriggerEnter has failed to detect the collision
    //                if (hit.collider.name.Contains("Player") && triggerEntered == false)
    //                {

    //                    Debug.Log($"333333333333333Hit {hit.collider.name} at {hit.point}, Distance: {hit.distance} units in direction {direction}");


    //                    // This is so that the player cannot hurt himself with his own spell
    //                    if (this.OwnerClientId == hit.transform.gameObject.GetComponent<NetworkBehaviour>().OwnerClientId) return;


    //                    // hit.transform.gameObject.GetComponent<NewPlayerBehavior>().DirectDamage(SpellDataScriptableObject.directDamageAmount);

    //                    //other.gameObject.GetComponent<K_ProjectileSpell>().DestroySpellRpc();

    //                    // If a collision with the player has been made, apply damage

    //                    // Assign the values to the payload to be sent with the event emission upon hitting the player
    //                    SpellPayloadConstructor
    //                    (
    //                        this.gameObject.GetInstanceID(),
    //                        hit.transform.gameObject.GetComponent<NetworkObject>().OwnerClientId,
    //                        SpellDataScriptableObject.element.ToString(),
    //                        SpellDataScriptableObject.incapacitationName,
    //                        SpellDataScriptableObject.incapacitationDuration,
    //                        SpellDataScriptableObject.visionImpairmentType,
    //                        SpellDataScriptableObject.visionImpairmentDuration,
    //                        SpellDataScriptableObject.directDamageAmount,
    //                        SpellDataScriptableObject.damageOverTimeAmount,
    //                        SpellDataScriptableObject.damageOverTimeDuration,
    //                        SpellDataScriptableObject.spellAttribute,
    //                        SpellDataScriptableObject.pushback
    //                    );


    //                    PlayerIsHit(); // This emits an event that applies damage to the target on the behavior and the GM script  >> NEED TO PASS ALL RELEVANT DATA HERE
    //                    HasHitPlayer = true;

    //                    //DestroySpellRpc();

    //                }
    //            }
    //            // Handle hit logic here
    //        }

    //        // Visualize the ray in the Scene view
    //        Debug.DrawRay(transform.position, direction * rayDistance, Color.red);
    //    }
    //}

    // !!!IMPORTANT!!! Side-note: When testing locally, and for some weird, if the server did not look around, the projectiles on the cast client side
    //do not fly correctly
    public void FixedUpdate()
    {
        if (IsSpawned)
        {

            if (!IsOwner) return;

            Debug.Log($"projectile update");


            if (currentPosition != transform.position)
            {
                Debug.Log($"222 projectile update 222");

                currentPosition = transform.position;

                // Raycast from previous position to current position
                Ray ray = new Ray(previousPosition, currentPosition - previousPosition);
                float distance = Vector3.Distance(previousPosition, currentPosition);

                if (Physics.Raycast(ray, out RaycastHit hitInfo, distance))
                {
                    // Log the hit info
                    //Debug.Log($"Raycast hit: {hitInfo.collider.name} at {hitInfo.point}");

                    if (hitInfo.collider.name.Contains("Player"))
                    {

                        Debug.Log($"oooooooooooooooProjectile owner {OwnerClientId}");
                        Debug.Log($"333333333333333Hit {hitInfo.collider.name} owner {hitInfo.collider.gameObject.GetComponent<NewPlayerBehavior>().OwnerClientId}, Damage: {SpellDataScriptableObject.directDamageAmount} units in direction ");


                        // This is so that the player cannot hurt himself with his own spell
                        if (this.OwnerClientId == hitInfo.transform.gameObject.GetComponent<NetworkBehaviour>().OwnerClientId) return;

                        hitInfo.collider.gameObject.GetComponent<NewPlayerBehavior>().DirectDamage(SpellDataScriptableObject.directDamageAmount);

                        // hit.transform.gameObject.GetComponent<NewPlayerBehavior>().DirectDamage(SpellDataScriptableObject.directDamageAmount);

                        //other.gameObject.GetComponent<K_ProjectileSpell>().DestroySpellRpc();

                        // If a collision with the player has been made, apply damage

                        // Assign the values to the payload to be sent with the event emission upon hitting the player
                        //SpellPayloadConstructor
                        //(
                        //    this.gameObject.GetInstanceID(),
                        //    hitInfo.transform.gameObject.GetComponent<NetworkObject>().OwnerClientId,
                        //    SpellDataScriptableObject.element.ToString(),
                        //    SpellDataScriptableObject.incapacitationName,
                        //    SpellDataScriptableObject.incapacitationDuration,
                        //    SpellDataScriptableObject.visionImpairmentType,
                        //    SpellDataScriptableObject.visionImpairmentDuration,
                        //    SpellDataScriptableObject.directDamageAmount,
                        //    SpellDataScriptableObject.damageOverTimeAmount,
                        //    SpellDataScriptableObject.damageOverTimeDuration,
                        //    SpellDataScriptableObject.spellAttribute,
                        //    SpellDataScriptableObject.pushback
                        //);


                        //PlayerIsHit(); // This emits an event that applies damage to the target on the behavior and the GM script  >> NEED TO PASS ALL RELEVANT DATA HERE
                        //HasHitPlayer = true;

                        //DestroySpellRpc();

                    }
                }

                // Visualize the ray in the editor
                Debug.DrawLine(previousPosition, currentPosition, Color.blue, 1f);

                // Update the previous position
                //previousPosition.Value = currentPosition.Value;
            }





            if (spellHasSpawned)
            {
                // Direction here has to match the direction that the wand tip gameobject is looking in
                //transform.Translate(Vector3.forward * Time.deltaTime * SpellDataScriptableObject.moveSpeed);
                Debug.Log($"333 projectile update 333");
                // Calculate the new position
                Vector3 moveDirection = transform.forward * SpellDataScriptableObject.moveSpeed * Time.deltaTime;
                //rb.MovePosition(rb.position + moveDirection);
                //rb.AddForce(rb.position + moveDirection);
                transform.Translate(Vector3.forward * Time.deltaTime * SpellDataScriptableObject.moveSpeed);
            }

            // Destroy the GO after it applies force to player
            if (CanDestroy)
            {
                StartCoroutine(DelayedDestruction());

            }
        }

        else
        {
            Vector3 moveDirection = transform.forward * SpellDataScriptableObject.moveSpeed * Time.deltaTime;
            //rb.MovePosition(rb.position + moveDirection);
            //rb.AddForce(rb.position + moveDirection);
            transform.Translate(Vector3.forward * Time.deltaTime * SpellDataScriptableObject.moveSpeed);
        }
    }


    public override void Fire()
    {
        //AddForceToProjectileRpc();
    }

    IEnumerator DelayedDestruction()
    {
        yield return new WaitForSeconds(0.3f); // The value here seems to be working for now. Might need to revise it later.
        DestroySpellRpc();
    }

    //[Rpc(SendTo.Server)]
    //void AddForceToProjectileRpc()
    //{
    //    //transform.SetPositionAndRotation(caster.position + caster.forward + new Vector3(0f, 1f, 0f), caster.rotation);

    //    transform.Translate(Vector3.forward);
    //}

    //[Rpc(SendTo.Server)]
    //public void DestroyProjectileRpc()
    //{
    //    //despawn
    //    //destroy
    //    Destroy(gameObject);
    //    gameObject.GetComponent<NetworkObject>().Despawn();
    //}


    //public void SpellTriggerEvent(Collider other)
    //{
    //    SpellPayload(other);
    //    DestroyProjectileRpc();
    //}

    public PlayerHitPayload FetchSpellPayload(Collider other)
    {
        return Payload(other);
    }


    public IEnumerator DestroySelf()
    {
        //this.gameObject.SetActive(false);
        yield return new WaitForSeconds(1);
        DestroySpellRpc();
    }


    public override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        Debug.LogFormat("<color=orange> >>> PROJECTILE HIT >>> (" + other.gameObject.name + ")</color>");

        if (other.gameObject.name.Contains("Player"))
        {
            triggerEntered = true;
            //other.gameObject.GetComponent<NewPlayerBehavior>().DirectDamage(SpellDataScriptableObject.directDamageAmount);
        }

        if (other.gameObject.layer == 7)
        {
            if (IsSpawned)
            {
                DestroySpellRpc();
            }
        }

        //// The below code can nbe simplified, making .
        ////
        //if (other.gameObject.CompareTag("Spell"))
        //{
        //    if (other.gameObject.name.Contains("Barrier"))
        //    {
        //        BarrierSpell barrierScript = other.gameObject.GetComponentInParent<BarrierSpell>();

        //        if (barrierScript.SpellDataScriptableObject.health > 1) // 1 is minimum ie. undamageable
        //        {
        //            other.gameObject.GetComponent<BarrierSpell>().ApplyDamage(SpellDataScriptableObject.directDamageAmount);
        //            //DestroySpellRpc();
        //        }

        //        Debug.LogFormat("<color=orange> 2222222 >>> PROJECTILE HIT >>> (" + other.gameObject.name + ")</color>");
        //        DestroySpellRpc();
        //    }

        //    if (other.gameObject.name.Contains("Scepter"))
        //    {
        //        InvocationSpell invocationSpell = other.gameObject.GetComponentInParent<InvocationSpell>();

        //        if (invocationSpell.SpellDataScriptableObject.health > 1)
        //        {
        //            invocationSpell.ApplyDamage(SpellDataScriptableObject.directDamageAmount);
        //        }

        //        Debug.LogFormat("<color=orange> SSSSSSSS >>> PROJECTILE HIT >>> (" + other.gameObject.name + ")</color>");
        //        DestroySpellRpc();

        //    }
        //}


        // Pushback(other.gameObject.GetComponent<Rigidbody>());
        //if (other.gameObject.name != "InvocationBounds")
        //{
        //    Debug.Log("otherotherotherother (" + other + ")");

        //    DestroySpellRpc();
        //    //StartCoroutine(DestroySelf());
        //}
    }
}
