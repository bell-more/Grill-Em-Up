using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChefSpawnManager : MonoBehaviour
{
    //chefs
    [SerializeField] private NetworkObject[] chefPrefabs;
    [SerializeField] private float spawnHeight = 0.91f;

    //spawn points list
    [SerializeField] private Transform[] spawnPoints;

    private NetworkManager networkManager;
    private readonly Dictionary<ulong, int> assignedSpawnPoints = new Dictionary<ulong, int>();

    private void Awake()
    {
        networkManager = NetworkManager.Singleton;

        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void Start()
    {
        if (networkManager == null || !networkManager.IsServer) return;

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            SpawnChef(clientId);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (networkManager == null || !networkManager.IsServer) return;

        SpawnChef(clientId);
    }

    private void SpawnChef(ulong clientId)
    {
        if (assignedSpawnPoints.ContainsKey(clientId)) return;

        if (chefPrefabs == null || chefPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        int spawnIndex = -1;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null || assignedSpawnPoints.ContainsValue(i)) continue;

            spawnIndex = i;
            break;
        }

        if (spawnIndex == -1 || spawnIndex >= chefPrefabs.Length || chefPrefabs[spawnIndex] == null)
        {
            networkManager.DisconnectClient(clientId);
            return;
        }

        Transform spawnPoint = spawnPoints[spawnIndex];
        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.y = spawnHeight;
        NetworkObject chef = Instantiate(chefPrefabs[spawnIndex], spawnPosition, spawnPoint.rotation);

        assignedSpawnPoints.Add(clientId, spawnIndex);
        chef.SpawnAsPlayerObject(clientId, true);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        assignedSpawnPoints.Remove(clientId);
    }

    private void OnDestroy()
    {
        if (networkManager == null) return;

        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }
}