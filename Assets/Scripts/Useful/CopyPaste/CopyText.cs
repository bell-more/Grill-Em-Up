using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class CopyText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomCodeText;

    public void CopyTextToClipboard()
    {
        if (roomCodeText == null) return;

        string code = roomCodeText.text.Trim();

        if (!string.IsNullOrEmpty(code))
        {
            GUIUtility.systemCopyBuffer = code;

            Invoke(nameof(DeselectButton), 0.5f);
        }
    }

    private void DeselectButton()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}