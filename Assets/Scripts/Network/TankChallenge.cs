using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class TankChallenge : MonoBehaviourPun
{
    // Hàm này tự động kích hoạt khi xe tăng của bạn húc vào một vật khác
    private void OnCollisionEnter(Collision collision)
    {
        // 1. Kiểm tra xem xe này có phải CỦA MÌNH đang lái không
        // 2. Kiểm tra xem vật mình vừa húc trúng có thẻ (Tag) là "Player" không
        if (photonView.IsMine && collision.gameObject.CompareTag("Player"))
        {
            // Lấy chip mạng của xe đối thủ
            PhotonView targetView = collision.gameObject.GetComponent<PhotonView>();
            
            // Nếu xe đó đúng là của người khác (không phải tự húc chính mình)
            if (targetView != null && !targetView.IsMine)
            {
                Debug.Log("Đã húc trúng người chơi khác! Đang phát tín hiệu khiêu chiến...");
                
                // GỬI TIN NHẮN MẠNG: Gọi hàm "NhanLoiMoi" trên máy tính của người kia!
                photonView.RPC("NhanLoiMoi", targetView.Owner);
            }
        }
    }

    // Gắn mác [PunRPC] để Photon biết đây là hàm nhận tin nhắn mạng
    [PunRPC]
    public void NhanLoiMoi()
    {
        Debug.Log("Máy tôi vừa nhận được tin nhắn khiêu chiến! Đang kéo cả 2 sang Main 2...");
        
        // Vì tính năng AutomaticallySyncScene đã bật, khi 1 người gọi lệnh này, 
        // Photon sẽ BÊ NGUYÊN CẢ PHÒNG (bao gồm cả bạn và người kia) sang Scene Main 2.
        if (PhotonNetwork.IsMasterClient) 
        {
            PhotonNetwork.LoadLevel("Main 2");
        }
    }
}