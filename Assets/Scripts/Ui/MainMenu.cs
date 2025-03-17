using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using Unity.Netcode.Transports.UTP;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button exitGame;
    [SerializeField] private Button joinGame;
    Matchmaking matchmaking;
    

    //[SerializeField] private TextMeshPro PlayersInGameText;
    
    void Awake()
    {
        matchmaking = GetComponent<Matchmaking>();

        Debug.Log(matchmaking);

        joinGame.onClick.AddListener(() => {
            if (matchmaking != null)
            {
                matchmaking.CreateOrJoinLobby();
            } else {
                return;
            }
        });

        exitGame.onClick.AddListener(() => {
            if (matchmaking != null)
            {
                Application.Quit();
            } else {
                return;
            }
        });
    }


    // void Awake()
    // {
    //     // hostServer.onClick.AddListener(() => {
    //     //     NetworkManager.Singleton.StartHost();
    //     //     //this.gameObject.SetActive(false);
    //     // });

    //     // joinGame.onClick.AddListener(() => {
    //     //     //NetworkManager.Singleton.StartClient();
    //     //     //this.gameObject.SetActive(false);
    //     // });
    // }

    

    
}
