using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class K_SphereSpell : K_Spell
{

    public delegate void ShieldLifeStatus(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool status);
    public static event ShieldLifeStatus shieldExists;



    bool isAlive = true;

    NetworkVariable <float> armorPoints = new NetworkVariable<float>(0);

    public override void OnNetworkSpawn()
    {        
        //if (!IsOwner) return;

        base.OnNetworkSpawn();

        Debug.LogFormat($"<color=orange>SPHERE PARENT: {this.gameObject.transform.parent} ID: {OwnerClientId} </color>");
        Debug.LogFormat($"<color=orange>SPHERE HEALTH: {SpellDataScriptableObject.health} </color>");


        armorPoints.Value = SpellDataScriptableObject.health;

        ShieldStatus(isAlive);


        StartCoroutine(LifeTime(SpellDataScriptableObject.spellDuration, gameObject));

        // Only set position if parent exists
        if (gameObject.transform.parent != null)
            gameObject.GetComponent<NetworkObject>().transform.position = gameObject.transform.parent.transform.position;
        else
            Debug.LogWarning("K_SphereSpell.OnNetworkSpawn: Parent is null, position not set.");
    }

    public override void OnNetworkDespawn()
    {        
        //if (!IsOwner) return;    
        
        base.OnNetworkDespawn();

        ShieldStatus(!isAlive);

        // This destroys the local instance of the shield
        gameObject.transform.parent.GetComponent<K_SpellLauncher>().DestroyLocalShield();
    }


    // Add comment!
    void ShieldStatus(bool status)
    {
        Debug.LogFormat($"<color=orange>statusstatusstatusstatus: {status}</color>");

        if (shieldExists != null) shieldExists(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), status);
    }

    public void AssignParent(Transform gO)
    {
        gameObject.GetComponent<NetworkObject>().TrySetParent(gO);
    }


    public void TakeDamage(float damage)
    {
        armorPoints.Value -= damage;
        Debug.LogFormat($"<color=orange>armorPoints: {armorPoints.Value}</color>");

        //TakeDamageRpc(damage);

        CheckStatus();
    }

    [Rpc(SendTo.Server)]
    void TakeDamageRpc(float damage)
    {
        armorPoints.Value -= damage;
        CheckStatus();
    }

    void CheckStatus()
    {
        if (armorPoints.Value <= 0)
        {
            Debug.LogFormat($"<color=orange>2!!!!armorPoints: {armorPoints.Value}</color>");
            DestroySpell(gameObject);
        }
    }

    public override void Fire()
    {
        //StartCoroutine(LerpScale(new Vector3(1.8f, 1.8f, 1.8f), 1f / spellData.moveSpeed));
    }
}
