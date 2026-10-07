using Unity.Netcode;
using UnityEngine;

public class Stove : Counter
{
    public GameObject fireVisuals;

    void Update()
    {
        if (!IsServer) return;

        bool isCooking = false;

        if (HasItem())
        {
            if (heldItem.TryGetComponent<FryingPan>(out var pan))
            {
                pan.Cook(Time.deltaTime);
                isCooking = true;
            }
        }

        if (fireVisuals != null && fireVisuals.activeSelf != isCooking)
        {
            fireVisuals.SetActive(isCooking);
        }
    }
    public override bool AcceptsItem(NetworkObject item)
    {
        return item.TryGetComponent<FryingPan>(out var pan);
    }
}