using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
public class PlayerIcon : NetworkBehaviour
{
    [SerializeField] Transform playerIcon;
    public TMP_Text playerNameText;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string playerName = GameManager.Instance.playerNameField.text;
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
}
