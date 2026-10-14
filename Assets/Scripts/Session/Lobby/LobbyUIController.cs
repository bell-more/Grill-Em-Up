using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private Toggle myReadyToggle;

    private void OnEnable()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionChange += RefreshLobbyUI;
            RefreshLobbyUI();
        }
    }

    private void OnDisable()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionChange -= RefreshLobbyUI;
        }
    }

    private void Start()
    {
        myReadyToggle.onValueChanged.AddListener(async ready =>
        {
            if (SessionManager.Instance != null) await SessionManager.Instance.SetReadyAsync(ready);
        });
    }

    private void RefreshLobbyUI()
    {
        var session = SessionManager.Instance?.GetSession();
        if (session == null) return;

        roomCodeText.text = session.Code;

        foreach (Transform child in playerListContainer) Destroy(child.gameObject);

        foreach (var player in session.Players)
        {
            GameObject slot = Instantiate(playerSlotPrefab, playerListContainer);

            TMP_Text nameText = slot.GetComponentInChildren<TMP_Text>();
            Transform readyImageIcon = slot.transform.Find("ReadyImage");

            if (player.Properties.TryGetValue("PlayerName", out var nameProp)) nameText.text = nameProp.Value;

            bool isReady = false;
            if (player.Properties.TryGetValue("PlayerReady", out var readyProp))
            {
                isReady = (readyProp.Value != null && (readyProp.Value.Equals("True", System.StringComparison.OrdinalIgnoreCase) || readyProp.Value == "1"));
            }

            if (player.Id == session.CurrentPlayer.Id) myReadyToggle.SetIsOnWithoutNotify(isReady);

            if (readyImageIcon != null) readyImageIcon.gameObject.SetActive(isReady);
        }
    }
}