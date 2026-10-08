using UnityEngine;

public class TurretAI : MonoBehaviour {
    [Header("Chỉ số Ụ súng")]
    public float range = 40f;      // TĂNG TẦM BẮN (cũ: 25)
    public float fireRate = 0.5f;  // BẮN CHẬM LẠI (0.5 nghĩa là 2 giây mới bắn 1 viên. Cũ: 1.5)
    public float lifeTime = 10f;   // THỜI GIAN TỒN TẠI (Ụ súng sẽ biến mất sau 10 giây)

    public Rigidbody shellPrefab;
    public Transform firePoint;
    public float launchForce = 25f; // Tăng nhẹ lực bắn để đạn bay được xa hơn
    
    private float fireCountdown = 0f;
    private Transform target;

    void Start() {
        // Lệnh này giúp Ụ súng tự động biến mất sau [lifeTime] giây
        Destroy(gameObject, lifeTime);
    }

    void Update() {
        FindTarget();
        if (target == null) return;
        
        // Xoay súng về phía kẻ địch
        Vector3 dir = target.position - transform.position;
        transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(dir).eulerAngles.y, 0f);
        
        // Bắn đạn
        if (fireCountdown <= 0f) {
            if (firePoint != null && shellPrefab != null) {
                Rigidbody shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
                shell.velocity = firePoint.forward * launchForce;
            }
            fireCountdown = 1f / fireRate; // Đặt lại thời gian chờ
        }
        fireCountdown -= Time.deltaTime;
    }

    void FindTarget() {
        TankMovement[] tanks = FindObjectsOfType<TankMovement>();
        float shortest = Mathf.Infinity;
        Transform nearest = null;
        foreach (var tank in tanks) {
            // Chỉ bắn xe có PlayerNumber >= 2 (Bot)
            if (tank.m_PlayerNumber >= 2 && tank.gameObject.activeSelf) {
                float dist = Vector3.Distance(transform.position, tank.transform.position);
                if (dist < shortest && dist <= range) { 
                    shortest = dist; 
                    nearest = tank.transform; 
                }
            }
        }
        target = nearest;
    }
}