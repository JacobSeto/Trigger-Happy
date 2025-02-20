using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Card representative of action
/// </summary>
public class ActionCard : MonoBehaviour
{
    public PlayerAction actionType;
    Player player;
    [SerializeField] GameObject selectedUI;
    [SerializeField] TMP_Text cardName;

    [Header("Card Images")]
    [SerializeField] Image cardImage;
    [SerializeField] Sprite reloadSprite;
    [SerializeField] Sprite shootSprite;
    [SerializeField] Sprite deflectSprite;
    [SerializeField] Sprite stealSprite;
    bool selected;
    public void Init(PlayerAction actionType, Player player)
    {
        this.actionType = actionType;
        this.player = player;
    }

    private void Start()
    {
        selectedUI.SetActive(false);
        cardName.text = actionType.ToString();
        switch (actionType)
        {
            case PlayerAction.Reload:
                cardImage.sprite = reloadSprite;
                cardName.enabled = false;
                break;
            case PlayerAction.Shoot:
                cardImage.sprite= shootSprite;
                cardName.enabled = false;
                break;
            case PlayerAction.Deflect:
                cardImage.sprite = deflectSprite;
                cardName.enabled = false;
                break;
            case PlayerAction.Steal:
                cardImage.sprite= stealSprite;
                cardName.enabled = false;
                break;

        }
    }

    public void ToggleSelection()
    {
        if (selected)
        {
            UnSelectAction();
        }
        else
        {
            SelectAction();
        }
    }

    public void SelectAction()
    {
        if(player.selectedCard != null)
        {
            player.selectedCard.UnSelectAction();
        }
        selectedUI.SetActive(true);
        player.selectedCard = this;
        selected = true;
        player.UpdateActionRpc((int)actionType);
    }


    public void UnSelectAction()
    {
        selectedUI.SetActive(false);
        player.selectedCard = null;
        selected = false;
        player.UpdateActionRpc((int)PlayerAction.Mulligan);
    }
}