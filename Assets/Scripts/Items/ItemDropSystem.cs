using UnityEngine;
using Photon.Pun;

public static class ItemDropSystem
{
    private static readonly string[] itemPrefabNames =
    {
        "ToolChest3D",
        "FuelTank3D",
        "ReflectShield_Prefab"
    };

    public static void TryDrop(Vector3 position, float chance, float groundHeight)
    {
        if (Random.value > Mathf.Clamp01(chance))
            return;

        string prefabName = itemPrefabNames[Random.Range(0, itemPrefabNames.Length)];
        Quaternion rotation = Quaternion.identity;
        if (prefabName == "ToolChest3D" || prefabName == "FuelTank3D")
            rotation = Quaternion.Euler(-90f, 0f, 0f);

        GameObject pickup;
        if (PhotonNetwork.InRoom)
        {
            pickup = PhotonNetwork.Instantiate(prefabName, position, rotation);
        }
        else
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                Debug.LogError("Could not load pickup prefab from Resources: " + prefabName);
                return;
            }
            pickup = Object.Instantiate(prefab, position, rotation);
        }

        if (pickup != null)
        {
            ItemScaleUtility.MatchTankSize(pickup);
            ItemScaleUtility.AlignBottomToHeight(pickup, groundHeight);
            ItemPickup item = pickup.GetComponent<ItemPickup>();
            if (item == null)
                item = pickup.AddComponent<ItemPickup>();
            item.type = GetPickupType(prefabName);
        }
    }

    private static PickupType GetPickupType(string prefabName)
    {
        if (prefabName == "FuelTank3D")
            return PickupType.FuelTank;
        if (prefabName == "ReflectShield_Prefab")
            return PickupType.ReflectShield;
        return PickupType.ToolChest;
    }
}
