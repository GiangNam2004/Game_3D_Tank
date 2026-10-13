using UnityEditor;
using UnityEngine;
using Photon.Pun;

[InitializeOnLoad]
public class SetupItemPrefabs
{
    static SetupItemPrefabs()
    {
        EditorApplication.update += RunOnce;
    }

    private static void RunOnce()
    {
        EditorApplication.update -= RunOnce;
        EnsurePrefabsConfigured();
    }

    [MenuItem("Tools/Ensure Item Prefabs Configured")]
    public static void EnsurePrefabsConfigured()
    {
        ConfigurePickupPrefab("Assets/Resources/ToolChest3D.prefab", PickupType.ToolChest);
        ConfigurePickupPrefab("Assets/Resources/FuelTank3D.prefab", PickupType.FuelTank);
        ConfigurePickupPrefab("Assets/Resources/ReflectShield_Prefab.prefab", PickupType.ReflectShield);
        ConfigureMedicalPrefab("Assets/Resources/Medical3D.prefab");
        ConfigureHomingShellPrefab("Assets/Resources/HomingShell.prefab");
        ConfigureHomingShellPrefab("Assets/Resources/Diamond_Rocket.prefab");
        EnsureTankDecoyPrefab("Assets/Resources/Tank1_Clone.prefab");
        AuditAndFixSkills();
    }

    private static void EnsureTankDecoyPrefab(string assetPath)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
        if (contents == null) return;

        bool changed = false;

        TankDecoy decoy = contents.GetComponent<TankDecoy>();
        if (decoy == null)
        {
            contents.AddComponent<TankDecoy>();
            changed = true;
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
            Debug.Log("[SetupItemPrefabs] Added TankDecoy component to " + assetPath);
        }
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void AuditAndFixSkills()
    {
        string[] tankPrefabs = { "Tank", "Tank1", "Tank2", "Tank3" };
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (var tankName in tankPrefabs)
        {
            string tankPath = "Assets/Resources/" + tankName + ".prefab";
            GameObject tankGO = AssetDatabase.LoadAssetAtPath<GameObject>(tankPath);
            if (tankGO == null) continue;

            TankShooting ts = tankGO.GetComponent<TankShooting>();
            if (ts == null) continue;

            // Kiểm tra và gán Shield cho Tank2 nếu thiếu
            if (tankName == "Tank2" && ts.m_HasShield && ts.m_ShieldPrefab == null)
            {
                GameObject shieldRef = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ReflectShield_Prefab.prefab");
                if (shieldRef != null)
                {
                    GameObject editableTank = PrefabUtility.LoadPrefabContents(tankPath);
                    TankShooting editableTs = editableTank.GetComponent<TankShooting>();
                    if (editableTs != null)
                    {
                        editableTs.m_ShieldPrefab = shieldRef;
                        PrefabUtility.SaveAsPrefabAsset(editableTank, tankPath);
                        sb.AppendLine("-> Fixed Tank2: Assigned ReflectShield_Prefab to m_ShieldPrefab");
                    }
                    PrefabUtility.UnloadPrefabContents(editableTank);
                }
            }

            // Đọc lại prefab sau khi sửa
            tankGO = AssetDatabase.LoadAssetAtPath<GameObject>(tankPath);
            ts = tankGO.GetComponent<TankShooting>();
            PhotonView tankPv = tankGO.GetComponent<PhotonView>();
            NetworkPlayerSetup nps = tankGO.GetComponent<NetworkPlayerSetup>();
            TankMovement tm = tankGO.GetComponent<TankMovement>();
            TankHealth th = tankGO.GetComponent<TankHealth>();

            sb.AppendLine("=== " + tankName + " ===");
            sb.AppendLine("  Networking: PhotonView=" + (tankPv != null) + ", NetworkPlayerSetup=" + (nps != null));
            if (tankPv != null)
            {
                sb.AppendLine("  ObservedComponents count: " + (tankPv.ObservedComponents != null ? tankPv.ObservedComponents.Count : 0));
                if (tankPv.ObservedComponents != null)
                {
                    foreach (var obs in tankPv.ObservedComponents)
                    {
                        sb.AppendLine("    Observed: " + (obs != null ? obs.GetType().Name : "null"));
                    }
                }
            }
            Component[] allComps = tankGO.GetComponents<Component>();
            foreach (var c in allComps) if (c != null) sb.AppendLine("    Comp: " + c.GetType().Name);
            sb.AppendLine("  Core Components: TankMovement=" + (tm != null) + ", TankShooting=" + (ts != null) + ", TankHealth=" + (th != null));
            sb.AppendLine("  Slot 1: Spread=" + ts.m_HasSpreadShot + ", Giant=" + ts.m_HasGiantShell + ", Elemental=" + ts.m_HasElemental + ", DeathSpin=" + ts.m_HasDeathSpin);
            sb.AppendLine("  Slot 2: Mine=" + ts.m_HasMine + ", Turret=" + ts.m_HasTurret + ", Smoke=" + ts.m_HasSmoke + ", MagPull=" + ts.m_HasMagneticPull);
            sb.AppendLine("  Slot 3: Homing=" + ts.m_HasHomingMissile + ", Clone=" + ts.m_HasClone + ", Shield=" + ts.m_HasShield + ", Dash=" + ts.m_HasDash);

            CheckPrefab(ts.m_Shell != null ? ts.m_Shell.gameObject : null, "m_Shell", sb);
            CheckPrefab(ts.m_IceShellPrefab != null ? ts.m_IceShellPrefab.gameObject : null, "m_IceShellPrefab", sb);
            CheckPrefab(ts.m_ToxicShellPrefab != null ? ts.m_ToxicShellPrefab.gameObject : null, "m_ToxicShellPrefab", sb);
            CheckPrefab(ts.m_HomingShellPrefab != null ? ts.m_HomingShellPrefab.gameObject : null, "m_HomingShellPrefab", sb);
            CheckPrefab(ts.m_MinePrefab, "m_MinePrefab", sb);
            CheckPrefab(ts.m_TurretPrefab, "m_TurretPrefab", sb);
            CheckPrefab(ts.m_SmokePrefab, "m_SmokePrefab", sb);
            CheckPrefab(ts.m_ClonePrefab, "m_ClonePrefab", sb);
            CheckPrefab(ts.m_ShieldPrefab, "m_ShieldPrefab", sb);
            sb.AppendLine();
        }

        // Kiểm tra đạn và gắn BulletSync nếu là đạn bắn
        string[] projectileNames = { "Shell", "Silver_Rocket", "Golden_Rocket_Prefab Variant", "Ice_Rocket", "Toxic_Rocket", "Diamond_Rocket", "HomingShell" };
        sb.AppendLine("=== PROJECTILES AUDIT ===");
        foreach (var pName in projectileNames)
        {
            string pPath = "Assets/Resources/" + pName + ".prefab";
            GameObject pGO = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
            if (pGO == null)
            {
                sb.AppendLine("  " + pName + ": NOT FOUND at " + pPath);
                continue;
            }

            PhotonView pv = pGO.GetComponent<PhotonView>();
            BulletSync bs = pGO.GetComponent<BulletSync>();
            ShellExplosion se = pGO.GetComponent<ShellExplosion>();
            ElementalShell es = pGO.GetComponent<ElementalShell>();
            HomingMissile hm = pGO.GetComponent<HomingMissile>();
            sb.AppendLine("  " + pName + ": PhotonView=" + (pv != null) + ", BulletSync=" + (bs != null) + ", ShellExplosion=" + (se != null) + ", ElementalShell=" + (es != null ? es.bulletType.ToString() : "null") + ", HomingMissile=" + (hm != null));

            Component[] comps = pGO.GetComponents<Component>();
            foreach (var c in comps) if (c != null) sb.AppendLine("    Comp: " + c.GetType().Name);
            if (se != null)
            {
                sb.AppendLine("    ShellExplosion: damage=" + se.m_MaxDamage + ", radius=" + se.m_ExplosionRadius + ", particles=" + (se.m_ExplosionParticles != null) + ", audio=" + (se.m_ExplosionAudio != null));
            }

            if (!pv || !bs)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(pPath);
                if (contents.GetComponent<PhotonView>() == null) contents.AddComponent<PhotonView>();
                if (contents.GetComponent<BulletSync>() == null) contents.AddComponent<BulletSync>();
                PrefabUtility.SaveAsPrefabAsset(contents, pPath);
                PrefabUtility.UnloadPrefabContents(contents);
                sb.AppendLine("    -> Added missing PhotonView/BulletSync to " + pName);
            }
        }

        GameObject turretGO = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Machine_gun_004.prefab");
        if (turretGO != null)
        {
            GameObject editableTurret = PrefabUtility.LoadPrefabContents("Assets/Resources/Machine_gun_004.prefab");
            TurretAI editTai = editableTurret.GetComponent<TurretAI>();
            if (editTai != null)
            {
                editTai.range = 50f;
                editTai.launchForce = 50f;
                editTai.fireRate = 1f;
                if (editTai.shellPrefab == null)
                {
                    Rigidbody defaultShell = AssetDatabase.LoadAssetAtPath<Rigidbody>("Assets/Resources/Shell.prefab");
                    editTai.shellPrefab = defaultShell;
                }
                PrefabUtility.SaveAsPrefabAsset(editableTurret, "Assets/Resources/Machine_gun_004.prefab");
            }
            PrefabUtility.UnloadPrefabContents(editableTurret);
            turretGO = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Machine_gun_004.prefab");
            TurretAI tai = turretGO.GetComponent<TurretAI>();

            sb.AppendLine("=== MACHINE GUN 004 AUDIT ===");
            if (tai != null)
            {
                sb.AppendLine("  range: " + tai.range);
                sb.AppendLine("  fireRate: " + tai.fireRate);
                sb.AppendLine("  lifeTime: " + tai.lifeTime);
                sb.AppendLine("  launchForce: " + tai.launchForce);
                sb.AppendLine("  shellPrefab: " + (tai.shellPrefab != null ? tai.shellPrefab.name : "null"));
                sb.AppendLine("  firePoint: " + (tai.firePoint != null ? tai.firePoint.name : "null"));
                if (tai.firePoint != null)
                {
                    sb.AppendLine("    firePoint localPos: " + tai.firePoint.localPosition);
                    sb.AppendLine("    firePoint localRot: " + tai.firePoint.localRotation.eulerAngles);
                }
            }
            Component[] tComps = turretGO.GetComponentsInChildren<Component>(true);
            foreach (var c in tComps) if (c != null) sb.AppendLine("    " + c.gameObject.name + " -> " + c.GetType().Name);
        }

        string[] otherPrefabs = { "Enemy", "Boss" };
        foreach (var op in otherPrefabs)
        {
            GameObject oGO = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/" + op + ".prefab");
            if (oGO != null)
            {
                sb.AppendLine("=== " + op + " AUDIT ===");
                TankMovement tm = oGO.GetComponent<TankMovement>();
                TankShooting ts = oGO.GetComponent<TankShooting>();
                TankHealth th = oGO.GetComponent<TankHealth>();
                sb.AppendLine("  TM=" + (tm != null) + ", TS=" + (ts != null) + ", TH=" + (th != null));
                if (tm != null) sb.AppendLine("  TM.PlayerNumber=" + tm.m_PlayerNumber);
            }
        }

        System.IO.File.WriteAllText("audit_tanks.txt", sb.ToString());
        Debug.Log("[AuditAndFixSkills] Completed comprehensive audit:\n" + sb.ToString());
    }

    private static void CheckPrefab(GameObject prefab, string fieldName, System.Text.StringBuilder sb)
    {
        if (prefab == null)
        {
            sb.AppendLine("  " + fieldName + ": NULL");
            return;
        }

        string path = AssetDatabase.GetAssetPath(prefab);
        bool inResources = path.Contains("/Resources/");
        PhotonView pv = prefab.GetComponent<PhotonView>();

        sb.AppendLine("  " + fieldName + ": " + prefab.name + " (Path: " + path + ", InResources: " + inResources + ", HasPhotonView: " + (pv != null) + ")");

        if (!pv)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (contents.GetComponent<PhotonView>() == null)
            {
                contents.AddComponent<PhotonView>();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                sb.AppendLine("    -> ADDED PhotonView to " + path);
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }
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

