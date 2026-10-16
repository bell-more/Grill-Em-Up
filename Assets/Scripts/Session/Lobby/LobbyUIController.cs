using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text roomCodeText;
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

        if (roomCodeText != null) roomCodeText.text = session.Code;

        if (myReadyToggle != null && session.CurrentPlayer != null)
        {
            bool isReady = false;
            if (session.CurrentPlayer.Properties.TryGetValue("PlayerReady", out var readyProp))
            {
                isReady = (readyProp.Value != null && (readyProp.Value.Equals("True", System.StringComparison.OrdinalIgnoreCase) || readyProp.Value == "1"));
            }
            myReadyToggle.SetIsOnWithoutNotify(isReady);
        }
    }
}