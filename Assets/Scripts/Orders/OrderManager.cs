using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OrderManager : NetworkBehaviour
{
    public NetworkList<int> activeOrders;

    [Header("Orders Config")]
    [SerializeField] private float initialSpawnDelay = 5f;
    [SerializeField] private float timeBetweenOrders = 10f;
    [SerializeField] private int maxActiveOrders = 5;
    [SerializeField] private float orderPatienceTime = 50f;
    [SerializeField] private Recipe[] availableRecipes;

    [Header("Coins")]
    [SerializeField] private int coinsPerSuccess = 50;
    [SerializeField] private int coinsPenaltyWrong = 15;
    [SerializeField] private int coinsPenaltyExpired = 20;

    [Header("UI References")]
    [SerializeField] private Transform ordersContainer;
    [SerializeField] private GameObject orderTicketPrefab;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip newOrderSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip orderExpiredSound;
    [SerializeField] private AudioClip wrongOrderSound;

    private float spawnTimer;

    private List<float> orderTimers = new List<float>();
    private List<GameObject> spawnedTicketObjects = new List<GameObject>();

    private void Awake()
    {
        activeOrders = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            spawnTimer = initialSpawnDelay;
        }

        activeOrders.OnListChanged += HandleOrdersChanged;

        for (int i = 0; i < activeOrders.Count; i++)
        {
            CreateTicketUI(activeOrders[i]);
        }
    }

    public override void OnNetworkDespawn()
    {
        activeOrders.OnListChanged -= HandleOrdersChanged;
        ClearAllTicketsUI();
    }

    private void Update()
    {
        if (!IsServer) return;

        if (LevelTimer.Instance != null && !LevelTimer.Instance.isPlaying.Value) return;

        if (activeOrders.Count < maxActiveOrders)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnNewOrder();
                spawnTimer = timeBetweenOrders;
            }
        }

        for (int i = orderTimers.Count - 1; i >= 0; i--)
        {
            orderTimers[i] -= Time.deltaTime;

            if (orderTimers[i] <= 0f)
            {
                Debug.Log("missed order");
                LevelProgressManager.RegisterMissedOrder();
                PlayResolutionSoundClientRpc(1);

                if (CoinManager.Instance != null) CoinManager.Instance.RemoveCoins(coinsPenaltyExpired);

                activeOrders.RemoveAt(i);
                orderTimers.RemoveAt(i);
            }
        }
    }

    private void SpawnNewOrder()
    {
        if (availableRecipes == null || availableRecipes.Length == 0) return;

        int randomRecipeIndex = UnityEngine.Random.Range(0, availableRecipes.Length);
        activeOrders.Add(randomRecipeIndex);
        orderTimers.Add(orderPatienceTime);
    }

    private void HandleOrdersChanged(NetworkListEvent<int> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<int>.EventType.Add:
                CreateTicketUI(changeEvent.Value);
                if (audioSource != null && newOrderSound != null)
                {
                    audioSource.PlayOneShot(newOrderSound);
                }
                break;

            case NetworkListEvent<int>.EventType.RemoveAt:
                RemoveTicketUI(changeEvent.Index);
                break;

            case NetworkListEvent<int>.EventType.Clear:
                ClearAllTicketsUI();
                break;
        }
    }

    private void CreateTicketUI(int recipeIndex)
    {
        if (ordersContainer == null || orderTicketPrefab == null) return;

        GameObject newTicket = Instantiate(orderTicketPrefab, ordersContainer);
        spawnedTicketObjects.Add(newTicket);

        if (newTicket.TryGetComponent<OrderTicketUI>(out var ticketUI))
        {
            ticketUI.Setup(availableRecipes[recipeIndex], orderPatienceTime);
        }
    }

    private void RemoveTicketUI(int index)
    {
        if (index >= 0 && index < spawnedTicketObjects.Count)
        {
            if (spawnedTicketObjects[index] != null)
            {
                Destroy(spawnedTicketObjects[index]);
            }
            spawnedTicketObjects.RemoveAt(index);
        }
    }

    private void ClearAllTicketsUI()
    {
        foreach (var ticket in spawnedTicketObjects)
        {
            if (ticket != null) Destroy(ticket);
        }
        spawnedTicketObjects.Clear();
    }

    public void SubmitOrder(List<string> submittedIngredients)
    {
        if (!IsServer) return;

        for (int i = 0; i < activeOrders.Count; i++)
        {
            int recipeIndex = activeOrders[i];
            Recipe recipe = availableRecipes[recipeIndex];

            if (AreIngredientsMatching(submittedIngredients, recipe.requiredIngredients))
            {
                Debug.Log("Completed order: " + recipe.recipeName);
                PlayResolutionSoundClientRpc(0);

                if (CoinManager.Instance != null)
                    CoinManager.Instance.AddCoins(coinsPerSuccess);

                activeOrders.RemoveAt(i);

                if (i < orderTimers.Count)
                    orderTimers.RemoveAt(i);

                LevelProgressManager.RegisterSuccessfulOrder();

                return;
            }
        }

        Debug.Log("Wrong order");
        PlayResolutionSoundClientRpc(2);

        if (CoinManager.Instance != null) CoinManager.Instance.RemoveCoins(coinsPenaltyWrong);
    }

    private bool AreIngredientsMatching(List<string> submitted, List<string> required)
    {
        if (submitted.Count != required.Count) return false;

        List<string> tempRequired = new List<string>(required);

        foreach (string ing in submitted)
        {
            if (tempRequired.Contains(ing))
            {
                tempRequired.Remove(ing);
            }
            else
            {
                return false;
            }
        }

        return tempRequired.Count == 0;
    }

    [ClientRpc]
    private void PlayResolutionSoundClientRpc(int resultType)
    {
        if (audioSource == null) return;

        if (resultType == 0 && successSound != null)
        {
            audioSource.PlayOneShot(successSound);
        }
        else if (resultType == 1 && orderExpiredSound != null)
        {
            audioSource.PlayOneShot(orderExpiredSound);
        }
        else if (resultType == 2 && wrongOrderSound != null)
        {
            audioSource.PlayOneShot(wrongOrderSound);
        }
    }
}