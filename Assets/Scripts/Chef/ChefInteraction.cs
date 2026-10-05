using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChefInteraction : NetworkBehaviour
{
    public AttachableNode handNode;
    private AttachableBehaviour heldItem = null;
    private NetworkObject heldNetObj = null;

    [Header("Interaction config")]
    public float interactRadius = 1.5f;
    public float maxInteractAngle = 60f;

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
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactRadius);
        Counter closestCounter = null;
        AttachableBehaviour closestIngredientOnFloor = null;
        float minCounterDist = float.MaxValue;
        float minIngDist = float.MaxValue;

        foreach (var col in nearbyColliders)
        {
            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            directionToTarget.y = 0;

            float angle = Vector3.Angle(transform.forward, directionToTarget);
            if (angle > maxInteractAngle) continue;

            if (col.TryGetComponent<Counter>(out var counter))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < minCounterDist)
                {
                    minCounterDist = dist;
                    closestCounter = counter;
                }
            }
            else if (col.CompareTag("Ingredient"))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < minIngDist)
                {
                    minIngDist = dist;
                    closestIngredientOnFloor = col.GetComponentInChildren<AttachableBehaviour>();
                }
            }
        }

        // --- LÓGICA DE INTERACCIÓN ---
        if (heldItem != null)
        {
            if (closestCounter != null)
            {
                if (!closestCounter.HasItem())
                {
                    closestCounter.PlaceItem(heldNetObj);
                    heldItem.Detach();

                    heldItem = null;
                    heldNetObj = null;
                }
                else
                {
                    Debug.Log("Table occupied");
                }
            }
            else
            {
                DropObjectOnFloor(handPos, handRot);
            }
        }
        else
        {
            if (closestCounter != null && closestCounter.HasItem())
            {
                NetworkObject itemOnCounter = closestCounter.GetItem();
                closestCounter.ClearItem();

                heldNetObj = itemOnCounter;
                heldItem = itemOnCounter.GetComponentInChildren<AttachableBehaviour>();

                if (heldNetObj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;

                heldItem.Attach(handNode);
            }
            else if (closestIngredientOnFloor != null)
            {
                heldItem = closestIngredientOnFloor;
                heldNetObj = heldItem.GetComponentInParent<NetworkObject>();
                heldItem.Attach(handNode);
            }
        }
    }

    private void DropObjectOnFloor(Vector3 dropPos, Quaternion dropRot)
    {
        if (heldNetObj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;

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