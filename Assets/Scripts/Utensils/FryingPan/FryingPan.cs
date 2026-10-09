using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class FryingPan : NetworkBehaviour
{
    [Header("Pan settings")]
    public Transform placePoint;

    [Header("Cooking times")]
    public float timeToCook = 20f;
    public float timeToBurn = 30f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fryingSound;

    private NetworkObject ingredientInside;

    // Esta variable sincroniza el sonido en la red para todos los jugadores
    public NetworkVariable<bool> isFrying = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Cuando isFrying cambia, prendemos o apagamos el sonido
        isFrying.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal && audioSource != null && fryingSound != null)
            {
                audioSource.clip = fryingSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            else if (!newVal && audioSource != null)
            {
                audioSource.Stop();
            }
        };
    }

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

        // Avisamos a la red que la sartén está friendo (arranca el sonido)
        if (IsServer) isFrying.Value = true;

        // ¡YA NO PONEMOS EL TIEMPO EN CERO! La hamburguesa mantiene su progreso.
        return true;
    }

    public NetworkObject ExtractIngredient()
    {
        if (ingredientInside == null) return null;

        NetworkObject extracted = ingredientInside;
        ingredientInside = null;

        // Avisamos a la red que la sartén está vacía (corta el sonido)
        if (IsServer) isFrying.Value = false;

        extracted.TryRemoveParent();

        if (extracted.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = false;

        return extracted;
    }

    public void Cook(float deltaTime)
    {
        if (!IsServer || ingredientInside == null) return;

        if (ingredientInside.TryGetComponent<IngredientVisuals>(out var visuals))
        {
            if (visuals.currentStage.Value >= 2) return; // Si ya se quemó, deja de cocinar

            // Le sumamos el tiempo AL INGREDIENTE, no a la sartén
            visuals.currentCookingTime.Value += deltaTime;

            // Evaluamos usando la memoria de la hamburguesa
            if (visuals.currentStage.Value == 0 && visuals.currentCookingTime.Value >= timeToCook)
            {
                visuals.currentStage.Value = 1; // Cocinado
            }
            else if (visuals.currentStage.Value == 1 && visuals.currentCookingTime.Value >= timeToBurn)
            {
                visuals.currentStage.Value = 2; // Quemado
            }
        }
    }

    public bool HasIngredient()
    {
        return ingredientInside != null;
    }
}