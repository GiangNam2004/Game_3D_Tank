using UnityEngine;
using Photon.Pun;

public class ToxicEffect : MonoBehaviourPun
{
    private TankHealth health;
    private int ticks = 5; 

    void Start()
    {
        health = GetComponentInParent<TankHealth>();
        
        // Đổi sang màu Xanh Lá nếu xe không đang bị đóng băng
        IceEffect ice = GetComponent<IceEffect>();
        if (health != null && (ice == null || !ice.enabled))
        {
            health.SetEffectColor(Color.green);
        }

        InvokeRepeating("TakePoison", 1f, 1f); // Mỗi giây rút máu 1 lần
    }

    public void RefreshToxic()
    {
        ticks = 5;
    }

    void TakePoison()
    {
        if (health != null) 
        {
            // Trong Multiplayer, chỉ chủ sở hữu của xe (IsMine) mới gọi TakeDamage để tránh trừ máu x2 lần
            PhotonView pv = health.GetComponent<PhotonView>();
            if (pv == null || !PhotonNetwork.InRoom || pv.IsMine)
            {
                health.TakeDamage(10f); // Mỗi giây mất 10 máu
            }
        }
        
        ticks--;
        
        // Hết độc
        if (ticks <= 0)
        {
            CancelInvoke("TakePoison");
            enabled = false;
            if (health != null) health.RestoreOriginalColor();
            Destroy(this); 
        }
    }

    private void OnDestroy()
    {
        CancelInvoke("TakePoison");
        enabled = false;
        if (health != null)
        {
            health.RestoreOriginalColor();
        }
    }
}