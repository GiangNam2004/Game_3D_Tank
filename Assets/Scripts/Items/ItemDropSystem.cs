using UnityEngine;

public static class ItemDropSystem
{
    private static readonly string[] itemPrefabNames =
    {
        "ToolChest3D",
        "FuelTank3D",
        "ReflectShield_Prefab"
    };

    public static void TryDrop(Vector3 position, float chance)
    {
        if (Random.value > Mathf.Clamp01(chance))
            return;

        string prefabName = itemPrefabNames[Random.Range(0, itemPrefabNames.Length)];
        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab == null)
        {
            Debug.LogError("Could not load pickup prefab from Resources: " + prefabName);
            return;
        }

        GameObject pickup = Object.Instantiate(prefab, position + Vector3.up * 0.35f, Quaternion.identity);
        ItemScaleUtility.MatchTankSize(pickup);
        ItemPickup item = pickup.GetComponent<ItemPickup>();
        if (item == null)
            item = pickup.AddComponent<ItemPickup>();
        item.type = GetPickupType(prefabName);
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
