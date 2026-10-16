using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject loadingGamePanel;
    [SerializeField] private GameObject joinGamePanel;
    [SerializeField] private GameObject createGamePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject connectionLostPanel;

    [Header("Error texts")]
    [SerializeField]private TextMeshProUGUI errorText;

    [Header("UGS")]
    [SerializeField] private TMP_InputField joinCodeInput;

    [Header("Audio")]
    [SerializeField] private AudioMixerSnapshot audioMixerSnapshot;

    private void Awake()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.MainMenuRef = this;
        }
        if (SessionManager.showConnectionLostOnLoad)
        {
            SessionManager.showConnectionLostOnLoad = false;
            ConnectionLostPanel();
        }
    }

    public async void CreateGame()
    {
        if (mainMenuPanel!= null) mainMenuPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (loadingGamePanel != null) loadingGamePanel.SetActive(true);

        string playerName = "";

        if (SessionManager.Instance != null)
        {
            await SessionManager.Instance.InitializeLobbyAsync(playerName, 4, false, MatchType.CookingCoop);
        }
    }

    public async void JoinGame()
    {
        string joinCode = joinCodeInput != null ? joinCodeInput.text : "";
        string playerName = "";

        if (!string.IsNullOrEmpty(joinCode) && joinCode.Length == 6 && SessionManager.Instance != null)
        {
            if (joinGamePanel != null) joinGamePanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (loadingGamePanel != null) loadingGamePanel.SetActive(true);

            await SessionManager.Instance.JoinLobbyWithCode(joinCode, playerName);
        }
        else
        {
            Debug.LogWarning("Wrong code");
            JoinError();
        }
    }

    public void ShowLobbyPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (loadingGamePanel != null) loadingGamePanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (createGamePanel != null) createGamePanel.SetActive(true);
    }

    public void Play()
    {
        TransitionTo(audioMixerSnapshot);
        Invoke(nameof(disableMainMenu), 1f);
        if (loadingGamePanel != null) loadingGamePanel.SetActive(true);
        Invoke(nameof(LoadScene),3f);
    }

    void TransitionTo(AudioMixerSnapshot snap, float fade = 1f)
    {
        snap.TransitionTo(fade);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void disableMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    public void LoadScene()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void JoinGamePanel()
    {
        if (joinGamePanel != null) joinGamePanel.SetActive(true);
    }

    public void Settings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void Info()
    {
        if (infoPanel != null) infoPanel.SetActive(true);
    }

    public void JoinError()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (loadingGamePanel != null) loadingGamePanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(true);
        if (errorText != null) errorText.gameObject.SetActive(true);

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        CancelInvoke(nameof(HideErrorText));
        Invoke(nameof(HideErrorText), 3f);
    }

    public void ShowLoadingScreen()
    {

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (createGamePanel != null) createGamePanel.SetActive(false); 
        if (loadingGamePanel != null) loadingGamePanel.SetActive(true);
    }

    private void HideErrorText()
    {
        if (errorText != null) errorText.gameObject.SetActive(false); ;
    }

    public async void ClosePanel()
    {
        if (SessionManager.Instance != null && SessionManager.Instance.GetSession() != null)
        {
            await SessionManager.Instance.LeaveSessionAsync();
        }

        if (infoPanel != null) infoPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (joinGamePanel != null) joinGamePanel.SetActive(false);
        if (createGamePanel != null) createGamePanel.SetActive(false);
        if (connectionLostPanel != null) connectionLostPanel.SetActive(false);
        if (loadingGamePanel != null) loadingGamePanel.SetActive(false);

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void ConnectionLostPanel()
    {
        if (createGamePanel != null) createGamePanel.SetActive(false);
        if (loadingGamePanel != null) loadingGamePanel.SetActive(false);
        if (connectionLostPanel != null) connectionLostPanel.SetActive(true);
    }
}