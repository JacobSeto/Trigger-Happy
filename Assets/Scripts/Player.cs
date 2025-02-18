using System.Globalization;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using System.Collections;

public class Player : NetworkBehaviour
{
    /// <summary>
    /// Tell server a player has spawned
    /// </summary>
    public override void OnNetworkSpawn()
    {
        GameManager.Instance.players.Add(this);
    }
}
