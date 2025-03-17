using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class parry : NetworkBehaviour
{
    // This script is placed on the spell object itself
    public LOSletterGen associatedLetterScript;
    public string letter = null;
    // Start is called before the first frame update
    public GameObject inputUI;
    public GameObject letterOnUI;
    public GameObject deActivateUiLetter;
    public bool isParriable = false;

    void Awake()
    {
        //inputUI = GameObject.FindGameObjectWithTag("InputUI");
        
    }
    void Start()
    {
        // Get the script that has contains the letter associated with the spell object to be destroyed
         if (associatedLetterScript == null)
        {
            associatedLetterScript = transform.GetComponentInParent<LOSletterGen>();
        }

        // Save the letter to a variable to be used to destroy the spell object
        letter = associatedLetterScript.associatedLetter;

        // Destroy the object after some time if it does not hit the player (or any other object afterwards --to code--)
        //StartCoroutine(DestroyObjectAfterLimit());
        //letterOnUI = GameObject.FindGameObjectWithTag(letter);
    }

    void DeactivateLetter()
    {
        inputUI = GameObject.FindGameObjectWithTag("InputUI");
        deActivateUiLetter = inputUI.transform.Find(letter.ToUpper()).gameObject;
        deActivateUiLetter.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
        //print(gameObject + "'s parent script is: " + associatedLetterScript);

        // The spell is only parriable if the player is within the effective range
        //of the spell, which is determined by a box collider

        // >> REACTIVATE CODE BELOW FOR PARRY SYSTEM
        // if (isParriable == true)
        // {
        //     if (Input.GetKey(letter.ToLower()))
        //     {
        //         //print("THE GO" + transform.parent.parent.gameObject);

        //         // parent.parent is the bolt object
        //         DestroyBoltServerRpc();

        //         // If the player successfuly parries the spell, deactivate it from the Ui
        //         DeactivateLetter();
        //     }
        // }
        
        //letter = associatedLetterScript.associatedLetter;
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyBoltServerRpc()
    {
        NetworkObject.Despawn(transform.parent.parent.gameObject);
        return;
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (other.gameObject.CompareTag("Player"))
        {
            // By setting parriable to true, the player is able to parry
            //otherwise he cannot
            isParriable = true;
            //print("Is parriable");
           
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            //print("Letter is: " + letter);

            // If the player successfuly parries the spell, deactivate it from the Ui
            // DeactivateLetter();
            isParriable = false;
        }
    }

    
}
