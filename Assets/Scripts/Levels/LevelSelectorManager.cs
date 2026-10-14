using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectorManager : MonoBehaviour
{
    [Header("UI del Host")]
    public GameObject hostPanel;

    [Header("UI de los Clientes")]
    public GameObject clientPanel; 

    private void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            hostPanel.SetActive(true);
            clientPanel.SetActive(false);
        }
        else
        {
            hostPanel.SetActive(false);
            clientPanel.SetActive(true);
        }
    }

    public void LoadLevel1()
    {
        LoadNetworkScene("3.Level1"); 
    }

    public void LoadLevel2()
    {
        LoadNetworkScene("4.Level2"); 
    }

    public void LoadLevel3()
    {
        LoadNetworkScene("5.Level3"); 
    }

    //centraliza el cambio de escena por red
    private void LoadNetworkScene(string sceneName)
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
}
