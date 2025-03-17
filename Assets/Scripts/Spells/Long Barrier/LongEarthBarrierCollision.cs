using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// I made a separate script for this but potentially could use the same one as the regular earth barrier
public class LongEarthBarrierCollision : MonoBehaviour
{
    [SerializeField] EarthBarrier longEarthBarrierScript;

    Collider earthBarrierCollider;

    private void Awake()
    {
        earthBarrierCollider = this.gameObject.GetComponentInChildren<Collider>();

        earthBarrierCollider.enabled = false;
    }

    public void Start()
    {
        longEarthBarrierScript.GetComponentInParent<EarthBarrier>();

        //Debug.LogFormat($"<color=red>Interaction: {this.gameObject.GetComponent<Collider>()}</color>");

    }

    public void ActivateEffect()
    {

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
                longEarthBarrierScript.earthBarrierHealth.Value -= boltDamage;
                // other.gameObject.SetActive(false);
                other.gameObject.GetComponent<EarthBolt>().DestroyBolt();

                longEarthBarrierScript.IsBarrierDestroyed();

                // Got to figure out how to handle interaction of sphere shield
                // with this arcane shield

                return;
        }



    }
}
