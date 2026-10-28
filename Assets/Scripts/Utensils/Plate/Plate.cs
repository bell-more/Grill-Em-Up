using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Plate : NetworkBehaviour
{
    [SerializeField] private float defaultThickness = 0.08f;
    [SerializeField] private GameObject bottomBunPrefab;
    [SerializeField] private GameObject topBunPrefab;
    [SerializeField] private AttachableNode ingredientsNode;

    private NetworkObject bottomBunInstance;
    private NetworkObject topBunInstance;

    private readonly List<NetworkObject> middleIngredients = new List<NetworkObject>();
    private readonly Dictionary<NetworkObject, AttachableBehaviour> attachedVisuals = new Dictionary<NetworkObject, AttachableBehaviour>();

    private void Awake()
    {
        FindIngredientsNode();
    }

    public override void OnNetworkSpawn()
    {
        FindIngredientsNode();
    }

    private void FindIngredientsNode()
    {
        if (ingredientsNode != null) return;
        AttachableNode[] nodes = GetComponentsInChildren<AttachableNode>(true);
        foreach (AttachableNode node in nodes)
        {
            if (node.gameObject.name == "IngredientsNode")
            {
                ingredientsNode = node;
                return;
            }
        }
    }

    public bool TryAddIngredient(NetworkObject ingredient)
    {
        if (ingredient == null) return false;
        AttachableBehaviour visual = ingredient.GetComponentInChildren<AttachableBehaviour>(true);
        return TryAddIngredient(ingredient, visual);
    }

    public bool TryAddIngredient(NetworkObject ingredient, AttachableBehaviour visual)
    {
        if (!IsServer || ingredient == null || visual == null) return false;
        FindIngredientsNode();
        if (ingredientsNode == null) return false;
        if (ingredient.TryGetComponent<FryingPan>(out _)) return false;

        if (!ingredient.IsSpawned || !visual.IsSpawned || !ingredientsNode.IsSpawned)
        {
            return false;
        }

        if (visual.NetworkObject != ingredient)
        {
            return false;
        }

        if (middleIngredients.Contains(ingredient) || attachedVisuals.ContainsKey(ingredient))
        {
            return false;
        }

        if (ingredient.TryGetComponent<IngredientVisuals>(out var ingredientVisuals))
        {
            string ingredientName = ingredient.name.ToLower();

            bool requiresChopping =
                ingredientName.Contains("tomato") ||
                ingredientName.Contains("cheese") ||
                ingredientName.Contains("lettuce") ||
                ingredientName.Contains("onion");

            if (requiresChopping && ingredientVisuals.currentStage.Value != 2)
            {
                return false;
            }

            if (ingredientVisuals.canBeFried && ingredientVisuals.currentStage.Value != 1)
            {
                return false;
            }
        }

        bool isBun = ingredient.name.Contains("Bun");

        if (isBun)
        {
            if (bottomBunInstance != null) return false;

            if (!IsValidBunPrefab(bottomBunPrefab) || !IsValidBunPrefab(topBunPrefab))
            {
                return false;
            }

            NetworkObject newBottom = SpawnBun(bottomBunPrefab);
            if (newBottom == null) return false;

            NetworkObject newTop = SpawnBun(topBunPrefab);
            if (newTop == null)
            {
                DespawnStackObject(newBottom);
                return false;
            }

            visual.Detach();
            if (ingredient.IsSpawned) ingredient.Despawn(true);

            bottomBunInstance = newBottom;
            topBunInstance = newTop;
            UpdateAllPositions();
            return true;
        }

        if (middleIngredients.Count >= 7)
        {
            return false;
        }

        visual.Attach(ingredientsNode);

        if (visual.transform.parent != ingredientsNode.transform)
        {
            return false;
        }

        attachedVisuals[ingredient] = visual;
        middleIngredients.Add(ingredient);
        SetObjectPhysics(ingredient);
        UpdateAllPositions();
        return true;
    }

    private bool IsValidBunPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        if (prefab.GetComponent<NetworkObject>() == null) return false;
        if (prefab.GetComponentInChildren<AttachableBehaviour>(true) == null) return false;
        return true;
    }

    private NetworkObject SpawnBun(GameObject prefab)
    {
        GameObject bunObject = Instantiate(prefab);
        NetworkObject netObj = bunObject.GetComponent<NetworkObject>();
        AttachableBehaviour visual = bunObject.GetComponentInChildren<AttachableBehaviour>(true);

        if (netObj == null || visual == null)
        {
            Destroy(bunObject);
            return null;
        }

        netObj.Spawn(true);

        if (!netObj.IsSpawned || !visual.IsSpawned || !ingredientsNode.IsSpawned)
        {
            if (netObj.IsSpawned) netObj.Despawn(true);
            else Destroy(bunObject);
            return null;
        }

        visual.Attach(ingredientsNode);

        if (visual.transform.parent != ingredientsNode.transform)
        {
            netObj.Despawn(true);
            return null;
        }

        attachedVisuals[netObj] = visual;
        SetObjectPhysics(netObj);
        return netObj;
    }

    private void SetObjectPhysics(NetworkObject netObj)
    {
        if (netObj == null) return;
        if (netObj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        if (netObj.TryGetComponent<Collider>(out var col)) col.enabled = false;
    }

    private float GetObjectThickness(GameObject obj)
    {
        if (obj.TryGetComponent<IngredientItem>(out var item)) return item.thickness;
        return defaultThickness;
    }

    private void UpdateAllPositions()
    {
        if (!IsServer || ingredientsNode == null) return;
        float currentY = 0f;

        if (bottomBunInstance != null)
        {
            SetStackPosition(bottomBunInstance, currentY);
            currentY += GetObjectThickness(bottomBunInstance.gameObject) * 2f;
        }

        foreach (NetworkObject ingredient in middleIngredients)
        {
            if (ingredient == null) continue;
            SetStackPosition(ingredient, currentY);
            currentY += GetObjectThickness(ingredient.gameObject);
        }

        if (topBunInstance != null)
        {
            SetStackPosition(topBunInstance, currentY);
        }
    }

    private void SetStackPosition(NetworkObject obj, float height)
    {
        if (obj == null) return;

        if (!attachedVisuals.TryGetValue(obj, out var visual) || visual == null)
        {
            return;
        }

        Vector3 localPosition = new Vector3(0f, height, 0f);
        Quaternion localRotation = Quaternion.identity;

        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = localRotation;

        SyncStackPositionRpc(new NetworkObjectReference(obj), localPosition, localRotation);
    }

    [Rpc(SendTo.NotServer)]
    private void SyncStackPositionRpc(NetworkObjectReference reference, Vector3 localPosition, Quaternion localRotation)
    {
        StartCoroutine(ApplyStackPosition(reference, localPosition, localRotation));
    }

    private IEnumerator ApplyStackPosition(NetworkObjectReference reference, Vector3 localPosition, Quaternion localRotation)
    {
        for (int attempt = 0; attempt < 120; attempt++)
        {
            FindIngredientsNode();

            if (ingredientsNode != null && reference.TryGet(out NetworkObject obj, NetworkManager))
            {
                AttachableBehaviour[] visuals = ingredientsNode.GetComponentsInChildren<AttachableBehaviour>(true);
                foreach (AttachableBehaviour visual in visuals)
                {
                    if (visual != null && visual.NetworkObject == obj && visual.transform.parent == ingredientsNode.transform)
                    {
                        visual.transform.localPosition = localPosition;
                        visual.transform.localRotation = localRotation;
                        yield break;
                    }
                }
            }

            yield return null;
        }

    }

    public GameObject GetBottomBun()
    {
        return bottomBunInstance != null ? bottomBunInstance.gameObject : null;
    }

    public GameObject GetTopBun()
    {
        return topBunInstance != null ? topBunInstance.gameObject : null;
    }

    public List<GameObject> GetMiddleIngredients()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (NetworkObject ingredient in middleIngredients)
        {
            if (ingredient != null) list.Add(ingredient.gameObject);
        }
        return list;
    }

    private void DespawnStackObject(NetworkObject netObj)
    {
        if (netObj == null) return;
        if (netObj.IsSpawned) netObj.Despawn(true);
        attachedVisuals.Remove(netObj);
    }

    public void ClearAndDestroyPlate()
    {
        if (!IsServer) return;

        foreach (NetworkObject ingredient in middleIngredients) DespawnStackObject(ingredient);
        middleIngredients.Clear();

        DespawnStackObject(bottomBunInstance);
        DespawnStackObject(topBunInstance);

        bottomBunInstance = null;
        topBunInstance = null;

        NetworkObject plateNetObj = GetComponent<NetworkObject>();
        if (plateNetObj != null && plateNetObj.IsSpawned) plateNetObj.Despawn(true);
    }


}