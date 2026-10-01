using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement; // Thư viện để kiểm tra tên màn chơi

public class NetworkPlayerSetup : MonoBehaviourPun 
{
    private Transform mainCam;

    void Start()
    {
        if (photonView.IsMine)
        {
            // NẾU ĐANG Ở LOBBY: Lấy Main Camera để chuẩn bị đi theo sau lưng
            if (SceneManager.GetActiveScene().name == "Lobby")
            {
                mainCam = Camera.main.transform;
                mainCam.rotation = Quaternion.Euler(45f, 0f, 0f);
            }
        }
        else
{
    // Khóa bánh xe đối thủ
    GetComponent<TankMovement>().enabled = false;

    // BẠN CẦN THÊM DÒNG NÀY (Thay TankShooting bằng tên file code bắn súng của bạn):
    GetComponent<TankShooting>().enabled = false; 
}
    }

    void LateUpdate()
    {
        // NẾU ĐANG Ở LOBBY: Bắt Camera chạy theo sau lưng xe tăng
        if (photonView.IsMine && mainCam != null && SceneManager.GetActiveScene().name == "Lobby")
        {
            mainCam.position = transform.position + new Vector3(0f, 10f, -10f);
        }
    }
}