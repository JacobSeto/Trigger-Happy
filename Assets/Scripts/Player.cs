using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

public class Player : NetworkBehaviour
{
    [SerializeField] int health;
    [Tooltip("Draw this amount and place one of drawn cards in the discard")]
    [SerializeField] int drawAmount;
    [SerializeField] Transform deckBuilder;
    List<ActionCard> deck = new List<ActionCard>();
    List<ActionCard> hand = new List<ActionCard>();
    List<ActionCard> discard = new List<ActionCard>();

    [Tooltip("true when someone has shot at this player. If already true, damage should not be blocked")]
    bool wasShot;
    bool tookDamage;
    [Tooltip("The card selected that round. Null if no card selected")]
    [HideInInspector] public ActionCard selectedCard = null;
    public PlayerIcon selectedPlayerIcon = null;

    [SerializeField] ActionCard actionCardPrefab;


    /// <summary>
    /// Tell server a player has spawned
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            deckBuilder.SetParent(GameManager.Instance.deckBuilderUI, false);
        }
        GameManager.Instance.players.Add(this);
        foreach (var player in GameManager.Instance.players)
        {
            if (NetworkManager.Singleton.LocalClientId == player.OwnerClientId)
            {
                foreach (var p in GameManager.Instance.players)
                {
                    p.GetComponent<PlayerIcon>().targetPlayer = player;
                }
                break;
            }
        }
    }

    public void AddCardToDeck(int actionType)
    {
        if (IsOwner)
        {
            ActionCard card = Instantiate(actionCardPrefab, GameManager.Instance.deckUI);
            deck.Add(card);
            card.Init((PlayerAction)actionType, this);
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
                foreach (ActionCard card in discard)
                {
                    card.transform.SetParent(GameManager.Instance.deckUI, false);
                }
                discard.Clear();
            }
            else
            {
                ActionCard card = deck[Random.Range(0, deck.Count)];
                hand.Add(card);
                card.transform.SetParent(GameManager.Instance.handUI, false);
                deck.Remove(card);
            }
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
        selectedCard = null;
        GameManager.Instance.discardCardUI.SetActive(false);
    }
    [Rpc(SendTo.Owner)]
    public void PlayActionRpc()
    {
        GameManager.Instance.ServerLogRpc("Player " + OwnerClientId + ": " +
         (selectedCard != null ? selectedCard.actionType.ToString() : "none") + " targeting Player " +
         (selectedPlayerIcon != null ? selectedPlayerIcon.player.OwnerClientId.ToString() : "none"));
        if (selectedCard == null)
        {
            Mulligan();
        }
        else
        {
            selectedCard.UnSelectAction();
            discard.Add(selectedCard);
            selectedCard.transform.SetParent(GameManager.Instance.discardUI, false);
            hand.Remove(selectedCard);
            selectedCard = null;
        }
        if(selectedPlayerIcon != null)
        {
            selectedPlayerIcon.UnSelectPlayer();
            selectedPlayerIcon = null;
        }
        
    }




}
