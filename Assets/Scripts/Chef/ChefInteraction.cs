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

    [Header("Cutting config")]
    public float chopInterval = 0.25f;
    private float chopTimer = 0f;

    void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            InteractServerRpc(handNode.transform.position, handNode.transform.rotation);
        }

        if (Keyboard.current != null && Keyboard.current.jKey.isPressed)
        {
            chopTimer += Time.deltaTime;

            if (chopTimer >= chopInterval)
            {
                chopTimer = 0f;
                TryChopServerRpc();
            }
        }
        else
        {
            chopTimer = 0f;
        }
    }

    private void SetHeldPhysics(NetworkObject netObj, bool held)
    {
        if (netObj == null) return;

        if (netObj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = held;

        if (netObj.TryGetComponent<Collider>(out var col)) col.enabled = !held;
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc(Vector3 handPos, Quaternion handRot)
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactRadius);

        Counter closestCounter = null;
        IngredientCrate closestCrate = null;
        AttachableBehaviour closestIngredientOnFloor = null;

        float minCounterDist = float.MaxValue;
        float minBoxDist = float.MaxValue;
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
            else if (col.TryGetComponent<IngredientCrate>(out var box))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);

                if (dist < minBoxDist)
                {
                    minBoxDist = dist;
                    closestCrate = box;
                }
            }
            else if (col.CompareTag("Ingredient"))
            {
                float distance =Vector3.Distance(transform.position, col.transform.position);

                if (distance < minIngDist)
                {
                    minIngDist = distance;
                    closestIngredientOnFloor = col.GetComponentInChildren<AttachableBehaviour>();
                }
            }
        }

        if (heldItem != null)
        {
            if (closestCounter != null)
            {
                if (!closestCounter.HasItem())
                {
                    if (closestCounter.AcceptsItem(heldNetObj))
                    {
                        closestCounter.PlaceItem(heldNetObj);

                        heldItem.Detach();
                        heldItem = null;
                        heldNetObj = null;
                    }
                    else
                    {
                        Debug.Log("Incorrect object");
                    }
                }
                else
                {
                    if (closestCounter.GetItem().TryGetComponent<FryingPan>(out var pan))
                    {
                        if (pan.TryAddIngredient(heldNetObj))
                        {
                            heldItem.Detach();

                            heldItem = null;
                            heldNetObj = null;
                        }
                        else
                        {
                            Debug.Log("The ingredient can't be fried");
                        }
                    }
                    else if (closestCounter.GetItem().TryGetComponent<Plate>(out var plate))
                    {
                        if (plate.TryAddIngredient(heldNetObj))
                        {
                            heldItem.Detach();

                            heldItem = null;
                            heldNetObj = null;
                        }
                    }
                    else
                    {
                        Debug.Log("Table occupied");
                    }
                }
            }
            else
            {
                DropObjectOnFloor(handPos, handRot);
            }
        }
        else
        {
            if (closestCrate != null)
            {
                NetworkObject spawnedItem = closestCrate.GrabIngredient(handNode.transform.position, handNode.transform.rotation);

                if (spawnedItem != null)
                {
                    heldNetObj = spawnedItem;

                    heldItem = spawnedItem.GetComponentInChildren<AttachableBehaviour>();

                    if (heldItem != null) heldItem.Attach(handNode);

                    SetHeldPhysics(heldNetObj, true);
                }
            }
            else if (closestCounter != null && closestCounter.HasItem())
            {
                NetworkObject itemOnCounter = closestCounter.GetItem();

                if (itemOnCounter.TryGetComponent<FryingPan>(out var pan) && pan.HasIngredient())
                {
                    NetworkObject extractedIngredient = pan.ExtractIngredient();

                    heldNetObj = extractedIngredient;

                    heldItem = extractedIngredient.GetComponentInChildren<AttachableBehaviour>();

                    if (heldItem != null) heldItem.Attach(handNode);

                    SetHeldPhysics(heldNetObj, true);
                }
                else
                {
                    closestCounter.ClearItem();

                    heldNetObj = itemOnCounter;

                    heldItem =
                        itemOnCounter.GetComponent<AttachableBehaviour>();

                    if (heldItem == null)
                    {
                        heldItem = itemOnCounter.GetComponentInChildren<AttachableBehaviour>();
                    }

                    heldNetObj.TryRemoveParent();

                    if (heldItem != null) heldItem.Attach(handNode);

                    SetHeldPhysics(heldNetObj, true);
                }
            }
            else if (closestIngredientOnFloor != null)
            {
                heldItem = closestIngredientOnFloor;

                heldNetObj = heldItem.GetComponentInParent<NetworkObject>();

                heldItem.Attach(handNode);

                SetHeldPhysics(heldNetObj, true);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void TryChopServerRpc()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactRadius);

        CuttingCounter closestCuttingCounter = null;
        float minCounterDist = float.MaxValue;

        foreach (var col in nearbyColliders)
        {
            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;

            directionToTarget.y = 0;

            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle > maxInteractAngle) continue;

            if (col.TryGetComponent<CuttingCounter>(out var cuttingCounter))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);

                if (dist < minCounterDist)
                {
                    minCounterDist = dist;
                    closestCuttingCounter = cuttingCounter;
                }
            }
        }

        if (closestCuttingCounter != null && closestCuttingCounter.HasItem())
        {
            closestCuttingCounter.Chop(this);
        }
    }

    private void DropObjectOnFloor(Vector3 dropPos, Quaternion dropRot)
    {
        heldNetObj.transform.SetPositionAndRotation(dropPos, dropRot);

        heldItem.transform.SetPositionAndRotation(dropPos, dropRot);

        if (heldNetObj.TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(dropPos, dropRot, heldNetObj.transform.localScale);
        }

        heldItem.Detach();

        SetHeldPhysics(heldNetObj, false);

        heldItem = null;
        heldNetObj = null;
    }
}