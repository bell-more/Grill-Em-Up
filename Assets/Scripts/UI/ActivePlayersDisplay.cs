using UnityEngine;
using TMPro;

public class ActivePlayersDisplay : MonoBehaviour
{
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Transform playerListContainer;

    private void OnEnable()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionChange += RefreshPlayersUI;
            RefreshPlayersUI();
        }
    }

    private void OnDisable()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionChange -= RefreshPlayersUI;
        }
    }

    private void RefreshPlayersUI()
    {
        var session = SessionManager.Instance?.GetSession();
        if (session == null) return;

        foreach (Transform child in playerListContainer) Destroy(child.gameObject);

        foreach (var player in session.Players)
        {
            GameObject slot = Instantiate(playerSlotPrefab, playerListContainer);

            TMP_Text nameText = slot.GetComponentInChildren<TMP_Text>();
            if (nameText != null && player.Properties.TryGetValue("PlayerName", out var nameProp))
            {
                nameText.text = nameProp.Value;
            }

            Transform readyImageIcon = slot.transform.Find("ReadyImage");

            if (readyImageIcon != null)
            {
                bool isReady = false;
                if (player.Properties.TryGetValue("PlayerReady", out var readyProp))
                {
                    isReady = (readyProp.Value != null && (readyProp.Value.Equals("True", System.StringComparison.OrdinalIgnoreCase) || readyProp.Value == "1"));
                }
                readyImageIcon.gameObject.SetActive(isReady);
            }
        }
    }
}