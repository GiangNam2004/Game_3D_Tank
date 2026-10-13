using UnityEngine;
using Photon.Pun;

public class TankDecoy : MonoBehaviourPun
{
    private void Awake()
    {
        FreezeDecoy();
    }

    private void Start()
    {
        FreezeDecoy();
    }

    public void FreezeDecoy()
    {
        // 1. Tắt di chuyển để phân thân đứng im, không chạy theo xe thật
        TankMovement move = GetComponent<TankMovement>();
        if (move != null)
        {
            move.enabled = false;
        }

        // 2. Tắt súng để phân thân không bắn theo xe thật
        TankShooting shoot = GetComponent<TankShooting>();
        if (shoot != null)
        {
            shoot.enabled = false;
        }

        // 3. Khóa cứng Rigidbody để phân thân không bị đẩy hoặc trôi
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}

