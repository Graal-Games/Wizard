using Unity.Netcode;
using Singletons;
using UnityEngine;
//Import all players

public class PlayersManager : NetworkSingleton<PlayersManager>
{
    private NetworkVariable<int> playersInGame = new NetworkVariable<int>();

    //[SerializeField]
    //GameObject playerPrefab;

    //// Future implementation: Any player or player character specific information that is linked to the player steam id (or similar) is to also be sent through this event
    //public delegate void PlayerHasSpawnedEventPayload(ulong clientId, NetworkObjectReference playerObjRef, NetworkBehaviour playerBehaviorNetScript, NetworkObject netObj);
    //public static event PlayerHasSpawnedEventPayload playerHasSpawnedEvent; 

    public int PlayersInGame
    {
        get
        {
            return playersInGame.Value;
        }
    }


    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            //playerPrefab.GetComponent<NetworkObject>().SpawnWithOwnership(id);

            if (IsServer)
            {

                print($"{id} has connected");
                playersInGame.Value++;
                print("players in game: " + playersInGame.Value);
            }
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            if (IsServer)
            {
                print($"{id} has disconnected");
                playersInGame.Value--;
            }
        };
    }

    private void Update()
    {
        // Option 1
        // Check for players whose health has reached 0, if it did Despawn them for now.
    }

    // Option 2: If a player's health reaches 0. Call this despawn method
    void DespawnPlayer()
    {
        // Add code here
    }

    public override void OnDestroy()
    {
        
    }
}
