using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public List<Player> players = new List<Player>();
    public Dictionary<ulong, Player> idToPlayer = new Dictionary<ulong, Player>();
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
        foreach(Player player in players)
        {
            if(player.selectedActionServer == PlayerAction.Mulligan)
            {
                player.MulliganRpc();
                ServerLogRpc(player.name + " mulligans");
            }
        }
        foreach(Player player in players)
        {
            if (player.selectedActionServer == PlayerAction.Reload)
            {
                Reload(player);
                ServerLogRpc(player.name + " reloads");
            }
        }
        foreach (Player player in players)
        {
            if (player.selectedActionServer == PlayerAction.Steal)
            {
                string message = Steal(player);
                ServerLogRpc(message);
            }
        }
        foreach (Player player in players)
        {
            if (player.selectedActionServer == PlayerAction.Shoot)
            {
                string message = Shoot(player);
                ServerLogRpc(message);
            }
        }
        foreach (Player player in players)
        {
            if (player.selectedActionServer == PlayerAction.SplitShot)
            {
                string message = SplitShot(player);
                ServerLogRpc(message);
            }
        }
        foreach (Player player in players)
        {
            if (player.selectedActionServer == PlayerAction.Deflect)
            {
                string message = Deflect(player);
                ServerLogRpc(message);
            }
        }

        foreach (Player player in players.ToList())
        {
            player.selectedActionServer = 0;
            player.shotCounterServer = 0;
            player.selectedPlayerIDServer = 99;
            player.ResolveRoundRpc(player.ammo, player.health);
        }

        yield return new WaitForSeconds(resolveTime);
        EndRoundServer();
    }

    /// <summary>
    /// Adds 1 ammo to this player
    /// </summary>
    void Reload(Player player)
    {
        player.ammo++;
    }
    /// <summary>
    /// Shoot the target player
    /// </summary>
    string Shoot(Player player)
    {
        if (player.ammo >= 1 && player.selectedPlayerIDServer != 99)
        {
            player.ammo--;
            Player selectedPlayer = idToPlayer[player.selectedPlayerIDServer];
            selectedPlayer.shotCounterServer++;
            if(selectedPlayer.shotCounterServer < 2)
            {
                player.health--;
            }
            return player.name + "shot at " + selectedPlayer.name;
        }
        else
        {
            return player.name + "shot with no ammo";
        }
    }
    /// <summary>
    /// Deflect one Shoot to target player if did not take damage
    /// </summary>
    string Deflect(Player player)
    {
        if (player.shotCounterServer == 1)
        {
            player.health++;
            if (player.selectedPlayerIDServer != 99)
            {
                Player selectedPlayer = idToPlayer[player.selectedPlayerIDServer];
                selectedPlayer.shotCounterServer++;
                if (selectedPlayer.shotCounterServer < 2)
                {
                    player.health--;
                }
                return player.name + " deflected at " + selectedPlayer.name;
            }
            return player.name + " deflected";
        }
        else if(player.shotCounterServer >= 2)
        {
            return player.name + " deflect broke";
        }
        else
        {
            return player.name + " deflected nothing";
        }

    }
    /// <summary>
    /// Steal one ammo from target player
    /// </summary>
    string Steal(Player player)
    {
        if (player.selectedPlayerIDServer != 99 && idToPlayer[player.selectedPlayerIDServer].ammo > 0)
        {
            idToPlayer[player.selectedPlayerIDServer].ammo--;
            player.ammo++;
            return player.name + " stole from " + idToPlayer[player.selectedPlayerIDServer].name;
        }
        else
        {
            return player.name + " stole nothing";
        }
    }
    /// <summary>
    /// Slow shot that hits 2 random players. Cannot be the same player
    /// </summary>
    string SplitShot(Player player)
    {
        if(player.ammo > 0)
        {
            string message = player.name + " splitshot ";
            List<Player> list = new List<Player>();
            list.AddRange(players);
            list.Remove(player);
            Player randomPlayer = list[Random.Range(0, list.Count)];
            randomPlayer.shotCounterServer++;
            if (randomPlayer.shotCounterServer < 2)
            {
                randomPlayer.health--;
            }
            message += randomPlayer.name;
            list.Remove(randomPlayer);
            if(list.Count > 0)
            {
                randomPlayer = list[Random.Range(0, list.Count)];
                randomPlayer.shotCounterServer++;
                if (randomPlayer.shotCounterServer < 2)
                {
                    randomPlayer.health--;
                }
                message += " and " + randomPlayer.name;
            }
            return message;
        }
        else
        {
            return player.name + " shot with no ammo";
        }
    }
    void EndRoundServer()
    {
        if (players.Count <= 1)
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
        winText.text = players.Count == 1 ? players[0].GetComponent<PlayerIcon>().playerNameText.text + " Wins!"
            : "It's a Draw!!!";
    }
    [Rpc(SendTo.Everyone)]
    public void ServerLogRpc(string message)
    {
        GameObject serverLog = Instantiate(serverLogPrefab, serverLogUI);
        serverLog.GetComponent<TMP_Text>().text = message;
        Destroy(serverLog, 10f);
    }

}
