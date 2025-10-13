using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;






public class Matchmaking : MonoBehaviour
{
    private Lobby _connectedLobby;
    private UnityTransport _transport;
    private QueryResponse _lobbies;
    private const string JoinCodeKey = "j";
    private const string SceneNameKey = "s";
    private string _playerId;

//#if UNITY_SERVER

    void Awake() => _transport = FindFirstObjectByType<UnityTransport>();



    public async void CreateOrJoinLobby(string sceneToLoad)
    {
        await Authenticate();

        _connectedLobby = await QuickJoinLobby() ?? await CreateLobby(sceneToLoad);
    }



    private async Task Authenticate()
    {
        var options = new InitializationOptions();

// #if UNITY_EDITOR
//         // Remove this if you don't have ParrelSync installed
//         // It's used to differentiate the clients, otherwise lobby will count as the same
//         options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
// #endif

        // 1 - Initializes unity services
        await UnityServices.InitializeAsync(options);

        // 2 - Check if the user is already authenticated
        if (AuthenticationService.Instance.IsSignedIn)
        {
            _playerId = AuthenticationService.Instance.PlayerId;
        }
        else
        {
            // 3 - Sign in anonymously (creates an account for the user)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        
            // 4 - Get the playerId created by the authentication
            _playerId = AuthenticationService.Instance.PlayerId;
        }
    
    }



    private async Task<Lobby> QuickJoinLobby()
    {
        try {
            var lobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            var a = await RelayService.Instance.JoinAllocationAsync(lobby.Data[JoinCodeKey].Value);

            SetTransformAsClient(a);

            string sceneName = lobby.Data[SceneNameKey].Value;
            await LoadScene(sceneName);

            NetworkManager.Singleton.StartClient();

            return lobby;

        } catch (Exception e) {
            Debug.Log($"No lobbies available via quick join: {e}");
            return null;
        }
    }



    private async Task<Lobby> CreateLobby(string sceneToLoad)
    {
        try {
            // Set max number of players to allocate resources
            const int maxPlayers = 2;

            // 4 - Create relay allocation for the instance for the amount of players in a game
            Allocation a = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

            Debug.Log("Allocation Id: " + a.AllocationId);
            Debug.Log("a2: " + a.RelayServer.IpV4 + (ushort)a.RelayServer.Port + a.AllocationIdBytes + a.Key + a.ConnectionData);

            // 5 - Get the joinCode to join that allocation
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

            // 6 - Define a lobby 'Options' variable to use to create a lobby with the acquired joinCode  
            var options = new CreateLobbyOptions {
                Data = new Dictionary<string, DataObject> { 
                    { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                    { SceneNameKey, new DataObject(DataObject.VisibilityOptions.Public, sceneToLoad) }
                }
            };
            // 7 - Create the lobby with the lobby options specified 
            var lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby name", maxPlayers, options);

            // 8 - Set a heartbeat coroutine to keep the lobby alive for more than 30 seconds
            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));

            // 9 - Set this variable to hold all the rlay data
            var relayServerData = AllocationUtils.ToRelayServerData(a, "dtls");
            //RelayServerData relayServerData = new RelayServerData(a, "dtls");

            // 10 - Pass the relay data to the Network manager
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // 11 - Load scene before starting the host / Spawning the player
            await LoadScene(sceneToLoad);

            // 12 - Start the host
            NetworkManager.Singleton.StartHost();

            return lobby;
        } catch (Exception e) {
            Debug.Log($"Failed creating body: {e}");
            return null;
        }
    }



    private async Task LoadScene(string sceneToLoad)
    {
        // Scene to be loaded
        AsyncOperation loadScene = SceneManager.LoadSceneAsync(sceneToLoad);

        // Not sure - I think this is what makes sure the scene is loaded before step 12 
        while (!loadScene.isDone)
        {
            await Task.Yield();
        }
    }



    private void SetTransformAsClient(JoinAllocation a)
    {
        // Sets client relay data
        _transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
    }



    private static IEnumerator HeartbeatLobbyCoroutine(string lobbyId, int waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true) {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }



    private void OnDestroy() {
        try {
            StopAllCoroutines();
            if (_connectedLobby != null)
            {
                if (_connectedLobby.HostId == _playerId) LobbyService.Instance.DeleteLobbyAsync(_connectedLobby.Id);
                else LobbyService.Instance.RemovePlayerAsync(_connectedLobby.Id, _playerId);
            }
        } catch (Exception e) {
            Debug.Log($"Error shutting down lobby: {e}");
        }
    }
//#endif
}
