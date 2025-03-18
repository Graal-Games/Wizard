using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class enemySpellcast : MonoBehaviour
{
    public GameObject bolt;
    //public GameObject spellsManager;

    // Start is called before the first frame update
    void Start()
    {
        //spellsManager = GameObject.FindGameObjectWithTag("SpellsManager");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    

    public void CastBolt()
    {
        // Vector3 position = new Vector3(0, 0, 0);
        // Quaternion rotation = Quaternion.identity;

        GameObject boltInstance = Instantiate(bolt, transform.position, transform.rotation);
        boltInstance.GetComponent<NetworkObject>().Spawn();
    }
}
