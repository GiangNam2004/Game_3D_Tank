using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun; 

public class TankShooting : MonoBehaviourPun 
{
    [Header("Icon Kỹ Năng UI")]
    public Sprite iconSkill1;
    public Sprite iconSkill2;
    public Sprite iconSkill3;

    [Header("--- KỸ NĂNG NÚT 1 (Tấn Công) ---")]
    public bool m_HasSpreadShot = true;    // Đạn chùm (Tank cũ)
    public bool m_HasGiantShell = false;   // Đạn khổng lồ (Tank 1)
    public bool m_HasElemental = false;    // Đạn nguyên tố (Tank 2)
    public Rigidbody m_IceShellPrefab;     
    public Rigidbody m_ToxicShellPrefab;   
    private bool m_IsNextIce = true;       

    [Header("--- KỸ NĂNG NÚT 2 (Chiến Thuật) ---")]
    public bool m_HasMine = true;          // Thả mìn (Tank cũ)
    public bool m_HasTurret = false;       // Đặt Ụ súng (Tank 1)
    public GameObject m_TurretPrefab;      
    public bool m_HasSmoke = false;        // Thả Khói (Tank 2)
    public GameObject m_SmokePrefab;

    [Header("--- KỸ NĂNG NÚT 3 (Phòng Thủ/Cơ Động) ---")]
    public bool m_HasHomingMissile = true; // Đạn đuổi (Tank cũ)
    public bool m_HasClone = false;        // Phân thân (Tank 1)
    public GameObject m_ClonePrefab;       
    public bool m_HasShield = false;       // Khiên phản đạn (Tank 2)
    public GameObject m_ShieldPrefab;

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
    public float m_MinLaunchForce = 15f;        
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
                    if (overlay != null) imgCooldownDanChum = overlay.GetComponent<Image>();
                } else if (m_HasGiantShell) {
                    btnDanChum.GetComponent<Button>().onClick.AddListener(FireGiantShell);
                    if (overlay != null) imgCooldownDanChum = overlay.GetComponent<Image>();
                } else if (m_HasElemental) {
                    btnDanChum.GetComponent<Button>().onClick.AddListener(FireElementalShot);
                    if (overlay != null) imgCooldownDanChum = overlay.GetComponent<Image>();
                } else {
                    btnDanChum.SetActive(false);
                }
            }

            // NÚT KỸ NĂNG 2
            GameObject btnThaMin = GameObject.Find("Btn_Skill_Mine");
            if (btnThaMin != null) 
            {
                if (iconSkill2 != null) btnThaMin.GetComponent<Image>().sprite = iconSkill2;

                Transform overlay = btnThaMin.transform.Find("CooldownOverlay");
                if (m_HasMine) {
                    btnThaMin.GetComponent<Button>().onClick.AddListener(DropMine);
                    if (overlay != null) imgCooldownThaMin = overlay.GetComponent<Image>();
                } else if (m_HasTurret) {
                    btnThaMin.GetComponent<Button>().onClick.AddListener(PlaceTurret);
                    if (overlay != null) imgCooldownThaMin = overlay.GetComponent<Image>();
                } else if (m_HasSmoke) {
                    btnThaMin.GetComponent<Button>().onClick.AddListener(DropSmoke);
                    if (overlay != null) imgCooldownThaMin = overlay.GetComponent<Image>();
                } else {
                    btnThaMin.SetActive(false); 
                }
            }

            // NÚT KỸ NĂNG 3
            GameObject btnTenLua = GameObject.Find("Btn_Skill_Homing");
            if (btnTenLua != null) 
            {
                if (iconSkill3 != null) btnTenLua.GetComponent<Image>().sprite = iconSkill3;

                Transform overlay = btnTenLua.transform.Find("CooldownOverlay");
                if (m_HasHomingMissile) {
                    btnTenLua.GetComponent<Button>().onClick.AddListener(FireHomingMissile);
                    if (overlay != null) imgCooldownTenLua = overlay.GetComponent<Image>();
                } else if (m_HasClone) {
                    btnTenLua.GetComponent<Button>().onClick.AddListener(CreateClone);
                    if (overlay != null) imgCooldownTenLua = overlay.GetComponent<Image>();
                } else if (m_HasShield) {
                    btnTenLua.GetComponent<Button>().onClick.AddListener(ActivateShield);
                    if (overlay != null) imgCooldownTenLua = overlay.GetComponent<Image>();
                } else {
                    btnTenLua.SetActive(false); 
                }
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

        if (m_ShootingAudio != null)
        {
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play ();
        }
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
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire ();
            }
            else if (isCharging && !m_Fired)
            {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                m_AimSlider.value = m_CurrentLaunchForce;
            }
        }
        else if (IsBot())
        {
            BotUpdate();
        }

        if (imgCooldownDanChum != null) 
            imgCooldownDanChum.fillAmount = Mathf.Max(0, (m_NextSkillTime - Time.time) / m_SkillCooldown);
            
        if (imgCooldownThaMin != null) 
            imgCooldownThaMin.fillAmount = Mathf.Max(0, (m_NextMineTime - Time.time) / m_MineCooldown);
            
        if (imgCooldownTenLua != null) 
            imgCooldownTenLua.fillAmount = Mathf.Max(0, (m_NextHomingTime - Time.time) / m_HomingCooldown);
    }

    private void BotUpdate()
    {
        if (playerTarget == null)
        {
            FindPlayerTarget();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer < 25f)
        {
            botFireTimer += Time.deltaTime;
            if (botFireTimer >= botFireInterval)
            {
                botFireTimer = 0f;
                m_CurrentLaunchForce = Random.Range(m_MinLaunchForce, m_MaxLaunchForce);
                Fire();
            }
        }
        else
        {
            botFireTimer = 0f; 
        }
    }

    private void Fire ()
    {
        m_Fired = true;
        isCharging = false;

        Rigidbody shellInstance = Instantiate (m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
        shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward; 

        if (m_ShootingAudio != null) {
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play ();
        }
        m_CurrentLaunchForce = m_MinLaunchForce;
    }

    // ============================================
    // CÁC KỸ NĂNG CŨ VÀ TANK 1
    // ============================================
    public void FireSpreadShot()
    {
        if (Time.time < m_NextSkillTime) return; 
        float angle = 20f; 
        Vector3 spawnPos = m_FireTransform.position;
        Vector3 offset = m_FireTransform.right * 1f; 
        Quaternion centerRot = m_FireTransform.rotation;
        Quaternion leftRot = m_FireTransform.rotation * Quaternion.Euler(0, -angle, 0);
        Quaternion rightRot = m_FireTransform.rotation * Quaternion.Euler(0, angle, 0);

        Rigidbody shellCenter = Instantiate(m_Shell, spawnPos, centerRot) as Rigidbody;
        Rigidbody shellLeft = Instantiate(m_Shell, spawnPos - offset, leftRot) as Rigidbody;
        Rigidbody shellRight = Instantiate(m_Shell, spawnPos + offset, rightRot) as Rigidbody;

        float skillForce = m_MaxLaunchForce;
        shellCenter.velocity = skillForce * (centerRot * Vector3.forward);
        shellLeft.velocity = skillForce * (leftRot * Vector3.forward);
        shellRight.velocity = skillForce * (rightRot * Vector3.forward);
        
        if(m_ShootingAudio != null) { m_ShootingAudio.clip = m_FireClip; m_ShootingAudio.Play(); }
        m_NextSkillTime = Time.time + m_SkillCooldown; 
    }

    public void DropMine()
    {
        if (Time.time < m_NextMineTime) return;
        Vector3 dropPos = transform.position - transform.forward * 2.5f;
        dropPos.y += 0.5f; 
        if (m_MinePrefab != null) Instantiate(m_MinePrefab, dropPos, transform.rotation);
        m_NextMineTime = Time.time + m_MineCooldown;
    }

    public void FireHomingMissile()
    {
        if (Time.time < m_NextHomingTime) return;
        Rigidbody shellInstance = Instantiate(m_HomingShellPrefab, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
        HomingMissile homingScript = shellInstance.GetComponent<HomingMissile>();
        if (homingScript != null) homingScript.m_ShooterPlayerNumber = m_PlayerNumber;

        if(m_ShootingAudio != null) { m_ShootingAudio.clip = m_FireClip; m_ShootingAudio.Play(); }
        m_NextHomingTime = Time.time + m_HomingCooldown;
    }

    public void FireGiantShell()
    {
        if (Time.time < m_NextSkillTime) return; 

        Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
        shellInstance.transform.localScale = new Vector3(3f, 3f, 3f);
        shellInstance.mass = 5f; 
        shellInstance.velocity = m_MaxLaunchForce * m_FireTransform.forward;
        
        if(m_ShootingAudio != null) {
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
        }
        m_NextSkillTime = Time.time + m_SkillCooldown; 
    }

    public void PlaceTurret()
    {
        if (Time.time < m_NextMineTime) return;

        Vector3 dropPos = transform.position + transform.right * 2f;
        if (m_TurretPrefab != null)
        {
            Instantiate(m_TurretPrefab, dropPos, transform.rotation);
        }
        m_NextMineTime = Time.time + m_MineCooldown;
    }

    public void CreateClone()
    {
        if (Time.time < m_NextHomingTime) return;

        if (m_ClonePrefab != null)
        {
            GameObject clone = Instantiate(m_ClonePrefab, transform.position, transform.rotation);
            Destroy(clone, 7f); 
        }

        float dashDistance = 12f;
        Vector3 dashTarget = transform.position + transform.forward * dashDistance;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, dashDistance))
        {
            dashTarget = hit.point - transform.forward * 1.5f; 
        }
        
        transform.position = dashTarget;
        m_NextHomingTime = Time.time + m_HomingCooldown;
    }

    // ============================================
    // CÁC KỸ NĂNG MỚI (DÀNH CHO TANK 2)
    // ============================================
    public void DropSmoke()
    {
        if (Time.time < m_NextMineTime) return;
        Vector3 dropPos = transform.position - transform.forward * 2f;
        if (m_SmokePrefab != null) Instantiate(m_SmokePrefab, dropPos, transform.rotation);
        m_NextMineTime = Time.time + m_MineCooldown;
    }

    public void FireElementalShot()
    {
        if (Time.time < m_NextSkillTime) return; 

        Rigidbody shellPrefab = m_IsNextIce ? m_IceShellPrefab : m_ToxicShellPrefab;
        if (shellPrefab != null)
        {
            Rigidbody shell = Instantiate(shellPrefab, m_FireTransform.position, m_FireTransform.rotation);
            shell.velocity = m_CurrentLaunchForce * m_FireTransform.forward;
        }

        // Đổi loại đạn cho lần bắn sau
        m_IsNextIce = !m_IsNextIce; 
        
        if (m_ShootingAudio != null) {
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
        }
        m_NextSkillTime = Time.time + m_SkillCooldown; 
    }

    public void ActivateShield()
    {
        if (Time.time < m_NextHomingTime) return;

        if (m_ShieldPrefab != null)
        {
            Instantiate(m_ShieldPrefab, transform.position, transform.rotation, transform);
        }
        m_NextHomingTime = Time.time + m_HomingCooldown;
    }
}