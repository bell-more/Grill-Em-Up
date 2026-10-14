

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

    private Component FindInteractionTarget()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactRadius);

        Component selectedTarget = null;
        float bestAngle = float.MaxValue;
        float bestDistance = float.MaxValue;

        foreach (var col in nearbyColliders)
        {
            if (heldNetObj != null && col.GetComponentInParent<NetworkObject>() == heldNetObj) continue;

            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            directionToTarget.y = 0;

            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle > maxInteractAngle) continue;

            Component objectToInteract = null;

            if (col.TryGetComponent<Counter>(out var counter)) objectToInteract = counter;
            else if (col.TryGetComponent<IngredientCrate>(out var box)) objectToInteract = box;
            else if (col.TryGetComponent<PlateHolder>(out var plateBox)) objectToInteract = plateBox;
            else if (col.TryGetComponent<TrashBin>(out var trashBin)) objectToInteract = trashBin;
            else if (col.CompareTag("Ingredient")) objectToInteract = col.GetComponentInChildren<AttachableBehaviour>();

            if (objectToInteract == null) continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);

            bool isBetterTarget = angle < bestAngle - 0.5f || (Mathf.Abs(angle - bestAngle) <= 0.5f && distance < bestDistance);

            if (!isBetterTarget) continue;

            selectedTarget = objectToInteract;
            bestAngle = angle;
            bestDistance = distance;
        }

        return selectedTarget;
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc(Vector3 handPos, Quaternion handRot)
    {
        Component target = FindInteractionTarget();

        Counter closestCounter = target as Counter;
        IngredientCrate closestCrate = target as IngredientCrate;
        PlateHolder closestPlateCrate = target as PlateHolder;
        TrashBin closestTrashBin = target as TrashBin;
        AttachableBehaviour closestIngredientOnFloor = target as AttachableBehaviour;

        if (heldItem != null)
        {
            if (closestTrashBin != null)
            {
                if (closestTrashBin.ThrowAway(heldNetObj))
                {
                    heldItem = null;
                    heldNetObj = null;
                }
            }
            else if (closestPlateCrate != null && heldNetObj.TryGetComponent<Plate>(out var plateComponent))
            {
                bool plateIsEmpty =
                    plateComponent.GetBottomBun() == null &&
                    plateComponent.GetMiddleIngredients().Count == 0 &&
                    plateComponent.GetTopBun() == null;

                if (!plateIsEmpty)
                    return;

                if (!closestPlateCrate.ReturnPlate(heldNetObj))
                    return;

                heldItem.Detach();
                heldNetObj.Despawn(true);

                heldItem = null;
                heldNetObj = null;
            }
            else if (closestCounter != null)
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
                        if (heldNetObj.TryGetComponent<Plate>(out _))
                        {
                            return;
                        }

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
                        if (heldNetObj.TryGetComponent<Plate>(out _))
                        {
                            Debug.Log("A plate cannot be added to another plate");
                            return;
                        }

                        if (plate.TryAddIngredient(heldNetObj, heldItem))
                        {
                            heldItem = null;
                            heldNetObj = null;
                        }
                        else
                        {
                            Debug.Log("The ingredient can't be added to the plate");
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
                NetworkObject spawnedItem = closestCrate.GrabIngredient(
                    handNode.transform.position,
                    handNode.transform.rotation
                );

                if (spawnedItem != null)
                {
                    heldNetObj = spawnedItem;
                    heldItem = spawnedItem.GetComponentInChildren<AttachableBehaviour>();

                    if (heldItem != null)
                        heldItem.Attach(handNode);

                    SetHeldPhysics(heldNetObj, true);
                }
            }
            else if (closestPlateCrate != null)
            {
                NetworkObject spawnedPlate = closestPlateCrate.GrabPlate(
                    handNode.transform.position,
                    handNode.transform.rotation
                );

                if (spawnedPlate != null)
                {
                    heldNetObj = spawnedPlate;
                    heldItem = spawnedPlate.GetComponent<AttachableBehaviour>();

                    if (heldItem == null)
                        heldItem = spawnedPlate.GetComponentInChildren<AttachableBehaviour>();

                    if (heldItem != null)
                        heldItem.Attach(handNode);

                    SetHeldPhysics(heldNetObj, true);
                }
                else
                {
                    Debug.Log("No plates left");
                }
            }
            else if (closestCounter != null && closestCounter.HasItem())
            {
                NetworkObject itemOnCounter = closestCounter.GetItem();

                if (itemOnCounter.TryGetComponent<FryingPan>(out var pan) && pan.HasIngredient())
                {
                    NetworkObject extractedIngredient = pan.ExtractIngredient();

                    if (extractedIngredient == null)
                        return;

                    heldNetObj = extractedIngredient;
                    heldItem = extractedIngredient.GetComponentInChildren<AttachableBehaviour>();

                    if (heldItem != null)
                        heldItem.Attach(handNode);

                    SetHeldPhysics(heldNetObj, true);
                }
                else
                {
                    closestCounter.ClearItem();

                    heldNetObj = itemOnCounter;
                    heldItem = itemOnCounter.GetComponent<AttachableBehaviour>();

                    if (heldItem == null)
                        heldItem = itemOnCounter.GetComponentInChildren<AttachableBehaviour>();

                    heldNetObj.TryRemoveParent();

                    if (heldItem != null)
                        heldItem.Attach(handNode);

                    SetHeldPhysics(heldNetObj, true);
                }
            }
            else if (closestIngredientOnFloor != null)
            {
                heldItem = closestIngredientOnFloor;
                heldNetObj = heldItem.GetComponentInParent<NetworkObject>();

                if (heldNetObj == null)
                {
                    heldItem = null;
                    return;
                }

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
        float bestAngle = float.MaxValue;
        float bestDistance = float.MaxValue;

        foreach (var col in nearbyColliders)
        {
            if (!col.TryGetComponent<CuttingCounter>(out var cuttingCounter))
                continue;

            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            directionToTarget.y = 0;

            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle > maxInteractAngle)
                continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);

            bool isBetterTarget =
                angle < bestAngle - 0.5f ||
                (Mathf.Abs(angle - bestAngle) <= 0.5f && distance < bestDistance);

            if (!isBetterTarget)
                continue;

            closestCuttingCounter = cuttingCounter;
            bestAngle = angle;
            bestDistance = distance;
        }

        if (closestCuttingCounter != null && closestCuttingCounter.HasItem())
            closestCuttingCounter.Chop(this);
    }

    private void DropObjectOnFloor(Vector3 dropPos, Quaternion dropRot)
    {
        if (heldNetObj == null || heldItem == null)
            return;

        heldNetObj.transform.SetPositionAndRotation(dropPos, dropRot);
        heldItem.transform.SetPositionAndRotation(dropPos, dropRot);

        if (heldNetObj.TryGetComponent<NetworkTransform>(out var netTransform))
            netTransform.Teleport(dropPos, dropRot, heldNetObj.transform.localScale);

        heldItem.Detach();
        SetHeldPhysics(heldNetObj, false);

        heldItem = null;
        heldNetObj = null;
    }
}
