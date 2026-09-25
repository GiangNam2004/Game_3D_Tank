using UnityEngine;

public class Landmine : MonoBehaviour
{
    public float m_Damage = 50f; 
    public GameObject m_ExplosionParticles; 

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody targetRigidbody = other.GetComponent<Rigidbody>();
        if (targetRigidbody == null) return; 

        TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
        if (targetHealth != null)
        {
            // Trừ máu xe đạp trúng
            targetHealth.TakeDamage(m_Damage);
            
            // Xử lý vụ nổ khói lửa
            if (m_ExplosionParticles != null)
            {
                // 1. Sinh ra vật thể vụ nổ
                GameObject explosion = Instantiate(m_ExplosionParticles, transform.position, transform.rotation);
                
                // 2. Ép hệ thống hạt (Particle) phải phun lửa
                ParticleSystem particle = explosion.GetComponent<ParticleSystem>();
                if (particle != null)
                {
                    particle.Play();
                    // Hủy vật thể vụ nổ sau khi lửa tàn để tránh làm giật game
                    Destroy(explosion, particle.main.duration); 
                }
                
                // 3. Ép loa (Audio) phải phát tiếng BÙM
                AudioSource explosionAudio = explosion.GetComponent<AudioSource>();
                if (explosionAudio != null)
                {
                    explosionAudio.Play();
                }
            }
            
            // Hủy quả mìn đi
            Destroy(gameObject);
        }
    }
}