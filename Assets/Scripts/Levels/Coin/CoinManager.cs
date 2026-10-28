using Unity.Netcode;
using UnityEngine;
using TMPro;

public class CoinManager : NetworkBehaviour
{
    public static CoinManager Instance { get; private set; }
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private int startingCoins = 0;
    [SerializeField] private bool allowNegativeCoins = false; 

    private NetworkVariable<int> totalCoins = new NetworkVariable<int>(
        value: 0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public int CurrentCoins => totalCoins.Value;

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