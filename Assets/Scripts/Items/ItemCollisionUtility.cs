using UnityEngine;
using UnityEngine.AI;

public static class ItemCollisionUtility
{
    public static void ConfigureSolidObstacle(GameObject item)
    {
        Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].isTrigger = false;

        if (colliders.Length == 0)
            item.AddComponent<BoxCollider>();

        NavMeshObstacle obstacle = item.GetComponent<NavMeshObstacle>();
        if (obstacle == null)
            obstacle = item.AddComponent<NavMeshObstacle>();

        Bounds bounds = GetLocalBounds(item);
        obstacle.carving = true;
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = bounds.center;
        obstacle.size = bounds.size;
    }

    public static BoxCollider CreatePickupTrigger(GameObject item)
    {
        Bounds bounds = GetLocalBounds(item);
        GameObject triggerObject = new GameObject("Pickup Trigger");
        triggerObject.transform.SetParent(item.transform, false);
        triggerObject.transform.localPosition = bounds.center;

        BoxCollider trigger = triggerObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = bounds.size;
        return trigger;
    }

    private static Bounds GetLocalBounds(GameObject item)
    {
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(Vector3.zero, Vector3.one);

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = item.transform.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = item.transform.InverseTransformVector(worldBounds.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        return new Bounds(localCenter, localSize);
    }
}
