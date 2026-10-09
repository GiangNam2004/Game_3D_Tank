using UnityEngine;

public class ToxicEffect : MonoBehaviour
{
    private TankHealth health;
    private MeshRenderer[] renderers;
    private Color[] originalColors;
    private int ticks = 5; 

    void Start()
    {
        health = GetComponent<TankHealth>();
        
        // Nhuốm màu xe thành Xanh Lá (Trúng độc)
        renderers = GetComponentsInChildren<MeshRenderer>();
        originalColors = new Color[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
            renderers[i].material.color = Color.green; // Đổi sang màu xanh lá
        }

        InvokeRepeating("TakePoison", 1f, 1f); // Mỗi giây rút máu 1 lần
    }

    void TakePoison()
    {
        if (health != null) 
        {
            health.TakeDamage(10f); // Mỗi giây mất 10 máu (bạn có thể tự chỉnh số này)
        }
        
        ticks--;
        
        // Hết độc
        if (ticks <= 0)
        {
            CancelInvoke("TakePoison");

            // Trả lại màu sơn gốc cho xe
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].material.color = originalColors[i];
            }

            Destroy(this); 
        }
    }
}