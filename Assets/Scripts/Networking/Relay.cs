using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class Relay : MonoBehaviour
{
    public static Relay Instance { get; private set; }

    // Events for the UI to subscribe to
    public event Action<string> OnRelayCreated;
    public event Action OnJoinSuccess;
    public event Action<string> OnJoinFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private async void Start()
    {
        // Initialize and sign in if not already done
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in: " + AuthenticationService.Instance.PlayerId);
        }

        // Tell the NetworkManager that our ApprovalCheck method is the bouncer's "guest list".
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        }
    }

    public async Task CreateRelay()
    {
        // Don't allow creating a relay if we are already connected
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1); // Max 1 other player
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log("Relay Join Code: " + joinCode);
            OnRelayCreated?.Invoke(joinCode); // Fire event for UI

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async Task JoinRelay(string joinCode)
    {
        // Don't allow joining if we are already connected
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

        try
        {
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
            OnJoinSuccess?.Invoke(); // Fire event for UI
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            OnJoinFailed?.Invoke(e.Message); // Fire event for UI
        }
    }

    // This is the "guest list" method that runs on the SERVER for every connecting client.
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // This is where you could add logic to check a password, player limit, etc.
        // For now, we will simply approve everyone.

        response.Approved = true;       // Let the player in
        response.CreatePlayerObject = true; // Create a player object for them

        // You can leave PlayerPrefabHash null to use the default prefab assigned in the NetworkManager's Inspector
        response.PlayerPrefabHash = null;

        Debug.Log($"Approving connection for client: {request.ClientNetworkId}");

        // Get the spawn position and rotation from your SpawnManager
        // We use request.ClientNetworkId to get the ID of the player who is trying to connect.
        Vector3 spawnPosition = GameEvents.RequestSpawnPoint(request.ClientNetworkId);
        Quaternion spawnRotation = GameEvents.RequestSpawnRotation(request.ClientNetworkId);

        // Tell the NetworkManager to spawn the player at this specific location
        response.Position = spawnPosition;
        response.Rotation = spawnRotation;
    }

    // It's good practice to unsubscribe from events when the object is destroyed.
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        }
    }
}