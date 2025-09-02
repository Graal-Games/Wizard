using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// This script should be on a GameObject in your scene that also has a NetworkObject component.
public class GameManager2 : NetworkBehaviour
{
    // This dictionary will exist ONLY ON THE SERVER.
    // It will store information about each connected player.
    private Dictionary<ulong, PlayerClass> playersInfo = new Dictionary<ulong, PlayerClass>();

    public override void OnNetworkSpawn()
    {
        // This script should only run on the server.
        if (!IsServer)
        {
            this.enabled = false;
            return;
        }

        // Subscribe to the reliable NetworkManager callbacks.
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    // This method is called on the SERVER whenever a new client connects.
    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"SERVER: New player connected with ClientId: {clientId}");

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            // Get components from the newly spawned player object
            var playerHealth = client.PlayerObject.GetComponent<PlayerHealth>();
            var playerBehavior = client.PlayerObject.GetComponent<NewPlayerBehavior>();

            if (playerHealth != null && playerBehavior != null)
            {
                // Now you can create your data container using the 'new' keyword
                // and pass in the relevant data.
                PlayerClass newPlayerInfo = new PlayerClass(clientId, $"Player {clientId}");

                // Add the data object to your server-side dictionary
                playersInfo.Add(clientId, newPlayerInfo);

                Debug.Log($"SERVER: Registered new player {clientId} in the game manager.");
            }
        }
    }

    // This method is called on the SERVER whenever a client disconnects.
    private void HandleClientDisconnect(ulong clientId)
    {
        Debug.Log($"SERVER: Player disconnected with ClientId: {clientId}");

        if (playersInfo.ContainsKey(clientId))
        {
            playersInfo.Remove(clientId);
        }
    }

    public override void OnNetworkDespawn()
    {
        // It's good practice to unsubscribe from events when this object is destroyed.
        if (IsServer)
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }
        }
    }
}