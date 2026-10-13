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
    private MeshRenderer[] m_MeshRenderers;
    private Color[] m_OriginalColors;

    private void Awake()
    {
        m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
        m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
        m_ExplosionParticles.gameObject.SetActive(false);

        // Lưu màu gốc của xe để khôi phục chính xác sau các hiệu ứng (Băng, Độc, Hồi máu, Tăng tốc)
        m_MeshRenderers = GetComponentsInChildren<MeshRenderer>();
        if (m_MeshRenderers != null && m_MeshRenderers.Length > 0)
        {
            m_OriginalColors = new Color[m_MeshRenderers.Length];
            for (int i = 0; i < m_MeshRenderers.Length; i++)
            {
                if (m_MeshRenderers[i] != null && m_MeshRenderers[i].material != null)
                {
                    m_OriginalColors[i] = m_MeshRenderers[i].material.color;
                }
            }
        }
    }

    private void OnEnable()
    {
        m_CurrentHealth = m_StartingHealth;
        m_Dead = false;
        SetHealthUI();

        IceEffect ice = GetComponent<IceEffect>();
        if (ice != null) Destroy(ice);
        ToxicEffect toxic = GetComponent<ToxicEffect>();
        if (toxic != null) Destroy(toxic);

        RestoreOriginalColor();
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
                if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0)
                    photonView.RPC("ApplySpeedBoostRPC", RpcTarget.All, speedMultiplier, speedDuration);
                else
                    ApplySpeedBoostRPC(speedMultiplier, speedDuration);
                return true;

            case PickupType.ReflectShield:
                if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0)
                    photonView.RPC("ActivateReflectShieldRPC", RpcTarget.All);
                else
                    ActivateReflectShield();
                return true;
        }

        return false;
    }

    [PunRPC]
    public void ActivateReflectShieldRPC()
    {
        ActivateReflectShield();
    }

    // HÀM: Đồng bộ lượng máu được hồi qua mạng kèm hiệu ứng phát sáng xanh lá
    [PunRPC]
    public void HealRPC(float amount)
    {
        m_CurrentHealth = Mathf.Min(m_StartingHealth, m_CurrentHealth + amount);
        SetHealthUI();
        StartCoroutine(HealFlashRoutine());
    }

    private System.Collections.IEnumerator HealFlashRoutine()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers == null || renderers.Length == 0) yield break;

        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                originalColors[i] = renderers[i].material.color;
                renderers[i].material.color = Color.Lerp(originalColors[i], Color.green, 0.6f);
            }
        }

        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }

    // HÀM: Đồng bộ tăng tốc độ qua mạng kèm hiệu ứng vàng rực
    [PunRPC]
    public void ApplySpeedBoostRPC(float multiplier, float duration)
    {
        TankMovement movement = GetComponent<TankMovement>();
        if (movement != null)
        {
            movement.ApplySpeedBoost(multiplier, duration);
        }
        StartCoroutine(SpeedBoostFlashRoutine(duration));
    }

    private System.Collections.IEnumerator SpeedBoostFlashRoutine(float duration)
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers == null || renderers.Length == 0) yield break;

        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                originalColors[i] = renderers[i].material.color;
                renderers[i].material.color = Color.Lerp(originalColors[i], Color.yellow, 0.45f);
            }
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }

    // HÀM: Đồng bộ hiệu ứng Đóng Băng qua mạng
    public void ApplyIceEffect()
    {
        if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("ApplyIceEffectRPC", RpcTarget.All);
        }
        else
        {
            ApplyIceEffectRPC();
        }
    }

    [PunRPC]
    public void ApplyIceEffectRPC()
    {
        IceEffect existingIce = GetComponent<IceEffect>();
        if (existingIce == null)
        {
            gameObject.AddComponent<IceEffect>();
        }
        else
        {
            existingIce.RefreshIce();
        }
    }

    // HÀM: Đồng bộ hiệu ứng Nhiễm Độc qua mạng
    public void ApplyToxicEffect()
    {
        if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("ApplyToxicEffectRPC", RpcTarget.All);
        }
        else
        {
            ApplyToxicEffectRPC();
        }
    }

    [PunRPC]
    public void ApplyToxicEffectRPC()
    {
        ToxicEffect existingToxic = GetComponent<ToxicEffect>();
        if (existingToxic == null)
        {
            gameObject.AddComponent<ToxicEffect>();
        }
        else
        {
            existingToxic.RefreshToxic();
        }
    }

    public void SetEffectColor(Color color)
    {
        if (m_MeshRenderers == null) return;
        for (int i = 0; i < m_MeshRenderers.Length; i++)
        {
            if (m_MeshRenderers[i] != null && m_MeshRenderers[i].material != null)
            {
                m_MeshRenderers[i].material.color = color;
            }
        }
    }

    public void RestoreOriginalColor()
    {
        // Nếu vẫn còn hiệu ứng Băng đang bật thì hiển thị màu Cyan
        IceEffect ice = GetComponent<IceEffect>();
        if (ice != null && ice.enabled)
        {
            SetEffectColor(Color.cyan);
            return;
        }

        // Nếu vẫn còn hiệu ứng Độc đang bật thì hiển thị màu Green
        ToxicEffect toxic = GetComponent<ToxicEffect>();
        if (toxic != null && toxic.enabled)
        {
            SetEffectColor(Color.green);
            return;
        }

        // Khôi phục màu gốc chính xác tuyệt đối
        if (m_MeshRenderers == null || m_OriginalColors == null) return;
        for (int i = 0; i < m_MeshRenderers.Length; i++)
        {
            if (m_MeshRenderers[i] != null && m_MeshRenderers[i].material != null && i < m_OriginalColors.Length)
            {
                m_MeshRenderers[i].material.color = m_OriginalColors[i];
            }
        }
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

        // Xóa ItemPickup nếu có để khiên bảo vệ trên xe không vô tình hành xử như vật phẩm nhặt
        ItemPickup pickup = shieldObject.GetComponent<ItemPickup>();
        if (pickup != null)
            Destroy(pickup);

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

        if (PhotonNetwork.InRoom && photonView != null && photonView.IsMine && GetComponent<TankDecoy>() == null)
            ReportMatchResult();

        gameObject.SetActive(false);
    }

    private void ReportMatchResult()
    {
        if (GetComponent<TankDecoy>() != null) return;

        TankHealth[] remainingTanks = FindObjectsOfType<TankHealth>();
        for (int i = 0; i < remainingTanks.Length; i++)
        {
            if (remainingTanks[i].GetComponent<TankDecoy>() != null)
                continue;

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