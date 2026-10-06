using Unity.Netcode;
using UnityEngine;

public class IngredientVisuals : NetworkBehaviour
{
    [Header("State meshes")]
    public Mesh wholeMesh;
    public Mesh intermediateMesh;
    public Mesh choppedMesh;

    [Header("Cutting Configuration")]
    public int chopsForIntermediate = 2; 
    public int chopsForFinal = 3;       
    public NetworkVariable<int> currentStage = new NetworkVariable<int>(0);

    public NetworkVariable<int> currentChops = new NetworkVariable<int>(0);

    private MeshFilter[] meshFilters;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        meshFilters = GetComponentsInChildren<MeshFilter>(true);

        UpdateVisuals(currentStage.Value);
        currentStage.OnValueChanged += (oldVal, newVal) => UpdateVisuals(newVal);
    }

    private void UpdateVisuals(int stage)
    {
        Mesh targetMesh = wholeMesh;

        if (stage == 1 && intermediateMesh != null) targetMesh = intermediateMesh;
        else if (stage == 2 && choppedMesh != null) targetMesh = choppedMesh;

        if (meshFilters != null)
        {
            foreach (var mf in meshFilters)
            {
                mf.sharedMesh = targetMesh;
            }
        }
    }

    public void AddChop()
    {
        if (!IsServer) return;
        if (currentStage.Value >= 2) return;

        currentChops.Value++;

        if (currentStage.Value == 0 && currentChops.Value >= chopsForIntermediate)
        {
            currentStage.Value = 1;
        }
        else if (currentStage.Value == 1 && currentChops.Value >= chopsForFinal)
        {
            currentStage.Value = 2;
        }
    }
}