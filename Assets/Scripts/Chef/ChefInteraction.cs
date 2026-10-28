using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChefInteraction : NetworkBehaviour
{
    [SerializeField] private AttachableNode handNode;

    private AttachableBehaviour heldItem = null;
    private NetworkObject heldNetObj = null;
    private Animator animator;

    //interaction
    [SerializeField] private float interactRadius = 1.5f;
    [SerializeField] private float maxInteractAngle = 60f;
    [SerializeField] private float chopInterval = 0.25f;
    private float chopTimer = 0f;
    [SerializeField] private float interactCooldown = 0.25f;
    private float interactCooldownTimer = 0f;

    //audio
    [SerializeField] private AudioClip interactSound;
    private AudioSource audioSource;
    //anim
    private readonly NetworkVariable<bool> isChopping = new NetworkVariable<bool>(false);
    private bool wasChoppingInput;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        isChopping.OnValueChanged += OnChoppingChanged;

        if (animator != null) animator.SetBool("Chop", isChopping.Value);
    }

    public override void OnNetworkDespawn()
    {
        isChopping.OnValueChanged -= OnChoppingChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (LevelTimer.Instance != null && !LevelTimer.Instance.isPlaying.Value) return;

        if (interactCooldownTimer > 0f)
        {
            interactCooldownTimer -= Time.deltaTime;
        }

        HandleInteractionInput();
        HandleChoppingInput();
    }

    private void HandleInteractionInput()
    {
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            if (interactCooldownTimer > 0f) return;
            interactCooldownTimer = interactCooldown;

            bool canInteract = heldItem != null || FindInteractionTarget() != null;

            if (canInteract)
            {
                if (interactSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(interactSound);
                }

                if (animator != null)
                {
                    animator.SetTrigger("PickUp");
                }

                SyncPickUpServerRpc();
            }

            InteractServerRpc(handNode.transform.position, handNode.transform.rotation);
        }
    }

    [Rpc(SendTo.Server)]
    private void SyncPickUpServerRpc()
    {
        SyncPickUpClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void SyncPickUpClientRpc()
    {
        if (IsOwner) return;

        if (animator != null) animator.SetTrigger("PickUp");
    }

    private void HandleChoppingInput()
    {
        bool isChoppingInput = Keyboard.current != null && Keyboard.current.jKey.isPressed;

        if (isChoppingInput)
        {
            if (animator != null && !wasChoppingInput)
            {
                animator.SetBool("Chop", true);
            }

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

        if (wasChoppingInput && !isChoppingInput)
        {
            if (animator != null) animator.SetBool("Chop", false);
            StopChopServerRpc();
        }

        wasChoppingInput = isChoppingInput;
    }

    private void OnChoppingChanged(bool previousValue, bool newValue)
    {
        if (IsOwner) return;

        if (animator != null) animator.SetBool("Chop", newValue);
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
        float bestDistance = float.MaxValue;

        foreach (var col in nearbyColliders)
        {
            if (heldNetObj != null && col.GetComponentInParent<NetworkObject>() == heldNetObj) continue;

            Vector3 closestPoint = col.bounds.ClosestPoint(transform.position);

            Vector3 directionToTarget = (closestPoint - transform.position).normalized;
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

            float distance = Vector3.Distance(transform.position, closestPoint);

            if (distance < bestDistance)
            {
                selectedTarget = objectToInteract;
                bestDistance = distance;
            }
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
            HandleHeldItemInteraction(closestCounter, closestCrate, closestPlateCrate, closestTrashBin);
        }
        else
        {
            HandlePickupInteraction(closestCounter, closestCrate, closestPlateCrate, closestIngredientOnFloor);
        }
    }

    private void HandleHeldItemInteraction(Counter closestCounter, IngredientCrate closestCrate, PlateHolder closestPlateCrate, TrashBin closestTrashBin)
    {
        if (closestCrate != null && closestCrate.IsCorrectIngredient(heldNetObj))
        {
            Debug.Log("correct");
            heldNetObj.Despawn(true);
            heldItem = null;
            heldNetObj = null;
            return;
        }

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
            bool plateIsEmpty = plateComponent.GetBottomBun() == null && plateComponent.GetMiddleIngredients().Count == 0 && plateComponent.GetTopBun() == null;

            if (!plateIsEmpty) return;

            if (!closestPlateCrate.ReturnPlate(heldNetObj)) return;

            heldItem.Detach();
            heldNetObj.Despawn(true);
            heldItem = null;
            heldNetObj = null;
        }
        else if (closestCounter != null)
        {
            HandleCounterInteraction(closestCounter);
        }
    }

    private void HandleCounterInteraction(Counter closestCounter)
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
        }
        else
        {
            NetworkObject itemOnCounter = closestCounter.GetItem();

            if (itemOnCounter.TryGetComponent<Plate>(out var plate))
            {
                if (heldNetObj.TryGetComponent<Plate>(out _)) return;

                if (plate.TryAddIngredient(heldNetObj, heldItem))
                {
                    heldItem = null;
                    heldNetObj = null;
                }
            }
        }
    }

    private void HandlePickupInteraction(Counter closestCounter, IngredientCrate closestCrate, PlateHolder closestPlateCrate, AttachableBehaviour closestIngredientOnFloor)
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
        else if (closestPlateCrate != null)
        {
            NetworkObject spawnedPlate = closestPlateCrate.GrabPlate(handNode.transform.position, handNode.transform.rotation);

            if (spawnedPlate != null)
            {
                heldNetObj = spawnedPlate;
                heldItem = spawnedPlate.GetComponent<AttachableBehaviour>();

                if (heldItem == null) heldItem = spawnedPlate.GetComponentInChildren<AttachableBehaviour>();

                if (heldItem != null) heldItem.Attach(handNode);

                SetHeldPhysics(heldNetObj, true);
            }
        }
        else if (closestCounter != null && closestCounter.HasItem())
        {
            NetworkObject itemOnCounter = closestCounter.GetItem();
            closestCounter.ClearItem();

            heldNetObj = itemOnCounter;
            heldItem = itemOnCounter.GetComponentInChildren<AttachableBehaviour>();

            if (heldItem == null) heldItem = itemOnCounter.GetComponent<AttachableBehaviour>();

            heldNetObj.TryRemoveParent();

            if (heldItem != null) heldItem.Attach(handNode);

            SetHeldPhysics(heldNetObj, true);
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

    [Rpc(SendTo.Server)]
    private void TryChopServerRpc()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactRadius);

        CuttingCounter closestCuttingCounter = null;
        float bestAngle = float.MaxValue;
        float bestDistance = float.MaxValue;

        foreach (var col in nearbyColliders)
        {
            if (!col.TryGetComponent<CuttingCounter>(out var cuttingCounter)) continue;

            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            directionToTarget.y = 0;

            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle > maxInteractAngle) continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);
            bool isBetterTarget = angle < bestAngle - 0.5f || (Mathf.Abs(angle - bestAngle) <= 0.5f && distance < bestDistance);

            if (!isBetterTarget) continue;

            closestCuttingCounter = cuttingCounter;
            bestAngle = angle;
            bestDistance = distance;
        }

        bool shouldChop = closestCuttingCounter != null && closestCuttingCounter.HasItem();

        if (isChopping.Value != shouldChop) isChopping.Value = shouldChop;

        if (shouldChop) closestCuttingCounter.Chop(this);
    }

    [Rpc(SendTo.Server)]
    private void StopChopServerRpc()
    {
        isChopping.Value = false;
    }
}