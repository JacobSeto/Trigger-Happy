using TMPro;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Card representative of action
/// </summary>
public class ActionCard : MonoBehaviour
{
    public PlayerAction actionType;
    Player player;
    [SerializeField] GameObject selectedUI;
    [SerializeField] TMP_Text cardName;
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
    }


    public void UnSelectAction()
    {
        selectedUI.SetActive(false);
        player.selectedCard = null;
        selected = false;
    }
}