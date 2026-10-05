using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Plate : NetworkBehaviour
{
    public List<string> currentIngredients = new List<string>();

    [Header("Visual Stacking")]
    public Transform stackRoot; // Un objeto vacío en el centro de tu plato
    public float stackOffset = 0.2f;
    private float currentStackHeight = 0f;

    [Rpc(SendTo.Server)]
    public void AddIngredientServerRpc(NetworkObjectReference ingredientRef, string ingredientName)
    {
        if (ingredientRef.TryGet(out NetworkObject ingredientNetObj))
        {
            currentIngredients.Add(ingredientName);

            if (ingredientNetObj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }

            ingredientNetObj.TrySetParent(stackRoot, false);

            Vector3 targetLocalPos = new Vector3(0, currentStackHeight, 0);
            ingredientNetObj.transform.localPosition = targetLocalPos;
            ingredientNetObj.transform.localRotation = Quaternion.identity;

            currentStackHeight += stackOffset;
        }
    }

    public void ClearPlate()
    {
        currentIngredients.Clear();
        currentStackHeight = 0f;

        foreach (Transform child in stackRoot)
        {
            if (child.TryGetComponent<NetworkObject>(out var netObj))
            {
                netObj.Despawn();
            }
        }
    }
}