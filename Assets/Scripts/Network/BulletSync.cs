using UnityEngine;
using Photon.Pun;

public class BulletSync : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    // Hàm này tự động chạy ngay khoảnh khắc viên đạn vừa xuất hiện trên mạng
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (info.photonView != null && info.photonView.InstantiationData != null)
        {
            object[] data = info.photonView.InstantiationData;
            if (data.Length > 0 && data[0] is Vector3)
            {
                Vector3 velocity = (Vector3)data[0];
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null && velocity != Vector3.zero)
                {
                    rb.velocity = velocity;
                }
            }

            if (data.Length > 1 && data[1] is Vector3)
            {
                Vector3 scale = (Vector3)data[1];
                if (scale != Vector3.zero)
                {
                    transform.localScale = scale;
                }
            }
        }
    }
}