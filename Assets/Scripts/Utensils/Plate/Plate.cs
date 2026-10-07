using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Plate : NetworkBehaviour
{
    [Header("Plate Settings")]
    public Transform placePoint;
    public float stackHeightOffset = 0.1f;

    private List<NetworkObject> ingredientsOnPlate = new List<NetworkObject>();

    public bool TryAddIngredient(NetworkObject ingredient)
    {
        ingredientsOnPlate.Add(ingredient);

        if (ingredient.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
        }


        int index = ingredientsOnPlate.Count - 1;

        Vector3 targetPos = placePoint.position + (placePoint.up * (index * stackHeightOffset));

        ingredient.transform.SetPositionAndRotation(targetPos, placePoint.rotation);

        if (ingredient.TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(targetPos, placePoint.rotation, ingredient.transform.localScale);
        }

        return true;
    }

    void LateUpdate()
    {
        if (!IsServer) return;


        for (int i = 0; i < ingredientsOnPlate.Count; i++)
        {
            if (ingredientsOnPlate[i] == null || placePoint == null)
                continue;

            Vector3 targetPos = placePoint.position + (placePoint.up * (i * stackHeightOffset));

            ingredientsOnPlate[i].transform.SetPositionAndRotation(targetPos,placePoint.rotation);
        }
    }
}