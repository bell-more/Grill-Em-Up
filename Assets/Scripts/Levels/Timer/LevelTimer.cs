using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LevelTimer : NetworkBehaviour
{
    public static LevelTimer Instance { get; private set; }

    [Header("UI")]
    public TMP_Text timerText;          
    public TMP_Text countdownText;    
    public TMP_Text waitingText;         

    [Header("Level config")]
    public float durationInMinutes = 4f;
    public float countdownDuration = 3f;

    // variables sincronizadas
    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> countdownRemaining = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isPlaying = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> allPlayersLoaded = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timeRemaining.Value = durationInMinutes * 60f;
            countdownRemaining.Value = countdownDuration;
            isPlaying.Value = false;
            allPlayersLoaded.Value = false;

            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnAllPlayersLoaded;
            }
        }

        isPlaying.OnValueChanged += HandleGameStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        isPlaying.OnValueChanged -= HandleGameStateChanged;

        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnAllPlayersLoaded;
        }
    }

    private void OnAllPlayersLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (IsServer)
        {
            allPlayersLoaded.Value = true;
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            if (!allPlayersLoaded.Value) return;

            if (!isPlaying.Value)
            {
                if (countdownRemaining.Value > 0)
                {
                    countdownRemaining.Value -= Time.deltaTime;
                    if (countdownRemaining.Value <= 0)
                    {
                        countdownRemaining.Value = 0;
                        isPlaying.Value = true;
                    }
                }
            }
            else
            {
                if (timeRemaining.Value > 0)
                {
                    timeRemaining.Value -= Time.deltaTime;
                    if (timeRemaining.Value <= 0)
                    {
                        timeRemaining.Value = 0;
                        TimeUp();
                    }
                }
            }
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining.Value / 60);
            int seconds = Mathf.FloorToInt(timeRemaining.Value % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        if (!isPlaying.Value)
        {
            if (!allPlayersLoaded.Value)
            {
                if (waitingText != null) waitingText.gameObject.SetActive(true);
                if (countdownText != null) countdownText.gameObject.SetActive(false);
            }
            else
            {
                if (waitingText != null) waitingText.gameObject.SetActive(false);
                if (countdownText != null)
                {
                    countdownText.gameObject.SetActive(true);
                    int ceilTime = Mathf.CeilToInt(countdownRemaining.Value);
                    if (ceilTime > 0)
                    {
                        countdownText.text = ceilTime.ToString();
                    }
                }
            }
        }
    }

    private void HandleGameStateChanged(bool previous, bool current)
    {
        if (current == true)
        {
            if (countdownText != null)
            {
                countdownText.text = "START!";
                Invoke(nameof(HideAllUI), 1f);
            }
        }
    }

    private void HideAllUI()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (waitingText != null) waitingText.gameObject.SetActive(false);
    }

    private void TimeUp()
    {
        isPlaying.Value = false;
    }
}