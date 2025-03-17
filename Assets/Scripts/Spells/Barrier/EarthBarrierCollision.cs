using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class EarthBarrierCollision : NetworkBehaviour
{
    [SerializeField] EarthBarrier earthBarrierScript;

    Collider earthBarrierCollider;

    private void Awake()
    {
        //earthBarrierCollider = this.gameObject.GetComponentInChildren<Collider>();

        //earthBarrierCollider.enabled = false;

        Debug.LogFormat($"<color=red>Interaction: {earthBarrierCollider.enabled}</color>");
    }

    public void Start()
    {
        earthBarrierScript.GetComponentInParent<EarthBarrier>();

        //Debug.LogFormat($"<color=red>Interaction: {this.gameObject.GetComponent<Collider>()}</color>");

    }

    public void ActivateEffect()
    {
        Debug.Log("STUN AND DAMAGE");
        earthBarrierCollider.enabled = true;

        Debug.LogFormat($"<color=red>Interaction: {earthBarrierCollider.enabled}</color>");
    }


    private void OnTriggerEnter(Collider other)
    {
        Debug.LogFormat($"<color=red>Interaction: {other}</color>");
        if (!other.gameObject.CompareTag("Spell")) return;

        string boltType = other.gameObject.name.Replace(" Bolt(Clone)", "");

        Debug.LogFormat($"<color=red>boltType:{boltType}:</color>");

        switch (boltType)
        {
            case "Arcane":
                other.gameObject.GetComponent<Bolt>().DestroyBolt();
                return;
            case "Water":
                other.gameObject.GetComponent<WaterBolt>().DestroyBolt();
                return;
            case "Fire":
                other.gameObject.GetComponent<FireBolt>().DestroyBolt();
                return;
            case "Air":
                other.gameObject.GetComponent<AirBolt>().DestroyBolt();
                return;
            case "Earth":
                
                // Debug.LogFormat($"<color=red>NOT ARCANE: {other}</color>");

                float boltDamage = other.gameObject.GetComponent<EarthBolt>().damage;
                earthBarrierScript.earthBarrierHealth.Value -= boltDamage;
                // other.gameObject.SetActive(false);
                other.gameObject.GetComponent<EarthBolt>().DestroyBolt();

                earthBarrierScript.IsBarrierDestroyed();

                // Got to figure out how to handle interaction of sphere shield
                // with this arcane shield
                
                return;
        }

        
        
    }
}
