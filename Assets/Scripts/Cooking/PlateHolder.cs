using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlateHolder : NetworkBehaviour
{
    [SerializeField] private GameObject platePrefab;
    [SerializeField] private int maxPlates = 5;
    [SerializeField] private List<GameObject> visualPlatesInStack;

    private NetworkVariable<int> currentPlates = new NetworkVariable<int>(
        value: 5,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private HashSet<ulong> dispensedPlates = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentPlates.Value = maxPlates;
        }

        UpdateVisualStack(0, currentPlates.Value);
        currentPlates.OnValueChanged += UpdateVisualStack;
    }

    public override void OnNetworkDespawn()
    {
        currentPlates.OnValueChanged -= UpdateVisualStack;

        if (!IsServer) return;

        foreach (ulong plateId in dispensedPlates)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(plateId, out NetworkObject plate))
            {
                if (plate != null && plate.IsSpawned) plate.Despawn(true);
            }
        }

        dispensedPlates.Clear();
    }

    public NetworkObject GrabPlate(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (!IsServer) return null;
        if (platePrefab == null) return null;
        if (currentPlates.Value <= 0) return null;

        GameObject instance = Instantiate(platePrefab, spawnPos, spawnRot);
        NetworkObject netObj = instance.GetComponent<NetworkObject>();

        if (netObj == null) return null;

        netObj.Spawn();

        dispensedPlates.Add(netObj.NetworkObjectId);
        currentPlates.Value--;

        return netObj;
    }

    public bool ReturnPlate(NetworkObject plate)
    {
        if (!IsServer) return false;
        if (plate == null) return false;

        if (!dispensedPlates.Contains(plate.NetworkObjectId))
        {
            return false;
        }

        dispensedPlates.Remove(plate.NetworkObjectId);

        if (currentPlates.Value < maxPlates)
        {
            currentPlates.Value++;
        }

        return true;
    }

    private void UpdateVisualStack(int previousValue, int newValue)
    {
        for (int i = 0; i < visualPlatesInStack.Count; i++)
        {
            if (visualPlatesInStack[i] != null)
            {
                visualPlatesInStack[i].SetActive(i < newValue);
            }
        }
    }
}