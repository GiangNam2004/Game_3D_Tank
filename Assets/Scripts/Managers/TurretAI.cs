using UnityEngine;
using Photon.Pun;

public class TurretAI : MonoBehaviourPun {
    [Header("Chỉ số Ụ súng")]
    public float range = 50f;          // Tầm bắn nâng lên 40m để bao quát chiến trường
    public float fireRate = 1f;         // Tốc độ bắn máy: 1 viên/giây (cách nhau 0.33s)   
    public float lifeTime = 12f;        // Thời gian tồn tại của ụ súng

    public Rigidbody shellPrefab;
    public Transform firePoint;
    public float launchForce = 50f;     // Tốc độ đạn bay nhanh, căng và thẳng (30f)
    
    [HideInInspector] public GameObject m_OwnerTank;
    [HideInInspector] public int m_OwnerPlayerNumber = 1;

    private float fireCountdown = 0f;
    private Transform target;

    public void SetOwner(GameObject ownerTank, int playerNumber)
    {
        m_OwnerTank = ownerTank;
        m_OwnerPlayerNumber = playerNumber;
    }

    void Start() {
        if (shellPrefab == null)
        {
            shellPrefab = Resources.Load<Rigidbody>("Shell");
        }

        if (PhotonNetwork.InRoom)
        {
            if (photonView.IsMine)
            {
                StartCoroutine(NetworkDestroyTurret(lifeTime));
            }
        }
        else
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private System.Collections.IEnumerator NetworkDestroyTurret(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameObject != null && photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    void Update() {
        // Chỉ chủ sở hữu đặt ụ súng mới điều khiển xoay và bắn qua mạng
        if (PhotonNetwork.InRoom && photonView != null && !photonView.IsMine)
            return;

        FindTarget();
        if (target == null) return;
        
        // Xoay súng về phía kẻ địch
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
        
        // Bắn đạn
        if (fireCountdown <= 0f) {
            FireShell();
            fireCountdown = 1f / fireRate;
        }
        fireCountdown -= Time.deltaTime;
    }

    private void FireShell()
    {
        if (shellPrefab == null || target == null) return;

        // Điểm bắn nâng cao khỏi mặt đất để tránh đạn vừa ra đã va chạm đất
        Vector3 spawnPos = (firePoint != null) ? firePoint.position : (transform.position + Vector3.up * 1.2f + transform.forward * 0.8f);
        if (spawnPos.y < transform.position.y + 0.5f)
        {
            spawnPos += Vector3.up * 1.0f + transform.forward * 0.5f;
        }

        // Nhắm thẳng vào tâm thân xe địch
        Vector3 targetCenter = target.position + Vector3.up * 0.9f;
        Vector3 aimDir = (targetCenter - spawnPos).normalized;
        Vector3 velocity = aimDir * launchForce;
        Quaternion fireRot = Quaternion.LookRotation(aimDir);

        GameObject spawnedShell = null;

        if (PhotonNetwork.InRoom)
        {
            object[] data = new object[] { velocity, Vector3.one };
            spawnedShell = PhotonNetwork.Instantiate(shellPrefab.gameObject.name, spawnPos, fireRot, 0, data);
        }
        else
        {
            Rigidbody shell = Instantiate(shellPrefab, spawnPos, fireRot);
            shell.velocity = velocity;
            spawnedShell = shell.gameObject;
        }

        if (spawnedShell != null)
        {
            Collider[] shellCols = spawnedShell.GetComponentsInChildren<Collider>();

            // 1. Tuyệt đối bỏ qua va chạm giữa đạn ụ súng và xe chủ sở hữu đặt ra nó
            if (m_OwnerTank != null)
            {
                Collider[] ownerCols = m_OwnerTank.GetComponentsInChildren<Collider>();
                foreach (var sc in shellCols)
                {
                    foreach (var oc in ownerCols)
                    {
                        Physics.IgnoreCollision(sc, oc, true);
                    }
                }
            }

            // 2. Bỏ qua va chạm với chính khối ụ súng
            Collider[] turretCols = GetComponentsInChildren<Collider>();
            foreach (var sc in shellCols)
            {
                foreach (var tc in turretCols)
                {
                    Physics.IgnoreCollision(sc, tc, true);
                }
            }

            // 3. Đánh dấu ShooterTank để ShellExplosion miễn nhiễm sát thương nổ cho chủ sở hữu
            ShellExplosion se = spawnedShell.GetComponent<ShellExplosion>();
            if (se != null)
            {
                se.m_ShooterTank = m_OwnerTank;
            }
        }
    }

    void FindTarget() {
        TankMovement[] tanks = FindObjectsOfType<TankMovement>();
        float shortest = Mathf.Infinity;
        Transform nearest = null;

        foreach (var tank in tanks) {
            if (!tank.gameObject.activeSelf) continue;

            // Bỏ qua xe đã chết
            TankHealth th = tank.GetComponent<TankHealth>();
            if (th != null && th.m_CurrentHealth <= 0) continue;

            // Tuyệt đối không bao giờ nhắm vào xe chủ sở hữu đặt ụ súng
            if (m_OwnerTank != null && (tank.gameObject == m_OwnerTank || tank.transform.IsChildOf(m_OwnerTank.transform)))
                continue;

            // Bỏ qua phân thân Decoy của người chơi
            TankDecoy decoy = tank.GetComponent<TankDecoy>();
            if (decoy != null && m_OwnerPlayerNumber == 1)
                continue;

            bool isEnemy = false;
            if (PhotonNetwork.InRoom)
            {
                PhotonView tankPv = tank.GetComponent<PhotonView>();
                // Khi Online: Kẻ địch là bất kỳ xe nào không phải của chủ sở hữu
                if (tankPv != null && photonView != null && tankPv.OwnerActorNr != photonView.OwnerActorNr)
                {
                    isEnemy = true;
                }
            }
            else
            {
                // Khi Offline:
                // Nếu chủ sở hữu là Player 1 -> Kẻ địch là Bot (PlayerNumber >= 2)
                // Nếu chủ sở hữu là Bot AI -> Kẻ địch là Player (PlayerNumber == 1)
                if (m_OwnerPlayerNumber == 1)
                {
                    if (tank.m_PlayerNumber >= 2) isEnemy = true;
                }
                else
                {
                    if (tank.m_PlayerNumber == 1) isEnemy = true;
                }
            }

            if (!isEnemy) continue;

            float dist = Vector3.Distance(transform.position, tank.transform.position);
            if (dist < shortest && dist <= range) { 
                shortest = dist; 
                nearest = tank.transform; 
            }
        }
        target = nearest;
    }
}