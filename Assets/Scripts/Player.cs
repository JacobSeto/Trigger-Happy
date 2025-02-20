using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Player : NetworkBehaviour
{
    [SerializeField] int health;
    public int ammo;
    [SerializeField] int maxAmmo;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text ammoText;
    [Tooltip("Draw this amount and place one of drawn cards in the discard")]
    [SerializeField] int drawAmount;
    [SerializeField] float discardTime;
    [SerializeField] Transform deckBuilder;

    List<ActionCard> deck = new List<ActionCard>();
    List<ActionCard> hand = new List<ActionCard>();
    List<ActionCard> discard = new List<ActionCard>();

    [Header("Round States")]
    int shotCounter;
    public bool resolvedAction;

    [Tooltip("The card selected that round. Null if no card selected")]
    [HideInInspector] public ActionCard selectedCard = null;
    [Tooltip("0 means no selection and mulligan, updates server on selections")]
    [HideInInspector] public int serverAction;
    [HideInInspector] public PlayerIcon selectedPlayerIcon = null;

    [SerializeField] ActionCard actionCardPrefab;
    public PlayerIcon icon;


    /// <summary>
    /// Tell server a player has spawned
    /// </summary>
    public override void OnNetworkSpawn()
    {
        GameManager.Instance.players.Add(this);
        healthText.text = "Health: " + health.ToString();
        ammoText.text = "Ammo: " + ammo.ToString() + "/" + maxAmmo.ToString();
        if (IsOwner)
        {
            deckBuilder.SetParent(GameManager.Instance.deckBuilderUI, false);
            foreach (var player in GameManager.Instance.players)
            {
                if (NetworkManager.Singleton.LocalClientId == player.OwnerClientId)
                {
                    foreach (var p in GameManager.Instance.players)
                    {
                        p.icon.targetPlayer = player;
                    }
                    break;
                }
            }
        }
        else
        {
            foreach (var player in GameManager.Instance.players)
            {
                if (NetworkManager.Singleton.LocalClientId == player.OwnerClientId)
                {
                    icon.targetPlayer = player;
                    break;
                }
            }
        }
    }

    public void AddCardToDeck(int actionType)
    {
        ActionCard card = Instantiate(actionCardPrefab, GameManager.Instance.deckUI);
        deck.Add(card);
        card.Init((PlayerAction)actionType, this);
    }

    public void RemoveCardFromDeck(int actionType)
    {
        if(deck.Count != 0)
        {
            foreach(var card in deck)
            {
                if((int)card.actionType == actionType)
                {
                    Debug.Log("Removing card");
                    deck.Remove(card);
                    Destroy(card.gameObject);
                    break;
                }
            }
        }
    }
    /// <summary>
    /// Set to -1 if no action card was selected
    /// </summary>
    /// <param name="action"></param>
    [Rpc(SendTo.Server)]
    public void UpdateActionRpc(int action)
    {
        serverAction = action;
    }

    public void Mulligan()
    {
        discard.AddRange(hand);
        foreach(ActionCard card in hand)
        {
            card.transform.SetParent(GameManager.Instance.discardUI, false);
        }
        hand.Clear();
        for(int i = 0; i < drawAmount; i++)
        {
            if(deck.Count == 0)
            {
                deck.AddRange(discard);
                foreach (ActionCard c in discard)
                {
                    c.transform.SetParent(GameManager.Instance.deckUI, false);
                }
                discard.Clear();
            }
            ActionCard card = deck[Random.Range(0, deck.Count)];
            hand.Add(card);
            card.transform.SetParent(GameManager.Instance.handUI, false);
            deck.Remove(card);
            
        }
        StartCoroutine(DiscardCard());
    }

    IEnumerator DiscardCard()
    {
        GameManager.Instance.discardCardUI.SetActive(true);
        yield return new WaitForSeconds(discardTime);
        if(selectedCard != null)
        {
            discard.Add(selectedCard);
            selectedCard.transform.SetParent(GameManager.Instance.discardUI, false);
            hand.Remove(selectedCard);
            selectedCard.UnSelectAction();
            selectedCard = null;
        }
        else
        {
            ActionCard selectCard = hand[Random.Range(0, hand.Count)];
            discard.Add(selectCard);
            selectCard.transform.SetParent(GameManager.Instance.discardUI, false);
            hand.Remove(selectCard);
        }
        GameManager.Instance.discardCardUI.SetActive(false);
    }

    [Rpc(SendTo.Owner)]
    public void PlayActionRpc()
    {
        GameManager.Instance.ServerLogRpc("Player " + OwnerClientId + ": " +
         (selectedCard != null ? selectedCard.actionType.ToString() : "none") + " targeting Player " +
         (selectedPlayerIcon != null ? selectedPlayerIcon.representedPlayer.OwnerClientId.ToString() : "none"));
        if (selectedCard == null)
        {
            Mulligan();
        }
        else
        {
            ResolveAction();
        }
        if (selectedPlayerIcon != null)
        {
            selectedPlayerIcon.UnSelectPlayer();
            selectedPlayerIcon = null;
        }
        ResolvedActionRpc();
    }

    [Rpc(SendTo.Server)]
    public void ResolvedActionRpc()
    {
        resolvedAction = true;
    }

    void ResolveAction()
    {
        switch (selectedCard.actionType)
        {
            case PlayerAction.Reload:
                Reload();
                break;
            case PlayerAction.Shoot:
                Shoot();
                break;
            case PlayerAction.Deflect:
                Deflect();
                break;
            case PlayerAction.Steal:
                Steal();
                break;
            case PlayerAction.SplitShot:
                SplitShot();
                break;
        }
        discard.Add(selectedCard);
        selectedCard.transform.SetParent(GameManager.Instance.discardUI, false);
        hand.Remove(selectedCard);
        selectedCard.UnSelectAction();
        selectedCard = null;
    }


    /// <summary>
    /// Adds 1 ammo to this player
    /// </summary>
    void Reload()
    {
        UpdateAmmoRpc(ammo+1);

    }
    /// <summary>
    /// Shoot the target player
    /// </summary>
    void Shoot()
    {
        if(ammo >= 1 && selectedPlayerIcon.representedPlayer != null)
        {
            UpdateAmmoRpc(ammo - 1);
            selectedPlayerIcon.representedPlayer.TakeDamageRpc();
        }
    }
    /// <summary>
    /// Deflect one Shoot to target player if did not take damage
    /// </summary>
    void Deflect()
    {
        if (shotCounter < 2 && selectedPlayerIcon.representedPlayer != null)
        {
            selectedPlayerIcon.representedPlayer.TakeDamageRpc();
            UpdateHealthRpc(health + 1);
        }

    }
    /// <summary>
    /// Steal one ammo from target player
    /// </summary>
    void Steal()
    {
        if(selectedPlayerIcon.representedPlayer.ammo > 0)
        {
            UpdateAmmoRpc(ammo + 1);
            selectedPlayerIcon.representedPlayer.UpdateAmmoRpc(ammo - 1);
        }
    }
    /// <summary>
    /// Slow shot that hits 2 random players. Cannot be the same player
    /// </summary>
    void SplitShot()
    {
        if (ammo >= 1)
        {
            UpdateAmmoRpc(ammo - 1);
            List<Player> list = new List<Player>();
            list.AddRange(GameManager.Instance.players);
            list.Remove(this);
            Player randomPlayer = list[Random.Range(0, list.Count)];
            randomPlayer.TakeDamageRpc();
            list.Remove(randomPlayer);
            if(list.Count > 0)
            {
                randomPlayer = list[Random.Range(0, list.Count)];
                randomPlayer.TakeDamageRpc();
            }
        }
    }
    [Rpc(SendTo.Owner)]
    public void ResetRoundStatesRpc()
    {
        shotCounter = 0;
    }
    [Rpc(SendTo.Everyone)]
    public void UpdateAmmoRpc(int ammo)
    {
        this.ammo = ammo;
        ammoText.text = "Ammo: " + ammo.ToString() + "/" + maxAmmo.ToString();
    }

    [Rpc(SendTo.Owner)]
    public void TakeDamageRpc()
    {
        shotCounter += 1;
        if(shotCounter < 2)
        {
            UpdateHealthRpc(health - 1);
        }
    }

    [Rpc(SendTo.Everyone)]
    void UpdateHealthRpc(int health)
    {
        this.health = health;
        healthText.text = "Health: " + health.ToString();
        if(health == 0)
        {
            Die();
        }
    }

    public void Die()
    {
        GameManager.Instance.players.Remove(this);
        gameObject.SetActive(false);
        icon.iconTransform.gameObject.SetActive(false);
    }




}
