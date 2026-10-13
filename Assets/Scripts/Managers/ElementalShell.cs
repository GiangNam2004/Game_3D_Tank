using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class ElementalShell : MonoBehaviour
{
    public enum ElementType { Ice, Toxic }
    public ElementType bulletType; 
    public int m_ShooterPlayerNumber = 1;
    [HideInInspector] public bool m_IsReflected = false;
    
    void OnTriggerEnter(Collider other)
    {
        ReflectShield shield = other.GetComponentInParent<ReflectShield>();
        if (shield != null) return;

        if (PhotonNetwork.InRoom)
        {
            PhotonView pv = GetComponent<PhotonView>();
            if (pv != null && !pv.IsMine) return;
        }

        // 1. Kiểm tra trúng trực tiếp xe tăng
        TankHealth directTarget = other.GetComponentInParent<TankHealth>();
        if (directTarget != null && directTarget.isActiveAndEnabled)
        {
            if (!IsShooter(directTarget))
            {
                ApplyElement(directTarget);
            }
        }
        else
        {
            // 2. Nếu trúng đất hoặc vật cản lân cận, quét bán kính nổ áp dụng hiệu ứng cho các xe trong tầm nổ
            ShellExplosion se = GetComponent<ShellExplosion>();
            float splashRadius = (se != null) ? se.m_ExplosionRadius : 3.5f;
            Collider[] colliders = Physics.OverlapSphere(transform.position, splashRadius);
            HashSet<TankHealth> hitTanks = new HashSet<TankHealth>();

            for (int i = 0; i < colliders.Length; i++)
            {
                TankHealth splashHealth = colliders[i].GetComponentInParent<TankHealth>();
                if (splashHealth != null && splashHealth.isActiveAndEnabled && hitTanks.Add(splashHealth))
                {
                    if (!IsShooter(splashHealth))
                    {
                        ApplyElement(splashHealth);
                    }
                }
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

    private bool IsShooter(TankHealth target)
    {
        // Nếu đạn đã bị phản xạ bởi Khiên (ReflectShield) thì người bắn vẫn sẽ bị dính đạn của chính mình
        if (m_IsReflected) return false;

        if (PhotonNetwork.InRoom)
        {
            PhotonView myPv = GetComponent<PhotonView>();
            PhotonView targetPv = target.GetComponent<PhotonView>();
            if (myPv != null && targetPv != null && myPv.OwnerActorNr == targetPv.OwnerActorNr)
            {
                return true;
            }
        }
        else
        {
            TankMovement tm = target.GetComponent<TankMovement>();
            if (tm != null && tm.m_PlayerNumber == m_ShooterPlayerNumber)
            {
                return true;
            }
        }
        return false;
    }

    private void ApplyElement(TankHealth targetHealth)
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
}