using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Counter : NetworkBehaviour
{
    [Header("Settings")]
    public Transform holdPoint;

    protected NetworkObject heldItem = null;

    public bool HasItem()
    {
        return heldItem != null;
    }

    public NetworkObject GetItem()
    {
        return heldItem;
    }

    public virtual void PlaceItem(NetworkObject item)
    {
        heldItem = item;

        if (item.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
        }

        item.TrySetParent(transform, false);

        if (item.TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(holdPoint.position, holdPoint.rotation, item.transform.localScale);
        }
        else
        {
            item.transform.position = holdPoint.position;
            item.transform.rotation = holdPoint.rotation;
        }
    }

    public virtual void ClearItem()
    {
        heldItem = null;
    }
}