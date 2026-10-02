using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChefInteraction : NetworkBehaviour
{
    public AttachableNode handNode;
    private AttachableBehaviour heldItem = null;
    private NetworkObject heldNetObj = null;

    void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            InteractServerRpc(handNode.transform.position, handNode.transform.rotation);
        }
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc(Vector3 handPos, Quaternion handRot)
    {
        if (heldItem != null)
        {
            DropObject(handPos, handRot);
        }
        else
        {
            TryGrabObject();
        }
    }

    private void TryGrabObject()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 1.5f);

        float minDistance = float.MaxValue;
        AttachableBehaviour closestIngredient = null;

        foreach (var col in nearbyColliders)
        {
            if (col.CompareTag("Ingredient"))
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);

                if (distance < 1.5f && distance < minDistance)
                {
                    minDistance = distance;
                    closestIngredient = col.GetComponentInChildren<AttachableBehaviour>();
                }
            }
        }

        if (closestIngredient != null)
        {
            heldItem = closestIngredient;
            heldNetObj = heldItem.GetComponentInParent<NetworkObject>();
            heldItem.Attach(handNode);
        }
    }

    private void DropObject(Vector3 dropPos, Quaternion dropRot)
    {
        if (heldItem != null)
        {

            heldNetObj.transform.SetPositionAndRotation(dropPos, dropRot);
            heldItem.transform.SetPositionAndRotation(dropPos, dropRot);

            if (heldNetObj.TryGetComponent<NetworkTransform>(out var netTransform))
            {
                netTransform.Teleport(dropPos, dropRot, heldNetObj.transform.localScale);
            }

            heldItem.Detach();
            heldItem = null;
            heldNetObj = null;
        }
    }
}