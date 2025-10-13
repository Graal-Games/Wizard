using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using IngameDebugConsole;
using Singletons;
using Unity.Multiplayer;
using Unity.Services.Lobbies;
using Unity.Services.Multiplayer;
using Unity.Services.Lobbies.Models; 

public class GameLobby : Singleton<GameLobby>
{
    private Lobby hostLobby;
    private float heartBeatTimer;
    private async void Start()
    {
        // Initialize unity services
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        // Authentication
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    /* <Join lobby>
    If one is available > join it
    If none is available > Create it
    If created but current players != 2 > wait // To Do 
    > Send heartbeats To keep the lobby open 
    */

    private void Update() 
    {
        HandleLobbyHeartbeat();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer < 0f)
            {
                float heartBeatTimerMax = 150;
                heartBeatTimer = heartBeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    [ConsoleMethod( "CreateLobby", "Creates a Lobby" )]
    public static async void CreateLobby()
    {
        try {

            string lobbyName = "My Lobby";
            int maxPlayers = 2;

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);
            
            GameLobby myLobby = new GameLobby();
            myLobby.hostLobby = lobby;
        
            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id);
        
        } catch (LobbyServiceException e) {

            Debug.Log(e);

        }
        
    }

    // Change to JoinLobby
    [ConsoleMethod( "ListLobbies", "Lists available lobbies" )]
    public static async void ListLobbies()
    {
        try {
            // Add a filter
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions {
                // Get 25 lobbies
                Count = 25,
                // Filter the query by available slots being Greater than (LT) 2 
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "2" , QueryFilter.OpOptions.LT)
                },
                // Order them by when they were created by oldest to newest
                Order = new List<QueryOrder> {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            // Query available lobbies
            //QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);

            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id);
            }

        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
        
    }
    
    [ConsoleMethod( "JoinLobby", "Creates a cube at specified position" )]
    public static async void JoinLobby()
    {
        try {

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            
            await LobbyService.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);

            Debug.Log("Joined Lobby: " + queryResponse.Results[0].Id);

        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
        
    }

    public void StartGame()
    {
        print("Game Starts");
    }
}
