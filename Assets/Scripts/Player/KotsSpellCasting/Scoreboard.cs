using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Scoreboard : NetworkBehaviour
{
    [SerializeField] NewPlayerBehavior newPlayerBehavior;
    [SerializeField] TMP_Text P1ScoreText;
    [SerializeField] TMP_Text P2ScoreText;

    public NetworkVariable<int> p1Score = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> p2Score = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    private void Start()
    {
        // When a player connects check if to subscribe to death count network variables.
        NetworkManager.Singleton.OnClientConnectedCallback += ProcessPlayerConnection;

        // Set default value to 0 on the Ui
        P1ScoreText.text = 0.ToString();
        P2ScoreText.text = 0.ToString();
    }

    // ONLY when both players are connected subscribe to the death count network variables
    //on each player, which will thereafter be used to update the score for each.
    void ProcessPlayerConnection(ulong player)
    {
        if (!IsServer) return;

        if (player == 1)
        {
            NetworkManager.Singleton.ConnectedClients[1].PlayerObject.GetComponent<NewPlayerBehavior>().deathCount.OnValueChanged += UpdateP1Score;
            NetworkManager.Singleton.ConnectedClients[0].PlayerObject.GetComponent<NewPlayerBehavior>().deathCount.OnValueChanged += UpdateP2Score;
        }

    }

    // The score is updated on both the server and the client.
    private void UpdateP2Score(int previous, int current)
    {
        UpdateP1OnServerRpc(current);
        UpdateP1OnClientRpc(current);
    }
    private void UpdateP1Score(int previous, int current)
    {
        UpdateP2OnServerRpc(current);
        UpdateP2OnClientRpc(current);
    }

    [Rpc(SendTo.NotServer)]
    void UpdateP1OnClientRpc(int score)
    {
        P1ScoreText.text = score.ToString();
    }

    [Rpc(SendTo.NotServer)]
    void UpdateP2OnClientRpc(int score)
    {
        P2ScoreText.text = score.ToString();
    }

    [Rpc(SendTo.Server)]
    void UpdateP1OnServerRpc(int score)
    {
        P1ScoreText.text = score.ToString();
    }

    [Rpc(SendTo.Server)]
    void UpdateP2OnServerRpc(int score)
    {
        P2ScoreText.text = score.ToString();
    }
}
