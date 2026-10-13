using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class MainItemSpawner : MonoBehaviourPunCallbacks
{
    public int pickupsPerSpawn = 1;
    public int initialPickups = 3;
    public Vector2 spawnArea = new Vector2(28f, 28f);
    public float groundHeight = 0.5f;
    public float spawnInterval = 7f;
    public bool spawnOnNavMesh = true;
    public float navMeshSampleDistance = 2f;
    public float obstacleCheckRadius = 1.2f;
    public int maxSpawnPositionAttempts = 20;
    public LayerMask blockingLayers = ~0;

    private Bounds mapBounds;
    private bool hasMapBounds;

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
        return sceneName == "Main" || sceneName == "Main 1" || sceneName == "Main 2";
    }

    private void Start()
    {
        hasMapBounds = TryGetMapBounds(out mapBounds);

        // Khi Online: Chỉ có MasterClient mới sinh item để đảm bảo đồng bộ hoàn toàn giữa hai người chơi
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            SpawnPickups(initialPickups);
            InvokeRepeating("SpawnPickups", spawnInterval, spawnInterval);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);

        // Nếu người chơi này được chỉ định làm MasterClient mới, tiếp tục chu kỳ sinh item
        if (PhotonNetwork.IsMasterClient && !IsInvoking("SpawnPickups"))
        {
            InvokeRepeating("SpawnPickups", spawnInterval, spawnInterval);
        }
    }

    private void SpawnPickups()
    {
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return;

        SpawnPickups(pickupsPerSpawn);
    }

    private void SpawnPickups(int pickupCount)
    {
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return;

        for (int i = 0; i < pickupCount; i++)
        {
            string prefabName = GetRandomPickupPrefabName();
            Vector3 position;
            if (!TryGetSpawnPosition(out position))
            {
                Debug.LogWarning("Could not find a clear position for pickup " + prefabName);
                continue;
            }

            Quaternion rotation = Quaternion.identity;
            if (prefabName == "Medical3D" || prefabName == "ToolChest3D" || prefabName == "FuelTank3D")
                rotation = Quaternion.Euler(-90f, 0f, 0f);

            GameObject pickup;
            if (PhotonNetwork.InRoom)
            {
                pickup = PhotonNetwork.Instantiate(prefabName, position, rotation);
            }
            else
            {
                GameObject pickupPrefab = Resources.Load<GameObject>(prefabName);
                if (pickupPrefab == null)
                {
                    Debug.LogError("Could not load pickup prefab from Resources: " + prefabName);
                    continue;
                }
                pickup = Instantiate(pickupPrefab, position, rotation);
            }

            if (pickup != null)
            {
                if (prefabName == "Medical3D")
                {
                    ItemScaleUtility.MatchTankSize(pickup, 2f);
                    if (pickup.GetComponent<MedicalHealth>() == null)
                        pickup.AddComponent<MedicalHealth>();
                }
                else
                {
                    ItemScaleUtility.MatchTankSize(pickup);
                    ItemPickup item = pickup.GetComponent<ItemPickup>();
                    if (item == null)
                        item = pickup.AddComponent<ItemPickup>();
                    item.type = GetPickupType(prefabName);
                }
                ItemScaleUtility.AlignBottomToHeight(pickup, position.y);
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
        if (hasMapBounds)
        {
            return new Vector3(
                Random.Range(mapBounds.min.x, mapBounds.max.x),
                mapBounds.max.y,
                Random.Range(mapBounds.min.z, mapBounds.max.z));
        }

        if (spawnOnNavMesh)
        {
            Vector3 candidate = new Vector3(
                Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f),
                groundHeight,
                Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f));

            NavMeshHit hit;
            if (NavMesh.SamplePosition(
                candidate,
                out hit,
                navMeshSampleDistance,
                NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return new Vector3(
            Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f),
            groundHeight,
            Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f));
    }

    private bool TryGetMapBounds(out Bounds bounds)
    {
        Collider[] colliders = FindObjectsOfType<Collider>();
        float largestArea = 0f;
        bounds = new Bounds();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger || collider.transform.IsChildOf(transform))
                continue;

            Bounds candidateBounds = collider.bounds;
            float area = candidateBounds.size.x * candidateBounds.size.z;
            if (area <= largestArea)
                continue;

            largestArea = area;
            bounds = candidateBounds;
        }

        return largestArea > 0f;
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
