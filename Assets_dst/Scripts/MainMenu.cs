using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button joinServer;
    [SerializeField] private Button hostServer;

    private bool gameJoinedOrHosted;

    // Start is called before the first frame update
    void Awake()
    {
        hostServer.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            this.gameObject.SetActive(false);
        });

        joinServer.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            this.gameObject.SetActive(false);
        });

    }

    // Update is called once per frame
    void Update()
    {
       
    }
}