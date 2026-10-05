using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Counter : NetworkBehaviour
{
    [Header("Settings")]
    public Transform placePoint;

    private NetworkObject itemOnCounter = null;

    public bool HasItem()
    {
        return itemOnCounter != null;
    }

    public NetworkObject GetItem()
    {
        return itemOnCounter;
    }

    public void PlaceItem(NetworkObject item)
    {
        itemOnCounter = item;

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

    public void ClearItem()
    {
        itemOnCounter = null;
    }
}