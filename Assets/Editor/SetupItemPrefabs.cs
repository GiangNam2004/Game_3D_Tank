using UnityEditor;
using UnityEngine;
using Photon.Pun;

[InitializeOnLoad]
public class SetupItemPrefabs
{
    static SetupItemPrefabs()
    {
        EditorApplication.delayCall += EnsurePrefabsConfigured;
    }

    [MenuItem("Tools/Ensure Item Prefabs Configured")]
    public static void EnsurePrefabsConfigured()
    {
        ConfigurePickupPrefab("Assets/Resources/ToolChest3D.prefab", PickupType.ToolChest);
        ConfigurePickupPrefab("Assets/Resources/FuelTank3D.prefab", PickupType.FuelTank);
        ConfigurePickupPrefab("Assets/Resources/ReflectShield_Prefab.prefab", PickupType.ReflectShield);
        ConfigureMedicalPrefab("Assets/Resources/Medical3D.prefab");
        ConfigureHomingShellPrefab("Assets/Resources/HomingShell.prefab");
    }

    private static void ConfigureHomingShellPrefab(string assetPath)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
        if (contents == null) return;

        bool changed = false;

        PhotonView pv = contents.GetComponent<PhotonView>();
        if (pv == null)
        {
            contents.AddComponent<PhotonView>();
            changed = true;
        }

        HomingMissile hm = contents.GetComponent<HomingMissile>();
        if (hm == null)
        {
            contents.AddComponent<HomingMissile>();
            changed = true;
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
            Debug.Log("[SetupItemPrefabs] Successfully updated HomingShell prefab: " + assetPath);
        }
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void ConfigurePickupPrefab(string assetPath, PickupType type)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
        if (contents == null) return;

        bool changed = false;

        PhotonView pv = contents.GetComponent<PhotonView>();
        if (pv == null)
        {
            contents.AddComponent<PhotonView>();
            changed = true;
        }

        ItemPickup pickup = contents.GetComponent<ItemPickup>();
        if (pickup == null)
        {
            pickup = contents.AddComponent<ItemPickup>();
            changed = true;
        }

        if (pickup.type != type)
        {
            pickup.type = type;
            changed = true;
        }

        Collider[] cols = contents.GetComponentsInChildren<Collider>(true);
        if (cols.Length == 0)
        {
            SphereCollider sc = contents.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            changed = true;
        }
        else
        {
            for (int i = 0; i < cols.Length; i++)
            {
                if (!cols[i].isTrigger)
                {
                    cols[i].isTrigger = true;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
            Debug.Log("[SetupItemPrefabs] Successfully updated pickup prefab: " + assetPath);
        }
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void ConfigureMedicalPrefab(string assetPath)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
        if (contents == null) return;

        bool changed = false;

        PhotonView pv = contents.GetComponent<PhotonView>();
        if (pv == null)
        {
            contents.AddComponent<PhotonView>();
            changed = true;
        }

        MedicalHealth mh = contents.GetComponent<MedicalHealth>();
        if (mh == null)
        {
            mh = contents.AddComponent<MedicalHealth>();
            changed = true;
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
            Debug.Log("[SetupItemPrefabs] Successfully updated medical prefab: " + assetPath);
        }
        PrefabUtility.UnloadPrefabContents(contents);
    }
}

