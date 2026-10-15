using Unity.Netcode;
using UnityEngine;
using TMPro;

public class CoinManager : NetworkBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Interfaz (UI)")]
    [SerializeField] private TMP_Text coinText;

    [Header("Configuración")]
    [SerializeField] private int startingCoins = 0;
    [SerializeField] private bool allowNegativeCoins = false;
    public int CurrentCoins => totalCoins.Value;

    private NetworkVariable<int> totalCoins = new NetworkVariable<int>(
        value: 0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            totalCoins.Value = startingCoins;
        }

        UpdateCoinUI(0, totalCoins.Value);

        totalCoins.OnValueChanged += UpdateCoinUI;
    }

    public override void OnNetworkDespawn()
    {
        totalCoins.OnValueChanged -= UpdateCoinUI;
    }

    public void AddCoins(int amount)
    {
        if (IsServer)
        {
            totalCoins.Value += amount;
        }
    }

    public void RemoveCoins(int amount)
    {
        if (IsServer)
        {
            totalCoins.Value -= amount;

            if (!allowNegativeCoins && totalCoins.Value < 0)
            {
                totalCoins.Value = 0;
            }
        }
    }

    private void UpdateCoinUI(int previousValue, int newValue)
    {
        if (coinText != null)
        {
            coinText.text = newValue.ToString("0");
        }
    }
}