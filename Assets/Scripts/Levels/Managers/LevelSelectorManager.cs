using Unity.Netcode;
using UnityEngine;

public class LevelSelectorManager : MonoBehaviour
{
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject clientPanel;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    private void Start()
    {
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        if (hostPanel != null) hostPanel.SetActive(isHost);
        if (clientPanel != null) clientPanel.SetActive(!isHost);
    }

    public void LoadLevel1()
    {
        LoadNetworkScene("3.Level1", 1);
    }

    public void LoadLevel2()
    {
        LoadNetworkScene("4.Level2", 2);
    }

    public void LoadLevel3()
    {
        LoadNetworkScene("5.Level3", 3);
    }

    private void LoadNetworkScene(string sceneName, int levelNumber)
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsServer)
            return;

        if (levelNumber > 0 && !LevelProgressManager.IsLevelUnlocked(levelNumber))
        {
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void BackToMainMenu()
    {
        LoadNetworkScene("1.MainMenu", 0);
    }
}