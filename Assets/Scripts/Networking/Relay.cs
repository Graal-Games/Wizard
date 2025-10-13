using System.Collections;
using System.Collections.Generic;
using IngameDebugConsole;
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
    // private async void Start()
    // {
    //     // Sends a request to the unity services to initialize the Api
        
    //     // ---------I'm already singing in game lobby script ----------
    //     // await UnityServices.InitializeAsync();

    //     // AuthenticationService.Instance.SignedIn += () => {
    //     //     Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
    //     // };

    //     // await AuthenticationService.Instance.SignInAnonymouslyAsync();
    // }

    [ConsoleMethod( "CreateRelay", "Creates a cube at specified position" )]
    public static async void CreateRelay()
    {
        try {

            // Param for max number of connection minus the host
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);

            // Generate a joinCode and save to variable
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(joinCode);

            // Get the relayserver data to funnel into the network manager info
            var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

            // Inject the relay server data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            /* 
            To delay player prefab spawn use: 
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            Source: https://docs-multiplayer.unity3d.com/netcode/current/basics/connection-approval/index.html
            */
            NetworkManager.Singleton.StartHost();
            // private void ApprovalCheck()

        } catch (RelayServiceException e) {

            Debug.Log(e);

        }
        
    }

    [ConsoleMethod( "JoinRelay", "Joins the relay" )]
    // Create relay and join in the same function
    public static async void JoinRelay(string joinCode)
    {
        try {
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            /* 
            To delay player prefab spawn use: 
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            Source: https://docs-multiplayer.unity3d.com/netcode/current/basics/connection-approval/index.html
            */
            NetworkManager.Singleton.StartClient();
            // private void ApprovalCheck()

        } catch (RelayServiceException e) {
            Debug.Log(e);
        }
    }

    // private void OnDestroy()
    // {

    // }
}
