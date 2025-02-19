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
    [Header("Game Set Up UI")]
    [SerializeField] GameObject startGameButton;
    public TMP_InputField playerNameField;
    [Header("Game UI")]
    public Transform playerListUI;
    public Transform deckBuilderUI;
    [SerializeField] GameObject gameUI;
    [SerializeField] GameObject playerUI;
    [SerializeField] TMP_Text timerText;
    [SerializeField] GameObject serverLogPrefab;
    [SerializeField] Transform serverLogUI;
    [Header("Player UI")]
    public Transform deckUI;
    public Transform handUI;
    public Transform discardUI;
    public GameObject discardCardUI;
    public int discardTime;
    int timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        startGameButton.SetActive(false);
        gameUI.SetActive(false);
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            startGameButton.SetActive(true);
        }
        playerListUI.gameObject.SetActive(true);
    }

    /// <summary>
    /// Start game once min player conditions are met
    /// </summary>
    public void StartGame()
    {
        if (IsServer && players.Count >= minPlayers)
        {
            StartRoundServer();
            startGameButton.SetActive(false);
        }
    }

    void StartRoundServer()
    {
        timer = roundTime;
        SetTimerTextRpc(timer);
        InvokeRepeating(nameof(DecrementTimer), 0, 1f);
        StartRoundRpc();

    }

    [Rpc(SendTo.Everyone)]
    void StartRoundRpc()
    {
        gameUI.SetActive(true);
    }

    void DecrementTimer()
    {
        timer--;
        if (timer <= 0)
        {
            CancelInvoke(nameof(DecrementTimer));
            ResolveRoundServer();
        }
        SetTimerTextRpc(timer);
    }

    [Rpc(SendTo.Everyone)]
    private void SetTimerTextRpc(int time)
    {
        timerText.text = time.ToString();
    }

    void ResolveRoundServer()
    {
        if (!IsServer) return;
        foreach (Player player in players)
        {
            player.PlayActionRpc();
        }
        Debug.Log("Resolve Cards Played");
        EndRoundServer();
    }

    void EndRoundServer()
    {
        if (!IsServer) return;
        StartRoundServer();
    }

    void EndGame()
    {

    }
    [Rpc(SendTo.Everyone)]
    public void ServerLogRpc(string message)
    {
        GameObject serverLog = Instantiate(serverLogPrefab, serverLogUI);
        serverLog.GetComponent<TMP_Text>().text = message;
        Destroy(serverLog, 10f);
    }

}
