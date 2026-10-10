using UnityEngine;

public static class ItemScaleUtility
{
    public static void MatchTankSize(GameObject item, float sizeMultiplier = 1f)
    {
        GameObject tankPrefab = Resources.Load<GameObject>("Tank");
        if (tankPrefab == null)
        {
            Debug.LogError("Could not load Tank from Resources to size pickup.");
            return;
        }

        float itemSize = GetLargestRendererSize(item);
        float tankSize = GetLargestRendererSize(tankPrefab);
        if (itemSize <= 0f || tankSize <= 0f)
        {
            Debug.LogError("Could not measure pickup or Tank renderer bounds.");
            return;
        }

        item.transform.localScale *= tankSize / itemSize * sizeMultiplier;
    }

    public static void AlignBottomToHeight(GameObject item, float groundHeight)
    {
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        item.transform.position += Vector3.up * (groundHeight - bounds.min.y);
    }

    private static float GetLargestRendererSize(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (!hasBounds)
            return 0f;

        return Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
    }
}
