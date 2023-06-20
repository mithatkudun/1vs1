using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class HostManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string characterSelectSceneName = "CharacterSelect";
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private int maxConnections = 2;
    public static HostManager Instance { get; private set; }

    public Dictionary<ulong, ClientData> ClientData { get; private set; }

    private bool gameHasStarted;

    private string lobbyId;
    public string JoinCode { get; private set; }

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }


    public void StartServer()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;

        NetworkManager.Singleton.OnServerStarted += OnNetworkReady;

        ClientData = new Dictionary<ulong, ClientData>();

        NetworkManager.Singleton.StartServer();
    }


    public async void StartHost()
    {
        Allocation allocation;

        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        }

        catch (Exception e)
        {

            Debug.LogError($"Relay create allocation reques failed {e.Message}");
            throw;
        }

        Debug.Log($"server : {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"server : {allocation.AllocationId}");

        try
        {
           JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch 
        {
            Debug.LogError("Relay get join code reques failed");
            throw;
        }

        var relayServerData = new RelayServerData(allocation, "dtls");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        try
        {
            var createLobbyOptions = new CreateLobbyOptions();
            createLobbyOptions.IsPrivate = false;
            createLobbyOptions.Data = new Dictionary<string, DataObject>()
            {
                {
                    "JoinCode", new DataObject(
                        visibility: DataObject.VisibilityOptions.Member,
                        value: JoinCode
                        )
                }
            };


            Lobby lobby = await Lobbies.Instance.CreateLobbyAsync("My Lobby", maxConnections, createLobbyOptions);
            lobbyId = lobby.Id;
            StartCoroutine(HeartbeatLobbyCoroutine(15));

        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
            throw;
        }

        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;

        NetworkManager.Singleton.OnServerStarted += OnNetworkReady;

        ClientData = new Dictionary<ulong, ClientData>();

        NetworkManager.Singleton.StartHost();
    }


    private IEnumerator HeartbeatLobbyCoroutine(float waitTimeSeconds)
    {
        var delay = new WaitForSeconds(waitTimeSeconds);
        while(true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }


    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if(ClientData.Count >= maxConnections || gameHasStarted)
        {
            response.Approved = false;
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;

        ClientData[request.ClientNetworkId] = new ClientData(request.ClientNetworkId);

        Debug.Log($"Added  client {request.ClientNetworkId}");
    }

    private void OnNetworkReady()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        NetworkManager.Singleton.SceneManager.LoadScene(characterSelectSceneName, LoadSceneMode.Single);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if(ClientData.ContainsKey(clientId))
        {
            if(ClientData.Remove(clientId))
            {
                Debug.Log($"Removed client {clientId}");
            }
        }
    }


    public void SetCharacter(ulong clientId, int characterId)
    {
        if(ClientData.TryGetValue(clientId, out ClientData data))
        {
            data.characterId = characterId;
        }
    }


    public void StartGame()
    {
        gameHasStarted = true;

        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }


}
