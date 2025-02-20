using UnityEngine;

public class HideUI : MonoBehaviour
{
    [SerializeField] GameObject UI;

    public void SetActiveUI(bool isActive)
    {
        UI.SetActive(isActive);
    }
}
