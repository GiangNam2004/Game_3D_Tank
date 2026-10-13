using UnityEngine;
using Photon.Pun;

public class BulletSync : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    // Hàm này tự động chạy ngay khoảnh khắc viên đạn vừa xuất hiện trên mạng
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (info.photonView.InstantiationData != null)
        {
            // Đọc tốc độ bay được gói trong tin nhắn và gán vào viên đạn
            Vector3 velocity = (Vector3)info.photonView.InstantiationData[0];
            Rigidbody rb = GetComponent<Rigidbody>();
            
            if (rb != null && velocity != Vector3.zero)
            {
                rb.velocity = velocity;
            }
        }
    }
}