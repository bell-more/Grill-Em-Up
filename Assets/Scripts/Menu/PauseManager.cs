using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : NetworkBehaviour
{
    //oanels
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject controlsPanel;

    //scenes
    [SerializeField] private string levelSelectorScene = "2.LevelSelector";
    [SerializeField] private string mainMenuScene = "1.MainMenu";

    private NetworkVariable<bool> isPaused = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        isPaused.OnValueChanged += OnPauseChanged;
        ApplyPauseState(isPaused.Value);
    }

    public override void OnNetworkDespawn()
    {
        isPaused.OnValueChanged -= OnPauseChanged;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!IsClient) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused.Value)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        SetPauseServerRpc(true);
    }

    public void ResumeGame()
    {
        SetPauseServerRpc(false);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetPauseServerRpc(bool paused)
    {
        isPaused.Value = paused;
    }

    private void OnPauseChanged(bool previous, bool current)
    {
        ApplyPauseState(current);
    }

    private void ApplyPauseState(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
            pausePanel.SetActive(paused);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void OpenControls()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(true);
    }

    public void BackToPause()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }

    public void GoToLevelSelector()
    {
        LoadSceneServerRpc(levelSelectorScene);
    }

    public async void GoToMainMenu()
    {
        Time.timeScale = 1;
        if (SessionManager.Instance != null && SessionManager.Instance.GetSession() != null)
        {
            await SessionManager.Instance.LeaveSessionAsync();
            return;
        }
        await SceneManager.LoadSceneAsync(mainMenuScene);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void LoadSceneServerRpc(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return;
        }

        isPaused.Value = false;
        Time.timeScale = 1f;

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single );
    }
}
