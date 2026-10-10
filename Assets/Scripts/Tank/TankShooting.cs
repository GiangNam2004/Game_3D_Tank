using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun; 
using System.Collections; 
using System.Collections.Generic; 

public class TankShooting : MonoBehaviourPun 
{
    [Header("Icon Kỹ Năng UI")]
    public Sprite iconSkill1;
    public Sprite iconSkill2;
    public Sprite iconSkill3;

    [Header("--- KỸ NĂNG NÚT 1 (Tấn Công) ---")]
    public bool m_HasSpreadShot = true;    
    public bool m_HasGiantShell = false;   
    public bool m_HasElemental = false;    
    public Rigidbody m_IceShellPrefab;     
    public Rigidbody m_ToxicShellPrefab;   
    private bool m_IsNextIce = true;       
    // --- SKILL 1: TANK 3 ---
    public bool m_HasDeathSpin = false;    // Bắn đạn xoay vòng 8 hướng

    [Header("--- KỸ NĂNG NÚT 2 (Chiến Thuật) ---")]
    public bool m_HasMine = true;          
    public bool m_HasTurret = false;       
    public GameObject m_TurretPrefab;      
    public bool m_HasSmoke = false;        
    public GameObject m_SmokePrefab;
    // --- SKILL 2: TANK 3 ---
    public bool m_HasMagneticPull = false; // Hút quái và nổ
    public GameObject m_MagneticExplosion; // Kéo Prefab vụ nổ vào đây

    [Header("--- KỸ NĂNG NÚT 3 (Phòng Thủ/Cơ Động) ---")]
    public bool m_HasHomingMissile = true; 
    public bool m_HasClone = false;        
    public GameObject m_ClonePrefab;       
    public bool m_HasShield = false;       
    public GameObject m_ShieldPrefab;
    // --- SKILL 3: TANK 3 ---
    public bool m_HasDash = false;         
    public float m_DashSpeed = 30f;       
    public float m_DashDuration = 0.4f;   
    public float m_DashDamage = 40f;      
    public float m_KnockbackForce = 15f;  
    public float m_HitRadius = 3.5f;      
    private bool isDashing = false;       

    // --- Các biến hệ thống ---
    public Image imgCooldownDanChum;
    public Image imgCooldownThaMin;
    public Image imgCooldownTenLua;

    public Rigidbody m_HomingShellPrefab;
    public float m_HomingCooldown = 10f; 
    private float m_NextHomingTime = 0f;
    
    public int m_PlayerNumber = 1;              
    public Rigidbody m_Shell;                  
    public Transform m_FireTransform;          
    public Slider m_AimSlider;                  
    public AudioSource m_ShootingAudio;        
    public AudioClip m_ChargingClip;            
    public AudioClip m_FireClip;                
    public float m_MinLaunchForce = 5f;        
    public float m_MaxLaunchForce = 30f;        
    public float m_MaxChargeTime = 0.75f;     
    public GameObject m_MinePrefab;
    public float m_MineCooldown = 8f; 
    private float m_NextMineTime = 0f;  

    public float m_SkillCooldown = 5f;          
    private float m_NextSkillTime = 0f;         

    private float m_CurrentLaunchForce;         
    private float m_ChargeSpeed;                
    private bool m_Fired;                       
    private bool isCharging = false;

    private Transform playerTarget;
    private float botFireTimer = 0f;
    public float botFireInterval = 2.5f; 
    private float botSkillTimer = 0f;
    public float botSkillInterval = 6f;
    public float botAttackRange = 60f;
    public float botTargetRefreshInterval = 1f;
    public float botDesiredFlightTime = 2f;
    public float botMinimumLaunchForce = 5f;
    public float botMaximumLaunchForce = 30f;
    public bool botProjectileUsesGravity = false;
    private float botTargetRefreshTimer;

    private bool CanControl()
    {
        bool isOnlineGame = PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0;
        return (isOnlineGame && photonView.IsMine) || (!isOnlineGame && m_PlayerNumber == 1);
    }

    private bool IsBot()
    {
        bool isOnlineGame = PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0;
        return !isOnlineGame && m_PlayerNumber >= 2;
    }

    private void OnEnable()
    {
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;
        botSkillTimer = 0f;
        botTargetRefreshTimer = 0f;
    }

    private void Start ()
    {
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

        if (CanControl())
        {
            // Kết nối nút Bắn thường
            GameObject fireButton = GameObject.Find("FireButton");
            if (fireButton != null)
            {
                EventTrigger trigger = fireButton.GetComponent<EventTrigger>();
                if (trigger == null) trigger = fireButton.AddComponent<EventTrigger>();
                trigger.triggers.Clear();

                EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                pointerDown.callback.AddListener((eventData) => { OnPointerDown(); });
                trigger.triggers.Add(pointerDown);

                EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                pointerUp.callback.AddListener((eventData) => { OnPointerUp(); });
                trigger.triggers.Add(pointerUp);
            }

            // NÚT KỸ NĂNG 1
            GameObject btnDanChum = GameObject.Find("Btn_Skill_Spread");
            if (btnDanChum != null) 
            {
                if (iconSkill1 != null) btnDanChum.GetComponent<Image>().sprite = iconSkill1;
                Transform overlay = btnDanChum.transform.Find("CooldownOverlay");

                if (m_HasSpreadShot) {
                    btnDanChum.GetComponent<Button>().onClick.AddListener(FireSpreadShot);
                } else if (m_HasGiantShell) {
                    btnDanChum.GetComponent<Button>().onClick.AddListener(FireGiantShell);
                } else if (m_HasElemental) {
                    btnDanChum.GetComponent<Button>().onClick.AddListener(FireElementalShot);
                } else if (m_HasDeathSpin) { // <--- THÊM SKILL 1 TANK 3
                    btnDanChum.GetComponent<Button>().onClick.AddListener(FireDeathSpin);
                } else {
                    btnDanChum.SetActive(false);
                }
                if (btnDanChum.activeSelf && overlay != null) imgCooldownDanChum = overlay.GetComponent<Image>();
            }

            // NÚT KỸ NĂNG 2
            GameObject btnThaMin = GameObject.Find("Btn_Skill_Mine");
            if (btnThaMin != null) 
            {
                if (iconSkill2 != null) btnThaMin.GetComponent<Image>().sprite = iconSkill2;
                Transform overlay = btnThaMin.transform.Find("CooldownOverlay");

                if (m_HasMine) {
                    btnThaMin.GetComponent<Button>().onClick.AddListener(DropMine);
                } else if (m_HasTurret) {
                    btnThaMin.GetComponent<Button>().onClick.AddListener(PlaceTurret);
                } else if (m_HasSmoke) {
                    btnThaMin.GetComponent<Button>().onClick.AddListener(DropSmoke);
                } else if (m_HasMagneticPull) { // <--- THÊM SKILL 2 TANK 3
                    btnThaMin.GetComponent<Button>().onClick.AddListener(ActivateMagneticPull);
                } else {
                    btnThaMin.SetActive(false); 
                }
                if (btnThaMin.activeSelf && overlay != null) imgCooldownThaMin = overlay.GetComponent<Image>();
            }

            // NÚT KỸ NĂNG 3
            GameObject btnTenLua = GameObject.Find("Btn_Skill_Homing");
            if (btnTenLua != null) 
            {
                if (iconSkill3 != null) btnTenLua.GetComponent<Image>().sprite = iconSkill3;
                Transform overlay = btnTenLua.transform.Find("CooldownOverlay");

                if (m_HasHomingMissile) {
                    btnTenLua.GetComponent<Button>().onClick.AddListener(FireHomingMissile);
                } else if (m_HasClone) {
                    btnTenLua.GetComponent<Button>().onClick.AddListener(CreateClone);
                } else if (m_HasShield) {
                    btnTenLua.GetComponent<Button>().onClick.AddListener(ActivateShield);
                } else if (m_HasDash) { 
                    btnTenLua.GetComponent<Button>().onClick.AddListener(ActivateDash);
                } else {
                    btnTenLua.SetActive(false); 
                }
                if (btnTenLua.activeSelf && overlay != null) imgCooldownTenLua = overlay.GetComponent<Image>();
            }
        }
        else if (IsBot()) 
        {
            FindPlayerTarget();
        }
    }

    private void FindPlayerTarget()
    {
        TankMovement[] tanks = FindObjectsOfType<TankMovement>();
        for (int i = 0; i < tanks.Length; i++)
        {
            if (tanks[i].m_PlayerNumber == 1)
            {
                playerTarget = tanks[i].transform;
                break;
            }
        }
    }

    public void OnPointerDown()
    {
        if (!CanControl()) return; 
        
        isCharging = true;
        m_Fired = false;
        m_CurrentLaunchForce = m_MinLaunchForce;

        if (m_ShootingAudio != null) { m_ShootingAudio.clip = m_ChargingClip; m_ShootingAudio.Play (); }
    }

    public void OnPointerUp()
    {
        if (!CanControl() || !isCharging) return;
        
        isCharging = false;
        if (!m_Fired) Fire ();
    }

    private void Update ()
    {
        m_AimSlider.value = m_MinLaunchForce;

        if (CanControl())
        {
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired) {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire ();
            }
            else if (isCharging && !m_Fired) {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                m_AimSlider.value = m_CurrentLaunchForce;
            }
        }
        else if (IsBot()) { BotUpdate(); }

        if (imgCooldownDanChum != null) imgCooldownDanChum.fillAmount = Mathf.Max(0, (m_NextSkillTime - Time.time) / m_SkillCooldown);
        if (imgCooldownThaMin != null) imgCooldownThaMin.fillAmount = Mathf.Max(0, (m_NextMineTime - Time.time) / m_MineCooldown);
        if (imgCooldownTenLua != null) imgCooldownTenLua.fillAmount = Mathf.Max(0, (m_NextHomingTime - Time.time) / m_HomingCooldown);
    }

    private void BotUpdate()
    {
        botTargetRefreshTimer -= Time.deltaTime;
        if (playerTarget == null || botTargetRefreshTimer <= 0f)
        {
            FindPlayerTarget();
            botTargetRefreshTimer = botTargetRefreshInterval;
        }

        if (playerTarget == null)
            return;

        if (Vector3.Distance(transform.position, playerTarget.position) < botAttackRange)
        {
            botFireTimer += Time.deltaTime;
            botSkillTimer += Time.deltaTime;
            if (botFireTimer >= botFireInterval)
            {
                botFireTimer = 0f;
                m_CurrentLaunchForce = CalculateBotLaunchForce();
                Fire();
            }

            if (botSkillTimer >= botSkillInterval)
            {
                botSkillTimer = 0f;
                UseBotSkill();
            }
        }
        else
        {
            botFireTimer = 0f;
            botSkillTimer = 0f;
        }
    }

    private void UseBotSkill()
    {
        if (m_HasShield)
            ActivateShield();
        else if (m_HasDeathSpin)
            FireDeathSpin();
        else if (m_HasElemental)
            FireElementalShot();
        else if (m_HasGiantShell)
            FireGiantShell();
    }

    private float CalculateBotLaunchForce()
    {
        if (playerTarget == null)
            return botMinimumLaunchForce;

        Vector3 targetPosition = playerTarget.position + Vector3.up;
        Collider targetCollider = playerTarget.GetComponentInChildren<Collider>();
        if (targetCollider != null)
            targetPosition = targetCollider.bounds.center;

        float distance = Vector3.Distance(m_FireTransform.position, targetPosition);
        float flightTime = Mathf.Max(0.1f, botDesiredFlightTime);
        return Mathf.Clamp(distance / flightTime, botMinimumLaunchForce, botMaximumLaunchForce);
    }

    private void Fire ()
    {
        m_Fired = true;
        isCharging = false;
        Rigidbody shellInstance = Instantiate (m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
        shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;
        if (m_ShootingAudio != null) { m_ShootingAudio.clip = m_FireClip; m_ShootingAudio.Play (); }
        m_CurrentLaunchForce = m_MinLaunchForce;
    }

    // ============================================
    // CÁC KỸ NĂNG CŨ (TANK MẶC ĐỊNH, TANK 1, TANK 2)
    // ============================================
    public void FireSpreadShot() { /* Mã cũ... */ if (Time.time < m_NextSkillTime) return; float angle = 20f; Vector3 spawnPos = m_FireTransform.position; Vector3 offset = m_FireTransform.right * 1f; Quaternion centerRot = m_FireTransform.rotation; Quaternion leftRot = m_FireTransform.rotation * Quaternion.Euler(0, -angle, 0); Quaternion rightRot = m_FireTransform.rotation * Quaternion.Euler(0, angle, 0); Rigidbody shellCenter = Instantiate(m_Shell, spawnPos, centerRot) as Rigidbody; Rigidbody shellLeft = Instantiate(m_Shell, spawnPos - offset, leftRot) as Rigidbody; Rigidbody shellRight = Instantiate(m_Shell, spawnPos + offset, rightRot) as Rigidbody; float skillForce = m_MaxLaunchForce; shellCenter.velocity = skillForce * (centerRot * Vector3.forward); shellLeft.velocity = skillForce * (leftRot * Vector3.forward); shellRight.velocity = skillForce * (rightRot * Vector3.forward); if(m_ShootingAudio != null) { m_ShootingAudio.clip = m_FireClip; m_ShootingAudio.Play(); } m_NextSkillTime = Time.time + m_SkillCooldown; }
    public void DropMine() { /* Mã cũ... */ if (Time.time < m_NextMineTime) return; Vector3 dropPos = transform.position - transform.forward * 2.5f; dropPos.y += 0.5f; if (m_MinePrefab != null) Instantiate(m_MinePrefab, dropPos, transform.rotation); m_NextMineTime = Time.time + m_MineCooldown; }
    public void FireHomingMissile() { /* Mã cũ... */ if (Time.time < m_NextHomingTime) return; Rigidbody shellInstance = Instantiate(m_HomingShellPrefab, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody; HomingMissile homingScript = shellInstance.GetComponent<HomingMissile>(); if (homingScript != null) homingScript.m_ShooterPlayerNumber = m_PlayerNumber; if(m_ShootingAudio != null) { m_ShootingAudio.clip = m_FireClip; m_ShootingAudio.Play(); } m_NextHomingTime = Time.time + m_HomingCooldown; }
    public void FireGiantShell() { /* Mã cũ... */ if (Time.time < m_NextSkillTime) return; Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody; shellInstance.transform.localScale = new Vector3(3f, 3f, 3f); shellInstance.mass = 5f; shellInstance.velocity = m_MaxLaunchForce * m_FireTransform.forward; if(m_ShootingAudio != null) { m_ShootingAudio.clip = m_FireClip; m_ShootingAudio.Play(); } m_NextSkillTime = Time.time + m_SkillCooldown; }
    public void PlaceTurret() { /* Mã cũ... */ if (Time.time < m_NextMineTime) return; Vector3 dropPos = transform.position + transform.right * 2f; if (m_TurretPrefab != null) { Instantiate(m_TurretPrefab, dropPos, transform.rotation); } m_NextMineTime = Time.time + m_MineCooldown; }
    public void CreateClone() { /* Mã cũ... */ if (Time.time < m_NextHomingTime) return; if (m_ClonePrefab != null) { GameObject clone = Instantiate(m_ClonePrefab, transform.position, transform.rotation); Destroy(clone, 7f); } float dashDistance = 12f; Vector3 dashTarget = transform.position + transform.forward * dashDistance; RaycastHit hit; if (Physics.Raycast(transform.position, transform.forward, out hit, dashDistance)) { dashTarget = hit.point - transform.forward * 1.5f; } transform.position = dashTarget; m_NextHomingTime = Time.time + m_HomingCooldown; }
    public void DropSmoke() { /* Mã cũ... */ if (Time.time < m_NextMineTime) return; Vector3 dropPos = transform.position - transform.forward * 2f; if (m_SmokePrefab != null) Instantiate(m_SmokePrefab, dropPos, transform.rotation); m_NextMineTime = Time.time + m_MineCooldown; }
    public void FireElementalShot() { /* Mã cũ... */ if (Time.time < m_NextSkillTime) return; Rigidbody shellPrefab = m_IsNextIce ? m_IceShellPrefab : m_ToxicShellPrefab; if (shellPrefab != null) { Rigidbody shell = Instantiate(shellPrefab, m_FireTransform.position, m_FireTransform.rotation); shell.velocity = m_CurrentLaunchForce * m_FireTransform.forward; } m_IsNextIce = !m_IsNextIce; if (m_ShootingAudio != null) { m_ShootingAudio.clip = m_FireClip; m_ShootingAudio.Play(); } m_NextSkillTime = Time.time + m_SkillCooldown; }
    public void ActivateShield() { /* Mã cũ... */ if (Time.time < m_NextHomingTime) return; if (m_ShieldPrefab != null) { Instantiate(m_ShieldPrefab, transform.position, transform.rotation, transform); } m_NextHomingTime = Time.time + m_HomingCooldown; }

    // ============================================
    // KỸ NĂNG MỚI (DÀNH CHO TANK 3 - 3 NÚT)
    // ============================================
    
    // NÚT 1: Cối Xay Tử Thần (Bắn đạn ra 8 hướng)
    public void FireDeathSpin()
    {
        if (Time.time < m_NextSkillTime) return;
        StartCoroutine(DeathSpinRoutine());
        m_NextSkillTime = Time.time + m_SkillCooldown;
    }

    private IEnumerator DeathSpinRoutine()
    {
        float spinDuration = 2f;    // Kéo dài 2 giây
        float fireRate = 0.2f;      // Cứ 0.2s xả đạn 1 lần
        float timer = 0f;
        float spinOffset = 0f;

        while(timer < spinDuration)
        {
            for (int i = 0; i < 8; i++) // Bắn ra 8 viên đạn
            {
                float angle = (360f / 8) * i + spinOffset;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);
                Vector3 fireDir = rotation * Vector3.forward;
                
                // Nâng tâm bắn lên cao một chút và cách xe ra để khỏi tự dính đạn
                Vector3 spawnPos = transform.position + Vector3.up * 1.5f + fireDir * 2f;
                
                Rigidbody shell = Instantiate(m_Shell, spawnPos, rotation);
                shell.velocity = m_MaxLaunchForce * fireDir;
            }
            spinOffset += 15f; // Đợt sau xoay nòng đi 1 chút để tạo lốc xoáy
            if (m_ShootingAudio != null) { m_ShootingAudio.clip = m_FireClip; m_ShootingAudio.Play(); }
            
            timer += fireRate;
            yield return new WaitForSeconds(fireRate);
        }
    }

   // ============================================
    // KỸ NĂNG MỚI (DÀNH CHO TANK 3)
    // ============================================

    // --- NÚT 2: HÚT TỪ TRƯỜNG & NỔ ---
    public void ActivateMagneticPull()
    {
        if (Time.time < m_NextMineTime) return;
        StartCoroutine(MagneticPullRoutine());
        m_NextMineTime = Time.time + m_MineCooldown;
    }

  // --- NÚT 2: HÚT TỪ TRƯỜNG & NỔ ---
    private System.Collections.IEnumerator MagneticPullRoutine()
    {
        Debug.Log("==> ĐANG HÚT QUÁI!"); 
        float pullDuration = 1.5f;   
        float pullRadius = 15f;      
        float pullSpeed = 15f; 
        float safeDistance = 3.5f; // Khoảng cách an toàn để 2 xe không bị xuyên vào nhau
        float timer = 0f;

        while (timer < pullDuration)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, pullRadius);
            foreach(Collider hit in hits)
            {
                Rigidbody enemyRb = hit.GetComponentInParent<Rigidbody>();
                
                if (enemyRb != null && enemyRb.gameObject != this.gameObject)
                {
                    string objName = enemyRb.gameObject.name;
                    if (enemyRb.CompareTag("Enemy") || enemyRb.CompareTag("Player") || objName.Contains("Enemy") || objName.Contains("Boss") || objName.Contains("Tank"))
                    {
                        Vector3 targetPos = transform.position;
                        targetPos.y = enemyRb.position.y; 
                        
                        // CHỈ HÚT KHI XE ĐỊCH CÒN CÁCH MÌNH XA HƠN 3.5 MÉT
                        if (Vector3.Distance(enemyRb.position, targetPos) > safeDistance)
                        {
                            enemyRb.position = Vector3.MoveTowards(enemyRb.position, targetPos, pullSpeed * Time.deltaTime);
                        }
                    }
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        Debug.Log("==> BÙM!");
        if (m_MagneticExplosion != null)
        {
            Instantiate(m_MagneticExplosion, transform.position, Quaternion.identity);
        }

        Collider[] damageHits = Physics.OverlapSphere(transform.position, pullRadius);
        foreach(Collider hit in damageHits)
        {
            Rigidbody hitRb = hit.GetComponentInParent<Rigidbody>();
            if (hitRb != null && hitRb.gameObject != this.gameObject)
            {
                string objName = hitRb.gameObject.name;
                if (hitRb.CompareTag("Enemy") || hitRb.CompareTag("Player") || objName.Contains("Enemy") || objName.Contains("Boss") || objName.Contains("Tank"))
                {
                    hitRb.SendMessage("TakeDamage", 50f, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    // --- NÚT 3: HÚC (DASH) ---
    public void ActivateDash()
    {
        if (Time.time < m_NextHomingTime || isDashing) return;
        StartCoroutine(DashRoutine());
        m_NextHomingTime = Time.time + m_HomingCooldown;
    }

    // --- NÚT 3: HÚC (DASH) ---
    private System.Collections.IEnumerator DashRoutine()
    {
        Debug.Log("==> ĐANG HÚC!"); 
        isDashing = true;
        
        MonoBehaviour movementScript = GetComponent("TankMovement") as MonoBehaviour;
        if (movementScript != null) movementScript.enabled = false; 

        float timer = 0f;
        System.Collections.Generic.HashSet<GameObject> hitEnemies = new System.Collections.Generic.HashSet<GameObject>();

        while (timer < m_DashDuration)
        {
            transform.position += transform.forward * m_DashSpeed * Time.deltaTime;
            
            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward, m_HitRadius);
            foreach (Collider hit in hits)
            {
                Rigidbody enemyRb = hit.GetComponentInParent<Rigidbody>();
                
                if (enemyRb != null && enemyRb.gameObject != this.gameObject)
                {
                    string objName = enemyRb.gameObject.name;
                    if (enemyRb.CompareTag("Enemy") || enemyRb.CompareTag("Player") || objName.Contains("Enemy") || objName.Contains("Boss") || objName.Contains("Tank"))
                    {
                        if (!hitEnemies.Contains(enemyRb.gameObject))
                        {
                            hitEnemies.Add(enemyRb.gameObject); 
                            Debug.Log("Đã ủi trúng: " + enemyRb.name);
                            
                            Vector3 knockbackDir = (enemyRb.position - transform.position).normalized;
                            knockbackDir.y = 0; 
                            enemyRb.position += knockbackDir * 4f; 
                            
                            enemyRb.SendMessage("TakeDamage", m_DashDamage, SendMessageOptions.DontRequireReceiver);
                        }
                    }
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }
        
        isDashing = false; 
        if (movementScript != null) movementScript.enabled = true; 
    }
}