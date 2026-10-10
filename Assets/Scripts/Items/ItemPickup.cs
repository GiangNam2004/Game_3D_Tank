using UnityEngine;

public enum PickupType
{
    ToolChest,
    FuelTank,
    ReflectShield
}

public class ItemPickup : MonoBehaviour
{
    public PickupType type;
    public float healAmount = 35f;
    public float speedMultiplier = 1.5f;
    public float speedDuration = 8f;

    private bool collected;

    private void Awake()
    {
        Collider itemCollider = GetComponent<Collider>();
        if (itemCollider != null)
            itemCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TankHealth tank = other.GetComponentInParent<TankHealth>();
        if (tank == null || !tank.isActiveAndEnabled)
            return;

        if (collected || !tank.CanApplyPickup(type))
            return;

        if (tank.ApplyPickup(type, healAmount, speedMultiplier, speedDuration))
        {
            collected = true;
            Destroy(gameObject);
        }
    }
}
