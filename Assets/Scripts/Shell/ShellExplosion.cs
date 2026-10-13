using UnityEngine;
using Photon.Pun; 

public class ShellExplosion : MonoBehaviourPun 
{
    public LayerMask m_TankMask;                        
    public ParticleSystem m_ExplosionParticles;         
    public AudioSource m_ExplosionAudio;                
    public float m_MaxDamage = 100f;                    
    public float m_ExplosionForce = 1000f;              
    public float m_MaxLifeTime = 2f;                    
    public float m_ExplosionRadius = 5f;                

    private bool m_Exploded = false; 

    private void Start ()
    {
        if (PhotonNetwork.InRoom)
        {
            if (photonView.IsMine) StartCoroutine(NetworkDestroyRoutine());
        }
        else
        {
            Destroy (gameObject, m_MaxLifeTime);
        }
    }

    private System.Collections.IEnumerator NetworkDestroyRoutine()
    {
        yield return new WaitForSeconds(m_MaxLifeTime);
        if (photonView != null && photonView.IsMine && !m_Exploded)
        {
            m_Exploded = true;
            photonView.RPC("RPC_PlayExplosion", RpcTarget.All, transform.position);
            HideShell();
            StartCoroutine(DelayDestroy());
        }
    }

    private void OnTriggerEnter (Collider other)
    {
        if (m_Exploded) return;

        ReflectShield shield = other.GetComponentInParent<ReflectShield>();
        if (shield != null)
        {
            shield.ReflectProjectile(GetComponent<Collider>());
            return;
        }

        // ĐÃ SỬA: Chỉ kiểm tra InRoom. Bỏ điều kiện ViewID != 0 để chống tự sát nhầm
        if (PhotonNetwork.InRoom)
        {
            // Nếu không phải đạn do mình bắn ra, đứng im chờ lệnh nổ từ chủ nhân, tuyệt đối không tự xử lý!
            if (photonView == null || !photonView.IsMine) return; 
            
            m_Exploded = true;
            
            // 1. Gửi lệnh nổ
            photonView.RPC("RPC_PlayExplosion", RpcTarget.All, transform.position);

            // 2. Tính sát thương
            CalculateAndApplyDamage();

            // 3. Tàng hình đạn ngay lập tức
            HideShell();
            
            // 4. Chờ 0.05s cho mạng xác nhận gửi xong lệnh RPC rồi mới xóa
            StartCoroutine(DelayDestroy());
        }
        else 
        {
            // CHẾ ĐỘ OFFLINE
            m_Exploded = true;
            RPC_PlayExplosion(transform.position);
            CalculateAndApplyDamage();
            Destroy(gameObject);
        }
    }

    private void HideShell()
    {
        // Tắt va chạm và tàng hình vỏ đạn
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers) r.enabled = false;
    }

    private System.Collections.IEnumerator DelayDestroy()
    {
        yield return new WaitForSeconds(0.05f); 
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    public void RPC_PlayExplosion(Vector3 explosionPosition)
    {
        if (m_ExplosionParticles != null)
        {
            m_ExplosionParticles.transform.position = explosionPosition;
            m_ExplosionParticles.transform.parent = null;
            m_ExplosionParticles.Play();
            Destroy(m_ExplosionParticles.gameObject, m_ExplosionParticles.main.duration);
        }
        
        if (m_ExplosionAudio != null && m_ExplosionAudio.isActiveAndEnabled) 
        {
            m_ExplosionAudio.Play();
        }
    }

    private void CalculateAndApplyDamage()
    {
        Collider[] allColliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius);
        for (int i = 0; i < allColliders.Length; i++)
        {
            MedicalHealth medical = allColliders[i].GetComponentInParent<MedicalHealth>();
            if (medical != null)
                medical.TakeDamage(CalculateDamage(medical.transform.position));
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);
        for (int i = 0; i < colliders.Length; i++)
        {
            Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
            if (!targetRigidbody) continue;

            targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

            TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
            if (!targetHealth) continue;

            float damage = CalculateDamage(targetRigidbody.position);
            targetHealth.TakeDamage(damage); 
        }
    }

    private float CalculateDamage (Vector3 targetPosition)
    {
        Vector3 explosionToTarget = targetPosition - transform.position;
        float explosionDistance = explosionToTarget.magnitude;
        float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;
        float damage = relativeDistance * m_MaxDamage;
        return Mathf.Max (0f, damage);
    }
}