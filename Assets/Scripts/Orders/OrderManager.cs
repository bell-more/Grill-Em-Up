using Unity.Netcode;
using UnityEngine;

public class OrderManager : NetworkBehaviour
{
    public NetworkList<int> activeOrders;

    [Header("Orders Config")]
    public float timeBetweenOrders = 10f;
    public int maxActiveOrders = 5;
    public int totalAvailableRecipes = 3;

    private float spawnTimer;

    private void Awake()
    {
        activeOrders = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            spawnTimer = timeBetweenOrders;
            SpawnNewOrder();
        }

        activeOrders.OnListChanged += HandleOrdersChanged;
    }

    public override void OnNetworkDespawn()
    {
        activeOrders.OnListChanged -= HandleOrdersChanged;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (activeOrders.Count < maxActiveOrders)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnNewOrder();
                spawnTimer = timeBetweenOrders;
            }
        }
    }

    private void SpawnNewOrder()
    {
        int randomRecipeID = Random.Range(0, totalAvailableRecipes);
        activeOrders.Add(randomRecipeID);

        Debug.Log("Server - new order: Recipe ID "+randomRecipeID);
    }

    private void HandleOrdersChanged(NetworkListEvent<int> changeEvent)
    {
        // Acá vamos a conectar tu interfaz visual.
        // Si el evento fue de tipo "Add", instanciamos un prefab de ticket en el Canvas.
        // Si fue "Remove", destruimos el ticket entregado.

        Debug.Log("UI - Updated list. orders: "+activeOrders.Count);
    }
}

