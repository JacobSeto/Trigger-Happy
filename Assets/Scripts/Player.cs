using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Player : NetworkBehaviour
{
    public int health;
    public int ammo;
    [SerializeField] int maxAmmo;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text ammoText;
    [Tooltip("Draw this amount and place one of drawn cards in the discard")]
    [SerializeField] int drawAmount;
    [SerializeField] float discardTime;
    float discardTimer;
    [SerializeField] Transform deckBuilder;

    List<ActionCard> deck = new List<ActionCard>();
    List<ActionCard> hand = new List<ActionCard>();
    List<ActionCard> discard = new List<ActionCard>();

    [Header("Round States")]
    [HideInInspector] public int shotCounterServer;
    [HideInInspector] public PlayerAction selectedActionServer;
    [Tooltip("99 is a user id that doesn't exist")]
    public ulong selectedPlayerIDServer;

    [HideInInspector] public ActionCard selectedCard;
    [HideInInspector] public PlayerIcon selectedPlayerIcon;

    [SerializeField] ActionCard actionCardPrefab;
    public PlayerIcon icon;


    /// <summary>
    /// Tell server a player has spawned
    /// </summary>
    public override void OnNetworkSpawn()
    {
        GameManager.Instance.players.Add(this);
        ammoText.text = "Ammo: " + ammo.ToString() + "/" + maxAmmo.ToString();
        if (IsServer)
        {
            GameManager.Instance.idToPlayer.Add(OwnerClientId, this);
        }
        if (IsOwner)
        {
            deckBuilder.SetParent(GameManager.Instance.deckBuilderUI, false);
            foreach (var player in GameManager.Instance.players)
            {
                player.icon.targetPlayer = this;
            }
            SetPlayersHealthRpc();
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

    private void Update()
    {
        if(IsOwner && discardTimer > 0)
        {
            discardTimer -= Time.deltaTime;
            GameManager.Instance.discardCardUI.text = "Choose a card to discard: "
                + Mathf.CeilToInt(discardTimer);
            if (discardTimer <= 0)
            {
                DiscardCard();
            }
        }
    }


    [Rpc(SendTo.Server)]
    void SetPlayersHealthRpc()
    {
        foreach(var player in GameManager.Instance.players)
        {
            player.UpdateHealthRpc(GameManager.Instance.startingPlayerHealth);
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
                    Destroy(card.gameObject);
                    break;
                }
            }
        }
    }
    [Rpc(SendTo.Owner)]
    public void MulliganRpc()
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
        GameManager.Instance.discardCardUI.gameObject.SetActive(true);
        discardTimer = discardTime;
    }

    void DiscardCard()
    {
        GameManager.Instance.discardCardUI.gameObject.SetActive(false);
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
    }

    public void UpdateAmmo(int ammo)
    {
        this.ammo = Mathf.Min(ammo, maxAmmo);
        ammoText.text = "Ammo: " + this.ammo.ToString() + "/" + maxAmmo.ToString();
    }
    public void UpdateHealth(int health)
    {
        this.health = health;
        healthText.text = "Health: " + health.ToString();
        if(health == 0)
        {
            Die();
        }
    }
    [Rpc(SendTo.Everyone)]
    public void UpdateHealthRpc(int health)
    {
        UpdateHealth(health);
    }

    [Rpc(SendTo.Server)]
    public void UpdateSelectedActionServerRpc(int selectedAction)
    {
        selectedActionServer = (PlayerAction)selectedAction;
    }

    [Rpc(SendTo.Server)]
    public void UpdateSelectedPlayerIDServerRpc(ulong id)
    {
        selectedPlayerIDServer = id;
    }

    [Rpc(SendTo.Everyone)]
    public void ResolveRoundRpc(int ammo, int health)
    {
        UpdateAmmo(ammo);
        UpdateHealth(health);
        if (IsOwner)
        {
            if (selectedCard != null)
            {
                discard.Add(selectedCard);
                selectedCard.transform.SetParent(GameManager.Instance.discardUI, false);
                hand.Remove(selectedCard);
                selectedCard.UnSelectAction();

            }
            if (selectedPlayerIcon != null)
            {
                selectedPlayerIcon.UnSelectPlayer();
            }
        }
    }


    public void Die()
    {
        GameManager.Instance.players.Remove(this);
        gameObject.SetActive(false);
        icon.iconTransform.gameObject.SetActive(false);
    }




}
