using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [SerializeField] private Button hostServer;
    [SerializeField] private Button joinServer;
    [SerializeField] private TextMeshProUGUI playersInGameText;
      
     void Awake()
    {
        hostServer.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            // this.gameObject.SetActive(false);
        });

        joinServer.onClick.AddListener(() => {
            // This should become: GameLobby.Instance.StartGame()
            GameLobby.Instance.StartGame();
            //NetworkManager.Singleton.StartClient();
            // this.gameObject.SetActive(false);
        });
    }

    private void Update()
    {
        playersInGameText.text = $"Players in game: {PlayersManager.Instance.PlayersInGame}";
    } 

}
