using UnityEngine;
using UnityEngine.UI;
using Photon.Pun; 

public class TankHealth : MonoBehaviourPun 
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
        if (PhotonNetwork.InRoom && photonView.ViewID != 0)
        {
            photonView.RPC("TakeDamageRPC", RpcTarget.All, amount);
        }
        else 
        {
            TakeDamageRPC(amount);
        }
    }

    [PunRPC] 
    public void TakeDamageRPC(float amount)
    {
        m_CurrentHealth -= amount;
        SetHealthUI();

        if(m_CurrentHealth <= 0f && !m_Dead)
        {
            OnDeath();
        }
    }

    public bool CanApplyPickup(PickupType type)
    {
        if (type == PickupType.ToolChest)
            return m_CurrentHealth < m_StartingHealth;

        if (type == PickupType.ReflectShield)
            return GetComponentInChildren<ReflectShield>(true) == null;

        return true;
    }

    public bool ApplyPickup(PickupType type, float healAmount, float speedMultiplier, float speedDuration)
    {
        switch (type)
        {
            case PickupType.ToolChest:
            // Thêm kiểm tra photonView != null để chống văng lỗi NullReference ở chế độ Offline
            if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0)
                photonView.RPC("HealRPC", RpcTarget.All, healAmount);
            else
                HealRPC(healAmount);
            return true;

            case PickupType.FuelTank:
                TankMovement movement = GetComponent<TankMovement>();
                if (movement != null)
                    movement.ApplySpeedBoost(speedMultiplier, speedDuration);
                return true;

            case PickupType.ReflectShield:
                return ActivateReflectShield();
        }

        return false;
    }

    // HÀM MỚI: Đồng bộ lượng máu được hồi qua mạng
    [PunRPC]
    public void HealRPC(float amount)
    {
        m_CurrentHealth = Mathf.Min(m_StartingHealth, m_CurrentHealth + amount);
        SetHealthUI();
    }

    private bool ActivateReflectShield()
    {
        GameObject shieldPrefab = Resources.Load<GameObject>("ReflectShield_Prefab");
        if (shieldPrefab == null)
        {
            Debug.LogError("Could not load ReflectShield_Prefab from Resources.");
            return false;
        }

        GameObject shieldObject = Instantiate(shieldPrefab, transform);
        shieldObject.transform.localPosition = Vector3.zero;
        ReflectShield shield = shieldObject.GetComponent<ReflectShield>();
        if (shield == null)
            shield = shieldObject.AddComponent<ReflectShield>();
        shield.AttachToTank(transform);
        return true;
    }

    private void SetHealthUI()
    {
        if (m_Slider != null)
            m_Slider.value = m_CurrentHealth;

        if (m_FillImage != null)
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
    }

    private void OnDeath()
    {
        m_Dead = true;

        if (m_ExplosionParticles != null)
        {
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);
            m_ExplosionParticles.Play();
        }

        if (m_ExplosionAudio != null)
            m_ExplosionAudio.Play();

        if (PhotonNetwork.InRoom && photonView != null && photonView.IsMine)
            ReportMatchResult();

        gameObject.SetActive(false);
    }

    private void ReportMatchResult()
    {
        TankHealth[] remainingTanks = FindObjectsOfType<TankHealth>();
        for (int i = 0; i < remainingTanks.Length; i++)
        {
            PhotonView remainingView = remainingTanks[i].GetComponent<PhotonView>();
            if (remainingView == null || remainingView.ViewID == 0 || remainingView.OwnerActorNr == photonView.OwnerActorNr)
                continue;

            remainingView.RPC(
                "ReportMatchResult",
                RpcTarget.All,
                remainingView.OwnerActorNr,
                photonView.OwnerActorNr);
            return;
        }

        Debug.LogError("Unable to report the match result because no remaining player tank was found.");
    }

    [PunRPC]
    private void ReportMatchResult(int winnerActorNumber, int loserActorNumber)
    {
        GameManagerOnline gameManager = FindObjectOfType<GameManagerOnline>();
        if (gameManager != null)
            gameManager.ShowMatchResult(winnerActorNumber, loserActorNumber);
    }
}