using Unity.Netcode;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    public void StartHostSession()
    {
        NetworkManager.Singleton.StartHost();
        gameObject.SetActive(false);
    }

    public void StartClientSession()
    {
        NetworkManager.Singleton.StartClient();
        gameObject.SetActive(false);
    }
}
