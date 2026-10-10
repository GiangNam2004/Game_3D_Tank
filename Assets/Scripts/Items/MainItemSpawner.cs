using UnityEngine;
using UnityEngine.SceneManagement;

public class MainItemSpawner : MonoBehaviour
{
    public int pickupsPerSpawn = 1;
    public Vector2 spawnArea = new Vector2(28f, 28f);
    public float groundHeight = 0.5f;
    public float spawnInterval = 7f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Main" || FindObjectOfType<MainItemSpawner>() != null)
            return;

        GameObject managerObject = new GameObject("Main Item Spawner");
        managerObject.AddComponent<MainItemSpawner>();
    }

    private void Start()
    {
        InvokeRepeating("SpawnPickups", 0f, spawnInterval);
    }

    private void SpawnPickups()
    {
        for (int i = 0; i < pickupsPerSpawn; i++)
        {
            string prefabName = GetRandomPickupPrefabName();
            GameObject pickupPrefab = Resources.Load<GameObject>(prefabName);
            if (pickupPrefab == null)
            {
                Debug.LogError("Could not load pickup prefab from Resources: " + prefabName);
                continue;
            }

            Vector3 position = new Vector3(
                Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f),
                groundHeight,
                Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f));

            GameObject pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
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
        }

        Debug.Log("Spawned " + pickupsPerSpawn + " random pickup(s) in scene " + SceneManager.GetActiveScene().name);
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
