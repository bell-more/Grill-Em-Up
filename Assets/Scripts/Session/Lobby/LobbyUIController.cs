using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    public TMP_Text roomCodeText;
    public GameObject playerSlotPrefab;
    public Transform playerListContainer;
    public Toggle myReadyToggle;

    private void OnEnable()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionChange += RefreshLobbyUI;

            // ESTA ES LA LÍNEA QUE ARREGLA LA PANTALLA DE CARGA:
            // Dibuja la lista al instante sin esperar el ping de la red
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

        roomCodeText.text = $"Código de Sala: {session.Code}";

        foreach (Transform child in playerListContainer) Destroy(child.gameObject);

        foreach (var player in session.Players)
        {
            GameObject slot = Instantiate(playerSlotPrefab, playerListContainer);

            TMP_Text nameText = slot.GetComponentInChildren<TMP_Text>();
            Transform readyImageIcon = slot.transform.Find("ReadyImage");

            if (player.Properties.TryGetValue("PlayerName", out var nameProp))
                nameText.text = nameProp.Value;

            bool isReady = false;
            if (player.Properties.TryGetValue("PlayerReady", out var readyProp))
            {
                // Usamos la validación robusta que evita errores si el valor es nulo
                isReady = (readyProp.Value != null &&
                          (readyProp.Value.Equals("True", System.StringComparison.OrdinalIgnoreCase) ||
                           readyProp.Value == "1"));
            }

            // Actualiza tu propio toggle general
            if (player.Id == session.CurrentPlayer.Id)
                myReadyToggle.SetIsOnWithoutNotify(isReady);

            // Prende o apaga la imagen de "Listo" en el prefab
            if (readyImageIcon != null)
                readyImageIcon.gameObject.SetActive(isReady);
        }
    }
}