using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryTable : Counter
{
    [SerializeField] private OrderManager orderManager;

    [Header("Plate holder")]
    [SerializeField] private PlateHolder plateHolder;

    public override bool AcceptsItem(NetworkObject item)
    {
        return item.TryGetComponent<Plate>(out _);
    }

    public override void PlaceItem(NetworkObject item)
    {
        base.PlaceItem(item);

        if (!IsServer) return;

        EvaluateOrder();
    }

    private void EvaluateOrder()
    {
        if (heldItem != null && heldItem.TryGetComponent<Plate>(out var plateScript))
        {
            List<string> plateIngredients = new List<string>();

            GameObject bottom = plateScript.GetBottomBun();
            if (bottom != null) plateIngredients.Add(CleanName(bottom.name));

            List<GameObject> middle = plateScript.GetMiddleIngredients();
            foreach (var ing in middle)
            {
                if (ing != null) plateIngredients.Add(CleanName(ing.name));
            }

            GameObject top = plateScript.GetTopBun();
            if (top != null) plateIngredients.Add(CleanName(top.name));

            if (orderManager != null)
            {
                orderManager.SubmitOrder(plateIngredients);
            }

            StartCoroutine(DestroyPlateAfterDelay(plateScript, 1f));
        }
    }

    private IEnumerator DestroyPlateAfterDelay(Plate plateScript, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        if (plateScript != null)
        {
            NetworkObject plateNetworkObject = plateScript.GetComponent<NetworkObject>();

            if (plateHolder != null)
            {
                plateHolder.ReturnPlate(plateNetworkObject);
            }

            plateScript.ClearAndDestroyPlate();
        }

        ClearItem();
    }

    private string CleanName(string rawName)
    {
        return rawName.Replace("(Clone)", "").Trim();
    }
}