using Unity.Netcode;
using UnityEngine;

public class TrashBin : NetworkBehaviour
{
    [SerializeField] private PlateHolder plateHolder;

    public bool CanAccept(NetworkObject item)
    {
        if (item == null) return false;

        if (item.TryGetComponent<FryingPan>(out _)) return false;

        return true;
    }

    public bool ThrowAway(NetworkObject item)
    {
        if (!IsServer || !CanAccept(item))
            return false;

        if (item.TryGetComponent<Plate>(out Plate plate))
        {
            if (plateHolder == null)
                return false;

            plate.ClearAndDestroyPlate();

            if (!plateHolder.ReturnPlate(item))
                return false;

            return true;
        }

        if (item.IsSpawned)
            item.Despawn(true);

        return true;
    }

}
