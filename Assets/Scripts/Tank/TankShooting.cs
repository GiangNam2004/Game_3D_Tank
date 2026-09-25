using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TankShooting : MonoBehaviour
{
    public Rigidbody m_HomingShellPrefab;
    public float m_HomingCooldown = 10f; // Kỹ năng xịn nên hồi chiêu lâu chút (10s)
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
    public float m_MineCooldown = 8f; // Hồi chiêu 8 giây
    private float m_NextMineTime = 0f;  

    // --- Biến cho Kỹ năng Đạn Chùm ---
    public float m_SkillCooldown = 5f;          // Thời gian chờ (5 giây)
    private float m_NextSkillTime = 0f;         // Bộ đếm thời gian

    private float m_CurrentLaunchForce;         
    private float m_ChargeSpeed;                
    private bool m_Fired;                       
    
    // --- Biến cho Người chơi 1 ---
    private bool isCharging = false;

    // --- Biến cho Bot AI (Người chơi 2) ---
    private Transform playerTarget;
    private float botFireTimer = 0f;
    public float botFireInterval = 2.5f; 

    private void OnEnable()
    {
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;
    }

    private void Start ()
    {
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

        if (m_PlayerNumber == 1)
        {
            GameObject fireButton = GameObject.Find("FireButton");
            if (fireButton == null) return;

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
        else if (m_PlayerNumber == 2)
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
        if (m_PlayerNumber != 1) return; 
        
        isCharging = true;
        m_Fired = false;
        m_CurrentLaunchForce = m_MinLaunchForce;

        m_ShootingAudio.clip = m_ChargingClip;
        m_ShootingAudio.Play ();
    }

    public void OnPointerUp()
    {
        if (m_PlayerNumber != 1 || !isCharging) return;
        
        isCharging = false;
        if (!m_Fired) Fire ();
    }

    private void Update ()
    {
        m_AimSlider.value = m_MinLaunchForce;

        if (m_PlayerNumber == 1)
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
        else if (m_PlayerNumber == 2)
        {
            BotUpdate();
        }
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

        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play ();

        m_CurrentLaunchForce = m_MinLaunchForce;
    }

    // ==========================================
    // HÀM KÍCH HOẠT KỸ NĂNG ĐẠN CHÙM
    // ==========================================
    public void FireSpreadShot()
    {
        if (Time.time < m_NextSkillTime) return; 

        float angle = 20f; 
        
        // SỬA LỖI: Tách vị trí sinh đạn sang 2 bên hông (khoảng 1 đơn vị) để chúng không chạm nhau
        Vector3 spawnPos = m_FireTransform.position;
        Vector3 offset = m_FireTransform.right * 1f; 

        Quaternion centerRot = m_FireTransform.rotation;
        Quaternion leftRot = m_FireTransform.rotation * Quaternion.Euler(0, -angle, 0);
        Quaternion rightRot = m_FireTransform.rotation * Quaternion.Euler(0, angle, 0);

        // Đưa offset vào lệnh sinh đạn
        Rigidbody shellCenter = Instantiate(m_Shell, spawnPos, centerRot) as Rigidbody;
        Rigidbody shellLeft = Instantiate(m_Shell, spawnPos - offset, leftRot) as Rigidbody;
        Rigidbody shellRight = Instantiate(m_Shell, spawnPos + offset, rightRot) as Rigidbody;

        float skillForce = m_MaxLaunchForce;
        shellCenter.velocity = skillForce * (centerRot * Vector3.forward);
        shellLeft.velocity = skillForce * (leftRot * Vector3.forward);
        shellRight.velocity = skillForce * (rightRot * Vector3.forward);
        
        if(m_ShootingAudio != null)
        {
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
        }

        m_NextSkillTime = Time.time + m_SkillCooldown; 
    }
    public void DropMine()
    {
        if (Time.time < m_NextMineTime) return;

        // Vị trí thả: Lùi lại sau đuôi xe 2.5 mét và nâng lên khỏi mặt đất 0.5 mét
        Vector3 dropPos = transform.position - transform.forward * 2.5f;
        dropPos.y += 0.5f; 

        if (m_MinePrefab != null)
        {
            Instantiate(m_MinePrefab, dropPos, transform.rotation);
        }

        m_NextMineTime = Time.time + m_MineCooldown;
    }
    public void FireHomingMissile()
    {
        if (Time.time < m_NextHomingTime) return;

        // Sinh ra tên lửa đuổi
        Rigidbody shellInstance = Instantiate(m_HomingShellPrefab, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
        
        // Truyền thông tin để tên lửa biết ai là chủ nhân, tránh việc tự quay lại cắn chủ
        HomingMissile homingScript = shellInstance.GetComponent<HomingMissile>();
        if (homingScript != null)
        {
            homingScript.m_ShooterPlayerNumber = m_PlayerNumber;
        }

        if(m_ShootingAudio != null)
        {
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
        }

        m_NextHomingTime = Time.time + m_HomingCooldown;
    }
}