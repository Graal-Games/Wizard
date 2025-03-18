using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Singletons;

public class LOSletterGen : NetworkBehaviour
{

    private string[] availableLetters = {"R", "Y", "V", "N"};
    public string associatedLetter = null;
    //public spellsManager sM;
    //private bool hasEntered = true;
    private NetworkVariable<NetworkString> activeLetterGameObject = new NetworkVariable<NetworkString>();
    public GameObject inputUI;

    //public GameObject floatingLetter;

    // !! activate the commented lines below to reactivate Parry's letter generation
    void Awake()
    {
        // if (gameObject.name != "Enemy")
        // {
        //     Letter();
        // }
        
    }

    void Update()
    {
        //TextMeshPro floatingLetter = transform.GetComponentInChildren<TextMeshPro>();

        // //Modify the text of the TextMeshPro component // Need to send this over the network
        //floatingLetter.text = associatedLetter;
    }

    private string Letter()
    {
        
        var randomNumber = Random.Range(0, 4);
        associatedLetter = availableLetters[randomNumber];
        //print("Letter: " + associatedLetter);

        return associatedLetter;
    }
}
