using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class MainItemSpawner : MonoBehaviour
{
    public int pickupsPerSpawn = 1;
    public int initialPickups = 3;
    public Vector2 spawnArea = new Vector2(28f, 28f);
    public float groundHeight = 0.5f;
    public float spawnInterval = 7f;
    public bool spawnOnNavMesh = true;
    public float obstacleCheckRadius = 1.2f;
    public int maxSpawnPositionAttempts = 20;
    public LayerMask blockingLayers = ~0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsGameplayScene(scene.name) || FindObjectOfType<MainItemSpawner>() != null)
            return;

        GameObject managerObject = new GameObject("Main Item Spawner");
        managerObject.AddComponent<MainItemSpawner>();
    }

    private static bool IsGameplayScene(string sceneName)
    {
        return sceneName == "Main" || sceneName == "Main 1";
    }

    private void Start()
    {
        SpawnPickups(initialPickups);
        InvokeRepeating("SpawnPickups", spawnInterval, spawnInterval);
    }

    private void SpawnPickups()
    {
        SpawnPickups(pickupsPerSpawn);
    }

    private void SpawnPickups(int pickupCount)
    {
        for (int i = 0; i < pickupCount; i++)
        {
            string prefabName = GetRandomPickupPrefabName();
            GameObject pickupPrefab = Resources.Load<GameObject>(prefabName);
            if (pickupPrefab == null)
            {
                Debug.LogError("Could not load pickup prefab from Resources: " + prefabName);
                continue;
            }

            Vector3 position;
            if (!TryGetSpawnPosition(out position))
            {
                Debug.LogWarning("Could not find a clear position for pickup " + prefabName);
                continue;
            }

            GameObject pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
            if (prefabName == "Medical3D")
            {
                ItemScaleUtility.MatchTankSize(pickup, 2f);
                ItemScaleUtility.AlignBottomToHeight(pickup, position.y);
                if (pickup.GetComponent<MedicalHealth>() == null)
                    pickup.AddComponent<MedicalHealth>();
            }
            else
            {
                ItemScaleUtility.MatchTankSize(pickup);
                ItemScaleUtility.AlignBottomToHeight(pickup, position.y);
                ItemPickup item = pickup.GetComponent<ItemPickup>();
                if (item == null)
                    item = pickup.AddComponent<ItemPickup>();
                item.type = GetPickupType(prefabName);
            }
        }

        Debug.Log("Spawned " + pickupCount + " random pickup(s) in scene " + SceneManager.GetActiveScene().name);
    }

    private bool TryGetSpawnPosition(out Vector3 position)
    {
        for (int attempt = 0; attempt < maxSpawnPositionAttempts; attempt++)
        {
            position = GetSpawnPosition();
            if (IsSpawnPositionClear(position))
                return true;
        }

        position = Vector3.zero;
        return false;
    }

    private bool IsSpawnPositionClear(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(
            position + Vector3.up * obstacleCheckRadius,
            obstacleCheckRadius,
            blockingLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.transform.IsChildOf(transform))
                continue;

            Bounds bounds = collider.bounds;
            float groundTop = position.y + 0.1f;
            if (bounds.max.y <= groundTop)
                continue;

            return false;
        }

        return true;
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnOnNavMesh)
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices.Length > 0 && triangulation.indices.Length >= 3)
            {
                int triangleCount = triangulation.indices.Length / 3;
                float totalArea = 0f;
                for (int i = 0; i < triangleCount; i++)
                {
                    Vector3 first = triangulation.vertices[triangulation.indices[i * 3]];
                    Vector3 second = triangulation.vertices[triangulation.indices[i * 3 + 1]];
                    Vector3 third = triangulation.vertices[triangulation.indices[i * 3 + 2]];
                    totalArea += Vector3.Cross(second - first, third - first).magnitude * 0.5f;
                }

                float selectedArea = Random.Range(0f, totalArea);
                int triangleIndex = 0;
                for (int i = 0; i < triangleCount; i++)
                {
                    Vector3 first = triangulation.vertices[triangulation.indices[i * 3]];
                    Vector3 second = triangulation.vertices[triangulation.indices[i * 3 + 1]];
                    Vector3 third = triangulation.vertices[triangulation.indices[i * 3 + 2]];
                    selectedArea -= Vector3.Cross(second - first, third - first).magnitude * 0.5f;
                    if (selectedArea <= 0f)
                    {
                        triangleIndex = i * 3;
                        break;
                    }
                }

                Vector3 a = triangulation.vertices[triangulation.indices[triangleIndex]];
                Vector3 b = triangulation.vertices[triangulation.indices[triangleIndex + 1]];
                Vector3 c = triangulation.vertices[triangulation.indices[triangleIndex + 2]];

                float firstWeight = Random.value;
                float secondWeight = Random.value;
                if (firstWeight + secondWeight > 1f)
                {
                    firstWeight = 1f - firstWeight;
                    secondWeight = 1f - secondWeight;
                }

                return a + (b - a) * firstWeight + (c - a) * secondWeight;
            }
        }

        return new Vector3(
            Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f),
            groundHeight,
            Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f));
    }

    private static string GetRandomPickupPrefabName()
    {
        string[] prefabNames =
        {
            "Medical3D",
            "ToolChest3D",
            "FuelTank3D",
            "ReflectShield_Prefab"
        };
        return prefabNames[Random.Range(0, prefabNames.Length)];
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
