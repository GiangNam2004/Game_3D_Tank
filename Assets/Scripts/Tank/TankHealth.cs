using UnityEngine;
using UnityEngine.UI;
using Photon.Pun; // 1. Bắt buộc thêm thư viện mạng

public class TankHealth : MonoBehaviourPun // 2. Đổi từ MonoBehaviour sang MonoBehaviourPun
{
    public float m_StartingHealth = 100f;          
    public Slider m_Slider;                        
    public Image m_FillImage;                      
    public Color m_FullHealthColor = Color.green;  
    public Color m_ZeroHealthColor = Color.red;    
    public GameObject m_ExplosionPrefab;
    
    private AudioSource m_ExplosionAudio;          
    private ParticleSystem m_ExplosionParticles;   
    public float m_CurrentHealth; 
    private bool m_Dead;           

    private void Awake()
    {
        m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
        m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();

        m_ExplosionParticles.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        m_CurrentHealth = m_StartingHealth;
        m_Dead = false;

        SetHealthUI();
    }
    
    public void TakeDamage(float amount)
    {
        // 3. Thay vì tự trừ máu cục bộ, hãy gửi lệnh (RPC) cho TẤT CẢ các máy trong phòng
        // RpcTarget.All đảm bảo cả máy người bắn và máy nạn nhân đều chạy lệnh trừ máu cùng lúc
        photonView.RPC("TakeDamageRPC", RpcTarget.All, amount);
    }

    [PunRPC] // 4. Gắn nhãn này để Photon nhận diện đây là Hàm nhận tin nhắn qua mạng
    public void TakeDamageRPC(float amount)
    {
        // Mọi logic trừ máu, đổi màu thanh máu và nổ tung được dời vào đây
        m_CurrentHealth -= amount;
        SetHealthUI();

        if(m_CurrentHealth <= 0f && !m_Dead)
        {
            OnDeath();
        }
    }

    private void SetHealthUI()
    {
        m_Slider.value = m_CurrentHealth;
        m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth/m_StartingHealth);
    }

    private void OnDeath()
    {
        m_Dead = true;
        m_ExplosionParticles.transform.position = transform.position;
        m_ExplosionParticles.gameObject.SetActive(true);

        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();

        gameObject.SetActive(false);
    }
}