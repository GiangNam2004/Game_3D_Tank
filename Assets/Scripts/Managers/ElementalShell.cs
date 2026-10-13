using UnityEngine;
using Photon.Pun;

public class ElementalShell : MonoBehaviour
{
    public enum ElementType { Ice, Toxic }
    public ElementType bulletType; 
    
    void OnTriggerEnter(Collider other)
    {
        ReflectShield shield = other.GetComponentInParent<ReflectShield>();
        if (shield != null) return;

        // Trúng xe tăng
        TankHealth targetHealth = other.GetComponentInParent<TankHealth>();
        if (targetHealth != null && targetHealth.isActiveAndEnabled)
        {
            if (bulletType == ElementType.Ice)
            {
                targetHealth.ApplyIceEffect();
            }
            else if (bulletType == ElementType.Toxic)
            {
                targetHealth.ApplyToxicEffect();
            }
        }

        // Nếu không có ShellExplosion thì mới tự hủy
        if (GetComponent<ShellExplosion>() == null)
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonView pv = GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}