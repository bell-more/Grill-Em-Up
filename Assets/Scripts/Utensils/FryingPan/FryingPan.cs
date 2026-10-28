using Unity.Netcode;
using UnityEngine;

public class FryingPan : Counter
{
    public float timeToCook = 10f;
    public float timeToBurn = 20f;

    //audio
    public AudioSource audioSource;
    public AudioClip fryingSound;

    public NetworkVariable<bool> isFrying = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

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

    private void Update()
    {
        if (!IsServer || !HasItem()) return;

        if (heldItem.TryGetComponent<IngredientVisuals>(out var visuals))
        {
            if (visuals.currentStage.Value >= 2) return;

            visuals.currentCookingTime.Value += Time.deltaTime;

            if (visuals.currentStage.Value == 0 && visuals.currentCookingTime.Value >= timeToCook)
            {
                visuals.currentStage.Value = 1;
            }
            else if (visuals.currentStage.Value == 1 && visuals.currentCookingTime.Value >= timeToBurn)
            {
                visuals.currentStage.Value = 2;
            }
        }
    }

    public override bool AcceptsItem(NetworkObject item)
    {
        if (item.TryGetComponent<Plate>(out _)) return false;
        if (item.TryGetComponent<IngredientVisuals>(out var visuals) && !visuals.canBeFried) return false;

        return true;
    }

    public override void PlaceItem(NetworkObject item)
    {
        base.PlaceItem(item);
        if (IsServer) isFrying.Value = true;
    }

    public override void ClearItem()
    {
        base.ClearItem(); 
        if (IsServer) isFrying.Value = false;
    }
}