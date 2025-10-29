using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class FireBarrierCollision : NetworkBehaviour
{
    [SerializeField] FireBarrier fireBarrierScript;

    public void Start()
    {
        fireBarrierScript.GetComponentInParent<FireBarrier>();

        //Debug.LogFormat($"<color=red>Interaction: {this.gameObject.GetComponent<Collider>()}</color>");

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
                
                float boltDamage = other.gameObject.GetComponent<EarthBolt>().damage;
                fireBarrierScript.fireBarrierHealth.Value -= boltDamage;

                other.gameObject.GetComponent<WaterBolt>().DestroyBolt();

                fireBarrierScript.IsBarrierHealthNaught();

                // Got to figure out how to handle interaction of sphere shield
                // with this arcane shield
                return;
            case "Fire":
                other.gameObject.GetComponent<FireBolt>().DestroyBolt();
                return;
            case "Air":
                other.gameObject.GetComponent<AirBolt>().DestroyBolt();
                return;
            case "Earth":
                other.gameObject.GetComponent<EarthBolt>().DestroyBolt();


                return;
        }



    }
}
