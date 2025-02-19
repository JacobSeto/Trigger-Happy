using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using WebSocketSharp;
public class PlayerIcon : NetworkBehaviour
{
    [SerializeField] Transform playerIcon;
    public TMP_Text playerNameText;
    [SerializeField] GameObject selectedUI;
    [HideInInspector] public Player player;
    bool selected;

    private void Start()
    {
        player = GetComponent<Player>();
        selectedUI.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string playerName = GameManager.Instance.playerNameField.text;
            if (playerName.IsNullOrEmpty())
            {
                playerName = "Player " + OwnerClientId;
            }
            SetPlayerNameServerRpc(playerName);
        }
        playerIcon.SetParent(GameManager.Instance.playerListUI, false);
    }

    [Rpc(SendTo.Server)]
    void SetPlayerNameServerRpc(string playerName)
    {
        playerNameText.text = playerName;
        for (int i = 0; i < GameManager.Instance.players.Count; i++)
        {
            UpdatePlayerNameRpc(GameManager.Instance.players[i]
                .GetComponent<PlayerIcon>().playerNameText.text, i);
        }
    }

    [Rpc(SendTo.NotServer)]
    void UpdatePlayerNameRpc(string playerName, int playerIndex)
    {
        GameManager.Instance.players[playerIndex]
            .GetComponent<PlayerIcon>().playerNameText.text = playerName;
    }

    public void ToggleSelection()
    {
        if (selected)
        {
            UnSelectPlayer();
        }
        else
        {
            SelectPlayer();
        }
    }

    public void SelectPlayer()
    {
        if (player.selectedCard != null)
        {
            player.selectedCard.UnSelectAction();
        }
        selectedUI.SetActive(true);
        player.selectedPlayerIcon = this;
        selected = true;
    }


    public void UnSelectPlayer()
    {
        selectedUI.SetActive(false);
        player.selectedPlayerIcon = null;
        selected = false;
    }
}
