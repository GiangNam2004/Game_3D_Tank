using UnityEngine;
using Photon.Pun;

public class Landmine : MonoBehaviourPun
{
    public float m_Damage = 50f; 
    public GameObject m_ExplosionParticles; 
    private bool m_Triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (m_Triggered) return;

        Rigidbody targetRigidbody = other.GetComponent<Rigidbody>();
        if (targetRigidbody == null) return; 

        TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
        if (targetHealth == null || !targetHealth.isActiveAndEnabled) return;

        // Khi Online: Chỉ xử lý kích hoạt nổ trên máy của xe tăng dẫm phải
        if (PhotonNetwork.InRoom)
        {
            PhotonView targetPv = targetHealth.GetComponent<PhotonView>();
            if (targetPv != null && targetPv.ViewID != 0 && !targetPv.IsMine)
                return;
        }

        m_Triggered = true;
        targetHealth.TakeDamage(m_Damage);

        if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_ExplodeMine", RpcTarget.All, transform.position);
            StartCoroutine(DelayDestroyMine());
        }
        else
        {
            PlayExplosionEffect(transform.position);
            Destroy(gameObject);
        }
    }

    [PunRPC]
    public void RPC_ExplodeMine(Vector3 pos)
    {
        PlayExplosionEffect(pos);
    }

    private void PlayExplosionEffect(Vector3 pos)
    {
        if (m_ExplosionParticles != null)
        {
            GameObject explosion = Instantiate(m_ExplosionParticles, pos, transform.rotation);
            ParticleSystem particle = explosion.GetComponent<ParticleSystem>();
            if (particle != null)
            {
                particle.Play();
                Destroy(explosion, particle.main.duration);
            }
            else
            {
                Destroy(explosion, 2f);
            }

            AudioSource explosionAudio = explosion.GetComponent<AudioSource>();
            if (explosionAudio != null && explosionAudio.clip != null)
            {
                AudioSource.PlayClipAtPoint(explosionAudio.clip, pos);
            }
        }
    }

    private System.Collections.IEnumerator DelayDestroyMine()
    {
        yield return new WaitForSeconds(0.05f);
        if (photonView != null)
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                photonView.RPC("RPC_RequestDestroyMine", RpcTarget.MasterClient);
            }
        }
    }

    [PunRPC]
    public void RPC_RequestDestroyMine()
    {
        if (PhotonNetwork.IsMasterClient && gameObject != null)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}