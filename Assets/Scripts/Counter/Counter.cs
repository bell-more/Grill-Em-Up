using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Counter : NetworkBehaviour
{
    [SerializeField] private Transform placePoint;
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
            netTransform.Teleport(placePoint.position, placePoint.rotation, item.transform.localScale);
        }
        else
        {
            item.transform.position = placePoint.position;
            item.transform.rotation = placePoint.rotation;
        }
    }

    public virtual void ClearItem()
    {
        heldItem = null;
    }

    public virtual bool AcceptsItem(NetworkObject item)
    {
        return true;
    }
}