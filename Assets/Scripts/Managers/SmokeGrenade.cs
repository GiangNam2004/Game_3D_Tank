using UnityEngine;

public class SmokeGrenade : MonoBehaviour
{
    public ParticleSystem smokeParticles; 
    public float delayToSmoke = 1.5f; 
    public float smokeDuration = 8f;  

    void Start()
    {
        if (smokeParticles != null) 
        {
            smokeParticles.Stop(); 
        }
        
        // Hẹn giờ xì khói
        Invoke("StartSmoke", delayToSmoke); 
        
        // MỚI: Hẹn 0.5 giây sau khi quăng ra, khóa chặt lựu đạn xuống đất
        Invoke("LockGrenade", 0.5f);

        // Tự hủy quả lựu đạn
        Destroy(gameObject, smokeDuration + delayToSmoke); 
    }

    void StartSmoke()
    {
        if (smokeParticles != null) 
        {
            smokeParticles.Play(); 
        }
    }

    // --- HÀM MỚI ---
    void LockGrenade()
    {
        // 1. Xóa bỏ trọng lượng vật lý để nó không thể bị đá văng
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // 2. Biến nó thành bóng ma (xe tăng đi xuyên qua được không bị kẹt)
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }
}