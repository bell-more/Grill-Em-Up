
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectorManager : MonoBehaviour
{
    [Header("Host UI")]
    public GameObject hostPanel;

    [Header("Clients UI")]
    public GameObject clientPanel;

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

        if (!LevelProgressManager.IsLevelUnlocked(levelNumber))
        {
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
