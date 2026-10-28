using Unity.Netcode;
using UnityEngine;

public class IngredientCrate : NetworkBehaviour
{
    [SerializeField] private GameObject ingredientPrefab;
    public NetworkObject GrabIngredient(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (!IsServer) return null;
        if (ingredientPrefab == null) return null;

        GameObject instance = Instantiate(ingredientPrefab, spawnPos, spawnRot);
        NetworkObject netObj = instance.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        return netObj;
    }

    public bool IsCorrectIngredient(NetworkObject itemNetObj)
    {
        if (itemNetObj == null || ingredientPrefab == null) return false;
        return itemNetObj.gameObject.name.Replace("(Clone)", "").Trim() == ingredientPrefab.name.Replace("(Clone)", "").Trim();
    }
}