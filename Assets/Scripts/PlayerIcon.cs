using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using WebSocketSharp;
using UnityEngine.UI;
public class PlayerIcon : NetworkBehaviour
{
    public Transform iconTransform;
    public TMP_Text playerNameText;
    [SerializeField] GameObject selectedUI;
    public Player representedPlayer;
    [HideInInspector] public Player targetPlayer;
    public Button selectButton;
    bool selected;

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
            selectButton.interactable = false;
        }
        iconTransform.SetParent(GameManager.Instance.playerListUI, false);
        selectedUI.SetActive(false);
    }

    [Rpc(SendTo.Server)]
    void SetPlayerNameServerRpc(string playerName)
    {
        representedPlayer.name = playerName;
        playerNameText.text = playerName;
        for (int i = 0; i < GameManager.Instance.players.Count; i++)
        {
            GameManager.Instance.players[i].icon.UpdatePlayerNameRpc(GameManager.Instance.players[i]
                .GetComponent<PlayerIcon>().playerNameText.text);
        }
    }

    [Rpc(SendTo.NotServer)]
    void UpdatePlayerNameRpc(string playerName)
    {
        playerNameText.text = playerName;
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

    void SelectPlayer()
    {
        if (targetPlayer.selectedPlayerIcon != null)
        {
            targetPlayer.selectedPlayerIcon.UnSelectPlayer();
        }
        selectedUI.SetActive(true);
        targetPlayer.selectedPlayerIcon = this;
        selected = true;
        targetPlayer.UpdateSelectedPlayerIDServerRpc(representedPlayer.OwnerClientId);
    }


    public void UnSelectPlayer()
    {
        selectedUI.SetActive(false);
        targetPlayer.selectedPlayerIcon = null;
        selected = false;
        targetPlayer.UpdateSelectedPlayerIDServerRpc(99);
    }
}
