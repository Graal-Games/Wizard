using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

//TO BE RENAMED TO ARCANE BOLT
public class Bolt : NetworkBehaviour
{
    private float speed = 80f;
    public float damage = 4f;
    bool hitShield = false;
    LOSletterGen losLetterGen;

    //float damage;
    public bool hasHitShield = false;
    bool isColliding = false;


    void Awake()
    {
       //damage = 16;
    }
    
    void Start()
    {
        StartCoroutine(DestroyObjectAfterLimit());
        losLetterGen = GetComponentInChildren<LOSletterGen>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * Time.deltaTime * speed);
    }

    public bool HasHitShield(bool collider = false)
    {
        hitShield = collider;
        //DestroyBolt();
        return hitShield;
    }

    private void OnTriggerEnter(Collider other)
    {   
        Debug.Log("hitShield: " + hitShield);

        if (other.gameObject.name.Contains("SphereShield"))
        {
            //isColliding = true;
            Debug.Log("^^^^^^^^^ sphere hit ^^^^^^^^^");
            //HasHitShield(true);
            hasHitShield = true;
            //DestroyBolt();
        } 

        

        // if (isColliding == true)
        // {
        //     //this.gameObject.activeSelf
        // }

        if (other.gameObject.name.Contains("Player"))
        {
            // Check what this does
            if(isColliding) return;
            isColliding = true;

            Debug.LogFormat("<color=yellow>^^^^^^^^^ sphere hit ^^^^^^^^^</color>");

            //DestroyBolt();
        }
        else if (other.gameObject.name.Contains("SphereShield"))
        {
            Debug.Log("Shpere shield destroyed bolt");
            //DestroyBolt();
        }
           //DestroyBolt();
    }

    public void DestroyBolt()
    {
        
        if (this.GetComponent<NetworkObject>().IsSpawned)
        {
            DestroyBoltServerRpc();

            // if (IsServer)
            //     {   
            //         // Projectile hits Host, client calls serverRpc?
            //         DestroyBoltServerRpc();
            //         Debug.Log("IS SERVER");
                    
            //     } else {
            //         // This is what activates the letter on the CLIENT
            //         //ActivateLetterServerRpc(theLetter, true);
            //         //LetterActive(theLetter, true);
            //         Debug.Log("NOT SERVER");
            //     }
            
            return;
        }
        else
        {
            //Debug.Log("Not spawned");
            return;
        }
        
    }

    private IEnumerator DestroyObjectAfterLimit() 
    {
        // Destroy the object after some time if it does not hit the player 
        //(or any other object afterwards --to code--)
        yield return new WaitForSeconds(7);
        DestroyBoltServerRpc();
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyBoltServerRpc()
    {
        NetworkObject.Despawn(this.gameObject);
        return;
    }


    // [ClientRpc]
    // private void DestroyBoltClientRpc()
    // {
    //     NetworkObject.Despawn(this.gameObject);
    //     return;
    // }
}
