using Unity.Netcode;
using UnityEngine;

public class IngredientCrate : NetworkBehaviour
{
    [Header("Box Settings")]
    public GameObject ingredientPrefab;  
    public NetworkObject GrabIngredient(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (!IsServer) return null;
        if (ingredientPrefab == null) return null;

        GameObject instance = Instantiate(ingredientPrefab, spawnPos, spawnRot);
        NetworkObject netObj = instance.GetComponent<NetworkObject>();
        netObj.Spawn();

        return netObj;
    }
}