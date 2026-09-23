using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TankShooting : MonoBehaviour
{
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

    private float m_CurrentLaunchForce;         
    private float m_ChargeSpeed;                
    private bool m_Fired;                       
    
    // --- Biến cho Người chơi 1 ---
    private bool isCharging = false;

    // --- Biến cho Bot AI (Người chơi 2) ---
    private Transform playerTarget;
    private float botFireTimer = 0f;
    public float botFireInterval = 2.5f; // Chu kỳ khai hỏa của bot (giây)

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
            // Chỉ xe số 1 mới được phép liên kết với FireButton
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
            // Khởi tạo tầm nhìn cho Bot
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
        if (m_PlayerNumber != 1) return; // Chặn bot không được dùng UI
        
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

        // Tính toán khoảng cách để quyết định có bắn hay không
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer < 25f)
        {
            botFireTimer += Time.deltaTime;
            if (botFireTimer >= botFireInterval)
            {
                botFireTimer = 0f;
                // Bot chọn một lực bắn ngẫu nhiên trong khoảng cho phép
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
}