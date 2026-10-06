using Unity.Netcode;
using UnityEngine;

public class CuttingCounter : Counter
{
    [Header("Audio Settings")]
    public AudioClip chopSound;
    private AudioSource audioSource;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void Chop(ChefInteraction chef)
    {
        if (!IsServer) return;
        if (!HasItem()) return;

        if (heldItem.TryGetComponent<IngredientVisuals>(out var visuals))
        {
            if (visuals.currentStage.Value >= 2) return;

            visuals.AddChop();

            PlayChopSoundClientRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void PlayChopSoundClientRpc()
    {
        if (audioSource != null && chopSound != null)
        {
            audioSource.PlayOneShot(chopSound);
        }
    }
}