using TMPro;
using UnityEngine;

public class DeckBuilderButton : MonoBehaviour
{
    [SerializeField] TMP_Text buttonText;
    [SerializeField] PlayerAction cardName;
    int numCardsInDeck = 0;

    private void Start()
    {
        UpdateCounter();
    }
    public void AddCard()
    {
        numCardsInDeck++;
        UpdateCounter();
    }

    void UpdateCounter()
    {
        buttonText.text = cardName.ToString() + " x" + numCardsInDeck;

    }

}
