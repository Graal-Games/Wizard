using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class K_SphereSpell : K_Spell
{

    public delegate void ShieldLifeStatus(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool status);
    public static event ShieldLifeStatus shieldExists;

    bool isAlive = true;

    float armorPoints;

    public override void OnNetworkSpawn()
    {        
        //if (!IsOwner) return;

        base.OnNetworkSpawn();

        // Debug.LogFormat($"<color=orange>{this.gameObject.transform.parent}</color>");

        //RigidbodyCP.constraints = RigidbodyConstraints.FreezeAll;
        //RigidbodyCP.useGravity = false;
        //RigidbodyCP.isKinematic = false;
        armorPoints = SpellDataScriptableObject.health;
        //Debug.LogFormat($"<color=orange>armorPoints: {armorPoints}</color>");

        ShieldStatus(isAlive);

        //Debug.LogFormat($"<color=orange>Sphere parent OwnerClientId: {OwnerClientId}</color>");
        //gameObject.GetComponent<NetworkObject>().enabled = false;
    }

    public override void OnNetworkDespawn()
    {        
        //if (!IsOwner) return;    
        
        base.OnNetworkDespawn();

        ShieldStatus(!isAlive);

    }

    void ShieldStatus(bool status)
    {
        if (shieldExists != null) shieldExists(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), status);
    }


    public void DestroyShield()
    {
        DestroyShieldRpc();
    }


    [Rpc(SendTo.Server)]
    void DestroyShieldRpc()
    {
        Destroy(gameObject);
        gameObject.GetComponent<NetworkObject>().Despawn();
    }

    //public override void Update()
    //{
    //    base.Update();

    //    //transform.localPosition = gameObject.transform.parent.transform.localPosition + new Vector3(0f, 1f, 0f);
    //    //transform.rotation = gameObject.transform.parent.transform.rotation;        
        
    //    //transform.localPosition = gameObject.transform.parent.transform.position + new Vector3(0f, 1f, 0f);
    //    //transform.rotation = gameObject.transform.parent.transform.rotation;
    //}

    public void TakeDamage(float damage)
    {
        armorPoints -= damage;
        Debug.LogFormat($"<color=orange>armorPoints: {armorPoints}</color>");
        
        if (armorPoints <= 0 ) 
        {
            DestroyShieldRpc();
        }
        
    }



    public override void Fire()
    {
        //StartCoroutine(LerpScale(new Vector3(1.8f, 1.8f, 1.8f), 1f / spellData.moveSpeed));
    }

    /// <summary>
    /// Modifies the sphere scale over time.
    /// </summary>
    /// <param name="targetScale">The desired final scale.</param>
    /// <param name="duration">The time in seconds that it takes to reach the target scale.</param>
    //private IEnumerator LerpScale(Vector3 targetScale, float duration)
    //{
    //    float time = 0f;
    //    Vector3 startScale = transform.localScale;

    //    while (time < duration)
    //    {
    //        transform.localScale = Vector3.Lerp(startScale, targetScale, time / duration);
    //        time += Time.deltaTime;

    //        yield return null;
    //    }

    //    transform.localScale = targetScale;
    //}

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (!other.CompareTag("Spell")) return;
    //    //if (!IsOwner) return;
        
    //    //{

    //    //    Debug.LogFormat($"<color=orange>{other.gameObject}</color>");
    //    //}
    //}
}
