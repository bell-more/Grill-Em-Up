using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class FryingPan : NetworkBehaviour
{
    [Header("Pan settings")]
    public Transform placePoint;

    [Header("Cooking times")]
    public float timeToCook = 30f;
    public float timeToBurn = 40f;

    public NetworkVariable<float> currentCookingTime = new NetworkVariable<float>(0f);
    private NetworkObject ingredientInside;


    public bool TryAddIngredient(NetworkObject ingredient)
    {
        if (ingredientInside != null) return false;
        if (ingredient.TryGetComponent<IngredientVisuals>(out var visuals) && !visuals.canBeFried) return false;

        ingredientInside = ingredient;

        if (ingredient.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

        ingredient.TrySetParent(placePoint, false);

        ingredient.transform.position = placePoint.position;
        ingredient.transform.rotation = placePoint.rotation;

        if (ingredient.TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(placePoint.position, placePoint.rotation, ingredient.transform.localScale);
        }

        currentCookingTime.Value = 0f;
        return true;
    }

    public NetworkObject ExtractIngredient()
    {
        if (ingredientInside == null)
            return null;

        NetworkObject extracted = ingredientInside;

        ingredientInside = null;
        currentCookingTime.Value = 0f;

        extracted.TryRemoveParent();

        if (extracted.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = false;

        return extracted;
    }

    public void Cook(float deltaTime)
    {
        if (ingredientInside == null) return;

        if (ingredientInside.TryGetComponent<IngredientVisuals>(out var visuals))
        {
            if (visuals.currentStage.Value >= 2) return;

            currentCookingTime.Value += deltaTime;

            if (visuals.currentStage.Value == 0 && currentCookingTime.Value >= timeToCook)
            {
                visuals.currentStage.Value = 1;
            }
            else if (visuals.currentStage.Value == 1 && currentCookingTime.Value >= timeToBurn)
            {
                visuals.currentStage.Value = 2;
            }
        }
    }

    public bool HasIngredient()
    {
        return ingredientInside != null;
    }
}