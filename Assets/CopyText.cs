using UnityEngine;
using TMPro;

public class CopyText : MonoBehaviour
{
    [SerializeField] TMP_Text textToCopy;

    public void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = textToCopy.text;
    }
}
