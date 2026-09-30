using UnityEngine;
using UnityEngine.UI;

public class SkillButtonUI : MonoBehaviour
{
    public Image m_CooldownOverlay; 
    public Button m_SkillButton;    
    
    private float m_CooldownTime;
    private float m_Timer = 0f;
    private bool m_IsOnCooldown = false;

    private void Update()
    {
        if (m_IsOnCooldown)
        {
            m_Timer -= Time.deltaTime;
            m_CooldownOverlay.fillAmount = m_Timer / m_CooldownTime;

            if (m_Timer <= 0f)
            {
                m_IsOnCooldown = false;
                m_CooldownOverlay.fillAmount = 0f;
                m_SkillButton.interactable = true; // Bật lại nút cho phép bấm tiếp
            }
        }
    }

    public void StartCooldown(float cooldown)
    {
        m_CooldownTime = cooldown;
        m_Timer = cooldown;
        m_IsOnCooldown = true;
        m_CooldownOverlay.fillAmount = 1f;
        m_SkillButton.interactable = false; // Tạm khóa nút không cho bấm bồi
    }
}