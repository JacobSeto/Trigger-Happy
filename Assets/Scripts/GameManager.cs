using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public List<Player> players = new List<Player>();
    [SerializeField] int roundTime;
    [SerializeField] int resolveTime = 3;
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
    [SerializeField] TMP_Text winText;
    [Header("Player UI")]
    public Transform deckUI;
    public Transform handUI;
    public Transform discardUI;
    public GameObject discardCardUI;
    int timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        startGameButton.SetActive(false);
        gameUI.SetActive(false);
        discardCardUI.SetActive(false);
        winText.enabled = false;
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
        if (players.Count >= minPlayers)
        {
            StartRoundServer();
            startGameButton.SetActive(false);
        }
    }

    void StartRoundServer()
    {
        timer = roundTime;
        SetTimerTextRpc(timer);
        InvokeRepeating(nameof(DecrementTimer), 1f, 1f);
        StartRoundRpc();

    }

    [Rpc(SendTo.Everyone)]
    void StartRoundRpc()
    {
        gameUI.SetActive(true);
        deckBuilderUI.gameObject.SetActive(false);
    }

    void DecrementTimer()
    {
        timer--;
        if (timer <= 0)
        {
            CancelInvoke(nameof(DecrementTimer));
            StartCoroutine(ResolveRoundServer());
        }
        SetTimerTextRpc(timer);
    }

    [Rpc(SendTo.Everyone)]
    private void SetTimerTextRpc(int time)
    {
        if(time == 0)
        {
            timerText.text = "DRAW!";
        }
        else
        {
            timerText.text = time.ToString();
        }
    }

    IEnumerator ResolveRoundServer()
    {
        yield return StartCoroutine(ResolveActionBatch(PlayerAction.Mulligan));
        yield return StartCoroutine(ResolveActionBatch(PlayerAction.Reload));
        yield return StartCoroutine(ResolveActionBatch(PlayerAction.Steal));
        yield return StartCoroutine(ResolveActionBatch(PlayerAction.Shoot));
        yield return StartCoroutine(ResolveActionBatch(PlayerAction.SplitShot));
        yield return StartCoroutine(ResolveActionBatch(PlayerAction.Deflect));

        foreach (Player player in players)
        {
            player.ResetRoundStatesRpc();
            player.resolvedAction = false;
        }

        yield return new WaitForSeconds(resolveTime);
        EndRoundServer();
    }

    IEnumerator ResolveActionBatch(PlayerAction action)
    {
        List<Player> list = new List<Player>();
        foreach (Player player in players)
        {
            if (player.serverAction == (int)action)
            {
                list.Add(player);
                player.PlayActionRpc();
            }
        }
        // Wait for all players to acknowledge their action before moving to the next batch
        yield return StartCoroutine(WaitForResolvedActions(list));
    }

    IEnumerator WaitForResolvedActions(List<Player> players)
    {
        bool allAcknowledged = false;
        while (!allAcknowledged)
        {
            allAcknowledged = true;
            foreach (Player player in players)
            {
                if (!player.resolvedAction)
                {
                    allAcknowledged = false;
                    break;
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }


    void EndRoundServer()
    {
        if (players.Count == 1)
        {
            EndGameRpc();
        }
        else
        {
            StartRoundServer();
        }
    }
    [Rpc(SendTo.Everyone)]
    void EndGameRpc()
    {
        winText.enabled = true;
        winText.text = players[0].GetComponent<PlayerIcon>().playerNameText.text + " Wins!";
    }
    [Rpc(SendTo.Everyone)]
    public void ServerLogRpc(string message)
    {
        GameObject serverLog = Instantiate(serverLogPrefab, serverLogUI);
        serverLog.GetComponent<TMP_Text>().text = message;
        Destroy(serverLog, 10f);
    }

}
