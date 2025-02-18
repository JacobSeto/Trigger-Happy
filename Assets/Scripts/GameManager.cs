using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public List<Player> players = new List<Player>();
    [SerializeField] int roundTime;
    [SerializeField] int minPlayers;
    [Header("UI")]
    [SerializeField] GameObject startGameButton;
    public TMP_InputField playerNameField;
    public Transform playerListUI;
    public GameObject playerUI;
    [SerializeField] int timer;
    bool inRound;
    public NetworkVariable<int> playerCount = new NetworkVariable<int>(0);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        startGameButton.SetActive(false);
        playerListUI.gameObject.SetActive(false);
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeft;
            startGameButton.SetActive(true);
        }
        playerListUI.gameObject.SetActive(true);
    }
    private void OnPlayerJoined(ulong clientId)
    {
        if (IsServer)
        {
            playerCount.Value++;
        }
    }

    private void OnPlayerLeft(ulong clientId)
    {
        if (IsServer)
        {
            playerCount.Value = Mathf.Max(0, playerCount.Value - 1);
        }
    }

    /// <summary>
    /// Start game once min player conditions are met
    /// </summary>
    public void StartGame()
    {
        if (playerCount.Value >= minPlayers)
        {
            StartRound();
            startGameButton.SetActive(false);
        }

    }

    // Expose the timer for other scripts to access
    public float GetTimer()
    {
        return timer;
    }

    void StartRound()
    {
        timer = roundTime;
        SetTimerOnClientsClientRpc(timer);
        InvokeRepeating(nameof(DecrementTimer), 0, 1f);

    }
    void DecrementTimer()
    {
        timer--;
        if (timer <= 0)
        {
            CancelInvoke(nameof(DecrementTimer));
        }
        SetTimerOnClientsClientRpc(timer);
    }
    // Client RPC to update the timer on each client
    [Rpc(SendTo.NotServer)]
    private void SetTimerOnClientsClientRpc(int newTimer)
    {
        // Set the timer value locally
        timer = newTimer;
    }

    void ResolveRound()
    {

    }

    void EndRound()
    {

    }

    void EndGame()
    {

    }

}
