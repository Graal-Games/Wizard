using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class ArcaneBarrierCollision : NetworkBehaviour
{
    [SerializeField] ArcaneBarrier arcaneBarrierScript;

    public void Start()
    {
        //Debug.LogFormat($"<color=red>Interaction: {this.gameObject.GetComponent<Collider>()}</color>");
        arcaneBarrierScript = gameObject.GetComponentInParent<ArcaneBarrier>();

    }

    //public override void OnNetworkSpawn()
    //{
    //    base.OnNetworkSpawn();

    //}


    private void OnTriggerEnter(Collider other)
    {
        // Debug.LogFormat($"<color=red>Interaction: {other}</color>");
        if (!other.gameObject.CompareTag("Spell")) return;

        if (IsOwner)
        {
            if (other.gameObject.GetComponent<Bolt>())
            {
                float boltDamage = other.gameObject.GetComponent<Bolt>().damage;
                arcaneBarrierScript.arcaneBarrierHealth.Value -= boltDamage;
                // other.gameObject.SetActive(false);
                other.gameObject.GetComponent<Bolt>().DestroyBolt();

                arcaneBarrierScript.IsBarrierDestroyed();

                // Got to figure out how to handle interaction of sphere shield
                // with this arcane shield
            }
        }
    }


}
