using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using Unity.VisualScripting;

public class Player : NetworkBehaviour
{
    [SerializeField] int health;
    public int ammo;
    [SerializeField] int maxAmmo;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text ammoText;
    [Tooltip("Draw this amount and place one of drawn cards in the discard")]
    [SerializeField] int drawAmount;
    [SerializeField] Transform deckBuilder;
    [Tooltip("fastest speed")]
    [SerializeField] int speedOne = 0;
    [SerializeField] int speedTwo = 1;
    [SerializeField] int speedThree = 2;

    List<ActionCard> deck = new List<ActionCard>();
    List<ActionCard> hand = new List<ActionCard>();
    List<ActionCard> discard = new List<ActionCard>();

    [Header("Round States")]
    [Tooltip("true when someone has shot at this player. If already true, damage should not be blocked")]
    bool wasShot;
    bool tookDamage;
    bool deflecting;

    [Tooltip("The card selected that round. Null if no card selected")]
    [HideInInspector] public ActionCard selectedCard = null;
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
                    deck.Remove(card);
                    Destroy(card);
                    break;
                }
            }
        }
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
        yield return new WaitForSeconds(GameManager.Instance.discardTime);
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
            StartCoroutine(ResolveAction());
        }
    }

    IEnumerator ResolveAction()
    {
        switch (selectedCard.actionType)
        {
            case PlayerAction.Reload:
                yield return StartCoroutine(Reload());
                break;
            case PlayerAction.Shoot:
                yield return StartCoroutine(Shoot());
                break;
            case PlayerAction.Deflect:
                deflecting = true;
                yield return StartCoroutine(Deflect());
                break;
            case PlayerAction.Steal:
                yield return StartCoroutine(Steal());
                break;
            case PlayerAction.SplitShot:
                yield return StartCoroutine(SplitShot());
                break;
        }
        discard.Add(selectedCard);
        selectedCard.transform.SetParent(GameManager.Instance.discardUI, false);
        hand.Remove(selectedCard);
        selectedCard.UnSelectAction();
        selectedCard = null;
        if(selectedPlayerIcon != null)
        {
            selectedPlayerIcon.UnSelectPlayer();
            selectedPlayerIcon = null;
        }
    }


    /// <summary>
    /// Adds 1 ammo to this player
    /// </summary>
    IEnumerator Reload()
    {
        yield return new WaitForSeconds(speedOne);
        UpdateAmmoRpc(ammo+1);

    }
    /// <summary>
    /// Shoot the target player
    /// </summary>
    IEnumerator Shoot()
    {
        yield return new WaitForSeconds(speedTwo);
        if(ammo >= 1 && selectedPlayerIcon.representedPlayer != null)
        {
            UpdateAmmoRpc(ammo - 1);
            selectedPlayerIcon.representedPlayer.TakeDamageRpc();
        }
    }
    /// <summary>
    /// Deflect one Shoot to target player if did not take damage
    /// </summary>
    IEnumerator Deflect()
    {
        yield return new WaitForSeconds(speedThree);
        if (!tookDamage && ammo >= 1 && selectedPlayerIcon.representedPlayer != null)
        {
            UpdateAmmoRpc(ammo - 1);
            selectedPlayerIcon.representedPlayer.TakeDamageRpc();
        }

    }
    /// <summary>
    /// Steal one ammo from target player
    /// </summary>
    IEnumerator Steal()
    {
        yield return new WaitForSeconds(speedOne);
        if(selectedPlayerIcon.representedPlayer.ammo > 0)
        {
            UpdateAmmoRpc(ammo + 1);
            selectedPlayerIcon.representedPlayer.UpdateAmmoRpc(ammo - 1);
        }
    }
    /// <summary>
    /// Slow shot that hits 2 random players. Cannot be the same player
    /// </summary>
    IEnumerator SplitShot()
    {
        yield return new WaitForSeconds(speedThree);
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
        wasShot = tookDamage = deflecting = false;
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
        if (tookDamage)
        {
            return;
        }
        if(deflecting && !wasShot)
        {
            wasShot = true;
        }
        else
        {
            UpdateHealthRpc(health - 1);
            tookDamage = true;
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
