using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;

public class NetworkSetUp : MonoBehaviour
{
    [SerializeField] TMP_InputField joinCodeField;
    [SerializeField] TMP_Text joinCode;



    public async void StartHost()
    {
        Task<string> task = StartHostWithRelay();
        joinCode.text = await task;
    }

    /// <summary>
    /// Creates a relay server allocation and start a host
    /// </summary>
    /// <param name="maxConnections">The maximum amount of clients that can connect to the relay</param>
    /// <returns>The join code</returns>
    public async Task<string> StartHostWithRelay(int maxConnections = 8)
    {
        Debug.Log("Initialize Sync");
        //Initialize the Unity Services engine
        await UnityServices.InitializeAsync();
        //Always authenticate your users beforehand
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            //If not already logged, log the user in
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        Debug.Log("Allocate");
        // Request allocation and join code
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        Debug.Log("Create Join Code");
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        // Configure transport
        Debug.Log("Create Server Data");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
        // Start host
        Debug.Log(joinCode);
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }

    public async void StartClient()
    {
        Task<bool> task = StartClientWithRelay();
        bool clientJoined = await task;
        Debug.Log(clientJoined ? "Client Joined" : "Client Failed to Join");
    }

    /// <summary>
    /// Join a Relay server based on the JoinCode received from the Host or Server
    /// </summary>
    /// <param name="joinCode">The join code generated on the host or server</param>
    /// <returns>True if the connection was successful</returns>
    public async Task<bool> StartClientWithRelay()
    {
        //Initialize the Unity Services engine
        await UnityServices.InitializeAsync();
        //Always authenticate your users beforehand
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            //If not already logged, log the user in
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Join allocation
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCodeField.text);
        // Configure transport
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
        // Start client
        return !string.IsNullOrEmpty(joinCodeField.text) && NetworkManager.Singleton.StartClient();
    }

}
